#!/usr/bin/env python3
"""
Extract nouns and verbs from DPD database, creating a normalized structure
with separate tables for inflections and conjugations.

This script uses modular components from the extraction/ package:
- config.py: Constants and paths
- registry.py: Stable lemma ID management
- grammar.py: Grammar enum definitions and parsing
- forms.py: Form ID computation and stem cleaning
- html_parser.py: DPD HTML parsing for irregular forms
- plural_dedup.py: Redundant plural-only lemma detection
"""

import json
import sqlite3
import sys
from pathlib import Path
from typing import List, Dict, Any

# Import extraction modules
from extraction import (
    # Config
    IRREGULAR_NOUN_PATTERNS,
    IRREGULAR_VERB_PATTERNS,
    NOUN_POS_LIST,
    VERB_POS_LIST,
    # Grammar
    GrammarEnums,
    # Forms
    clean_stem,
    # Registry
    load_registry,
    save_registry,
    get_noun_lemma_id,
    get_verb_lemma_id,
    deep_copy_registry,
    # HTML Parser
    # Plural Deduplication
    PluralOnlyDeduplicator,
    # Translations
    TranslationAdjustments,
)
from extraction.config import (
    EXCLUDED_NOUN_LEMMAS,
    is_plural_only_pattern,
    MAX_LEMMA_LENGTH,
)
from extraction.grammar import pos_to_gender
from extraction.inputs import load_corpus_words, read_json
from extraction.compatibility import comparison
from extraction.templates import parse_template
from extraction.form_storage import store_forms, public_form_id
from extraction.identity import (load_baseline, require_historical_registry,
    historical_practice_registry, require_practice_registry, plan_practice)

from extraction.validate_inflections import InflectionValidator, PluralOnlyMatch

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "dpd-db"))

from sqlalchemy import create_engine
from sqlalchemy.orm import Session
from db.models import DpdHeadword


class NounVerbExtractor:
    """Extract nouns and verbs with grammatical categorization."""

    def __init__(self, *, dpd_path: Path, corpus_paths: list[Path],
                 registry_path: Path, adjustments_path: Path, practice_registry_path: Path,
                 corrections_path: Path, output_db_path: Path,
                 noun_limit: int, verb_limit: int, database_version: int):
        self.output_db_path = output_db_path
        self.registry_path = registry_path
        self.practice_registry_path = practice_registry_path
        self.corrections_path = corrections_path
        self.noun_limit = noun_limit
        self.verb_limit = verb_limit
        self.database_version = database_version
        self.engine = create_engine("sqlite+pysqlite://", creator=lambda: sqlite3.connect(
            dpd_path.resolve().as_uri() + "?mode=ro&immutable=1", uri=True
        ))
        self.db_session = Session(self.engine)
        self.all_tipitaka_words = load_corpus_words(corpus_paths)
        self.plural_dedup = PluralOnlyDeduplicator(self.db_session)
        self.translations = TranslationAdjustments(adjustments_path)

    def close(self):
        self.db_session.close()
        self.engine.dispose()

    def extract_word_variant(self, lemma_1: str, lemma_clean: str) -> str:
        """Extract the variant identifier from DPD lemma_1.

        Examples:
            lemma_1="dhamma", lemma_clean="dhamma" -> ""
            lemma_1="dhamma 1", lemma_clean="dhamma" -> "1"
            lemma_1="annati 1.1", lemma_clean="annati" -> "1.1"
        """
        if lemma_1 == lemma_clean:
            return ""
        # The variant is everything after the lemma_clean + space
        prefix = lemma_clean + " "
        if lemma_1.startswith(prefix):
            return lemma_1[len(prefix):]
        # Fallback: try splitting on last space
        parts = lemma_1.rsplit(" ", 1)
        return parts[1] if len(parts) > 1 else ""

    def create_schema(self):
        """Create a normalized database schema for nouns and verbs."""
        if self.output_db_path.exists():
            raise FileExistsError(f"Candidate database already exists: {self.output_db_path}")

        conn = sqlite3.connect(self.output_db_path)
        cursor = conn.cursor()

        # Nouns table (slim) - only fields needed for queue building + inflection
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS nouns (
                id INTEGER PRIMARY KEY,
                ebt_count INTEGER DEFAULT 0,
                practice_primary INTEGER NOT NULL DEFAULT 0,
                lemma_id INTEGER NOT NULL,
                lemma TEXT NOT NULL,
                gender INTEGER NOT NULL DEFAULT 0,
                stem TEXT,
                pattern TEXT
            )
        """)
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_nouns_lemma_id ON nouns(lemma_id)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_nouns_ebt_count ON nouns(ebt_count DESC)")

        # Noun details table - lazy loaded for flashcard display
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS nouns_details (
                id INTEGER PRIMARY KEY,
                lemma_id INTEGER NOT NULL,
                word TEXT NOT NULL DEFAULT '',
                root TEXT DEFAULT '',
                meaning TEXT,
                meaning_ru TEXT DEFAULT '',
                source_1 TEXT DEFAULT '',
                sutta_1 TEXT DEFAULT '',
                example_1 TEXT DEFAULT '',
                source_2 TEXT DEFAULT '',
                sutta_2 TEXT DEFAULT '',
                example_2 TEXT DEFAULT ''
            )
        """)
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_nouns_details_lemma_id ON nouns_details(lemma_id)")

        # Verbs table (slim)
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS verbs (
                id INTEGER PRIMARY KEY,
                ebt_count INTEGER DEFAULT 0,
                practice_primary INTEGER NOT NULL DEFAULT 0,
                lemma_id INTEGER NOT NULL,
                lemma TEXT NOT NULL,
                stem TEXT,
                pattern TEXT
            )
        """)
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_verbs_lemma_id ON verbs(lemma_id)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_verbs_ebt_count ON verbs(ebt_count DESC)")

        # Verb details table
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS verbs_details (
                id INTEGER PRIMARY KEY,
                lemma_id INTEGER NOT NULL,
                word TEXT NOT NULL DEFAULT '',
                root TEXT DEFAULT '',
                type TEXT DEFAULT '',
                trans TEXT DEFAULT '',
                meaning TEXT,
                meaning_ru TEXT DEFAULT '',
                source_1 TEXT DEFAULT '',
                sutta_1 TEXT DEFAULT '',
                example_1 TEXT DEFAULT '',
                source_2 TEXT DEFAULT '',
                sutta_2 TEXT DEFAULT '',
                example_2 TEXT DEFAULT ''
            )
        """)
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_verbs_details_lemma_id ON verbs_details(lemma_id)")

        # Non-reflexive verbs table
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS verbs_nonreflexive (
                lemma_id INTEGER PRIMARY KEY
            )
        """)

        # Ending indices are local to a headword's paradigm, not its cleaned lemma.
        for kind in ('nouns', 'verbs'):
            for suffix in ('corpus_forms', 'irregular_forms'):
                cursor.execute(f"""
                    CREATE TABLE {kind}_{suffix} (
                        headword_id INTEGER NOT NULL REFERENCES {kind}(id),
                        form_id INTEGER NOT NULL,
                        form TEXT NOT NULL,
                        PRIMARY KEY (headword_id, form_id)
                    )
                """)

        conn.commit()
        conn.close()
        print(f"Created database schema at {self.output_db_path}")

    def validate_noun_pattern_gender(self, word: DpdHeadword) -> bool:
        """Validate that noun pattern gender matches pos gender."""
        pos = word.pos.lower()
        pattern = word.pattern.lower() if word.pattern else ""

        if pos == 'masc' and 'masc' not in pattern:
            print(f"  SKIP: Pattern-POS mismatch: {word.lemma_1} pos={word.pos} pattern={word.pattern}")
            return False
        elif pos == 'fem' and 'fem' not in pattern:
            print(f"  SKIP: Pattern-POS mismatch: {word.lemma_1} pos={word.pos} pattern={word.pattern}")
            return False
        elif pos == 'nt' and 'nt' not in pattern:
            print(f"  SKIP: Pattern-POS mismatch: {word.lemma_1} pos={word.pos} pattern={word.pattern}")
            return False

        return True

    def filter_noun_templates(self, all_words: List[DpdHeadword]) -> List[DpdHeadword]:
        """Apply template, gender, length, and explicit noun eligibility rules."""
        # Filter to words with inflection templates, valid pattern-pos gender match, and reasonable length
        print("\nValidating noun pattern-pos gender matches...")
        words_with_templates = [
            w for w in all_words
            if w.it is not None
            and self.validate_noun_pattern_gender(w)
            and len(w.lemma_clean) <= MAX_LEMMA_LENGTH
            and w.lemma_clean not in EXCLUDED_NOUN_LEMMAS
        ]
        long_filtered = sum(1 for w in all_words if w.it and len(w.lemma_clean) > MAX_LEMMA_LENGTH)
        if long_filtered:
            print(f"  Filtered {long_filtered} nouns with lemma > {MAX_LEMMA_LENGTH} chars")
        excluded_filtered = sum(
            1 for w in all_words
            if w.it is not None and w.lemma_clean in EXCLUDED_NOUN_LEMMAS
        )
        if excluded_filtered:
            excluded_names = ", ".join(sorted(EXCLUDED_NOUN_LEMMAS))
            print(f"  Filtered {excluded_filtered} explicitly excluded noun rows: {excluded_names}")

        return words_with_templates

    def get_training_nouns(self) -> List[DpdHeadword]:
        """Get most frequent nouns suitable for training, limited by unique lemma_clean count."""
        # Fetch all candidate nouns
        all_words = self.db_session.query(DpdHeadword).filter(
            DpdHeadword.pos.in_(NOUN_POS_LIST),
            DpdHeadword.pattern.isnot(None),
            DpdHeadword.pattern != '',
            DpdHeadword.stem.isnot(None),
            DpdHeadword.stem != '-',
            DpdHeadword.ebt_count > 0,
            DpdHeadword.meaning_1.isnot(None),
            DpdHeadword.meaning_1 != '',
            DpdHeadword.sutta_1.isnot(None),
            DpdHeadword.sutta_1 != '',
            ~DpdHeadword.meaning_1.contains('(gram)'),
            ~DpdHeadword.meaning_1.contains('(abhi)'),
            ~DpdHeadword.meaning_1.startswith('(comm)'),
            ~DpdHeadword.meaning_1.contains('??'),
            ~DpdHeadword.meaning_1.contains('in reference to'),
            ~DpdHeadword.meaning_1.contains('people of'),
            ~DpdHeadword.meaning_1.contains('name of'),
            ~DpdHeadword.meaning_1.contains('names of'),
            ~DpdHeadword.meaning_1.contains('family name')
        ).order_by(DpdHeadword.id).all()

        words_with_templates = self.filter_noun_templates(all_words)

        # Build singular index from ALL DPD nouns (not just filtered ones)
        # This ensures plural-only deduplication finds matches even when
        # the singular form lacks meaning/sutta and wouldn't be extracted
        print("\nBuilding singular index from all DPD nouns...")
        all_dpd_nouns = self.db_session.query(DpdHeadword).filter(
            DpdHeadword.pos.in_(NOUN_POS_LIST),
            DpdHeadword.pattern.isnot(None),
            DpdHeadword.pattern != '',
            DpdHeadword.stem.isnot(None),
            DpdHeadword.stem != '-',
        ).order_by(DpdHeadword.id).all()
        all_dpd_nouns_with_templates = [w for w in all_dpd_nouns if w.it is not None]
        self.plural_dedup.build_singular_index(all_dpd_nouns_with_templates)

        # Filter out redundant plural-only lemmas
        print("\nChecking for redundant plural-only lemmas...")
        filtered_words = []
        redundant_skipped = []

        for word in words_with_templates:
            if is_plural_only_pattern(word.pattern):
                result = self.plural_dedup.check_redundant(word)
                if result.is_redundant:
                    redundant_skipped.append(
                        f"{word.lemma_1} ({word.pattern}) -> matches {result.matched_lemma}"
                    )
                    continue
            filtered_words.append(word)

        if redundant_skipped:
            print(f"  Skipped {len(redundant_skipped)} redundant plural-only lemmas:")
            for skip in redundant_skipped[:10]:
                print(f"    {skip}")
            if len(redundant_skipped) > 10:
                print(f"    ... and {len(redundant_skipped) - 10} more")

        # Group by lemma_clean and get max ebt_count for ordering
        lemma_max_ebt: Dict[str, int] = {}
        lemma_words: Dict[str, List[DpdHeadword]] = {}
        for word in filtered_words:
            lc = word.lemma_clean
            ebt = word.ebt_count or 0
            if lc not in lemma_max_ebt or ebt > lemma_max_ebt[lc]:
                lemma_max_ebt[lc] = ebt
            if lc not in lemma_words:
                lemma_words[lc] = []
            lemma_words[lc].append(word)

        # Get top N lemmas by max ebt_count
        top_lemmas = sorted(lemma_max_ebt.keys(), key=lambda lc: (-lemma_max_ebt[lc], lc))[:self.noun_limit]
        print(f"Selected {len(top_lemmas)} unique noun lemmas")

        # Collect all words from selected lemmas
        result = []
        for lc in top_lemmas:
            result.extend(lemma_words[lc])
        result.sort(key=lambda w: (-(w.ebt_count or 0), w.lemma_clean, w.id))

        # Report frequency variance
        print(f"\nNoun lemmas with frequency variance across senses:")
        variance_count = 0
        for lc in top_lemmas:
            words = lemma_words[lc]
            if len(words) > 1:
                freqs = [w.ebt_count or 0 for w in words]
                if min(freqs) != max(freqs):
                    variance_count += 1
                    if variance_count <= 10:
                        variants = ", ".join(f"{w.lemma_1}={w.ebt_count}" for w in sorted(words, key=lambda x: x.lemma_1))
                        print(f"  {lc}: [{variants}]")
        print(f"  ... {variance_count} lemmas total with frequency variance")

        print(f"\nFound {len(result)} noun rows ({len(top_lemmas)} unique lemmas) with inflection templates")
        if top_lemmas:
            first_lemma = top_lemmas[0]
            last_lemma = top_lemmas[-1]
            print(f"Lemma ranking frequency range: {lemma_max_ebt[first_lemma]} (highest: {first_lemma}) to {lemma_max_ebt[last_lemma]} (lowest: {last_lemma})")

        return result

    def get_training_verbs(self) -> List[DpdHeadword]:
        """Get most frequent verbs suitable for training, limited by unique lemma_clean count."""
        all_words = self.db_session.query(DpdHeadword).filter(
            DpdHeadword.pos.in_(VERB_POS_LIST),
            DpdHeadword.pattern.isnot(None),
            DpdHeadword.pattern != '',
            DpdHeadword.stem.isnot(None),
            DpdHeadword.stem != '-',
            DpdHeadword.ebt_count > 0,
            DpdHeadword.meaning_1.isnot(None),
            DpdHeadword.meaning_1 != '',
            DpdHeadword.sutta_1.isnot(None),
            DpdHeadword.sutta_1 != '',
            ~DpdHeadword.meaning_1.contains('(gram)'),
            ~DpdHeadword.meaning_1.contains('(abhi)'),
            ~DpdHeadword.meaning_1.startswith('(comm)'),
            ~DpdHeadword.meaning_1.contains('in reference to'),
            ~DpdHeadword.meaning_1.contains('name of'),
            ~DpdHeadword.meaning_1.contains('names of'),
            ~DpdHeadword.meaning_1.contains('family name'),
            ~DpdHeadword.grammar.contains('reflx')
        ).order_by(DpdHeadword.id).all()

        # Filter to words with inflection templates and reasonable length
        words_with_templates = [
            w for w in all_words
            if w.it is not None and len(w.lemma_clean) <= MAX_LEMMA_LENGTH
        ]
        long_filtered = sum(1 for w in all_words if w.it and len(w.lemma_clean) > MAX_LEMMA_LENGTH)
        if long_filtered:
            print(f"  Filtered {long_filtered} verbs with lemma > {MAX_LEMMA_LENGTH} chars")

        # Group by lemma_clean and get max ebt_count
        lemma_max_ebt: Dict[str, int] = {}
        lemma_words: Dict[str, List[DpdHeadword]] = {}
        for word in words_with_templates:
            lc = word.lemma_clean
            ebt = word.ebt_count or 0
            if lc not in lemma_max_ebt or ebt > lemma_max_ebt[lc]:
                lemma_max_ebt[lc] = ebt
            if lc not in lemma_words:
                lemma_words[lc] = []
            lemma_words[lc].append(word)

        # Get top N lemmas
        top_lemmas = sorted(lemma_max_ebt.keys(), key=lambda lc: (-lemma_max_ebt[lc], lc))[:self.verb_limit]
        print(f"Selected {len(top_lemmas)} unique verb lemmas")

        # Collect all words from selected lemmas
        result = []
        for lc in top_lemmas:
            result.extend(lemma_words[lc])
        result.sort(key=lambda w: (-(w.ebt_count or 0), w.lemma_clean, w.id))

        # Report frequency variance
        print(f"\nVerb lemmas with frequency variance across senses:")
        variance_count = 0
        for lc in top_lemmas:
            words = lemma_words[lc]
            if len(words) > 1:
                freqs = [w.ebt_count or 0 for w in words]
                if min(freqs) != max(freqs):
                    variance_count += 1
                    if variance_count <= 10:
                        variants = ", ".join(f"{w.lemma_1}={w.ebt_count}" for w in sorted(words, key=lambda x: x.lemma_1))
                        print(f"  {lc}: [{variants}]")
        print(f"  ... {variance_count} lemmas total with frequency variance")

        print(f"\nFound {len(result)} verb rows ({len(top_lemmas)} unique lemmas) with inflection templates")
        if top_lemmas:
            first_lemma = top_lemmas[0]
            last_lemma = top_lemmas[-1]
            print(f"Lemma ranking frequency range: {lemma_max_ebt[first_lemma]} (highest: {first_lemma}) to {lemma_max_ebt[last_lemma]} (lowest: {last_lemma})")

        return result

    def parse_inflection_template(self, word: DpdHeadword, word_type: str) -> tuple[List[Dict[str, Any]], int, int]:
        """Parse inflection/conjugation template to extract individual forms with grammar info."""
        return parse_template(word, word_type, self.all_tipitaka_words)

    def prepare_practice(self, nouns, verbs, registry):
        historical_registry, historical_words = load_baseline()
        require_historical_registry(registry, historical_registry)
        choices = read_json(self.practice_registry_path)
        corrections = read_json(self.corrections_path)
        require_practice_registry(choices, historical_practice_registry(historical_words),
                                  registry, corrections)
        words = {}
        for kind, selected_words, assign_id in (
            ('nouns', nouns, get_noun_lemma_id), ('verbs', verbs, get_verb_lemma_id)
        ):
            words[kind] = [{
                'id': word.id, 'lemma': word.lemma_clean,
                'lemma_id': assign_id(registry, word.lemma_clean),
                'pattern': word.pattern, 'stem': clean_stem(word.stem),
                'gender': pos_to_gender(word.pos) if kind == 'nouns' else 0,
                'ebt_count': word.ebt_count or 0,
            } for word in selected_words]
        return plan_practice(words, choices, corrections)

    @staticmethod
    def record_primary_forms(output, kind, word, lemma_id, forms, selected):
        if selected[lemma_id] == word.id:
            output.extend([word.id, public_form_id(kind, lemma_id, form), form['form'], form['in_corpus']]
                          for form in forms)

    def extract_and_save(self):
        """Main extraction process."""
        if self.database_version is None:
            raise ValueError("database_version must be provided before extraction")

        print("Starting noun and verb extraction...")
        print(f"Using database version: {self.database_version}")

        # Load lemma registry
        registry = load_registry(self.registry_path)
        original_registry = deep_copy_registry(registry)
        print(f"Loaded lemma registry: {len(registry['nouns'])} nouns, {len(registry['verbs'])} verbs")

        # Create schema
        self.create_schema()

        # Initialize inflection validator
        validator = InflectionValidator(log_dir=self.output_db_path.parent)

        # Get words
        nouns = self.get_training_nouns()
        verbs = self.get_training_verbs()
        selected, practice_registry, practice_changes = self.prepare_practice(nouns, verbs, registry)
        expected_forms = []

        conn = sqlite3.connect(self.output_db_path)
        cursor = conn.cursor()

        # Process nouns
        total_declensions = 0
        nouns_processed = 0
        nouns_discarded: List[str] = []
        total_noun_forms_generated = 0
        total_noun_forms_filtered = 0

        print(f"\nProcessing {len(nouns)} nouns...")
        for i, word in enumerate(nouns, 1):
            if i % 100 == 0:
                print(f"Processing noun {i}/{len(nouns)}: {word.lemma_1}")

            gender = pos_to_gender(word.pos)
            lemma_id = get_noun_lemma_id(registry, word.lemma_clean)
            word_variant = self.extract_word_variant(word.lemma_1, word.lemma_clean)

            cursor.execute("""
                INSERT INTO nouns (id, ebt_count, lemma_id, lemma, gender, stem, pattern, practice_primary)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                word.id, word.ebt_count or 0, lemma_id, word.lemma_clean, gender,
                clean_stem(word.stem), word.pattern, int(selected[lemma_id] == word.id)
            ))

            # Apply custom translation adjustments
            meaning = self.translations.apply(word.id, word.lemma_1, word.meaning_1 or '')
            meaning_ru = ''

            cursor.execute("""
                INSERT INTO nouns_details (
                    id, lemma_id, word, root, meaning, meaning_ru,
                    source_1, sutta_1, example_1, source_2, sutta_2, example_2
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                word.id, lemma_id, word_variant, word.family_root or '', meaning, meaning_ru,
                word.source_1 or '', word.sutta_1 or '', word.example_1 or '',
                word.source_2 or '', word.sutta_2 or '', word.example_2 or ''
            ))

            forms, generated, filtered = self.parse_inflection_template(word, 'noun')
            total_noun_forms_generated += generated
            total_noun_forms_filtered += filtered

            # Get match info for plural-only nouns (for validation report)
            plural_matches = None
            if is_plural_only_pattern(word.pattern):
                raw_matches = self.plural_dedup.get_all_matches(word)
                plural_matches = [
                    PluralOnlyMatch(lemma=m[0], pattern=m[1], match_ratio=m[2])
                    for m in raw_matches
                ]

            validator.validate_noun(word.lemma_clean, word.pattern, forms, plural_matches)
            self.record_primary_forms(expected_forms, 'nouns', word, lemma_id, forms, selected)

            if forms:
                has_nom_sg = any(
                    f.get('case_name') == GrammarEnums.CASE_NOMINATIVE and f.get('number') == GrammarEnums.NUMBER_SINGULAR
                    for f in forms
                )

                # For plural-only patterns, we don't require nom sg
                is_plural_only = is_plural_only_pattern(word.pattern)

                if has_nom_sg or is_plural_only:
                    nouns_processed += 1

                    total_declensions += store_forms(
                        cursor, 'nouns', word, lemma_id, forms, word.pattern in IRREGULAR_NOUN_PATTERNS)
                else:
                    nouns_discarded.append(word.lemma_1)
            else:
                nouns_discarded.append(word.lemma_1)

        # Process verbs
        total_conjugations = 0
        verbs_processed = 0
        total_verb_forms_generated = 0
        total_verb_forms_filtered = 0
        all_verb_lemma_ids: set[int] = set()
        reflexive_lemma_ids: set[int] = set()

        print(f"\nProcessing {len(verbs)} verbs...")
        for i, word in enumerate(verbs, 1):
            if i % 100 == 0:
                print(f"Processing verb {i}/{len(verbs)}: {word.lemma_1}")

            lemma_id = get_verb_lemma_id(registry, word.lemma_clean)
            all_verb_lemma_ids.add(lemma_id)

            forms, generated, filtered = self.parse_inflection_template(word, 'verb')
            total_verb_forms_generated += generated
            total_verb_forms_filtered += filtered

            validator.validate_verb(word.lemma_clean, word.pattern, forms)
            self.record_primary_forms(expected_forms, 'verbs', word, lemma_id, forms, selected)

            if selected[lemma_id] == word.id and any(f.get('reflexive', 0) == GrammarEnums.REFLEXIVE_YES for f in forms):
                reflexive_lemma_ids.add(lemma_id)

            word_variant = self.extract_word_variant(word.lemma_1, word.lemma_clean)
            cursor.execute("""
                INSERT INTO verbs (id, ebt_count, lemma_id, lemma, stem, pattern, practice_primary)
                VALUES (?, ?, ?, ?, ?, ?, ?)
            """, (
                word.id, word.ebt_count or 0, lemma_id, word.lemma_clean,
                clean_stem(word.stem), word.pattern, int(selected[lemma_id] == word.id)
            ))

            # Apply custom translation adjustments
            meaning = self.translations.apply(word.id, word.lemma_1, word.meaning_1 or '')
            meaning_ru = ''

            cursor.execute("""
                INSERT INTO verbs_details (
                    id, lemma_id, word, root, type, trans, meaning, meaning_ru,
                    source_1, sutta_1, example_1, source_2, sutta_2, example_2
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                word.id, lemma_id, word_variant, word.family_root or '', word.verb or '', word.trans or '', meaning, meaning_ru,
                word.source_1 or '', word.sutta_1 or '', word.example_1 or '',
                word.source_2 or '', word.sutta_2 or '', word.example_2 or ''
            ))

            if forms:
                verbs_processed += 1

                total_conjugations += store_forms(
                    cursor, 'verbs', word, lemma_id, forms, word.pattern in IRREGULAR_VERB_PATTERNS)

        # Insert non-reflexive verb lemma_ids
        nonreflexive_lemma_ids = all_verb_lemma_ids - reflexive_lemma_ids
        for lemma_id in sorted(nonreflexive_lemma_ids):
            cursor.execute("INSERT INTO verbs_nonreflexive (lemma_id) VALUES (?)", (lemma_id,))

        # Set database version for app cache invalidation
        cursor.execute(f"PRAGMA user_version = {self.database_version}")

        conn.commit()
        conn.close()

        (self.output_db_path.parent / "primary_forms.json").write_text(
            json.dumps(sorted(expected_forms), ensure_ascii=False, separators=(',', ':')) + "\n")
        (self.output_db_path.parent / "practice_registry.json").write_text(
            json.dumps(practice_registry, indent=2, ensure_ascii=False) + "\n")
        (self.output_db_path.parent / "compatibility.json").write_text(
            json.dumps(comparison(self.output_db_path.parent, practice_changes), indent=2, ensure_ascii=False) + "\n")

        # Registry changes are proposed beside the candidate, never published here.
        save_registry(registry, original_registry,
                      output_path=self.output_db_path.parent / "lemma_registry.json")

        print(f"\n=== EXTRACTION COMPLETE ===")
        print(f"Database: {self.output_db_path}")
        print(f"Total headwords: {len(nouns) + len(verbs)}")
        print(f"Nouns processed: {nouns_processed}/{len(nouns)}")
        if nouns_discarded:
            print(f"  Discarded (no nom sg): {', '.join(nouns_discarded)}")
        print(f"Verbs processed: {verbs_processed}/{len(verbs)}")
        print(f"Total declensions: {total_declensions}")
        print(f"Total conjugations: {total_conjugations}")

        # Corpus attestation statistics
        total_forms_generated = total_noun_forms_generated + total_verb_forms_generated
        total_forms_not_in_corpus = total_noun_forms_filtered + total_verb_forms_filtered
        total_forms_in_corpus = total_forms_generated - total_forms_not_in_corpus
        not_in_corpus_percentage = (total_forms_not_in_corpus / total_forms_generated * 100) if total_forms_generated > 0 else 0

        print(f"\n=== CORPUS ATTESTATION STATISTICS ===")
        print(f"Noun forms: {total_noun_forms_generated} total, {total_noun_forms_generated - total_noun_forms_filtered} in corpus, {total_noun_forms_filtered} theoretical ({total_noun_forms_filtered / total_noun_forms_generated * 100:.1f}%)")
        print(f"Verb forms: {total_verb_forms_generated} total, {total_verb_forms_generated - total_verb_forms_filtered} in corpus, {total_verb_forms_filtered} theoretical ({total_verb_forms_filtered / total_verb_forms_generated * 100:.1f}%)")
        print(f"Total: {total_forms_generated} forms")
        print(f"  - In Tipitaka corpus (in_corpus=1): {total_forms_in_corpus} ({100-not_in_corpus_percentage:.1f}%)")
        print(f"  - Theoretical only (in_corpus=0): {total_forms_not_in_corpus} ({not_in_corpus_percentage:.1f}%)")

        # Print plural-only deduplication summary
        self.plural_dedup.print_summary()

        # Write validation report
        log_path = validator.write_report(build_version=self.database_version)
        print(f"\nInflection validation log: {log_path}")
        validator.print_summary()

        self.print_summary_stats()

    def print_summary_stats(self):
        """Print summary statistics of extracted data."""
        conn = sqlite3.connect(self.output_db_path)
        cursor = conn.cursor()

        print("\n=== SUMMARY STATISTICS ===")

        cursor.execute("SELECT COUNT(*) FROM nouns")
        noun_count = cursor.fetchone()[0]
        cursor.execute("SELECT COUNT(*) FROM verbs")
        verb_count = cursor.fetchone()[0]
        print("\nWords by Type:")
        print(f"  nouns: {noun_count}")
        print(f"  verbs: {verb_count}")

        cursor.execute("SELECT gender, COUNT(*) FROM nouns GROUP BY gender ORDER BY COUNT(*) DESC")
        print("\nNouns by Gender:")
        gender_names = {1: 'masculine', 2: 'feminine', 3: 'neuter', 0: 'none'}
        for gender, count in cursor.fetchall():
            print(f"  {gender_names.get(gender, f'unknown({gender})')}: {count}")

        cursor.execute("SELECT COUNT(DISTINCT lemma_id) FROM verbs")
        total_lemmas = cursor.fetchone()[0]
        cursor.execute("SELECT COUNT(*) FROM verbs_nonreflexive")
        nonreflexive_count = cursor.fetchone()[0]
        reflexive_count = total_lemmas - nonreflexive_count
        print("\nVerb Lemmas by Reflexive Capability:")
        print(f"  with reflexive forms: {reflexive_count}")
        print(f"  active only: {nonreflexive_count}")

        cursor.execute("SELECT COUNT(*) FROM nouns")
        noun_slim_count = cursor.fetchone()[0]
        cursor.execute("SELECT COUNT(*) FROM nouns_details")
        noun_details_count = cursor.fetchone()[0]
        cursor.execute("SELECT COUNT(*) FROM verbs")
        verb_slim_count = cursor.fetchone()[0]
        cursor.execute("SELECT COUNT(*) FROM verbs_details")
        verb_details_count = cursor.fetchone()[0]

        print("\nTable Record Counts (slim and details should match):")
        print(f"  nouns: {noun_slim_count}, nouns_details: {noun_details_count}",
              "" if noun_slim_count == noun_details_count else " MISMATCH!")
        print(f"  verbs: {verb_slim_count}, verbs_details: {verb_details_count}",
              "" if verb_slim_count == verb_details_count else " MISMATCH!")

        cursor.execute("SELECT COUNT(DISTINCT lemma_id) FROM nouns")
        unique_noun_lemmas = cursor.fetchone()[0]
        cursor.execute("SELECT COUNT(DISTINCT lemma_id) FROM verbs")
        unique_verb_lemmas = cursor.fetchone()[0]
        print(f"\nUnique Lemmas:")
        print(f"  nouns: {unique_noun_lemmas} unique lemma_ids")
        print(f"  verbs: {unique_verb_lemmas} unique lemma_ids")

        cursor.execute("SELECT COUNT(*) FROM nouns_corpus_forms")
        decl_count = cursor.fetchone()[0]
        cursor.execute("SELECT COUNT(*) FROM verbs_corpus_forms")
        conj_count = cursor.fetchone()[0]
        print("\nCorpus Attestation Records:")
        print(f"  Noun forms in corpus: {decl_count}")
        print(f"  Verb forms in corpus: {conj_count}")
        print(f"  Total: {decl_count + conj_count}")

        cursor.execute("SELECT COUNT(*) FROM nouns_irregular_forms")
        irreg_noun_count = cursor.fetchone()[0]
        cursor.execute("SELECT COUNT(*) FROM verbs_irregular_forms")
        irreg_verb_count = cursor.fetchone()[0]
        print("\nIrregular Forms Records:")
        print(f"  Irregular noun forms: {irreg_noun_count}")
        print(f"  Irregular verb forms: {irreg_verb_count}")
        print(f"  Total: {irreg_noun_count + irreg_verb_count}")

        conn.close()


if __name__ == "__main__":
    from extraction.candidate import main
    main()

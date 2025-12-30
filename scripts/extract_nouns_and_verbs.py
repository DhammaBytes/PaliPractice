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
import os
import sys
from pathlib import Path
from typing import List, Dict, Any, Set

# Import extraction modules
from extraction import (
    # Config
    IRREGULAR_NOUN_PATTERNS,
    IRREGULAR_VERB_PATTERNS,
    NOUN_POS_LIST,
    VERB_POS_LIST,
    # Grammar
    GrammarEnums,
    parse_noun_grammar,
    parse_verb_grammar,
    # Forms
    clean_stem,
    compute_declension_form_id,
    compute_conjugation_form_id,
    # Registry
    RegistryError,
    load_registry,
    save_registry,
    get_noun_lemma_id,
    get_verb_lemma_id,
    deep_copy_registry,
    # HTML Parser
    parse_inflections_html,
    parse_noun_title,
    parse_verb_title,
    # Plural Deduplication
    PluralOnlyDeduplicator,
    # Translations
    TranslationAdjustments,
)
from extraction.config import (
    REGISTRY_PATH,
    NOUN_ID_START,
    NOUN_ID_MAX,
    VERB_ID_START,
    VERB_ID_MAX,
    ALL_NOUN_POS,
    ALL_VERB_POS,
    TIPITAKA_FREQ_PATH,
    TIPITAKA_WORDLIST_FILES,
    is_plural_only_pattern,
    MAX_LEMMA_LENGTH,
)
from extraction.grammar import pos_to_gender

from extraction.validate_inflections import InflectionValidator, PluralOnlyMatch

sys.path.append('../dpd-db')

from db.db_helpers import get_db_session
from db.models import DpdHeadword


def populate_all_lemmas_to_registry():
    """
    One-time operation: Populate registry with ALL lemmas from DPD.

    This ensures all lemma IDs are assigned upfront, ordered by ebt_count.
    After this, the registry should only ever have NEW lemmas appended.

    Run this once, then commit lemma_registry.json to version control.
    """
    print("=" * 60)
    print("POPULATING REGISTRY WITH ALL DPD LEMMAS")
    print("=" * 60)

    # Check if registry already has data
    if REGISTRY_PATH.exists():
        existing = json.loads(REGISTRY_PATH.read_text(encoding='utf-8'))
        if existing.get("nouns") or existing.get("verbs"):
            print(f"WARNING: Registry already contains {len(existing.get('nouns', {}))} nouns and {len(existing.get('verbs', {}))} verbs.")
            response = input("This will ADD new lemmas only. Continue? (yes/no): ")
            if response.lower() != 'yes':
                print("Aborted.")
                return

    registry = load_registry()
    original_registry = deep_copy_registry(registry)

    db_session = get_db_session(Path("../dpd-db/dpd.db"))

    # lemma_clean is a Python @property, not a DB column
    # We need to fetch all rows and group in Python

    print("\nQuerying all nouns...")
    all_nouns = db_session.query(DpdHeadword).filter(
        DpdHeadword.pos.in_(ALL_NOUN_POS),
        DpdHeadword.pattern.isnot(None),
        DpdHeadword.pattern != '',
        DpdHeadword.stem.isnot(None),
        DpdHeadword.stem != '-',
    ).all()

    # Group by lemma_clean and get max ebt_count for ordering
    noun_by_lemma: Dict[str, int] = {}
    for word in all_nouns:
        lc = word.lemma_clean
        ebt = word.ebt_count or 0
        if lc not in noun_by_lemma or ebt > noun_by_lemma[lc]:
            noun_by_lemma[lc] = ebt

    # Sort by max ebt_count descending
    noun_lemmas = sorted(noun_by_lemma.items(), key=lambda x: -x[1])
    print(f"Found {len(noun_lemmas)} unique noun lemmas")

    print("Querying all verbs...")
    all_verbs = db_session.query(DpdHeadword).filter(
        DpdHeadword.pos.in_(ALL_VERB_POS),
        DpdHeadword.pattern.isnot(None),
        DpdHeadword.pattern != '',
        DpdHeadword.stem.isnot(None),
        DpdHeadword.stem != '-',
    ).all()

    # Group by lemma_clean and get max ebt_count for ordering
    verb_by_lemma: Dict[str, int] = {}
    for word in all_verbs:
        lc = word.lemma_clean
        ebt = word.ebt_count or 0
        if lc not in verb_by_lemma or ebt > verb_by_lemma[lc]:
            verb_by_lemma[lc] = ebt

    # Sort by max ebt_count descending
    verb_lemmas = sorted(verb_by_lemma.items(), key=lambda x: -x[1])
    print(f"Found {len(verb_lemmas)} unique verb lemmas")

    # Assign IDs to all noun lemmas (in ebt_count order)
    new_nouns = 0
    for lemma_clean, max_ebt in noun_lemmas:
        if lemma_clean not in registry["nouns"]:
            get_noun_lemma_id(registry, lemma_clean)
            new_nouns += 1

    # Assign IDs to all verb lemmas (in ebt_count order)
    new_verbs = 0
    for lemma_clean, max_ebt in verb_lemmas:
        if lemma_clean not in registry["verbs"]:
            get_verb_lemma_id(registry, lemma_clean)
            new_verbs += 1

    print(f"\nAdded {new_nouns} new noun lemmas (total: {len(registry['nouns'])})")
    print(f"Added {new_verbs} new verb lemmas (total: {len(registry['verbs'])})")

    # Save with safety checks
    save_registry(registry, original_registry)
    print(f"\nRegistry saved to {REGISTRY_PATH}")
    print("=" * 60)


class NounVerbExtractor:
    """Extract nouns and verbs with grammatical categorization."""

    def __init__(self, output_db_path: str = "../PaliPractice/PaliPractice/Data/pali.db",
                 noun_limit: int = 3000, verb_limit: int = 2000):
        self.output_db_path = output_db_path
        self.noun_limit = noun_limit
        self.verb_limit = verb_limit
        self.db_session = get_db_session(Path("../dpd-db/dpd.db"))

        # Load all words found in the Pali Tipitaka corpus
        print("Loading Tipitaka word corpus from JSON wordlists...")
        self.all_tipitaka_words: Set[str] = self._load_tipitaka_words()
        print(f"Loaded {len(self.all_tipitaka_words)} words from Tipitaka corpus")

        # Initialize plural-only deduplicator
        self.plural_dedup = PluralOnlyDeduplicator(self.db_session)

        # Initialize translation adjustments
        self.translations = TranslationAdjustments()

    def _load_tipitaka_words(self) -> Set[str]:
        """Load all words from the Tipitaka corpus JSON wordlists."""
        all_words: Set[str] = set()

        for filename in TIPITAKA_WORDLIST_FILES:
            filepath = TIPITAKA_FREQ_PATH / filename
            if filepath.exists():
                with open(filepath) as f:
                    words = json.load(f)
                    all_words.update(words)
                    print(f"  Loaded {len(words)} words from {filename}")
            else:
                print(f"  Warning: {filename} not found, skipping")

        return all_words

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
        # Delete old database if it exists
        if os.path.exists(self.output_db_path):
            os.remove(self.output_db_path)
            print(f"Deleted old database: {self.output_db_path}")

        conn = sqlite3.connect(self.output_db_path)
        cursor = conn.cursor()

        # Nouns table (slim) - only fields needed for queue building + inflection
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS nouns (
                id INTEGER PRIMARY KEY,
                ebt_count INTEGER DEFAULT 0,
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
                meaning TEXT,
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
                type TEXT DEFAULT '',
                trans TEXT DEFAULT '',
                meaning TEXT,
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

        # Corpus attestation tables
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS nouns_corpus_forms (
                form_id INTEGER PRIMARY KEY
            )
        """)

        cursor.execute("""
            CREATE TABLE IF NOT EXISTS verbs_corpus_forms (
                form_id INTEGER PRIMARY KEY
            )
        """)

        # Irregular forms tables
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS nouns_irregular_forms (
                form_id INTEGER PRIMARY KEY,
                form TEXT NOT NULL
            )
        """)

        cursor.execute("""
            CREATE TABLE IF NOT EXISTS verbs_irregular_forms (
                form_id INTEGER PRIMARY KEY,
                form TEXT NOT NULL
            )
        """)

        cursor.execute("CREATE INDEX IF NOT EXISTS idx_nouns_gender ON nouns(gender)")

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
            ~DpdHeadword.meaning_1.contains('(comm)'),
            ~DpdHeadword.meaning_1.contains('in reference to'),
            ~DpdHeadword.meaning_1.contains('people of'),
            ~DpdHeadword.meaning_1.contains('name of'),
            ~DpdHeadword.meaning_1.contains('family name')
        ).all()

        # Filter to words with inflection templates, valid pattern-pos gender match, and reasonable length
        print("\nValidating noun pattern-pos gender matches...")
        words_with_templates = [
            w for w in all_words
            if w.it is not None
            and self.validate_noun_pattern_gender(w)
            and len(w.lemma_clean) <= MAX_LEMMA_LENGTH
        ]
        long_filtered = sum(1 for w in all_words if w.it and len(w.lemma_clean) > MAX_LEMMA_LENGTH)
        if long_filtered:
            print(f"  Filtered {long_filtered} nouns with lemma > {MAX_LEMMA_LENGTH} chars")

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
        ).all()
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
        top_lemmas = sorted(lemma_max_ebt.keys(), key=lambda lc: -lemma_max_ebt[lc])[:self.noun_limit]
        print(f"Selected {len(top_lemmas)} unique noun lemmas")

        # Collect all words from selected lemmas
        result = []
        for lc in top_lemmas:
            result.extend(lemma_words[lc])
        result.sort(key=lambda w: -(w.ebt_count or 0))

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
            ~DpdHeadword.meaning_1.contains('(comm)'),
            ~DpdHeadword.meaning_1.contains('in reference to'),
            ~DpdHeadword.meaning_1.contains('name of'),
            ~DpdHeadword.meaning_1.contains('names of'),
            ~DpdHeadword.meaning_1.contains('family name'),
            ~DpdHeadword.grammar.contains('reflx')
        ).all()

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
        top_lemmas = sorted(lemma_max_ebt.keys(), key=lambda lc: -lemma_max_ebt[lc])[:self.verb_limit]
        print(f"Selected {len(top_lemmas)} unique verb lemmas")

        # Collect all words from selected lemmas
        result = []
        for lc in top_lemmas:
            result.extend(lemma_words[lc])
        result.sort(key=lambda w: -(w.ebt_count or 0))

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
        if not word.it or not word.it.data:
            return [], 0, 0

        try:
            template_data = json.loads(word.it.data)
        except json.JSONDecodeError:
            return [], 0, 0

        forms = []
        total_generated = 0
        not_in_corpus = 0
        stem = clean_stem(word.stem)

        for row_idx, row in enumerate(template_data[1:], 1):
            if len(row) < 2:
                continue

            grammar_label = row[0][0] if row[0] else ""

            col_idx = 1
            while col_idx < len(row):
                if col_idx >= len(row) or not row[col_idx]:
                    col_idx += 2
                    continue

                endings = row[col_idx]
                if not isinstance(endings, list):
                    endings = [endings]

                grammar_info = ""
                if col_idx + 1 < len(row) and row[col_idx + 1]:
                    grammar_data = row[col_idx + 1]
                    grammar_info = grammar_data[0] if isinstance(grammar_data, list) else grammar_data

                for ending_index, ending in enumerate(endings):
                    if ending:
                        inflected_form = f"{stem}{ending}" if ending != "-" else stem
                        total_generated += 1

                        in_corpus = 1 if inflected_form in self.all_tipitaka_words else 0
                        if in_corpus == 0:
                            not_in_corpus += 1

                        if word_type == 'noun':
                            parsed_grammar = parse_noun_grammar(grammar_info, grammar_label, word.pos)
                        else:
                            parsed_grammar = parse_verb_grammar(grammar_info, grammar_label, word.pos)

                        form_data = {
                            'form': inflected_form,
                            'in_corpus': in_corpus,
                            'ending_index': ending_index,
                            **parsed_grammar
                        }
                        forms.append(form_data)

                col_idx += 2

        return forms, total_generated, not_in_corpus

    def extract_and_save(self):
        """Main extraction process."""
        print("Starting noun and verb extraction...")

        # Load lemma registry
        registry = load_registry()
        original_registry = deep_copy_registry(registry)
        print(f"Loaded lemma registry: {len(registry['nouns'])} nouns, {len(registry['verbs'])} verbs")

        # Create schema
        self.create_schema()

        # Initialize inflection validator
        validator = InflectionValidator(log_dir=Path(__file__).parent)

        # Get words
        nouns = self.get_training_nouns()
        verbs = self.get_training_verbs()

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
                INSERT INTO nouns (id, ebt_count, lemma_id, lemma, gender, stem, pattern)
                VALUES (?, ?, ?, ?, ?, ?, ?)
            """, (
                word.id, word.ebt_count or 0, lemma_id, word.lemma_clean, gender,
                clean_stem(word.stem), word.pattern
            ))

            # Apply custom translation adjustments
            meaning = self.translations.apply(word.id, word.lemma_1, word.meaning_1 or '')

            cursor.execute("""
                INSERT INTO nouns_details (
                    id, lemma_id, word, meaning, source_1, sutta_1, example_1, source_2, sutta_2, example_2
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                word.id, lemma_id, word_variant, meaning,
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

            if forms:
                has_nom_sg = any(
                    f.get('case_name') == GrammarEnums.CASE_NOMINATIVE and f.get('number') == GrammarEnums.NUMBER_SINGULAR
                    for f in forms
                )

                # For plural-only patterns, we don't require nom sg
                is_plural_only = is_plural_only_pattern(word.pattern)

                if has_nom_sg or is_plural_only:
                    nouns_processed += 1

                    for form in forms:
                        if form.get('in_corpus', 0) == 1:
                            form_id = compute_declension_form_id(
                                lemma_id=lemma_id,
                                case=form.get('case_name', GrammarEnums.CASE_NONE),
                                gender=gender,
                                number=form.get('number', GrammarEnums.NUMBER_NONE),
                                ending_index=form.get('ending_index', 0) + 1
                            )
                            try:
                                cursor.execute(
                                    "INSERT INTO nouns_corpus_forms (form_id) VALUES (?)",
                                    (form_id,)
                                )
                                total_declensions += 1
                            except sqlite3.IntegrityError:
                                pass

                    if word.pattern in IRREGULAR_NOUN_PATTERNS and word.inflections_html:
                        html_forms = parse_inflections_html(word.inflections_html)
                        for title, form_list in html_forms.items():
                            case_val, gender_val, number_val = parse_noun_title(title)
                            if case_val == GrammarEnums.CASE_NONE:
                                continue
                            for idx, full_form in enumerate(form_list):
                                form_id = compute_declension_form_id(
                                    lemma_id=lemma_id,
                                    case=case_val,
                                    gender=gender,
                                    number=number_val,
                                    ending_index=idx + 1
                                )
                                try:
                                    cursor.execute(
                                        "INSERT INTO nouns_irregular_forms (form_id, form) VALUES (?, ?)",
                                        (form_id, full_form)
                                    )
                                except sqlite3.IntegrityError:
                                    pass
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

            if any(f.get('reflexive', 0) == GrammarEnums.REFLEXIVE_YES for f in forms):
                reflexive_lemma_ids.add(lemma_id)

            word_variant = self.extract_word_variant(word.lemma_1, word.lemma_clean)
            cursor.execute("""
                INSERT INTO verbs (id, ebt_count, lemma_id, lemma, stem, pattern)
                VALUES (?, ?, ?, ?, ?, ?)
            """, (
                word.id, word.ebt_count or 0, lemma_id, word.lemma_clean,
                clean_stem(word.stem), word.pattern
            ))

            # Apply custom translation adjustments
            meaning = self.translations.apply(word.id, word.lemma_1, word.meaning_1 or '')

            cursor.execute("""
                INSERT INTO verbs_details (
                    id, lemma_id, word, type, trans, meaning,
                    source_1, sutta_1, example_1, source_2, sutta_2, example_2
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                word.id, lemma_id, word_variant, word.verb or '', word.trans or '', meaning,
                word.source_1 or '', word.sutta_1 or '', word.example_1 or '',
                word.source_2 or '', word.sutta_2 or '', word.example_2 or ''
            ))

            if forms:
                verbs_processed += 1

                for form in forms:
                    if form.get('in_corpus', 0) == 1:
                        form_id = compute_conjugation_form_id(
                            lemma_id=lemma_id,
                            tense=form.get('tense', GrammarEnums.TENSE_NONE),
                            person=form.get('person', GrammarEnums.PERSON_NONE),
                            number=form.get('number', GrammarEnums.NUMBER_NONE),
                            reflexive=form.get('reflexive', GrammarEnums.REFLEXIVE_NO),
                            ending_index=form.get('ending_index', 0) + 1
                        )
                        try:
                            cursor.execute(
                                "INSERT INTO verbs_corpus_forms (form_id) VALUES (?)",
                                (form_id,)
                            )
                            total_conjugations += 1
                        except sqlite3.IntegrityError:
                            pass

                if word.pattern in IRREGULAR_VERB_PATTERNS and word.inflections_html:
                    html_forms = parse_inflections_html(word.inflections_html)
                    for title, form_list in html_forms.items():
                        tense_val, person_val, number_val, reflexive_val = parse_verb_title(title)
                        if tense_val == GrammarEnums.TENSE_NONE or person_val == GrammarEnums.PERSON_NONE:
                            continue
                        for idx, full_form in enumerate(form_list):
                            form_id = compute_conjugation_form_id(
                                lemma_id=lemma_id,
                                tense=tense_val,
                                person=person_val,
                                number=number_val,
                                reflexive=reflexive_val,
                                ending_index=idx + 1
                            )
                            try:
                                cursor.execute(
                                    "INSERT INTO verbs_irregular_forms (form_id, form) VALUES (?, ?)",
                                    (form_id, full_form)
                                )
                            except sqlite3.IntegrityError:
                                pass

        # Insert non-reflexive verb lemma_ids
        nonreflexive_lemma_ids = all_verb_lemma_ids - reflexive_lemma_ids
        for lemma_id in sorted(nonreflexive_lemma_ids):
            cursor.execute("INSERT INTO verbs_nonreflexive (lemma_id) VALUES (?)", (lemma_id,))

        conn.commit()
        conn.close()

        # Save updated lemma registry
        new_nouns = len(registry['nouns']) - len(original_registry['nouns'])
        new_verbs = len(registry['verbs']) - len(original_registry['verbs'])
        if new_nouns > 0 or new_verbs > 0:
            save_registry(registry, original_registry)
            print(f"\nSaved lemma registry: {len(registry['nouns'])} nouns (+{new_nouns}), {len(registry['verbs'])} verbs (+{new_verbs})")
        else:
            print(f"\nNo new lemmas added to registry (unchanged)")

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
        log_path = validator.write_report()
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
    import argparse

    parser = argparse.ArgumentParser(
        description="Extract nouns and verbs from DPD database",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # First time: populate registry with ALL lemmas (run once, commit result)
  python extract_nouns_and_verbs.py --populate-registry

  # Normal extraction: create pali.db with top N lemmas
  python extract_nouns_and_verbs.py --nouns 3000 --verbs 2000
        """
    )
    parser.add_argument(
        "--populate-registry",
        action="store_true",
        help="Populate lemma_registry.json with ALL lemmas from DPD (one-time operation)"
    )
    parser.add_argument(
        "--nouns",
        type=int,
        default=3000,
        help="Number of unique noun lemmas to extract (default: 3000)"
    )
    parser.add_argument(
        "--verbs",
        type=int,
        default=2000,
        help="Number of unique verb lemmas to extract (default: 2000)"
    )

    args = parser.parse_args()

    if args.populate_registry:
        populate_all_lemmas_to_registry()
    else:
        extractor = NounVerbExtractor(noun_limit=args.nouns, verb_limit=args.verbs)
        extractor.extract_and_save()

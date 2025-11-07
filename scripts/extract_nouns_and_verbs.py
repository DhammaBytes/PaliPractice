#!/usr/bin/env python3
"""
Extract nouns and verbs from DPD database, creating a normalized structure
with separate tables for inflections and conjugations.
"""

import re
import json
import sqlite3
import os
import pickle
from pathlib import Path
from typing import List, Dict, Any, Optional, Tuple, Set
import sys

sys.path.append('../dpd-db')

from db.db_helpers import get_db_session
from db.models import DpdHeadword, InflectionTemplates
from tools.pos import CONJUGATIONS, DECLENSIONS


# Enum mappings matching C# Enums.cs
class GrammarEnums:
    """Enum values matching the C# Models/Enums.cs definitions."""

    # NounCase enum
    CASE_NONE = 0
    CASE_NOMINATIVE = 1
    CASE_ACCUSATIVE = 2
    CASE_INSTRUMENTAL = 3
    CASE_DATIVE = 4
    CASE_ABLATIVE = 5
    CASE_GENITIVE = 6
    CASE_LOCATIVE = 7
    CASE_VOCATIVE = 8

    # Number enum
    NUMBER_NONE = 0
    NUMBER_SINGULAR = 1
    NUMBER_PLURAL = 2

    # Gender enum
    GENDER_NONE = 0
    GENDER_MASCULINE = 1
    GENDER_NEUTER = 2
    GENDER_FEMININE = 3

    # Person enum
    PERSON_NONE = 0
    PERSON_FIRST = 1
    PERSON_SECOND = 2
    PERSON_THIRD = 3

    # Voice enum
    VOICE_NONE = 0
    VOICE_ACTIVE = 1
    VOICE_REFLEXIVE = 2
    VOICE_PASSIVE = 3
    VOICE_CAUSATIVE = 4

    # Tense enum
    TENSE_NONE = 0
    TENSE_PRESENT = 1
    TENSE_FUTURE = 2
    TENSE_AORIST = 3
    TENSE_IMPERFECT = 4
    TENSE_PERFECT = 5

    # Mood enum
    MOOD_NONE = 0
    MOOD_INDICATIVE = 1
    MOOD_OPTATIVE = 2
    MOOD_IMPERATIVE = 3
    MOOD_CONDITIONAL = 4

    # String to enum mappings
    CASE_MAP = {
        'nominative': CASE_NOMINATIVE,
        'accusative': CASE_ACCUSATIVE,
        'instrumental': CASE_INSTRUMENTAL,
        'dative': CASE_DATIVE,
        'ablative': CASE_ABLATIVE,
        'genitive': CASE_GENITIVE,
        'locative': CASE_LOCATIVE,
        'vocative': CASE_VOCATIVE
    }

    NUMBER_MAP = {
        'singular': NUMBER_SINGULAR,
        'plural': NUMBER_PLURAL
    }

    GENDER_MAP = {
        'masculine': GENDER_MASCULINE,
        'neuter': GENDER_NEUTER,
        'feminine': GENDER_FEMININE
    }

    PERSON_MAP = {
        'first': PERSON_FIRST,
        'second': PERSON_SECOND,
        'third': PERSON_THIRD
    }

    VOICE_MAP = {
        'active': VOICE_ACTIVE,
        'reflexive': VOICE_REFLEXIVE,
        'passive': VOICE_PASSIVE,
        'causative': VOICE_CAUSATIVE
    }

    TENSE_MAP = {
        'present': TENSE_PRESENT,
        'future': TENSE_FUTURE,
        'aorist': TENSE_AORIST,
        'imperfect': TENSE_IMPERFECT,
        'perfect': TENSE_PERFECT
    }

    MOOD_MAP = {
        'indicative': MOOD_INDICATIVE,
        'optative': MOOD_OPTATIVE,
        'imperative': MOOD_IMPERATIVE,
        'conditional': MOOD_CONDITIONAL
    }


class NounVerbExtractor:
    """Extract nouns and verbs with grammatical categorization."""

    def __init__(self, output_db_path: str = "../PaliPractice/PaliPractice/Data/training.db",
                 noun_limit: int = 3000, verb_limit: int = 2000):
        self.output_db_path = output_db_path
        self.noun_limit = noun_limit
        self.verb_limit = verb_limit
        self.db_session = get_db_session(Path("../dpd-db/dpd.db"))

        # Load all words found in the Pali Tipitaka corpus
        # This is used to filter out inflections that don't appear in actual texts
        tipitaka_words_path = Path("../dpd-db/shared_data/all_tipitaka_words")
        print(f"Loading Tipitaka word corpus from {tipitaka_words_path}")
        with open(tipitaka_words_path, "rb") as f:
            self.all_tipitaka_words: Set[str] = pickle.load(f)
        print(f"Loaded {len(self.all_tipitaka_words)} words from Tipitaka corpus")
        
    def create_schema(self):
        """Create a normalized database schema for nouns and verbs."""
        # Delete old database if it exists
        if os.path.exists(self.output_db_path):
            os.remove(self.output_db_path)
            print(f"Deleted old database: {self.output_db_path}")

        conn = sqlite3.connect(self.output_db_path)
        cursor = conn.cursor()

        # Nouns table
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS nouns (
                id INTEGER PRIMARY KEY,
                ebt_count INTEGER DEFAULT 0,
                lemma TEXT NOT NULL UNIQUE,
                lemma_clean TEXT NOT NULL,
                gender INTEGER NOT NULL DEFAULT 0,
                stem TEXT,
                pattern TEXT,
                family_root TEXT DEFAULT '',
                meaning TEXT,
                plus_case TEXT DEFAULT '',
                source_1 TEXT DEFAULT '',
                sutta_1 TEXT DEFAULT '',
                example_1 TEXT DEFAULT '',
                source_2 TEXT DEFAULT '',
                sutta_2 TEXT DEFAULT '',
                example_2 TEXT DEFAULT ''
            )
        """)

        # Verbs table
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS verbs (
                id INTEGER PRIMARY KEY,
                ebt_count INTEGER DEFAULT 0,
                lemma TEXT NOT NULL UNIQUE,
                lemma_clean TEXT NOT NULL,
                pos TEXT NOT NULL,
                type TEXT DEFAULT '',
                trans TEXT DEFAULT '',
                stem TEXT,
                pattern TEXT,
                family_root TEXT DEFAULT '',
                meaning TEXT,
                plus_case TEXT DEFAULT '',
                source_1 TEXT DEFAULT '',
                sutta_1 TEXT DEFAULT '',
                example_1 TEXT DEFAULT '',
                source_2 TEXT DEFAULT '',
                sutta_2 TEXT DEFAULT '',
                example_2 TEXT DEFAULT ''
            )
        """)
        
        # Declensions table - for nouns only (using enum integers)
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS declensions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                noun_id INTEGER NOT NULL,
                form TEXT NOT NULL,
                case_name INTEGER NOT NULL DEFAULT 0,  -- NounCase enum: 0=None, 1=Nominative, 2=Accusative, etc.
                number INTEGER NOT NULL DEFAULT 0,     -- Number enum: 0=None, 1=Singular, 2=Plural
                gender INTEGER NOT NULL DEFAULT 0,     -- Gender enum: 0=None, 1=Masculine, 2=Neuter, 3=Feminine
                in_corpus INTEGER NOT NULL DEFAULT 0,  -- 1 if form appears in Tipitaka corpus, 0 if theoretical only

                FOREIGN KEY (noun_id) REFERENCES nouns(id),
                UNIQUE(noun_id, form, case_name, number, gender)
            )
        """)
        
        # Conjugations table - for verbs only (using enum integers)
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS conjugations (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                verb_id INTEGER NOT NULL,
                form TEXT NOT NULL,
                person INTEGER NOT NULL DEFAULT 0,  -- Person enum: 0=None, 1=First, 2=Second, 3=Third
                tense INTEGER NOT NULL DEFAULT 0,   -- Tense enum: 0=None, 1=Present, 2=Future, 3=Aorist, 4=Imperfect, 5=Perfect
                mood INTEGER NOT NULL DEFAULT 0,    -- Mood enum: 0=None, 1=Indicative, 2=Optative, 3=Imperative, 4=Conditional
                voice INTEGER NOT NULL DEFAULT 0,   -- Voice enum: 0=None, 1=Active, 2=Reflexive, 3=Passive, 4=Causative
                in_corpus INTEGER NOT NULL DEFAULT 0,  -- 1 if form appears in Tipitaka corpus, 0 if theoretical only

                FOREIGN KEY (verb_id) REFERENCES verbs(id),
                UNIQUE(verb_id, form, person, tense, mood, voice)
            )
        """)
        
        # Inflection patterns table for reference
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS patterns (
                pattern_name TEXT PRIMARY KEY,
                like_word TEXT,
                pos_category TEXT,
                template_data TEXT
            )
        """)
        
        conn.commit()

        # Create indexes after tables are created
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_declensions_noun ON declensions(noun_id)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_declensions_form ON declensions(form)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_conjugations_verb ON conjugations(verb_id)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_conjugations_form ON conjugations(form)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_nouns_gender ON nouns(gender)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_verbs_pos ON verbs(pos)")
        
        conn.commit()
        conn.close()
        print(f"Created database schema at {self.output_db_path}")
    
    def get_noun_pos_list(self) -> List[str]:
        """Get list of noun POS categories."""
        return ['noun', 'masc', 'fem', 'neut', 'nt', 'abstr', 'act', 'agent', 'dimin']
    
    def get_verb_pos_list(self) -> List[str]:
        """Get list of verb POS categories."""
        return ['vb', 'pr', 'aor', 'fut', 'imperf', 'perf', 'opt', 'imp', 'cond', 
                'caus', 'pass', 'reflx', 'deno', 'desid', 'intens', 'trans', 
                'intrans', 'ditrans', 'impers', 'inf', 'abs', 'ger', 'pp', 
                'prp', 'app', 'ptp', 'comp vb']
    
    def get_training_nouns(self) -> List[DpdHeadword]:
        """Get most frequent nouns suitable for training, with filtering rules applied."""
        noun_pos = self.get_noun_pos_list()

        # Apply filters BEFORE limiting to top N
        words = self.db_session.query(DpdHeadword).filter(
            DpdHeadword.pos.in_(noun_pos),
            DpdHeadword.pattern.isnot(None),
            DpdHeadword.pattern != '',
            DpdHeadword.stem.isnot(None),
            DpdHeadword.stem != '-',
            DpdHeadword.ebt_count > 0,
            # Filter: must have meaning_1
            DpdHeadword.meaning_1.isnot(None),
            DpdHeadword.meaning_1 != '',
            # Filter: must have source_1
            DpdHeadword.source_1.isnot(None),
            DpdHeadword.source_1 != '',
            # Filter: meaning must not contain "(gram)", "(abhi)", "(comm)", "in reference to", "name of", or "family name"
            ~DpdHeadword.meaning_1.contains('(gram)'),
            ~DpdHeadword.meaning_1.contains('(abhi)'),
            ~DpdHeadword.meaning_1.contains('(comm)'),
            ~DpdHeadword.meaning_1.contains('in reference to'),
            ~DpdHeadword.meaning_1.contains('name of'),
            ~DpdHeadword.meaning_1.contains('family name')
        ).order_by(
            DpdHeadword.ebt_count.desc()
        ).limit(self.noun_limit).all()

        # Filter to only words with inflection templates
        words_with_templates = [w for w in words if w.it is not None]

        print(f"Found {len(words_with_templates)} nouns with inflection templates")
        if words_with_templates:
            print(f"Noun frequency range: {words_with_templates[0].ebt_count} (highest) to {words_with_templates[-1].ebt_count} (lowest)")
        return words_with_templates
    
    def get_training_verbs(self) -> List[DpdHeadword]:
        """Get most frequent verbs suitable for training, with filtering rules applied."""
        verb_pos = self.get_verb_pos_list()

        # Excluded verb POS types
        excluded_verb_pos = ['pp', 'prp', 'ptp', 'imperf', 'perf']

        # Apply filters BEFORE limiting to top N
        words = self.db_session.query(DpdHeadword).filter(
            DpdHeadword.pos.in_(verb_pos),
            # Filter: exclude specific POS types
            ~DpdHeadword.pos.in_(excluded_verb_pos),
            DpdHeadword.pattern.isnot(None),
            DpdHeadword.pattern != '',
            DpdHeadword.stem.isnot(None),
            DpdHeadword.stem != '-',
            DpdHeadword.ebt_count > 0,
            # Filter: must have meaning_1
            DpdHeadword.meaning_1.isnot(None),
            DpdHeadword.meaning_1 != '',
            # Filter: must have source_1
            DpdHeadword.source_1.isnot(None),
            DpdHeadword.source_1 != '',
            # Filter: meaning must not contain "(gram)", "(abhi)", "(comm)", "in reference to", "name of", or "family name"
            ~DpdHeadword.meaning_1.contains('(gram)'),
            ~DpdHeadword.meaning_1.contains('(abhi)'),
            ~DpdHeadword.meaning_1.contains('(comm)'),
            ~DpdHeadword.meaning_1.contains('in reference to'),
            ~DpdHeadword.meaning_1.contains('name of'),
            ~DpdHeadword.meaning_1.contains('family name')
        ).order_by(
            DpdHeadword.ebt_count.desc()
        ).limit(self.verb_limit).all()

        # Filter to only words with inflection templates
        words_with_templates = [w for w in words if w.it is not None]

        print(f"Found {len(words_with_templates)} verbs with inflection templates")
        if words_with_templates:
            print(f"Verb frequency range: {words_with_templates[0].ebt_count} (highest) to {words_with_templates[-1].ebt_count} (lowest)")
        return words_with_templates
    
    def parse_inflection_template(self, word: DpdHeadword, word_type: str) -> Tuple[List[Dict[str, Any]], int, int]:
        """Parse inflection/conjugation template to extract individual forms with grammar info.
        Returns: (forms list, total_forms_generated, forms_not_in_corpus)
        """
        if not word.it or not word.it.data:
            return [], 0, 0

        try:
            template_data = json.loads(word.it.data)
        except json.JSONDecodeError:
            return [], 0, 0

        forms = []
        total_generated = 0
        not_in_corpus = 0
        stem = re.sub(r"[!*]", "", word.stem or "")
        
        # Template structure:
        # Row 0: Headers
        # Column 0: Grammar labels (case names for nouns, tense for verbs)
        # Odd columns: declension/conjugation endings
        # Even columns: grammar info
        
        for row_idx, row in enumerate(template_data[1:], 1):  # Skip header row
            if len(row) < 2:
                continue
                
            grammar_label = row[0][0] if row[0] else ""
            
            # Process columns in pairs
            col_idx = 1
            while col_idx < len(row):
                if col_idx >= len(row) or not row[col_idx]:
                    col_idx += 2
                    continue
                
                # Get endings (can be a list)
                endings = row[col_idx]
                if not isinstance(endings, list):
                    endings = [endings]
                
                # Get grammar info
                grammar_info = ""
                if col_idx + 1 < len(row) and row[col_idx + 1]:
                    grammar_data = row[col_idx + 1]
                    grammar_info = grammar_data[0] if isinstance(grammar_data, list) else grammar_data
                
                # Create form entries
                for ending in endings:
                    if ending:
                        inflected_form = f"{stem}{ending}" if ending != "-" else stem
                        total_generated += 1

                        # Check if form appears in the Pali Tipitaka corpus
                        in_corpus = 1 if inflected_form in self.all_tipitaka_words else 0
                        if in_corpus == 0:
                            not_in_corpus += 1

                        # Parse grammar information based on word type
                        if word_type == 'noun':
                            parsed_grammar = self.parse_noun_grammar(grammar_info, grammar_label, word.pos)
                        else:  # verb
                            parsed_grammar = self.parse_verb_grammar(grammar_info, grammar_label, word.pos)

                        form_data = {
                            'form': inflected_form,
                            'in_corpus': in_corpus,
                            **parsed_grammar
                        }
                        forms.append(form_data)

                col_idx += 2

        return forms, total_generated, not_in_corpus
    
    def pos_to_gender(self, pos: str) -> int:
        """Map noun POS to Gender enum value."""
        pos_lower = pos.lower()
        if pos_lower in ['masc', 'masculine']:
            return GrammarEnums.GENDER_MASCULINE
        elif pos_lower in ['fem', 'feminine']:
            return GrammarEnums.GENDER_FEMININE
        elif pos_lower in ['nt', 'neut', 'neuter']:
            return GrammarEnums.GENDER_NEUTER
        else:
            # Default for other noun types (abstr, act, agent, dimin, etc.)
            # Try to infer from word patterns, default to None
            return GrammarEnums.GENDER_NONE

    def parse_noun_grammar(self, grammar_str: str, label: str, pos: str) -> Dict[str, int]:
        """Parse grammar string for nouns, returning enum integer values. Defaults to 0 (None) if not found."""
        result = {
            'case_name': GrammarEnums.CASE_NONE,
            'number': GrammarEnums.NUMBER_NONE,
            'gender': GrammarEnums.GENDER_NONE
        }
        parts = grammar_str.lower().split()

        # Gender - return enum integer
        if 'masc' in grammar_str:
            result['gender'] = GrammarEnums.GENDER_MASCULINE
        elif 'fem' in grammar_str:
            result['gender'] = GrammarEnums.GENDER_FEMININE
        elif 'nt' in grammar_str:
            result['gender'] = GrammarEnums.GENDER_NEUTER

        # Number - return enum integer
        if 'sg' in parts or 'singular' in grammar_str:
            result['number'] = GrammarEnums.NUMBER_SINGULAR
        elif 'pl' in parts or 'plural' in grammar_str:
            result['number'] = GrammarEnums.NUMBER_PLURAL

        # Case - map abbreviation to enum integer
        case_abbr_map = {
            'nom': GrammarEnums.CASE_NOMINATIVE,
            'acc': GrammarEnums.CASE_ACCUSATIVE,
            'instr': GrammarEnums.CASE_INSTRUMENTAL,
            'dat': GrammarEnums.CASE_DATIVE,
            'abl': GrammarEnums.CASE_ABLATIVE,
            'gen': GrammarEnums.CASE_GENITIVE,
            'loc': GrammarEnums.CASE_LOCATIVE,
            'voc': GrammarEnums.CASE_VOCATIVE
        }

        # Check grammar string first
        for abbr, enum_value in case_abbr_map.items():
            if abbr in parts:
                result['case_name'] = enum_value
                break

        # Check label if not found
        if result['case_name'] == GrammarEnums.CASE_NONE and label:
            for abbr, enum_value in case_abbr_map.items():
                if abbr in label.lower():
                    result['case_name'] = enum_value
                    break

        return result
    
    def parse_verb_grammar(self, grammar_str: str, label: str, pos: str) -> Dict[str, int]:
        """Parse grammar string for verbs, returning enum integer values. Defaults to 0 (None) if not found."""
        result = {
            'person': GrammarEnums.PERSON_NONE,
            'tense': GrammarEnums.TENSE_NONE,
            'mood': GrammarEnums.MOOD_NONE,
            'voice': GrammarEnums.VOICE_NONE
        }

        # Person - return enum integer
        if '1st' in grammar_str or 'first' in grammar_str:
            result['person'] = GrammarEnums.PERSON_FIRST
        elif '2nd' in grammar_str or 'second' in grammar_str:
            result['person'] = GrammarEnums.PERSON_SECOND
        elif '3rd' in grammar_str or 'third' in grammar_str:
            result['person'] = GrammarEnums.PERSON_THIRD

        # Tense - return enum integer (from POS or grammar string)
        if pos == 'pr' or 'pres' in grammar_str:
            result['tense'] = GrammarEnums.TENSE_PRESENT
        elif pos == 'aor' or 'aor' in grammar_str:
            result['tense'] = GrammarEnums.TENSE_AORIST
        elif pos == 'fut' or 'fut' in grammar_str:
            result['tense'] = GrammarEnums.TENSE_FUTURE
        elif pos == 'imperf' or 'imperf' in grammar_str:
            result['tense'] = GrammarEnums.TENSE_IMPERFECT
        elif pos == 'perf' or 'perf' in grammar_str:
            result['tense'] = GrammarEnums.TENSE_PERFECT

        # Mood - return enum integer
        if pos == 'opt' or 'opt' in grammar_str:
            result['mood'] = GrammarEnums.MOOD_OPTATIVE
        elif pos == 'imp' or 'imp' in grammar_str:
            result['mood'] = GrammarEnums.MOOD_IMPERATIVE
        elif pos == 'cond' or 'cond' in grammar_str:
            result['mood'] = GrammarEnums.MOOD_CONDITIONAL
        elif result['tense'] != GrammarEnums.TENSE_NONE:  # Default mood for tenses
            result['mood'] = GrammarEnums.MOOD_INDICATIVE

        # Voice - return enum integer (default to Active if not specified)
        if 'caus' in grammar_str:
            result['voice'] = GrammarEnums.VOICE_CAUSATIVE
        elif 'pass' in grammar_str:
            result['voice'] = GrammarEnums.VOICE_PASSIVE
        elif 'reflx' in grammar_str:
            result['voice'] = GrammarEnums.VOICE_REFLEXIVE
        else:
            result['voice'] = GrammarEnums.VOICE_ACTIVE

        return result
    
    def extract_and_save(self):
        """Main extraction process."""
        print("Starting noun and verb extraction...")
        
        # Create schema
        self.create_schema()
        
        # Get words
        nouns = self.get_training_nouns()
        verbs = self.get_training_verbs()
        
        conn = sqlite3.connect(self.output_db_path)
        cursor = conn.cursor()
        
        # Extract patterns first
        patterns_added = set()
        all_words = nouns + verbs
        for word in all_words:
            if word.pattern and word.pattern not in patterns_added and word.it:
                cursor.execute("""
                    INSERT OR REPLACE INTO patterns (pattern_name, like_word, pos_category, template_data)
                    VALUES (?, ?, ?, ?)
                """, (word.pattern, word.it.like, word.pos, word.it.data))
                patterns_added.add(word.pattern)
        
        # Process nouns
        total_declensions = 0
        nouns_processed = 0
        total_noun_forms_generated = 0
        total_noun_forms_filtered = 0

        print(f"\nProcessing {len(nouns)} nouns...")
        for i, word in enumerate(nouns, 1):
            if i % 100 == 0:
                print(f"Processing noun {i}/{len(nouns)}: {word.lemma_1}")

            # Insert noun
            gender = self.pos_to_gender(word.pos)
            cursor.execute("""
                INSERT INTO nouns (
                    id, ebt_count, lemma, lemma_clean, gender, stem, pattern, family_root,
                    meaning, plus_case, source_1, sutta_1, example_1, source_2, sutta_2, example_2
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                word.id, word.ebt_count or 0, word.lemma_1, word.lemma_clean, gender,
                word.stem, word.pattern, word.family_root or '',
                word.meaning_1, word.plus_case or '',
                word.source_1 or '', word.sutta_1 or '', word.example_1 or '',
                word.source_2 or '', word.sutta_2 or '', word.example_2 or ''
            ))

            # Extract declensions
            forms, generated, filtered = self.parse_inflection_template(word, 'noun')
            total_noun_forms_generated += generated
            total_noun_forms_filtered += filtered

            if forms:
                # For nouns, check if we have a nominative singular form
                has_nom_sg = any(
                    f.get('case_name') == GrammarEnums.CASE_NOMINATIVE and f.get('number') == GrammarEnums.NUMBER_SINGULAR
                    for f in forms
                )

                if has_nom_sg:
                    nouns_processed += 1

                    # Insert declensions
                    for form in forms:
                        try:
                            cursor.execute("""
                                INSERT INTO declensions (
                                    noun_id, form, case_name, number, gender, in_corpus
                                ) VALUES (?, ?, ?, ?, ?, ?)
                            """, (
                                word.id,
                                form['form'],
                                form.get('case_name', GrammarEnums.CASE_NONE),
                                form.get('number', GrammarEnums.NUMBER_NONE),
                                form.get('gender', GrammarEnums.GENDER_NONE),
                                form.get('in_corpus', 0)
                            ))
                            total_declensions += 1
                        except sqlite3.IntegrityError:
                            # Skip duplicates
                            pass
        
        # Process verbs
        total_conjugations = 0
        verbs_processed = 0
        total_verb_forms_generated = 0
        total_verb_forms_filtered = 0

        print(f"\nProcessing {len(verbs)} verbs...")
        for i, word in enumerate(verbs, 1):
            if i % 100 == 0:
                print(f"Processing verb {i}/{len(verbs)}: {word.lemma_1}")

            # Insert verb
            cursor.execute("""
                INSERT INTO verbs (
                    id, ebt_count, lemma, lemma_clean, pos, type, trans, stem, pattern, family_root,
                    meaning, plus_case, source_1, sutta_1, example_1, source_2, sutta_2, example_2
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                word.id, word.ebt_count or 0, word.lemma_1, word.lemma_clean, word.pos,
                word.verb or '', word.trans or '', word.stem, word.pattern, word.family_root or '',
                word.meaning_1, word.plus_case or '',
                word.source_1 or '', word.sutta_1 or '', word.example_1 or '',
                word.source_2 or '', word.sutta_2 or '', word.example_2 or ''
            ))

            # Extract conjugations
            forms, generated, filtered = self.parse_inflection_template(word, 'verb')
            total_verb_forms_generated += generated
            total_verb_forms_filtered += filtered

            if forms:
                verbs_processed += 1

                # Insert conjugations
                for form in forms:
                    try:
                        cursor.execute("""
                            INSERT INTO conjugations (
                                verb_id, form, person, tense, mood, voice, in_corpus
                            ) VALUES (?, ?, ?, ?, ?, ?, ?)
                        """, (
                            word.id,
                            form['form'],
                            form.get('person', GrammarEnums.PERSON_NONE),
                            form.get('tense', GrammarEnums.TENSE_NONE),
                            form.get('mood', GrammarEnums.MOOD_NONE),
                            form.get('voice', GrammarEnums.VOICE_NONE),
                            form.get('in_corpus', 0)
                        ))
                        total_conjugations += 1
                    except sqlite3.IntegrityError:
                        # Skip duplicates
                        pass
        
        conn.commit()
        conn.close()

        print(f"\n=== EXTRACTION COMPLETE ===")
        print(f"Database: {self.output_db_path}")
        print(f"Total headwords: {len(nouns) + len(verbs)}")
        print(f"Nouns processed: {nouns_processed}/{len(nouns)}")
        print(f"Verbs processed: {verbs_processed}/{len(verbs)}")
        print(f"Total declensions: {total_declensions}")
        print(f"Total conjugations: {total_conjugations}")
        print(f"Patterns: {len(patterns_added)}")

        # Show corpus attestation statistics
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
        
        self.print_summary_stats()
    
    def print_summary_stats(self):
        """Print summary statistics of extracted data."""
        conn = sqlite3.connect(self.output_db_path)
        cursor = conn.cursor()

        print("\n=== SUMMARY STATISTICS ===")

        # Words by type
        cursor.execute("SELECT COUNT(*) FROM nouns")
        noun_count = cursor.fetchone()[0]
        cursor.execute("SELECT COUNT(*) FROM verbs")
        verb_count = cursor.fetchone()[0]
        print("\nWords by Type:")
        print(f"  nouns: {noun_count}")
        print(f"  verbs: {verb_count}")

        # Nouns by gender
        cursor.execute("SELECT gender, COUNT(*) FROM nouns GROUP BY gender ORDER BY COUNT(*) DESC")
        print("\nNouns by Gender:")
        gender_names = {1: 'masculine', 2: 'neuter', 3: 'feminine', 0: 'none'}
        for gender, count in cursor.fetchall():
            print(f"  {gender_names.get(gender, f'unknown({gender})')}: {count}")

        # Verbs by POS
        cursor.execute("SELECT pos, COUNT(*) FROM verbs GROUP BY pos ORDER BY COUNT(*) DESC LIMIT 10")
        print("\nTop 10 Verb Types:")
        for pos, count in cursor.fetchall():
            print(f"  {pos}: {count}")

        # Sample declensions
        cursor.execute("""
            SELECT n.lemma, n.gender, d.form, d.case_name, d.number
            FROM nouns n
            JOIN declensions d ON n.id = d.noun_id
            WHERE d.case_name > 0
            LIMIT 5
        """)
        print("\nSample Noun Declensions:")
        for row in cursor.fetchall():
            gender_name = gender_names.get(row[1], f'unknown({row[1]})')
            print(f"  {row[0]} ({gender_name}): {row[2]} - {row[3]} {row[4]}")

        # Sample conjugations
        cursor.execute("""
            SELECT v.lemma, v.pos, c.form, c.person, c.tense, c.mood
            FROM verbs v
            JOIN conjugations c ON v.id = c.verb_id
            WHERE c.person > 0
            LIMIT 5
        """)
        print("\nSample Verb Conjugations:")
        for row in cursor.fetchall():
            print(f"  {row[0]} ({row[1]}): {row[2]} - {row[3]} {row[4]} {row[5]}")

        conn.close()


if __name__ == "__main__":
    extractor = NounVerbExtractor(noun_limit=3000, verb_limit=2000)
    extractor.extract_and_save()

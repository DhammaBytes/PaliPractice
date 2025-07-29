#!/usr/bin/env python3
"""
Extract nouns and verbs from DPD database, creating a normalized structure
with separate tables for inflections and conjugations.
"""

import re
import json
import sqlite3
from pathlib import Path
from typing import List, Dict, Any, Optional, Tuple
import sys

sys.path.append('../dpd-db')

from db.db_helpers import get_db_session
from db.models import DpdHeadword, InflectionTemplates
from tools.pos import CONJUGATIONS, DECLENSIONS


class NounVerbExtractor:
    """Extract nouns and verbs with grammatical categorization."""
    
    def __init__(self, output_db_path: str = "../PaliPractice/PaliPractice/Data/training.db",
                 noun_limit: int = 1000, verb_limit: int = 1000):
        self.output_db_path = output_db_path
        self.noun_limit = noun_limit
        self.verb_limit = verb_limit
        self.db_session = get_db_session(Path("../dpd-db/dpd.db"))
        
    def create_schema(self):
        """Create a normalized database schema for nouns and verbs."""
        conn = sqlite3.connect(self.output_db_path)
        cursor = conn.cursor()
        
        # Headwords table - basic information with type column
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS headwords (
                id INTEGER PRIMARY KEY,
                lemma_1 TEXT NOT NULL UNIQUE,
                lemma_clean TEXT NOT NULL,
                pos TEXT NOT NULL,
                type TEXT NOT NULL CHECK(type IN ('noun', 'verb')),
                stem TEXT,
                pattern TEXT,
                meaning_1 TEXT,
                ebt_count INTEGER DEFAULT 0,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP
            )
        """)
        
        # Declensions table - for nouns only (simplified columns)
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS declensions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                headword_id INTEGER NOT NULL,
                form TEXT NOT NULL,
                case_name TEXT,  -- nominative, accusative, etc.
                number TEXT,     -- singular, plural
                gender TEXT,     -- masculine, feminine, neuter
                
                FOREIGN KEY (headword_id) REFERENCES headwords(id),
                UNIQUE(headword_id, form, case_name, number, gender)
            )
        """)
        
        # Conjugations table - for verbs only (simplified columns)
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS conjugations (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                headword_id INTEGER NOT NULL,
                form TEXT NOT NULL,
                person TEXT,     -- first, second, third
                tense TEXT,      -- present, aorist, future, etc.
                mood TEXT,       -- indicative, optative, imperative
                voice TEXT,      -- active, passive, causative
                
                FOREIGN KEY (headword_id) REFERENCES headwords(id),
                UNIQUE(headword_id, form, person, tense, mood, voice)
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
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_declensions_headword ON declensions(headword_id)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_declensions_form ON declensions(form)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_conjugations_headword ON conjugations(headword_id)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_conjugations_form ON conjugations(form)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_headwords_type ON headwords(type)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_headwords_pos ON headwords(pos)")
        
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
        """Get most frequent nouns suitable for training."""
        noun_pos = self.get_noun_pos_list()
        
        words = self.db_session.query(DpdHeadword).filter(
            DpdHeadword.pos.in_(noun_pos),
            DpdHeadword.pattern.isnot(None),
            DpdHeadword.pattern != '',
            DpdHeadword.stem.isnot(None),
            DpdHeadword.stem != '-',
            DpdHeadword.ebt_count > 0
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
        """Get most frequent verbs suitable for training."""
        verb_pos = self.get_verb_pos_list()
        
        words = self.db_session.query(DpdHeadword).filter(
            DpdHeadword.pos.in_(verb_pos),
            DpdHeadword.pattern.isnot(None),
            DpdHeadword.pattern != '',
            DpdHeadword.stem.isnot(None),
            DpdHeadword.stem != '-',
            DpdHeadword.ebt_count > 0
        ).order_by(
            DpdHeadword.ebt_count.desc()
        ).limit(self.verb_limit).all()
        
        # Filter to only words with inflection templates
        words_with_templates = [w for w in words if w.it is not None]
        
        print(f"Found {len(words_with_templates)} verbs with inflection templates")
        if words_with_templates:
            print(f"Verb frequency range: {words_with_templates[0].ebt_count} (highest) to {words_with_templates[-1].ebt_count} (lowest)")
        return words_with_templates
    
    def parse_inflection_template(self, word: DpdHeadword, word_type: str) -> List[Dict[str, Any]]:
        """Parse inflection/conjugation template to extract individual forms with grammar info."""
        if not word.it or not word.it.data:
            return []
            
        try:
            template_data = json.loads(word.it.data)
        except json.JSONDecodeError:
            return []
        
        forms = []
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
                        
                        # Parse grammar information based on word type
                        if word_type == 'noun':
                            parsed_grammar = self.parse_noun_grammar(grammar_info, grammar_label, word.pos)
                        else:  # verb
                            parsed_grammar = self.parse_verb_grammar(grammar_info, grammar_label, word.pos)
                        
                        form_data = {
                            'form': inflected_form,
                            **parsed_grammar
                        }
                        forms.append(form_data)
                
                col_idx += 2
        
        return forms
    
    def parse_noun_grammar(self, grammar_str: str, label: str, pos: str) -> Dict[str, str]:
        """Parse grammar string for nouns."""
        result = {}
        parts = grammar_str.lower().split()
        
        # Gender
        if 'masc' in grammar_str:
            result['gender'] = 'masculine'
        elif 'fem' in grammar_str:
            result['gender'] = 'feminine'
        elif 'nt' in grammar_str:
            result['gender'] = 'neuter'
        
        # Number
        if 'sg' in parts or 'singular' in grammar_str:
            result['number'] = 'singular'
        elif 'pl' in parts or 'plural' in grammar_str:
            result['number'] = 'plural'
        
        # Case
        case_map = {
            'nom': 'nominative',
            'acc': 'accusative', 
            'instr': 'instrumental',
            'dat': 'dative',
            'abl': 'ablative',
            'gen': 'genitive',
            'loc': 'locative',
            'voc': 'vocative'
        }
        
        # Check grammar string first
        for abbr, full in case_map.items():
            if abbr in parts:
                result['case_name'] = full
                break
        
        # Check label if not found
        if 'case_name' not in result and label:
            for abbr, full in case_map.items():
                if abbr in label.lower():
                    result['case_name'] = full
                    break
        
        return result
    
    def parse_verb_grammar(self, grammar_str: str, label: str, pos: str) -> Dict[str, str]:
        """Parse grammar string for verbs."""
        result = {}
        
        # Person
        if '1st' in grammar_str or 'first' in grammar_str:
            result['person'] = 'first'
        elif '2nd' in grammar_str or 'second' in grammar_str:
            result['person'] = 'second'
        elif '3rd' in grammar_str or 'third' in grammar_str:
            result['person'] = 'third'
        
        # Tense (from POS or grammar string)
        if pos == 'pr' or 'pres' in grammar_str:
            result['tense'] = 'present'
        elif pos == 'aor' or 'aor' in grammar_str:
            result['tense'] = 'aorist'
        elif pos == 'fut' or 'fut' in grammar_str:
            result['tense'] = 'future'
        elif pos == 'imperf' or 'imperf' in grammar_str:
            result['tense'] = 'imperfect'
        elif pos == 'perf' or 'perf' in grammar_str:
            result['tense'] = 'perfect'
        
        # Mood
        if pos == 'opt' or 'opt' in grammar_str:
            result['mood'] = 'optative'
        elif pos == 'imp' or 'imp' in grammar_str:
            result['mood'] = 'imperative'
        elif pos == 'cond' or 'cond' in grammar_str:
            result['mood'] = 'conditional'
        elif result.get('tense'):  # Default mood for tenses
            result['mood'] = 'indicative'
        
        # Voice
        if 'caus' in grammar_str:
            result['voice'] = 'causative'
        elif 'pass' in grammar_str:
            result['voice'] = 'passive'
        elif 'reflx' in grammar_str:
            result['voice'] = 'reflexive'
        else:
            result['voice'] = 'active'
        
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
        
        print(f"\nProcessing {len(nouns)} nouns...")
        for i, word in enumerate(nouns, 1):
            if i % 100 == 0:
                print(f"Processing noun {i}/{len(nouns)}: {word.lemma_1}")
            
            # Insert headword
            cursor.execute("""
                INSERT INTO headwords (
                    id, lemma_1, lemma_clean, pos, type, stem, pattern, meaning_1, ebt_count
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                word.id, word.lemma_1, word.lemma_clean, word.pos, 'noun',
                word.stem, word.pattern, word.meaning_1, word.ebt_count or 0
            ))
            
            # Extract declensions
            forms = self.parse_inflection_template(word, 'noun')
            
            if forms:
                # For nouns, check if we have a nominative singular form
                has_nom_sg = any(
                    f.get('case_name') == 'nominative' and f.get('number') == 'singular'
                    for f in forms
                )
                
                if has_nom_sg:
                    nouns_processed += 1
                    
                    # Insert declensions
                    for form in forms:
                        try:
                            cursor.execute("""
                                INSERT INTO declensions (
                                    headword_id, form, case_name, number, gender
                                ) VALUES (?, ?, ?, ?, ?)
                            """, (
                                word.id, form['form'], form.get('case_name'),
                                form.get('number'), form.get('gender')
                            ))
                            total_declensions += 1
                        except sqlite3.IntegrityError:
                            # Skip duplicates
                            pass
        
        # Process verbs
        total_conjugations = 0
        verbs_processed = 0
        
        print(f"\nProcessing {len(verbs)} verbs...")
        for i, word in enumerate(verbs, 1):
            if i % 100 == 0:
                print(f"Processing verb {i}/{len(verbs)}: {word.lemma_1}")
            
            # Insert headword
            cursor.execute("""
                INSERT INTO headwords (
                    id, lemma_1, lemma_clean, pos, type, stem, pattern, meaning_1, ebt_count
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                word.id, word.lemma_1, word.lemma_clean, word.pos, 'verb',
                word.stem, word.pattern, word.meaning_1, word.ebt_count or 0
            ))
            
            # Extract conjugations
            forms = self.parse_inflection_template(word, 'verb')
            
            if forms:
                verbs_processed += 1
                
                # Insert conjugations
                for form in forms:
                    try:
                        cursor.execute("""
                            INSERT INTO conjugations (
                                headword_id, form, person, tense, mood, voice
                            ) VALUES (?, ?, ?, ?, ?, ?)
                        """, (
                            word.id, form['form'], form.get('person'),
                            form.get('tense'), form.get('mood'), form.get('voice')
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
        
        self.print_summary_stats()
    
    def print_summary_stats(self):
        """Print summary statistics of extracted data."""
        conn = sqlite3.connect(self.output_db_path)
        cursor = conn.cursor()
        
        print("\n=== SUMMARY STATISTICS ===")
        
        # Words by type
        cursor.execute("SELECT type, COUNT(*) FROM headwords GROUP BY type")
        print("\nWords by Type:")
        for word_type, count in cursor.fetchall():
            print(f"  {word_type}: {count}")
        
        # Nouns by POS
        cursor.execute("SELECT pos, COUNT(*) FROM headwords WHERE type = 'noun' GROUP BY pos ORDER BY COUNT(*) DESC")
        print("\nNouns by Gender/Type:")
        for pos, count in cursor.fetchall():
            print(f"  {pos}: {count}")
        
        # Verbs by POS
        cursor.execute("SELECT pos, COUNT(*) FROM headwords WHERE type = 'verb' GROUP BY pos ORDER BY COUNT(*) DESC LIMIT 10")
        print("\nTop 10 Verb Types:")
        for pos, count in cursor.fetchall():
            print(f"  {pos}: {count}")
        
        # Sample declensions
        cursor.execute("""
            SELECT h.lemma_1, h.pos, d.form, d.case_name, d.number
            FROM headwords h
            JOIN declensions d ON h.id = d.headword_id
            WHERE d.case_name IS NOT NULL
            LIMIT 5
        """)
        print("\nSample Noun Declensions:")
        for row in cursor.fetchall():
            print(f"  {row[0]} ({row[1]}): {row[2]} - {row[3]} {row[4]}")
        
        # Sample conjugations
        cursor.execute("""
            SELECT h.lemma_1, h.pos, c.form, c.person, c.tense, c.mood
            FROM headwords h
            JOIN conjugations c ON h.id = c.headword_id
            WHERE c.person IS NOT NULL
            LIMIT 5
        """)
        print("\nSample Verb Conjugations:")
        for row in cursor.fetchall():
            print(f"  {row[0]} ({row[1]}): {row[2]} - {row[3]} {row[4]} {row[5]}")
        
        conn.close()


if __name__ == "__main__":
    extractor = NounVerbExtractor(noun_limit=1000, verb_limit=1000)
    extractor.extract_and_save()

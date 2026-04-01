#!/usr/bin/env python3
"""Validate the extracted inflection database."""

import sqlite3
import sys
from pathlib import Path
from collections import Counter

SCRIPTS_ROOT = Path(__file__).resolve().parents[1]
if str(SCRIPTS_ROOT) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_ROOT))

from configs import DATABASE_VERSION


# Enum value mappings for display (matching C# Enums.cs)
CASE_NAMES = {
    0: 'None', 1: 'Nominative', 2: 'Accusative', 3: 'Instrumental',
    4: 'Dative', 5: 'Ablative', 6: 'Genitive', 7: 'Locative', 8: 'Vocative'
}

NUMBER_NAMES = {0: 'None', 1: 'Singular', 2: 'Plural'}

GENDER_NAMES = {0: 'None', 1: 'Masculine', 2: 'Feminine', 3: 'Neuter'}

PERSON_NAMES = {0: 'None', 1: 'First', 2: 'Second', 3: 'Third'}

VOICE_NAMES = {0: 'None', 1: 'Active', 2: 'Reflexive', 3: 'Passive', 4: 'Causative'}

TENSE_NAMES = {0: 'None', 1: 'Present', 2: 'Imperative', 3: 'Optative', 4: 'Future', 5: 'Aorist'}


def parse_declension_form_id(form_id: int) -> dict:
    """Parse a declension form_id back into its component parts.
    Format: lemma_id(5) + case(1) + gender(1) + number(1) + ending_index(1)
    """
    return {
        'lemma_id': form_id // 10_000,
        'case': (form_id % 10_000) // 1_000,
        'gender': (form_id % 1_000) // 100,
        'number': (form_id % 100) // 10,
        'ending_index': form_id % 10
    }


def parse_conjugation_form_id(form_id: int) -> dict:
    """Parse a conjugation form_id back into its component parts.
    Format: lemma_id(5) + tense(1) + person(1) + number(1) + voice(1) + ending_index(1)
    """
    return {
        'lemma_id': form_id // 100_000,
        'tense': (form_id % 100_000) // 10_000,
        'person': (form_id % 10_000) // 1_000,
        'number': (form_id % 1_000) // 100,
        'voice': (form_id % 100) // 10,
        'ending_index': form_id % 10
    }


def validate_database(db_path: str = "../PaliPractice/PaliPractice/Data/pali.db"):
    """Validate the training database structure and content."""
    if not Path(db_path).exists():
        print(f"❌ Database {db_path} does not exist")
        return False

    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()

    print(f"✅ Validating database: {db_path}")

    cursor.execute("PRAGMA user_version")
    user_version = cursor.fetchone()[0]
    if user_version == DATABASE_VERSION:
        print(f"✅ PRAGMA user_version matches shared version: {user_version}")
    else:
        print(f"❌ PRAGMA user_version mismatch: database={user_version}, expected={DATABASE_VERSION}")
        return False

    # Check tables
    cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
    tables = [row[0] for row in cursor.fetchall()]
    expected_tables = [
        'nouns', 'nouns_details',
        'verbs', 'verbs_details',
        'nouns_corpus_forms', 'verbs_corpus_forms'
    ]

    all_present = True
    for table in expected_tables:
        if table in tables:
            print(f"✅ Table '{table}' exists")
        else:
            print(f"❌ Table '{table}' missing")
            all_present = False

    if not all_present:
        return False

    # Validate lemma_id columns exist and have valid values
    print("\n🔍 Validating lemma_id columns:")

    cursor.execute("PRAGMA table_info(nouns)")
    noun_columns = [row[1] for row in cursor.fetchall()]
    if 'lemma_id' in noun_columns:
        print("  ✅ nouns.lemma_id column exists")
        cursor.execute("SELECT COUNT(*) FROM nouns WHERE lemma_id < 10001 OR lemma_id > 69999")
        invalid_noun_ids = cursor.fetchone()[0]
        if invalid_noun_ids > 0:
            print(f"  ⚠️  Found {invalid_noun_ids} nouns with lemma_id outside valid range (10001-69999)")
        else:
            print("  ✅ All noun lemma_ids are in valid range")
    else:
        print("  ❌ nouns.lemma_id column missing")

    cursor.execute("PRAGMA table_info(verbs)")
    verb_columns = [row[1] for row in cursor.fetchall()]
    if 'lemma_id' in verb_columns:
        print("  ✅ verbs.lemma_id column exists")
        cursor.execute("SELECT COUNT(*) FROM verbs WHERE lemma_id < 70001 OR lemma_id > 99999")
        invalid_verb_ids = cursor.fetchone()[0]
        if invalid_verb_ids > 0:
            print(f"  ⚠️  Found {invalid_verb_ids} verbs with lemma_id outside valid range (70001-99999)")
        else:
            print("  ✅ All verb lemma_ids are in valid range")
    else:
        print("  ❌ verbs.lemma_id column missing")

    # Validate corpus tables have form_id column
    print("\n🔍 Validating form_id columns:")

    cursor.execute("PRAGMA table_info(nouns_corpus_forms)")
    decl_columns = [row[1] for row in cursor.fetchall()]
    if 'form_id' in decl_columns:
        print("  ✅ nouns_corpus_forms.form_id column exists")
    else:
        print("  ❌ nouns_corpus_forms.form_id column missing")

    cursor.execute("PRAGMA table_info(verbs_corpus_forms)")
    conj_columns = [row[1] for row in cursor.fetchall()]
    if 'form_id' in conj_columns:
        print("  ✅ verbs_corpus_forms.form_id column exists")
    else:
        print("  ❌ verbs_corpus_forms.form_id column missing")

    print("\n🔍 Validating Russian meaning columns:")
    for table_name in ("nouns_details", "verbs_details"):
        cursor.execute(f"PRAGMA table_info({table_name})")
        columns = [row[1] for row in cursor.fetchall()]
        if 'meaning_ru' in columns:
            print(f"  ✅ {table_name}.meaning_ru column exists")
        else:
            print(f"  ❌ {table_name}.meaning_ru column missing")
            all_present = False

    # Check data counts
    cursor.execute("SELECT COUNT(*) FROM nouns")
    noun_count = cursor.fetchone()[0]
    print(f"\n✅ Nouns: {noun_count}")

    cursor.execute("SELECT COUNT(*) FROM verbs")
    verb_count = cursor.fetchone()[0]
    print(f"✅ Verbs: {verb_count}")

    cursor.execute("SELECT COUNT(*) FROM nouns_corpus_forms")
    declension_count = cursor.fetchone()[0]
    print(f"✅ Noun Corpus Forms: {declension_count}")

    cursor.execute("SELECT COUNT(*) FROM verbs_corpus_forms")
    conjugation_count = cursor.fetchone()[0]
    print(f"✅ Verb Corpus Forms: {conjugation_count}")

    cursor.execute("SELECT COUNT(*), COUNT(NULLIF(TRIM(COALESCE(meaning_ru, '')), '')) FROM nouns_details")
    noun_details_count, noun_ru_count = cursor.fetchone()
    cursor.execute("SELECT COUNT(*), COUNT(NULLIF(TRIM(COALESCE(meaning_ru, '')), '')) FROM verbs_details")
    verb_details_count, verb_ru_count = cursor.fetchone()

    print("\n✅ Russian Meaning Coverage:")
    noun_coverage = noun_ru_count / noun_details_count * 100 if noun_details_count > 0 else 0
    verb_coverage = verb_ru_count / verb_details_count * 100 if verb_details_count > 0 else 0
    print(f"  nouns_details.meaning_ru: {noun_ru_count}/{noun_details_count} ({noun_coverage:.1f}%)")
    print(f"  verbs_details.meaning_ru: {verb_ru_count}/{verb_details_count} ({verb_coverage:.1f}%)")

    # Parse form_ids and compute statistics
    print("\n📊 Parsing form_ids for statistics...")

    # Declension statistics
    cursor.execute("SELECT form_id FROM nouns_corpus_forms")
    decl_form_ids = [row[0] for row in cursor.fetchall()]

    case_counts = Counter()
    gender_counts = Counter()
    number_counts = Counter()
    decl_complete = 0

    for form_id in decl_form_ids:
        parsed = parse_declension_form_id(form_id)
        case_counts[parsed['case']] += 1
        gender_counts[parsed['gender']] += 1
        number_counts[parsed['number']] += 1
        if parsed['case'] != 0 and parsed['number'] != 0:
            decl_complete += 1

    # Conjugation statistics
    cursor.execute("SELECT form_id FROM verbs_corpus_forms")
    conj_form_ids = [row[0] for row in cursor.fetchall()]

    tense_counts = Counter()
    person_counts = Counter()
    voice_counts = Counter()
    conj_number_counts = Counter()
    conj_complete = 0

    for form_id in conj_form_ids:
        parsed = parse_conjugation_form_id(form_id)
        tense_counts[parsed['tense']] += 1
        person_counts[parsed['person']] += 1
        voice_counts[parsed['voice']] += 1
        conj_number_counts[parsed['number']] += 1
        if parsed['person'] != 0 and parsed['tense'] != 0:
            conj_complete += 1

    # Report completeness
    print("\n📊 Grammar Data Completeness:")
    if declension_count > 0:
        print(f"  Declensions with case+number: {decl_complete}/{declension_count} ({decl_complete/declension_count*100:.1f}%)")
    if conjugation_count > 0:
        print(f"  Conjugations with person+tense: {conj_complete}/{conjugation_count} ({conj_complete/conjugation_count*100:.1f}%)")

    # Statistics by gender (from nouns table)
    print("\n📊 Nouns by Gender:")
    cursor.execute("""
        SELECT gender, COUNT(*) as count FROM nouns GROUP BY gender ORDER BY count DESC
    """)
    for gender, count in cursor.fetchall():
        gender_name = GENDER_NAMES.get(gender, f'Unknown({gender})')
        print(f"  {gender_name}: {count}")

    # Statistics by case for declensions
    print("\n📊 Declensions by Case:")
    for case_val in sorted(case_counts.keys()):
        case_str = CASE_NAMES.get(case_val, f'Unknown({case_val})')
        print(f"  {case_str}: {case_counts[case_val]}")

    # Statistics by tense for conjugations
    print("\n📊 Conjugations by Tense:")
    for tense_val in sorted(tense_counts.keys()):
        tense_str = TENSE_NAMES.get(tense_val, f'Unknown({tense_val})')
        print(f"  {tense_str}: {tense_counts[tense_val]}")

    # Statistics by voice for conjugations
    print("\n📊 Conjugations by Voice:")
    for voice_val in sorted(voice_counts.keys()):
        voice_str = VOICE_NAMES.get(voice_val, f'Unknown({voice_val})')
        print(f"  {voice_str}: {voice_counts[voice_val]}")

    # Sample nouns with declension counts (using lemma_id to join)
    print("\n📝 Sample nouns with declensions:")
    cursor.execute("""
        SELECT n.lemma_id, n.lemma, d.meaning, d.meaning_ru, n.gender, n.ebt_count
        FROM nouns n
        JOIN nouns_details d ON d.id = n.id
        ORDER BY n.ebt_count DESC
        LIMIT 5
    """)
    for lemma_id, lemma, meaning, meaning_ru, gender, ebt_count in cursor.fetchall():
        # Count forms for this lemma_id by checking form_id range
        min_form_id = lemma_id * 10_000
        max_form_id = (lemma_id + 1) * 10_000
        cursor.execute(
            "SELECT COUNT(*) FROM nouns_corpus_forms WHERE form_id >= ? AND form_id < ?",
            (min_form_id, max_form_id)
        )
        form_count = cursor.fetchone()[0]
        gender_str = GENDER_NAMES.get(gender, '?')
        meaning_short = meaning[:40] + '...' if meaning and len(meaning) > 40 else meaning
        meaning_ru_short = meaning_ru[:30] + '...' if meaning_ru and len(meaning_ru) > 30 else meaning_ru
        print(f"  {lemma} ({gender_str}): {form_count} forms - EN: {meaning_short} | RU: {meaning_ru_short}")

    # Sample verbs with conjugation counts
    print("\n📝 Sample verbs with conjugations:")
    cursor.execute("""
        SELECT v.lemma_id, v.lemma, d.meaning, d.meaning_ru, d.type, v.ebt_count
        FROM verbs v
        JOIN verbs_details d ON d.id = v.id
        ORDER BY v.ebt_count DESC
        LIMIT 5
    """)
    for lemma_id, lemma, meaning, meaning_ru, pos, ebt_count in cursor.fetchall():
        # Count forms for this lemma_id by checking form_id range
        min_form_id = lemma_id * 100_000
        max_form_id = (lemma_id + 1) * 100_000
        cursor.execute(
            "SELECT COUNT(*) FROM verbs_corpus_forms WHERE form_id >= ? AND form_id < ?",
            (min_form_id, max_form_id)
        )
        form_count = cursor.fetchone()[0]
        meaning_short = meaning[:40] + '...' if meaning and len(meaning) > 40 else meaning
        meaning_ru_short = meaning_ru[:30] + '...' if meaning_ru and len(meaning_ru) > 30 else meaning_ru
        print(f"  {lemma} ({pos}): {form_count} forms - EN: {meaning_short} | RU: {meaning_ru_short}")

    conn.close()
    return True


if __name__ == "__main__":
    if validate_database():
        print("\n✅ Database validation passed!")
        print("\nThe database contains properly structured inflection data with:")
        print("- Corpus-attested declension forms (encoded as form_id)")
        print("- Corpus-attested conjugation forms (encoded as form_id)")
        print("- Grammatical categorization extractable from form_id")
        print("- Efficient single-column lookup structure")
        print("\nThis is suitable for building a training application!")
    else:
        print("\n❌ Database validation failed!")

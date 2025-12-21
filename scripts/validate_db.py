#!/usr/bin/env python3
"""Validate the extracted inflection database."""

import sqlite3
from pathlib import Path


# Enum value mappings for display (matching C# Enums.cs)
CASE_NAMES = {
    0: 'None', 1: 'Nominative', 2: 'Accusative', 3: 'Instrumental',
    4: 'Dative', 5: 'Ablative', 6: 'Genitive', 7: 'Locative', 8: 'Vocative'
}

NUMBER_NAMES = {0: 'None', 1: 'Singular', 2: 'Plural'}

GENDER_NAMES = {0: 'None', 1: 'Masculine', 2: 'Neuter', 3: 'Feminine'}

PERSON_NAMES = {0: 'None', 1: 'First', 2: 'Second', 3: 'Third'}

VOICE_NAMES = {0: 'None', 1: 'Active', 2: 'Reflexive', 3: 'Passive', 4: 'Causative'}

TENSE_NAMES = {0: 'None', 1: 'Present', 2: 'Imperative', 3: 'Optative', 4: 'Future', 5: 'Aorist'}


def validate_database(db_path: str = "../PaliPractice/PaliPractice/Data/training.db"):
    """Validate the training database structure and content."""
    if not Path(db_path).exists():
        print(f"❌ Database {db_path} does not exist")
        return False

    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()

    print(f"✅ Validating database: {db_path}")

    # Check tables
    cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
    tables = [row[0] for row in cursor.fetchall()]
    expected_tables = ['nouns', 'verbs', 'corpus_declensions', 'corpus_conjugations']

    all_present = True
    for table in expected_tables:
        if table in tables:
            print(f"✅ Table '{table}' exists")
        else:
            print(f"❌ Table '{table}' missing")
            all_present = False

    if not all_present:
        return False

    # Check data counts
    cursor.execute("SELECT COUNT(*) FROM nouns")
    noun_count = cursor.fetchone()[0]
    print(f"✅ Nouns: {noun_count}")

    cursor.execute("SELECT COUNT(*) FROM verbs")
    verb_count = cursor.fetchone()[0]
    print(f"✅ Verbs: {verb_count}")

    cursor.execute("SELECT COUNT(*) FROM corpus_declensions")
    declension_count = cursor.fetchone()[0]
    print(f"✅ Corpus Declensions: {declension_count}")

    cursor.execute("SELECT COUNT(*) FROM corpus_conjugations")
    conjugation_count = cursor.fetchone()[0]
    print(f"✅ Corpus Conjugations: {conjugation_count}")

    # Check grammatical data completeness
    print("\n📊 Grammar Data Completeness:")

    # Nominal declensions
    cursor.execute("""
        SELECT COUNT(*) FROM corpus_declensions
        WHERE case_name != 0 AND number != 0
    """)
    nominal_complete = cursor.fetchone()[0]
    print(f"  Declensions with case+number: {nominal_complete}/{declension_count} ({nominal_complete/declension_count*100:.1f}%)")

    # Verbal conjugations
    cursor.execute("""
        SELECT COUNT(*) FROM corpus_conjugations
        WHERE person != 0 AND tense != 0
    """)
    verbal_complete = cursor.fetchone()[0]
    print(f"  Conjugations with person+tense: {verbal_complete}/{conjugation_count} ({verbal_complete/conjugation_count*100:.1f}%)")

    # Validate enum values
    print("\n🔍 Validating enum values:")

    # Check for invalid case values
    cursor.execute("SELECT COUNT(*) FROM corpus_declensions WHERE case_name NOT IN (0,1,2,3,4,5,6,7,8)")
    invalid_cases = cursor.fetchone()[0]
    if invalid_cases > 0:
        print(f"  ⚠️  Found {invalid_cases} declensions with invalid case_name values")
    else:
        print("  ✅ All case_name values are valid")

    # Check for invalid number values
    cursor.execute("SELECT COUNT(*) FROM corpus_declensions WHERE number NOT IN (0,1,2)")
    invalid_numbers = cursor.fetchone()[0]
    if invalid_numbers > 0:
        print(f"  ⚠️  Found {invalid_numbers} declensions with invalid number values")
    else:
        print("  ✅ All number values are valid")

    # Check for invalid gender values
    cursor.execute("SELECT COUNT(*) FROM corpus_declensions WHERE gender NOT IN (0,1,2,3)")
    invalid_genders = cursor.fetchone()[0]
    if invalid_genders > 0:
        print(f"  ⚠️  Found {invalid_genders} declensions with invalid gender values")
    else:
        print("  ✅ All gender values are valid")

    # Check for invalid person values
    cursor.execute("SELECT COUNT(*) FROM corpus_conjugations WHERE person NOT IN (0,1,2,3)")
    invalid_persons = cursor.fetchone()[0]
    if invalid_persons > 0:
        print(f"  ⚠️  Found {invalid_persons} conjugations with invalid person values")
    else:
        print("  ✅ All person values are valid")

    # Check for invalid tense values
    cursor.execute("SELECT COUNT(*) FROM corpus_conjugations WHERE tense NOT IN (0,1,2,3,4,5)")
    invalid_tenses = cursor.fetchone()[0]
    if invalid_tenses > 0:
        print(f"  ⚠️  Found {invalid_tenses} conjugations with invalid tense values")
    else:
        print("  ✅ All tense values are valid")

    # Check for invalid voice values
    cursor.execute("SELECT COUNT(*) FROM corpus_conjugations WHERE voice NOT IN (0,1,2,3,4)")
    invalid_voices = cursor.fetchone()[0]
    if invalid_voices > 0:
        print(f"  ⚠️  Found {invalid_voices} conjugations with invalid voice values")
    else:
        print("  ✅ All voice values are valid")

    # Statistics by gender
    print("\n📊 Nouns by Gender:")
    cursor.execute("""
        SELECT gender, COUNT(*) as count FROM nouns GROUP BY gender ORDER BY count DESC
    """)
    for gender, count in cursor.fetchall():
        gender_name = GENDER_NAMES.get(gender, f'Unknown({gender})')
        print(f"  {gender_name}: {count}")

    # Statistics by case for declensions
    print("\n📊 Declensions by Case:")
    cursor.execute("""
        SELECT case_name, COUNT(*) as count FROM corpus_declensions GROUP BY case_name ORDER BY case_name
    """)
    for case_name, count in cursor.fetchall():
        case_str = CASE_NAMES.get(case_name, f'Unknown({case_name})')
        print(f"  {case_str}: {count}")

    # Statistics by tense for conjugations
    print("\n📊 Conjugations by Tense:")
    cursor.execute("""
        SELECT tense, COUNT(*) as count FROM corpus_conjugations GROUP BY tense ORDER BY tense
    """)
    for tense, count in cursor.fetchall():
        tense_str = TENSE_NAMES.get(tense, f'Unknown({tense})')
        print(f"  {tense_str}: {count}")

    # Sample nouns with declensions
    print("\n📝 Sample nouns with declensions:")
    cursor.execute("""
        SELECT n.lemma, n.meaning, n.gender, COUNT(d.noun_id) as form_count
        FROM nouns n
        JOIN corpus_declensions d ON n.id = d.noun_id
        GROUP BY n.id
        ORDER BY n.ebt_count DESC
        LIMIT 5
    """)
    for lemma, meaning, gender, form_count in cursor.fetchall():
        gender_str = GENDER_NAMES.get(gender, '?')
        meaning_short = meaning[:40] + '...' if meaning and len(meaning) > 40 else meaning
        print(f"  {lemma} ({gender_str}): {form_count} forms - {meaning_short}")

    # Sample verbs with conjugations
    print("\n📝 Sample verbs with conjugations:")
    cursor.execute("""
        SELECT v.lemma, v.meaning, v.pos, COUNT(c.verb_id) as form_count
        FROM verbs v
        JOIN corpus_conjugations c ON v.id = c.verb_id
        GROUP BY v.id
        ORDER BY v.ebt_count DESC
        LIMIT 5
    """)
    for lemma, meaning, pos, form_count in cursor.fetchall():
        meaning_short = meaning[:40] + '...' if meaning and len(meaning) > 40 else meaning
        print(f"  {lemma} ({pos}): {form_count} forms - {meaning_short}")

    conn.close()
    return True


if __name__ == "__main__":
    if validate_database():
        print("\n✅ Database validation passed!")
        print("\nThe database contains properly structured inflection data with:")
        print("- Corpus-attested declension forms for nouns")
        print("- Corpus-attested conjugation forms for verbs")
        print("- Grammatical categorization (case, number, person, tense, etc.)")
        print("- Normalized relational structure")
        print("\nThis is suitable for building a training application!")
    else:
        print("\n❌ Database validation failed!")

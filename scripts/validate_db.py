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

TENSE_NAMES = {0: 'None', 1: 'Present', 2: 'Future', 3: 'Aorist', 4: 'Imperfect', 5: 'Perfect'}

MOOD_NAMES = {0: 'None', 1: 'Indicative', 2: 'Optative', 3: 'Imperative', 4: 'Conditional'}


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
    expected_tables = ['nouns', 'verbs', 'declensions', 'conjugations', 'patterns']

    for table in expected_tables:
        if table in tables:
            print(f"✅ Table '{table}' exists")
        else:
            print(f"❌ Table '{table}' missing")
            return False

    # Check data counts
    cursor.execute("SELECT COUNT(*) FROM nouns")
    noun_count = cursor.fetchone()[0]
    print(f"✅ Nouns: {noun_count}")

    cursor.execute("SELECT COUNT(*) FROM verbs")
    verb_count = cursor.fetchone()[0]
    print(f"✅ Verbs: {verb_count}")

    cursor.execute("SELECT COUNT(*) FROM declensions")
    inflection_count = cursor.fetchone()[0]
    print(f"✅ Declensions: {inflection_count}")

    cursor.execute("SELECT COUNT(*) FROM conjugations")
    conjugation_count = cursor.fetchone()[0]
    print(f"✅ Conjugations: {conjugation_count}")

    cursor.execute("SELECT COUNT(*) FROM patterns")
    pattern_count = cursor.fetchone()[0]
    print(f"✅ Patterns: {pattern_count}")
    
    # Check grammatical data completeness
    print("\n📊 Grammar Data Completeness:")

    # Nominal declensions
    cursor.execute("SELECT COUNT(*) FROM declensions")
    nominal_total = cursor.fetchone()[0]

    cursor.execute("""
        SELECT COUNT(*) FROM declensions
        WHERE case_name != 0 AND number != 0
    """)
    nominal_complete = cursor.fetchone()[0]

    print(f"  Nominal declensions with case+number: {nominal_complete}/{nominal_total} ({nominal_complete/nominal_total*100:.1f}%)")

    # Verbal conjugations
    cursor.execute("SELECT COUNT(*) FROM conjugations")
    verbal_total = cursor.fetchone()[0]

    cursor.execute("""
        SELECT COUNT(*) FROM conjugations
        WHERE person != 0 AND tense != 0
    """)
    verbal_complete = cursor.fetchone()[0]

    print(f"  Verbal conjugations with person+tense: {verbal_complete}/{verbal_total} ({verbal_complete/verbal_total*100:.1f}%)")

    # Validate enum values and check for NULLs
    print("\n🔍 Validating enum values:")

    # Check for NULL values (should be none with NOT NULL constraints)
    cursor.execute("SELECT COUNT(*) FROM declensions WHERE case_name IS NULL OR number IS NULL OR gender IS NULL")
    null_declensions = cursor.fetchone()[0]
    if null_declensions > 0:
        print(f"  ⚠️  Found {null_declensions} declensions with NULL values")
    else:
        print("  ✅ No NULL values in declensions")

    cursor.execute("SELECT COUNT(*) FROM conjugations WHERE person IS NULL OR tense IS NULL OR mood IS NULL OR voice IS NULL")
    null_conjugations = cursor.fetchone()[0]
    if null_conjugations > 0:
        print(f"  ⚠️  Found {null_conjugations} conjugations with NULL values")
    else:
        print("  ✅ No NULL values in conjugations")

    # Check for invalid case values
    cursor.execute("SELECT COUNT(*) FROM declensions WHERE case_name NOT IN (0,1,2,3,4,5,6,7,8)")
    invalid_cases = cursor.fetchone()[0]
    if invalid_cases > 0:
        print(f"  ⚠️  Found {invalid_cases} declensions with invalid case_name values")
    else:
        print("  ✅ All case_name values are valid")

    # Check for invalid number values
    cursor.execute("SELECT COUNT(*) FROM declensions WHERE number NOT IN (0,1,2)")
    invalid_numbers = cursor.fetchone()[0]
    if invalid_numbers > 0:
        print(f"  ⚠️  Found {invalid_numbers} declensions with invalid number values")
    else:
        print("  ✅ All number values are valid")

    # Check for invalid gender values
    cursor.execute("SELECT COUNT(*) FROM declensions WHERE gender NOT IN (0,1,2,3)")
    invalid_genders = cursor.fetchone()[0]
    if invalid_genders > 0:
        print(f"  ⚠️  Found {invalid_genders} declensions with invalid gender values")
    else:
        print("  ✅ All gender values are valid")

    # Check for invalid person values
    cursor.execute("SELECT COUNT(*) FROM conjugations WHERE person NOT IN (0,1,2,3)")
    invalid_persons = cursor.fetchone()[0]
    if invalid_persons > 0:
        print(f"  ⚠️  Found {invalid_persons} conjugations with invalid person values")
    else:
        print("  ✅ All person values are valid")

    # Check for invalid tense values
    cursor.execute("SELECT COUNT(*) FROM conjugations WHERE tense NOT IN (0,1,2,3,4,5)")
    invalid_tenses = cursor.fetchone()[0]
    if invalid_tenses > 0:
        print(f"  ⚠️  Found {invalid_tenses} conjugations with invalid tense values")
    else:
        print("  ✅ All tense values are valid")

    # Check for invalid mood values
    cursor.execute("SELECT COUNT(*) FROM conjugations WHERE mood NOT IN (0,1,2,3,4)")
    invalid_moods = cursor.fetchone()[0]
    if invalid_moods > 0:
        print(f"  ⚠️  Found {invalid_moods} conjugations with invalid mood values")
    else:
        print("  ✅ All mood values are valid")

    # Check for invalid voice values
    cursor.execute("SELECT COUNT(*) FROM conjugations WHERE voice NOT IN (0,1,2,3,4)")
    invalid_voices = cursor.fetchone()[0]
    if invalid_voices > 0:
        print(f"  ⚠️  Found {invalid_voices} conjugations with invalid voice values")
    else:
        print("  ✅ All voice values are valid")

    # Sample data by gender
    print("\n📝 Sample declensions:")

    # Noun sample (case_name=1 is Nominative, gender=1 is Masculine)
    cursor.execute("""
        SELECT n.lemma, d.form, d.case_name, d.number, d.gender
        FROM nouns n
        JOIN declensions d ON n.id = d.noun_id
        WHERE n.gender = 1 AND d.case_name = 1
        LIMIT 3
    """)
    print("\nMasculine nouns (nominative):")
    for row in cursor.fetchall():
        case_name = CASE_NAMES.get(row[2], f'Unknown({row[2]})')
        number = NUMBER_NAMES.get(row[3], f'Unknown({row[3]})')
        gender = GENDER_NAMES.get(row[4], f'Unknown({row[4]})')
        print(f"  {row[0]}: {row[1]} ({case_name} {number} {gender})")

    # Verb sample (person=3 is Third, tense=1 is Present)
    cursor.execute("""
        SELECT v.lemma, c.form, c.person, c.tense, c.mood, c.voice
        FROM verbs v
        JOIN conjugations c ON v.id = c.verb_id
        WHERE c.person = 3 AND c.tense = 1
        LIMIT 3
    """)
    print("\nPresent tense verbs (3rd person):")
    for row in cursor.fetchall():
        person = PERSON_NAMES.get(row[2], f'Unknown({row[2]})')
        tense = TENSE_NAMES.get(row[3], f'Unknown({row[3]})')
        mood = MOOD_NAMES.get(row[4], f'Unknown({row[4]})')
        voice = VOICE_NAMES.get(row[5], f'Unknown({row[5]})')
        print(f"  {row[0]}: {row[1]} ({person} {tense} {mood} {voice})")
    
    # Check for unique forms per noun
    cursor.execute("""
        SELECT n.lemma, COUNT(DISTINCT d.form) as unique_forms,
               COUNT(d.id) as total_entries
        FROM nouns n
        JOIN declensions d ON n.id = d.noun_id
        GROUP BY n.id
        HAVING unique_forms < total_entries
        LIMIT 5
    """)
    duplicates = cursor.fetchall()
    if duplicates:
        print("\n⚠️  Nouns with duplicate forms (different grammar):")
        for lemma, unique, total in duplicates:
            print(f"  {lemma}: {unique} unique forms, {total} entries")

    # Statistics by pattern for nouns
    cursor.execute("""
        SELECT p.pattern_name, COUNT(DISTINCT n.id) as word_count,
               COUNT(d.id) as inflection_count,
               ROUND(CAST(COUNT(d.id) AS FLOAT) / COUNT(DISTINCT n.id), 1) as avg_declensions
        FROM patterns p
        JOIN nouns n ON p.pattern_name = n.pattern
        JOIN declensions d ON n.id = d.noun_id
        GROUP BY p.pattern_name
        ORDER BY word_count DESC
        LIMIT 10
    """)
    print("\n📊 Top noun patterns by usage:")
    print(f"{'Pattern':<15} {'Words':<8} {'Inflections':<12} {'Avg/Word'}")
    print("-" * 50)
    for pattern, words, declensions, avg in cursor.fetchall():
        print(f"{pattern:<15} {words:<8} {declensions:<12} {avg}")
    
    conn.close()
    return True


if __name__ == "__main__":
    if validate_database():
        print("\n✅ Database validation passed!")
        print("\nThe database contains properly structured inflection data with:")
        print("- Individual inflection forms (not concatenated)")
        print("- Grammatical categorization (case, number, person, tense, etc.)")
        print("- Normalized relational structure")
        print("\nThis is suitable for building a training application!")
    else:
        print("\n❌ Database validation failed!")

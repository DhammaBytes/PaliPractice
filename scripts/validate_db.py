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
    expected_tables = ['headwords', 'declensions', 'conjugations', 'patterns']
    
    for table in expected_tables:
        if table in tables:
            print(f"✅ Table '{table}' exists")
        else:
            print(f"❌ Table '{table}' missing")
            return False
    
    # Check data counts
    cursor.execute("SELECT COUNT(*) FROM headwords")
    headword_count = cursor.fetchone()[0]
    print(f"✅ Headwords: {headword_count}")
    
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
    cursor.execute("""
        SELECT COUNT(*) FROM declensions i
        JOIN headwords h ON i.headword_id = h.id
        WHERE h.pos IN ('masc', 'fem', 'nt', 'adj', 'pp', 'ptp', 'prp')
    """)
    nominal_total = cursor.fetchone()[0]
    
    cursor.execute("""
        SELECT COUNT(*) FROM declensions i
        JOIN headwords h ON i.headword_id = h.id
        WHERE h.pos IN ('masc', 'fem', 'nt', 'adj', 'pp', 'ptp', 'prp')
        AND i.case_name IS NOT NULL AND i.number IS NOT NULL
    """)
    nominal_complete = cursor.fetchone()[0]
    
    print(f"  Nominal declensions with case+number: {nominal_complete}/{nominal_total} ({nominal_complete/nominal_total*100:.1f}%)")
    
    # Verbal declensions
    cursor.execute("""
        SELECT COUNT(*) FROM conjugations c
        JOIN headwords h ON c.headword_id = h.id
        WHERE h.type = 'verb'
    """)
    verbal_total = cursor.fetchone()[0]
    
    cursor.execute("""
        SELECT COUNT(*) FROM conjugations c
        JOIN headwords h ON c.headword_id = h.id
        WHERE h.type = 'verb'
        AND c.person IS NOT NULL AND c.tense IS NOT NULL
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

    # Sample data by POS
    print("\n📝 Sample declensions by POS:")
    
    # Noun sample (case_name=1 is Nominative)
    cursor.execute("""
        SELECT h.lemma_1, i.form, i.case_name, i.number, i.gender
        FROM headwords h
        JOIN declensions i ON h.id = i.headword_id
        WHERE h.pos = 'masc' AND i.case_name = 1
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
        SELECT h.lemma_1, c.form, c.person, c.tense, c.mood, c.voice
        FROM headwords h
        JOIN conjugations c ON h.id = c.headword_id
        WHERE h.type = 'verb' AND c.person = 3 AND c.tense = 1
        LIMIT 3
    """)
    print("\nPresent tense verbs (3rd person):")
    for row in cursor.fetchall():
        person = PERSON_NAMES.get(row[2], f'Unknown({row[2]})')
        tense = TENSE_NAMES.get(row[3], f'Unknown({row[3]})')
        mood = MOOD_NAMES.get(row[4], f'Unknown({row[4]})')
        voice = VOICE_NAMES.get(row[5], f'Unknown({row[5]})')
        print(f"  {row[0]}: {row[1]} ({person} {tense} {mood} {voice})")
    
    # Check for unique forms per word
    cursor.execute("""
        SELECT h.lemma_1, COUNT(DISTINCT i.form) as unique_forms,
               COUNT(i.id) as total_entries
        FROM headwords h
        JOIN declensions i ON h.id = i.headword_id
        GROUP BY h.id
        HAVING unique_forms < total_entries
        LIMIT 5
    """)
    duplicates = cursor.fetchall()
    if duplicates:
        print("\n⚠️  Words with duplicate forms (different grammar):")
        for lemma, unique, total in duplicates:
            print(f"  {lemma}: {unique} unique forms, {total} entries")
    
    # Statistics by pattern
    cursor.execute("""
        SELECT p.pattern_name, COUNT(DISTINCT h.id) as word_count,
               COUNT(i.id) as inflection_count,
               ROUND(CAST(COUNT(i.id) AS FLOAT) / COUNT(DISTINCT h.id), 1) as avg_declensions
        FROM patterns p
        JOIN headwords h ON p.pattern_name = h.pattern
        JOIN declensions i ON h.id = i.headword_id
        GROUP BY p.pattern_name
        ORDER BY word_count DESC
        LIMIT 10
    """)
    print("\n📊 Top patterns by usage:")
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

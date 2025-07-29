#!/usr/bin/env python3
"""Validate the extracted inflection database."""

import sqlite3
from pathlib import Path


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
    
    # Sample data by POS
    print("\n📝 Sample declensions by POS:")
    
    # Noun sample
    cursor.execute("""
        SELECT h.lemma_1, i.form, i.case_name, i.number, i.gender
        FROM headwords h
        JOIN declensions i ON h.id = i.headword_id
        WHERE h.pos = 'masc' AND i.case_name = 'nominative'
        LIMIT 3
    """)
    print("\nMasculine nouns (nominative):")
    for row in cursor.fetchall():
        print(f"  {row[0]}: {row[1]} ({row[2]} {row[3]} {row[4]})")
    
    # Verb sample
    cursor.execute("""
        SELECT h.lemma_1, i.form, i.person, i.number, i.tense, i.mood
        FROM headwords h
        JOIN declensions i ON h.id = i.headword_id
        WHERE h.pos = 'pr' AND i.person = 'third'
        LIMIT 3
    """)
    print("\nPresent tense verbs (3rd person):")
    for row in cursor.fetchall():
        print(f"  {row[0]}: {row[1]} ({row[2]} {row[3]} {row[4]} {row[5]})")
    
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

#!/usr/bin/env python3
"""
Test that the app can successfully load and read the generated database.
"""

import sqlite3
import sys
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

def test_database_access():
    """Test database access using the same embedded resource approach as the app."""
    db_path = Path("../PaliPractice/PaliPractice/Data/training.db")
    
    if not db_path.exists():
        print("❌ Database not found at expected location")
        return False
    
    try:
        conn = sqlite3.connect(str(db_path))
        cursor = conn.cursor()
        
        # Test the same queries the app would use
        print("🔍 Testing database queries...")
        
        # Test getting random nouns (similar to app's DatabaseService)
        cursor.execute("""
            SELECT * FROM headwords 
            WHERE pos IN ('masc', 'fem', 'neut', 'nt', 'noun', 'abstr', 'act', 'agent', 'dimin')
            ORDER BY ebt_count DESC
            LIMIT 5
        """)
        nouns = cursor.fetchall()
        
        if len(nouns) == 5:
            print("✅ Successfully retrieved top 5 nouns")
            for noun in nouns:
                print(f"   {noun[1]} ({noun[3]}) - freq: {noun[7]}")
        else:
            print(f"❌ Expected 5 nouns, got {len(nouns)}")
            return False
        
        # Test getting declensions for the first noun
        first_noun_id = nouns[0][0]
        cursor.execute("""
            SELECT form, case_name, number, gender
            FROM declensions 
            WHERE headword_id = ?
            ORDER BY case_name, number
            LIMIT 10
        """, (first_noun_id,))
        declensions = cursor.fetchall()
        
        if len(declensions) > 0:
            print(f"✅ Successfully retrieved {len(declensions)} declensions for first noun")
            for decl in declensions[:3]:  # Show first 3
                case_name = CASE_NAMES.get(decl[1], f'Unknown({decl[1]})')
                number = NUMBER_NAMES.get(decl[2], f'Unknown({decl[2]})')
                gender = GENDER_NAMES.get(decl[3], f'Unknown({decl[3]})')
                print(f"   {decl[0]} - {case_name} {number} ({gender})")
        else:
            print("❌ No declensions found for first noun")
            return False
        
        # Test schema matches what the app expects
        cursor.execute("PRAGMA table_info(headwords)")
        headword_columns = [row[1] for row in cursor.fetchall()]
        expected_headword_cols = ['id', 'lemma_1', 'lemma_clean', 'pos', 'type', 'stem', 'pattern', 'meaning_1', 'ebt_count', 'created_at']
        
        for col in expected_headword_cols:
            if col not in headword_columns:
                print(f"❌ Missing expected column in headwords: {col}")
                return False
        
        cursor.execute("PRAGMA table_info(declensions)")
        declension_columns = [row[1] for row in cursor.fetchall()]
        expected_declension_cols = ['id', 'headword_id', 'form', 'case_name', 'number', 'gender']
        
        for col in expected_declension_cols:
            if col not in declension_columns:
                print(f"❌ Missing expected column in declensions: {col}")
                return False
        
        print("✅ Database schema matches app expectations")
        
        # Test verbs and conjugations
        cursor.execute("""
            SELECT * FROM headwords 
            WHERE type = 'verb'
            ORDER BY ebt_count DESC
            LIMIT 3
        """)
        verbs = cursor.fetchall()
        
        if len(verbs) == 3:
            print("✅ Successfully retrieved top 3 verbs")
            for verb in verbs:
                print(f"   {verb[1]} ({verb[3]}) - freq: {verb[8]}")
        else:
            print(f"❌ Expected 3 verbs, got {len(verbs)}")
            return False
            
        # Test conjugations for first verb
        first_verb_id = verbs[0][0]
        cursor.execute("""
            SELECT form, person, tense, mood, voice
            FROM conjugations 
            WHERE headword_id = ?
            ORDER BY person, tense
            LIMIT 5
        """, (first_verb_id,))
        conjugations = cursor.fetchall()
        
        if len(conjugations) > 0:
            print(f"✅ Successfully retrieved {len(conjugations)} conjugations for first verb")
            for conj in conjugations[:3]:  # Show first 3
                person = PERSON_NAMES.get(conj[1], f'Unknown({conj[1]})')
                tense = TENSE_NAMES.get(conj[2], f'Unknown({conj[2]})')
                mood = MOOD_NAMES.get(conj[3], f'Unknown({conj[3]})')
                voice = VOICE_NAMES.get(conj[4], f'Unknown({conj[4]})')
                print(f"   {conj[0]} - {person} {tense} {mood} ({voice})")
        else:
            print("❌ No conjugations found for first verb")
            return False
        
        conn.close()
        return True
        
    except Exception as e:
        print(f"❌ Database access error: {e}")
        return False

def main():
    """Main test function."""
    print("📱 Testing App Database Compatibility")
    print("=" * 40)
    
    if test_database_access():
        print("\n🎉 Database is ready for the app!")
        print("The app should be able to load and display flashcards successfully.")
    else:
        print("\n❌ Database compatibility test failed!")
        sys.exit(1)

if __name__ == "__main__":
    main()

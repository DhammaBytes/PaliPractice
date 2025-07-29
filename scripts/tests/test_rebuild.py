#!/usr/bin/env python3
"""
Test the complete rebuild pipeline from existing DPD database.
This assumes the DPD database is already built in dpd-db/dpd.db
"""

import subprocess
import sys
from pathlib import Path
import os

def check_dependencies():
    """Check if required dependencies are available."""
    try:
        import sqlalchemy
        import pandas
        import rich
        print("✅ Dependencies available")
        return True
    except ImportError as e:
        print(f"❌ Missing dependency: {e}")
        print("Please install dependencies first:")
        print("  python3 -m venv .venv")
        print("  source .venv/bin/activate")
        print("  pip install -r scripts/requirements.txt")
        return False

def check_dpd_database():
    """Check if DPD database exists."""
    dpd_db_path = Path("../dpd-db/dpd.db")
    if not dpd_db_path.exists():
        print("❌ DPD database not found at ../dpd-db/dpd.db")
        print("Please build it first by following the setup instructions in the README")
        return False
    print("✅ DPD database found")
    return True

def remove_existing_database():
    """Remove existing training database if it exists."""
    db_path = Path("../PaliPractice/PaliPractice/Data/training.db")
    if db_path.exists():
        print("🗑️  Removing existing training database...")
        db_path.unlink()
        print("✅ Existing database removed")

def test_extraction():
    """Test the extraction script."""
    print("\n🔄 Testing extraction script...")
    
    # Change to scripts directory
    original_cwd = os.getcwd()
    scripts_dir = Path(__file__).parent
    os.chdir(scripts_dir)
    
    try:
        result = subprocess.run([
            sys.executable, "extract_inflections.py"
        ], capture_output=True, text=True, timeout=300)  # 5 minute timeout
        
        if result.returncode == 0:
            print("✅ Extraction completed successfully")
            print("Output:", result.stdout.split('\n')[-10:])  # Last 10 lines
            return True
        else:
            print("❌ Extraction failed")
            print("Error:", result.stderr)
            return False
            
    except subprocess.TimeoutExpired:
        print("❌ Extraction timed out (took longer than 5 minutes)")
        return False
    except Exception as e:
        print(f"❌ Error running extraction: {e}")
        return False
    finally:
        os.chdir(original_cwd)

def verify_database():
    """Verify the generated database."""
    db_path = Path("../PaliPractice/PaliPractice/Data/training.db")
    
    if not db_path.exists():
        print("❌ Training database was not created")
        return False
    
    print("✅ Training database created")
    
    # Check database contents
    import sqlite3
    try:
        conn = sqlite3.connect(str(db_path))
        cursor = conn.cursor()
        
        cursor.execute("SELECT COUNT(*) FROM headwords")
        headwords = cursor.fetchone()[0]
        
        cursor.execute("SELECT COUNT(*) FROM inflections")
        inflections = cursor.fetchone()[0]
        
        print(f"📊 Database Statistics:")
        print(f"   Headwords: {headwords}")
        print(f"   Inflections: {inflections}")
        
        conn.close()
        
        if headwords > 0 and inflections > 0:
            print("✅ Database contains data")
            return True
        else:
            print("❌ Database is empty")
            return False
            
    except Exception as e:
        print(f"❌ Error checking database: {e}")
        return False

def main():
    """Main test function."""
    print("🧪 Testing PaliPractice Database Rebuild Pipeline")
    print("=" * 60)
    
    # Check dependencies
    if not check_dependencies():
        sys.exit(1)
    
    # Check DPD database
    if not check_dpd_database():
        sys.exit(1)
    
    # Remove existing database
    remove_existing_database()
    
    # Test extraction
    if not test_extraction():
        sys.exit(1)
    
    # Verify result
    if not verify_database():
        sys.exit(1)
    
    print("\n🎉 All tests passed!")
    print("The training database has been successfully rebuilt.")
    print("Location: ../PaliPractice/PaliPractice/Data/training.db")

if __name__ == "__main__":
    main()

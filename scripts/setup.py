#!/usr/bin/env python3
"""
Setup script to install dependencies for PaliPractice scripts.
"""

import subprocess
import sys
from pathlib import Path

def install_dependencies():
    """Install required Python dependencies."""
    requirements_file = Path(__file__).parent / "requirements.txt"
    
    print("📦 Installing Python dependencies...")
    result = subprocess.run([
        sys.executable, "-m", "pip", "install", "-r", str(requirements_file)
    ], capture_output=True, text=True)
    
    if result.returncode == 0:
        print("✅ Dependencies installed successfully")
        return True
    else:
        print("❌ Failed to install dependencies")
        print(f"Error: {result.stderr}")
        return False

def main():
    print("🔧 Setting up PaliPractice build environment")
    print("=" * 50)
    
    if install_dependencies():
        print("\n🎉 Setup complete!")
        print("You can now run: python3 build_training_db.py")
    else:
        print("\n❌ Setup failed!")
        sys.exit(1)

if __name__ == "__main__":
    main()
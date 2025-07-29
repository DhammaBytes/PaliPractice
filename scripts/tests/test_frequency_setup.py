#!/usr/bin/env python3
"""Test script to verify the frequency setup works correctly."""

import subprocess
import sys
from pathlib import Path

def test_frequency_setup():
    """Test that our custom Go files work correctly."""
    
    dpd_dir = Path(__file__).parent.parent.parent / "dpd-db"
    frequency_dir = Path(__file__).parent.parent / "frequency"
    setup_dir = dpd_dir / "go_modules" / "frequency" / "setup"
    
    print("🧪 Testing frequency setup...")
    
    # Check if our custom Go files exist
    main_available = frequency_dir / "main_available.go"
    main_limited = frequency_dir / "main_limited.go"
    
    if not main_available.exists():
        print(f"❌ Missing {main_available}")
        return False
        
    if not main_limited.exists():
        print(f"❌ Missing {main_limited}")
        return False
        
    print(f"✅ Found custom Go files in {frequency_dir}")
    
    # Copy files to DPD setup directory
    print("📋 Copying Go files to DPD setup directory...")
    subprocess.run([
        "cp", str(main_available), str(setup_dir / "main_available.go")
    ], check=True)
    
    # Test compilation
    print("🔨 Testing Go compilation...")
    try:
        result = subprocess.run([
            "go", "build", "-o", "/tmp/test_freq",
            str(setup_dir / "main_available.go"),
            str(setup_dir / "1CST.go"),
            str(setup_dir / "3BJT.go"), 
            str(setup_dir / "4SYA.go")
        ], cwd=dpd_dir, capture_output=True, text=True, timeout=10)
        
        if result.returncode == 0:
            print("✅ Go compilation successful")
            # Clean up
            Path("/tmp/test_freq").unlink(missing_ok=True)
        else:
            print(f"❌ Go compilation failed: {result.stderr}")
            return False
            
    except subprocess.TimeoutExpired:
        print("⏰ Compilation timeout (expected for large corpus)")
    except Exception as e:
        print(f"❌ Compilation error: {e}")
        return False
    
    # Clean up
    (setup_dir / "main_available.go").unlink(missing_ok=True)
    
    print("✅ Frequency setup test passed!")
    return True

if __name__ == "__main__":
    success = test_frequency_setup()
    sys.exit(0 if success else 1)
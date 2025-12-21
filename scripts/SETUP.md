# PaliPractice Setup and Database Scripts

This directory contains all setup instructions and scripts to build the PaliPractice app from scratch, including extracting structured noun declension and verb conjugation training data from the Digital Pāḷi Dictionary.

**Last verified**: December 2025

## Directory Structure

```
scripts/
├── extract_nouns_and_verbs.py   # Main extraction script
├── validate_db.py                # Database validation
├── setup.py                      # Python setup utilities
├── requirements.txt              # Python dependencies
├── frequency/                    # Custom Go files for corpus processing
│   ├── main_available.go        # Processes CST, BJT, SYA frequencies
├── tests/                        # Test scripts
│   ├── test_app_database.py     # Test database compatibility
│   ├── test_frequency_setup.py  # Test frequency Go files setup
│   └── test_rebuild.py           # Test rebuild process
```

## Main Scripts

### `extract_nouns_and_verbs.py`
The primary extraction script that:
- Extracts 3000 most frequent nouns and 2000 most frequent verbs
- Creates `corpus_declensions` table tracking which forms appear in Tipitaka
- Creates `corpus_conjugations` table tracking which forms appear in Tipitaka
- Uses EBT frequency counts for word selection
- Outputs to `../PaliPractice/PaliPractice/Data/training.db` for the app's consumption

### `validate_db.py`
Validates the generated database:
- Checks table structure
- Verifies data integrity
- Provides statistics on extracted data

---

## DPD Database Rebuild (Complete Instructions)

### Prerequisites
- **Python 3.12** (NOT 3.14 - pyarrow doesn't have wheels yet)
- **Go 1.22.2+** (install via `brew install go` on Mac)
- **UV** package manager (install via `curl -LsSf https://astral.sh/uv/install.sh | sh`)
- **Node.js** (install via `brew install node` on Mac)
- **.NET 10.0+** for building the app
- At least **20GB RAM** for DPD database generation

### Step 1: Update DPD Submodule
```bash
cd /Users/ivm/Sources/PaliPractice

# Pull latest dpd-db changes
cd dpd-db
git pull origin main
cd ..

# Commit the submodule update
git add dpd-db
git commit -m "Update dpd-db submodule"
```

### Step 2: Fix Submodules (if corrupted)
If `git submodule update` fails with "Unable to find current revision":
```bash
cd dpd-db

# Remove and reinitialize corrupted submodules
rm -rf resources/sc-data
git submodule update --init resources/sc-data

rm -rf resources/tpr_downloads
git submodule update --init resources/tpr_downloads

# Then update all submodules
git submodule update --init --recursive
```

### Step 3: Install Python Dependencies
```bash
cd dpd-db

# IMPORTANT: Force Python 3.12 (3.14 doesn't work with pyarrow)
uv sync --python 3.12
```

If you get timeout errors, increase the timeout:
```bash
UV_HTTP_TIMEOUT=120 uv sync --python 3.12
```

### Step 4: Run Initial Setup (generates frequency files)
**This must be run before `initial_build_db.py`!**
```bash
cd dpd-db
uv run python scripts/bash/initial_setup_run_once.py
```

This generates:
- `shared_data/frequency/cst_wordlist.json`
- `shared_data/frequency/sc_wordlist.json`
- `shared_data/frequency/bjt_wordlist.json`
- `shared_data/frequency/sya_wordlist.json`

### Step 5: Build DPD Database
```bash
cd dpd-db
uv run python scripts/bash/initial_build_db.py
```

This takes **~1 hour** and creates `dpd.db` (~450MB).

### Step 6: Extract Training Data
```bash
cd /Users/ivm/Sources/PaliPractice
source .venv/bin/activate
cd scripts
python3 extract_nouns_and_verbs.py
python3 validate_db.py
```

### Step 7: Build and Run the App
```bash
cd /Users/ivm/Sources/PaliPractice/PaliPractice/PaliPractice
dotnet build --framework net10.0-desktop
dotnet run --framework net10.0-desktop
```

---

## Quick Reference Commands

### Full Rebuild (after dpd-db update)
```bash
cd /Users/ivm/Sources/PaliPractice/dpd-db
git submodule update --init --recursive
UV_HTTP_TIMEOUT=120 uv sync --python 3.12
uv run python scripts/bash/initial_setup_run_once.py
uv run python scripts/bash/initial_build_db.py

cd /Users/ivm/Sources/PaliPractice
source .venv/bin/activate
cd scripts && python3 extract_nouns_and_verbs.py
```

### Just Re-extract Training Data (no dpd-db rebuild)
```bash
cd /Users/ivm/Sources/PaliPractice
source .venv/bin/activate
cd scripts && python3 extract_nouns_and_verbs.py
```

---

## Database Schema

### nouns
- `id`: Primary key (original DPD headword ID)
- `ebt_count`: Frequency in Early Buddhist Texts (sorted by this)
- `lemma`: Dictionary form (no _1 suffix)
- `lemma_clean`: Clean lemma
- `gender`: INTEGER - Gender enum (0=None, 1=Masculine, 2=Neuter, 3=Feminine)
- `stem`: Word stem for inflection
- `pattern`: Inflection pattern
- `family_root`: Root family classification (e.g., "√kar", "√gam")
- `meaning`: Primary meaning (no _1 suffix)
- `source_1`, `sutta_1`, `example_1`: Primary usage example
- `source_2`, `sutta_2`, `example_2`: Secondary usage example

### verbs
- `id`: Primary key (original DPD headword ID)
- `ebt_count`: Frequency in Early Buddhist Texts (sorted by this)
- `lemma`: Dictionary form (no _1 suffix)
- `lemma_clean`: Clean lemma
- `pos`: Part of speech / verb type
- `stem`: Word stem for inflection
- `pattern`: Inflection pattern
- `family_root`: Root family classification
- `meaning`: Primary meaning
- `source_1`, `sutta_1`, `example_1`: Primary usage example
- `source_2`, `sutta_2`, `example_2`: Secondary usage example

### corpus_declensions (for nouns)
- `noun_id`: Foreign key to nouns table
- `case_name`: INTEGER - NounCase enum (0=None, 1=Nominative, 2=Accusative, 3=Instrumental, 4=Dative, 5=Ablative, 6=Genitive, 7=Locative, 8=Vocative)
- `number`: INTEGER - Number enum (0=None, 1=Singular, 2=Plural)
- `gender`: INTEGER - Gender enum (0=None, 1=Masculine, 2=Neuter, 3=Feminine)
- `ending_index`: INTEGER - For multiple endings (0, 1, 2...)

### corpus_conjugations (for verbs)
- `verb_id`: Foreign key to verbs table
- `person`: INTEGER - Person enum (0=None, 1=First, 2=Second, 3=Third)
- `tense`: INTEGER - Tense enum (0=None, 1=Present, 2=Imperative, 3=Optative, 4=Future, 5=Aorist)
- `voice`: INTEGER - Voice enum (0=None, 1=Active, 2=Reflexive, 3=Passive, 4=Causative)
- `ending_index`: INTEGER - For multiple endings (0, 1, 2...)

**Note**: All grammatical attributes are stored as INTEGER values that map to C# enums defined in `PaliPractice/Models/Enums.cs`.

---

## Troubleshooting

### "Unable to find current revision in submodule path"
Remove the corrupted submodule directory and reinitialize:
```bash
rm -rf resources/<submodule-name>
git submodule update --init resources/<submodule-name>
```

### "Failed to build pyarrow" / "cmake not found"
You're using Python 3.14 which doesn't have pre-built wheels. Force Python 3.12:
```bash
uv sync --python 3.12
```

### "sc_wordlist.json: no such file or directory"
You need to run `initial_setup_run_once.py` before `initial_build_db.py`:
```bash
uv run python scripts/bash/initial_setup_run_once.py
```

### "all_tipitaka_words: no such file"
The pickle file was removed in late 2025. Update `extract_nouns_and_verbs.py` to use:
```python
from tools.all_tipitaka_words import make_all_tipitaka_word_set
all_words = make_all_tipitaka_word_set()
```

### Network timeouts during uv sync
Increase the HTTP timeout:
```bash
UV_HTTP_TIMEOUT=120 uv sync --python 3.12
```

### If Go frequency generation fails
The custom Go files (located in `scripts/frequency/`) handle missing corpus data:
- `main_available.go`: Processes CST, BJT, and SYA
- `main_limited.go`: Processes only CST and SYA if BJT is unavailable

### If corpus data is missing
Ensure all submodules are initialized:
```bash
cd dpd-db/resources
git submodule update --init --recursive --force
```

### If frequency data shows all zeros
Verify corpus text files exist:
```bash
find dpd-db/resources/dpd_submodules/cst -name "*.txt" | head -3
find dpd-db/resources/syāmaraṭṭha_1927 -name "*.txt" | head -3
```

---

## Platform-Specific Builds

```bash
# iOS
dotnet build --framework net10.0-ios

# Android
dotnet build --framework net10.0-android

# Desktop (Windows/Mac/Linux)
dotnet build --framework net10.0-desktop
```

---

## Legacy Setup (PaliPractice Python Environment)

For the extraction scripts only (not dpd-db):
```bash
cd /Users/ivm/Sources/PaliPractice
python3 -m venv .venv
source .venv/bin/activate
pip install -r scripts/requirements.txt
```

---

## Expected Results

After successful completion, you should have:
- **DPD Database**: `dpd-db/dpd.db` (~450MB) with 82,922+ words
- **Frequency Data**: 53,041+ words with EBT frequency counts > 0
- **Training Database**: `PaliPractice/PaliPractice/Data/training.db` with:
  - 3,000 most frequent nouns
  - 2,000 most frequent verbs
  - Corpus-attested declension forms
  - Corpus-attested conjugation forms

## Notes

- The full DPD build can take 30-60 minutes first time
- **Go installation is required** for frequency analysis
- **Frequency generation** is essential for frequency-based word selection
- The process requires converting XML corpus files to text before frequency analysis

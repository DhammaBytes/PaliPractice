# PaliPractice Setup and Database Scripts

This directory contains all setup instructions and scripts to build the PaliPractice app from scratch, including extracting structured noun declension and verb conjugation training data from the Digital Pāḷi Dictionary.

**Last verified**: December 2025

## Directory Structure

```
scripts/
├── extract_nouns_and_verbs.py   # Main extraction script
├── validate_db.py                # Database validation
├── lemma_registry.json           # Stable lemma ID assignments (version controlled)
├── requirements.txt              # Python dependencies
├── frequency/                    # Custom Go files for corpus processing
│   └── main_available.go        # Processes CST, BJT, SYA frequencies
```

## Main Scripts

### `extract_nouns_and_verbs.py`
The primary extraction script that:
- Extracts 3000 most frequent nouns and 2000 most frequent verbs
- Creates `corpus_declensions` table tracking which forms appear in Tipitaka
- Creates `corpus_conjugations` table tracking which forms appear in Tipitaka
- Assigns stable `lemma_id` values via `lemma_registry.json`
- Uses EBT frequency counts for word selection
- Outputs to `../PaliPractice/PaliPractice/Data/training.db` for the app's consumption

### `validate_db.py`
Validates the generated database:
- Checks table structure and form_id encoding
- Verifies lemma_id ranges (nouns: 10001-69999, verbs: 70001-99999)
- Provides statistics on extracted data

### `lemma_registry.json`
Version-controlled registry that assigns stable IDs to lemmas:
- Ensures lemma IDs never change across rebuilds
- New lemmas get appended with new IDs
- Critical for user progress tracking (bookmarks, history)

---

## Word Selection Criteria

### Noun Filtering
Words are included if they meet ALL of these criteria:
- **POS types (exact match)**: `masc`, `fem`, `nt` only
- **Has pattern**: `pattern` is not null or empty
- **Has stem**: `stem` is not null or `-`
- **Has frequency**: `ebt_count > 0`
- **Has meaning**: `meaning_1` is not null or empty
- **Has example**: `sutta_1` is not null or empty
- **Has inflection template**: DPD inflection data exists

Words are **excluded** if meaning contains:
- `(gram)` - grammatical terms
- `(abhi)` - Abhidhamma technical terms
- `(comm)` - commentary-only terms
- `in reference to` - contextual references
- `name of` - proper names
- `family name` - clan/family names

### Verb Filtering
Words are included if they meet ALL of these criteria:
- **POS types (exact match)**: `pr` only (present tense regular verbs)
- **Excluded grammar**: Verbs with `reflx` in grammar column are excluded
- **Future expansion**: `pp`, `prp`, `ptp`, `imperf`, `perf` may be added later
- **Has pattern**: `pattern` is not null or empty
- **Has stem**: `stem` is not null or `-`
- **Has frequency**: `ebt_count > 0`
- **Has meaning**: `meaning_1` is not null or empty
- **Has example**: `sutta_1` is not null or empty
- **Has inflection template**: DPD inflection data exists

Words are **excluded** if meaning contains: same exclusions as nouns.

### Grouping by Lemma
- Words are grouped by `lemma_clean` (lemma without numeric suffix)
- Top N lemmas are selected by highest `ebt_count` within the group
- All headword variants of selected lemmas are included

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
- `lemma_id`: Stable ID (10001-69999) from lemma_registry.json
- `ebt_count`: Frequency in Early Buddhist Texts (sorted by this)
- `lemma`: Dictionary form (e.g., "dhamma 1")
- `lemma_clean`: Clean lemma without suffix (e.g., "dhamma")
- `gender`: INTEGER - Gender enum (0=None, 1=Masculine, 2=Neuter, 3=Feminine)
- `stem`: Word stem for inflection
- `pattern`: Inflection pattern
- `family_root`: Root family classification (e.g., "√kar", "√gam")
- `meaning`: Primary meaning
- `source_1`, `sutta_1`, `example_1`: Primary usage example
- `source_2`, `sutta_2`, `example_2`: Secondary usage example

### verbs
- `id`: Primary key (original DPD headword ID)
- `lemma_id`: Stable ID (70001-99999) from lemma_registry.json
- `ebt_count`: Frequency in Early Buddhist Texts (sorted by this)
- `lemma`: Dictionary form
- `lemma_clean`: Clean lemma without suffix
- `has_reflexive`: INTEGER - 1 if verb has reflexive (middle voice) forms in template, 0 otherwise
- `type`: Verb type (e.g., "trans", "intrans")
- `trans`: Transitivity
- `stem`: Word stem for inflection
- `pattern`: Inflection pattern
- `family_root`: Root family classification
- `meaning`: Primary meaning
- `source_1`, `sutta_1`, `example_1`: Primary usage example
- `source_2`, `sutta_2`, `example_2`: Secondary usage example

### corpus_declensions (for nouns)
Single-column table storing corpus-attested noun forms as encoded `form_id`:
- `form_id`: INTEGER PRIMARY KEY - encodes all grammatical info

**FormId encoding**: `lemma_id(5) + case(1) + gender(1) + number(1) + ending_id(1)`
- Example: lemma_id=10789, case=3, gender=1, number=2, ending_id=2 → `107893122`

### corpus_conjugations (for verbs)
Single-column table storing corpus-attested verb forms as encoded `form_id`:
- `form_id`: INTEGER PRIMARY KEY - encodes all grammatical info

**FormId encoding**: `lemma_id(5) + tense(1) + person(1) + number(1) + reflexive(1) + ending_id(1)`
- Example: lemma_id=70683, tense=2, person=3, number=1, reflexive=1, ending_id=3 → `7068323113`

### Enum Values
All grammatical attributes map to C# enums in `PaliPractice/Models/Enums.cs`:

| Enum | Values |
|------|--------|
| Case | 0=None, 1=Nominative, 2=Accusative, 3=Instrumental, 4=Dative, 5=Ablative, 6=Genitive, 7=Locative, 8=Vocative |
| Number | 0=None, 1=Singular, 2=Plural |
| Gender | 0=None, 1=Masculine, 2=Neuter, 3=Feminine |
| Person | 0=None, 1=First, 2=Second, 3=Third |
| Tense | 0=None, 1=Present, 2=Imperative, 3=Optative, 4=Future, 5=Aorist |
| Reflexive | 0=Active (non-reflexive), 1=Reflexive (middle voice) |

**EndingId**: 1-based (1, 2, 3...). Value 0 is reserved for "combination reference" (the declension/conjugation group itself, not a specific form).

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
The extraction script now loads corpus words directly from JSON wordlists in `dpd-db/shared_data/frequency/`:
- `cst_wordlist.json`, `bjt_wordlist.json`, `sya_wordlist.json`, `sc_wordlist.json`

These are generated by `initial_setup_run_once.py`. If missing, re-run that script.

### Network timeouts during uv sync
Increase the HTTP timeout:
```bash
UV_HTTP_TIMEOUT=120 uv sync --python 3.12
```

### If Go frequency generation fails
The custom Go file `scripts/frequency/main_available.go` processes CST, BJT, and SYA corpora.
Ensure Go is installed (`brew install go` on Mac) and corpus submodules are initialized.

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
- **Lemma Registry**: `scripts/lemma_registry.json` with stable IDs for all lemmas
- **Training Database**: `PaliPractice/PaliPractice/Data/training.db` with:
  - 3,000 most frequent noun lemmas (with stable lemma_id 10001-69999)
  - 2,000 most frequent verb lemmas (with stable lemma_id 70001-99999)
  - Corpus-attested declension forms (encoded as form_id)
  - Corpus-attested conjugation forms (encoded as form_id)

## Notes

- The full DPD build can take 30-60 minutes first time
- **Go installation is required** for frequency analysis
- **Frequency generation** is essential for frequency-based word selection
- The process requires converting XML corpus files to text before frequency analysis

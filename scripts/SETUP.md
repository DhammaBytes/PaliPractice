# PaliPractice Setup and Database Scripts

This directory contains all setup instructions and scripts to build the PaliPractice app from scratch, including extracting structured noun declension and verb conjugation training data from the Digital Pāḷi Dictionary.

## Directory Structure

```
scripts/
├── extract_nouns_and_verbs.py   # Main extraction script
├── validate_db.py                # Database validation
├── generate_csharp_models.py     # C# model generation
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
- Extracts 1000 most frequent nouns and 1000 most frequent verbs
- Creates separate `declensions` table for noun forms
- Creates separate `conjugations` table for verb forms
- Uses EBT frequency counts for word selection
- Outputs to `../PaliPractice/PaliPractice/Data/training.db` for the app's consumption

### `validate_db.py`
Validates the generated database:
- Checks table structure
- Verifies data integrity
- Provides statistics on extracted data

### `generate_csharp_models.py`
Generates C# Entity Framework models from the database schema.

## Complete Setup Instructions

### Prerequisites
- **Python 3.8+** with pip
- **Go 1.22.2+** (install via `brew install go` on Mac)
- **UV** package manager for Python (install via `pip install uv`)
- **.NET 9.0+** for building the app
- At least **20GB RAM** for DPD database generation

### Step 1: Clone Repository with Submodules
```bash
git clone --recursive https://github.com/yourusername/PaliPractice.git
cd PaliPractice
```

If you already cloned without `--recursive`, initialize all submodules:
```bash
# Initialize the main DPD submodule
git submodule update --init dpd-db

# Initialize DPD submodules (this includes corpus data for frequency analysis)
cd dpd-db
git submodule update --init --recursive

# In case the corpus data submodules did not fetch:
cd resources
git submodule update --init --recursive --force
cd ../..
```

**Note**: The submodule initialization can take 10-20 minutes as it downloads several large corpus datasets (CST, BJT, SuttaCentral, etc.) needed for frequency analysis.

### Step 2: Set Up Python Environment
```bash
python3 -m venv .venv
source .venv/bin/activate  # On Windows: .venv\Scripts\activate
pip install -r scripts/requirements.txt
```

## Database Schema

### headwords
- `id`: Primary key
- `lemma_1`: Dictionary form
- `lemma_clean`: Clean lemma
- `pos`: Part of speech
- `type`: 'noun' or 'verb'
- `meaning_1`: Primary meaning
- `ebt_count`: Frequency in Early Buddhist Texts

### declensions (nouns only)
- `headword_id`: Foreign key
- `form`: Inflected form (TEXT)
- `case_name`: INTEGER - NounCase enum (1=Nominative, 2=Accusative, 3=Instrumental, 4=Dative, 5=Ablative, 6=Genitive, 7=Locative, 8=Vocative)
- `number`: INTEGER - Number enum (1=Singular, 2=Plural)
- `gender`: INTEGER - Gender enum (1=Masculine, 2=Neuter, 3=Feminine)

### conjugations (verbs only)
- `headword_id`: Foreign key
- `form`: Conjugated form (TEXT)
- `person`: INTEGER - Person enum (1=First, 2=Second, 3=Third)
- `tense`: INTEGER - Tense enum (1=Present, 2=Future, 3=Aorist, 4=Imperfect, 5=Perfect)
- `mood`: INTEGER - Mood enum (1=Indicative, 2=Optative, 3=Imperative, 4=Conditional)
- `voice`: INTEGER - Voice enum (1=Active, 2=Reflexive, 3=Passive, 4=Causative)

**Note**: All grammatical attributes (case, number, gender, person, tense, mood, voice) are stored as INTEGER values that map to C# enums defined in `PaliPractice/Models/Enums.cs`. This provides type safety and better performance in the .NET application.

## DPD Database Rebuild Instructions

If you need to rebuild the DPD database from scratch:

### Prerequisites
- Ensure `uv` is installed
- **Go 1.22.2+** is required for frequency analysis (install via `brew install go`)
- Have at least 20GB RAM available
- Clone the repository with submodules

### Step 1: Initial Setup (one-time only)
```bash
cd /Users/ivm/Sources/PaliPractice/dpd-db

# Initialize ALL git submodules recursively (includes corpus data)
git submodule update --init --recursive

# Run initial setup to prepare source files
uv run bash scripts/bash/initial_setup_run_once.sh
```

**Note**: The submodule initialization can take 10-20 minutes as it downloads large corpus datasets (CST, BJT, SYA, etc.)

### Step 2: Build Complete DPD Database
```bash
# Create empty bold_definitions.tsv to skip that step (optional)
echo "word	pos	source	book	chapter	example	example_english	nikaya	sutta_number	tag_number" > db/bold_definitions/bold_definitions.tsv

# Build base database from TSV files
uv run scripts/build/db_rebuild_from_tsv.py

# Generate inflections (most important step)
uv run python db/inflections/create_inflection_templates.py
uv run python db/inflections/generate_inflection_tables.py

# Optional: Run full component generation (takes longer)
# uv run bash scripts/bash/generate_components.sh
```

### Step 3: Convert XML Corpus Files to Text
```bash
# Convert CST XML files to text (required for frequency analysis)
uv run python scripts/build/cst4_xml_to_txt.py
```

### Step 4: Generate Frequency Data (Required for EBT Count)
```bash
# Copy our custom Go files to DPD setup directory
cp /Users/ivm/Sources/PaliPractice/scripts/frequency/main_available.go go_modules/frequency/setup/
cp /Users/ivm/Sources/PaliPractice/scripts/frequency/main_limited.go go_modules/frequency/setup/

# Generate frequency maps from available corpuses (CST, SYA)
# Note: Use the modified version that skips missing SuttaCentral data
go run go_modules/frequency/setup/main_available.go go_modules/frequency/setup/1CST.go go_modules/frequency/setup/3BJT.go go_modules/frequency/setup/4SYA.go

# Alternative: If the above fails, run individual corpus processing:
# go run go_modules/frequency/setup/main_limited.go go_modules/frequency/setup/1CST.go go_modules/frequency/setup/4SYA.go

# Populate EBT frequency counts in database
uv run python scripts/build/ebt_counter.py
```

### Step 5: Extract Training Data
```bash
cd scripts

# Extract structured noun and verb data
python3 extract_nouns_and_verbs.py

# Validate the extraction
python3 validate_db.py
```

### Step 6: Build and Run the App
```bash
cd PaliPractice/PaliPractice
dotnet build
dotnet run --framework net9.0-desktop
```

## Platform-Specific Builds

```bash
# iOS
dotnet build --framework net9.0-ios

# Android
dotnet build --framework net9.0-android

# Desktop (Windows/Mac/Linux)
dotnet build --framework net9.0-desktop
```

## Development Workflow

### Building from Scratch
If you need to rebuild everything:

```bash
# Clean previous builds
rm -f PaliPractice/PaliPractice/Data/training.db
rm -f dpd-db/dpd.db

# Rebuild DPD database from scratch (Steps 3-4 above)
cd dpd-db
uv run scripts/build/db_rebuild_from_tsv.py
uv run python db/inflections/create_inflection_templates.py
uv run python db/inflections/generate_inflection_tables.py
uv run python scripts/build/cst4_xml_to_txt.py
cp ../scripts/frequency/main_available.go go_modules/frequency/setup/
go run go_modules/frequency/setup/main_available.go go_modules/frequency/setup/1CST.go go_modules/frequency/setup/3BJT.go go_modules/frequency/setup/4SYA.go
uv run python scripts/build/ebt_counter.py
cd ..

# Generate new training database (Step 5)
source .venv/bin/activate
cd scripts && python3 extract_nouns_and_verbs.py && cd ..

# Build app (Step 6)
cd PaliPractice/PaliPractice && dotnet build
```

### Quick Script Usage (after initial setup)

Extract training data:
```bash
cd scripts
python3 extract_nouns_and_verbs.py
```

Validate extraction:
```bash
python3 validate_db.py
```

Run tests:
```bash
python3 tests/test_app_database.py
python3 tests/test_frequency_setup.py
```

## Expected Results

After successful completion, you should have about:
- **DPD Database**: `dpd-db/dpd.db` (~450MB) with 82,922 words
- **Frequency Data**: 53,041+ words with EBT frequency counts > 0
- **Training Database**: `PaliPractice/PaliPractice/Data/training.db` (~13MB) with:
  - 1,000 most frequent nouns (frequency range: ~37,600 to ~210)
  - 1,000 most frequent verbs (frequency range: ~18,961 to ~115)
  - 30,280 noun declensions with grammatical categorization
  - 62,509 verb conjugations with grammatical categorization

## Troubleshooting

### If Go frequency generation fails:
The custom Go files (located in `scripts/frequency/`) handle missing corpus data:
- `main_available.go`: Processes CST, BJT, and SYA
- `main_limited.go`: Processes only CST and SYA if BJT is unavailable

These files are automatically copied to the DPD setup directory during the build process.

### If corpus data is missing:
Ensure all submodules are initialized:
```bash
cd dpd-db/resources
git submodule update --init --recursive --force
```

### If frequency data shows all zeros:
Verify corpus text files exist:
```bash
find dpd-db/resources/dpd_submodules/cst -name "*.txt" | head -3
find dpd-db/resources/syāmaraṭṭha_1927 -name "*.txt" | head -3
```

## Notes
- Test scripts are now in `tests/` subdirectory
- The full DPD build can take 30-60 minutes first time
- **Go installation is required** for frequency analysis and EBT count population
- **Frequency generation** (Step 4) is essential for frequency-based word selection
- Without frequency data, extraction will fall back to alphabetical sorting
- The process requires converting XML corpus files to text before frequency analysis

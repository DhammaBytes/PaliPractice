#!/usr/bin/env python3
"""
Extract nouns and verbs from DPD database, creating a normalized structure
with separate tables for inflections and conjugations.
"""

import re
import json
import sqlite3
import os
from pathlib import Path
from typing import List, Dict, Any, Optional, Tuple, Set
import sys

# Lemma registry for stable IDs across rebuilds
REGISTRY_PATH = Path(__file__).parent / "lemma_registry.json"
REGISTRY_BACKUP_PATH = Path(__file__).parent / "lemma_registry.backup.json"
NOUN_ID_START = 10001
NOUN_ID_MAX = 69999
VERB_ID_START = 70001
VERB_ID_MAX = 99999


class RegistryError(Exception):
    """Raised when registry validation fails."""
    pass


def validate_registry(registry: Dict[str, Any]) -> None:
    """Validate registry structure and ID ranges. Raises RegistryError on failure."""
    required_keys = {"version", "next_noun_id", "next_verb_id", "nouns", "verbs"}
    if not required_keys.issubset(registry.keys()):
        raise RegistryError(f"Registry missing required keys: {required_keys - registry.keys()}")

    # Validate noun IDs are in valid range
    for lemma, lid in registry["nouns"].items():
        if not isinstance(lid, int) or lid < NOUN_ID_START or lid > NOUN_ID_MAX:
            raise RegistryError(f"Invalid noun ID {lid} for '{lemma}' (must be {NOUN_ID_START}-{NOUN_ID_MAX})")

    # Validate verb IDs are in valid range
    for lemma, lid in registry["verbs"].items():
        if not isinstance(lid, int) or lid < VERB_ID_START or lid > VERB_ID_MAX:
            raise RegistryError(f"Invalid verb ID {lid} for '{lemma}' (must be {VERB_ID_START}-{VERB_ID_MAX})")

    # Validate next_*_id is greater than all existing IDs
    if registry["nouns"]:
        max_noun_id = max(registry["nouns"].values())
        if registry["next_noun_id"] <= max_noun_id:
            raise RegistryError(f"next_noun_id ({registry['next_noun_id']}) must be > max existing ({max_noun_id})")

    if registry["verbs"]:
        max_verb_id = max(registry["verbs"].values())
        if registry["next_verb_id"] <= max_verb_id:
            raise RegistryError(f"next_verb_id ({registry['next_verb_id']}) must be > max existing ({max_verb_id})")

    # Check for ID collisions (same ID assigned to different lemmas)
    noun_ids = list(registry["nouns"].values())
    if len(noun_ids) != len(set(noun_ids)):
        raise RegistryError("Duplicate noun IDs detected!")

    verb_ids = list(registry["verbs"].values())
    if len(verb_ids) != len(set(verb_ids)):
        raise RegistryError("Duplicate verb IDs detected!")


def load_registry() -> Dict[str, Any]:
    """Load and validate lemma registry from JSON file."""
    if REGISTRY_PATH.exists():
        try:
            registry = json.loads(REGISTRY_PATH.read_text(encoding='utf-8'))
            validate_registry(registry)
            return registry
        except json.JSONDecodeError as e:
            raise RegistryError(f"Failed to parse registry JSON: {e}")
    return {
        "version": 1,
        "next_noun_id": NOUN_ID_START,
        "next_verb_id": VERB_ID_START,
        "nouns": {},
        "verbs": {}
    }


def save_registry(registry: Dict[str, Any], original_registry: Dict[str, Any]) -> None:
    """
    Save lemma registry with safety checks.
    - Validates registry before saving
    - Creates backup of existing file
    - Ensures no existing IDs were modified or removed
    - Uses atomic write (temp file + rename)
    """
    # Validate before saving
    validate_registry(registry)

    # Check that no existing IDs were modified or removed
    for lemma, original_id in original_registry["nouns"].items():
        if lemma not in registry["nouns"]:
            raise RegistryError(f"Noun '{lemma}' was removed from registry!")
        if registry["nouns"][lemma] != original_id:
            raise RegistryError(f"Noun '{lemma}' ID changed from {original_id} to {registry['nouns'][lemma]}!")

    for lemma, original_id in original_registry["verbs"].items():
        if lemma not in registry["verbs"]:
            raise RegistryError(f"Verb '{lemma}' was removed from registry!")
        if registry["verbs"][lemma] != original_id:
            raise RegistryError(f"Verb '{lemma}' ID changed from {original_id} to {registry['verbs'][lemma]}!")

    # Create backup of existing file
    if REGISTRY_PATH.exists():
        import shutil
        shutil.copy2(REGISTRY_PATH, REGISTRY_BACKUP_PATH)
        print(f"Created registry backup: {REGISTRY_BACKUP_PATH}")

    # Atomic write: write to temp file, then rename
    temp_path = REGISTRY_PATH.with_suffix('.tmp')
    temp_path.write_text(json.dumps(registry, indent=2, ensure_ascii=False), encoding='utf-8')
    temp_path.rename(REGISTRY_PATH)


def get_noun_lemma_id(registry: Dict[str, Any], lemma_clean: str) -> int:
    """Get or assign stable lemma_id for a noun's lemma_clean. Never modifies existing IDs."""
    if lemma_clean in registry["nouns"]:
        return registry["nouns"][lemma_clean]

    # Assign new ID
    new_id = registry["next_noun_id"]
    if new_id > NOUN_ID_MAX:
        raise RegistryError(f"Noun ID overflow! Max is {NOUN_ID_MAX}, tried to assign {new_id}")

    registry["nouns"][lemma_clean] = new_id
    registry["next_noun_id"] = new_id + 1
    return new_id


def get_verb_lemma_id(registry: Dict[str, Any], lemma_clean: str) -> int:
    """Get or assign stable lemma_id for a verb's lemma_clean. Never modifies existing IDs."""
    if lemma_clean in registry["verbs"]:
        return registry["verbs"][lemma_clean]

    # Assign new ID
    new_id = registry["next_verb_id"]
    if new_id > VERB_ID_MAX:
        raise RegistryError(f"Verb ID overflow! Max is {VERB_ID_MAX}, tried to assign {new_id}")

    registry["verbs"][lemma_clean] = new_id
    registry["next_verb_id"] = new_id + 1
    return new_id


def deep_copy_registry(registry: Dict[str, Any]) -> Dict[str, Any]:
    """Create a deep copy of registry for comparison."""
    return {
        "version": registry["version"],
        "next_noun_id": registry["next_noun_id"],
        "next_verb_id": registry["next_verb_id"],
        "nouns": dict(registry["nouns"]),
        "verbs": dict(registry["verbs"])
    }


def compute_declension_form_id(lemma_id: int, case: int, gender: int, number: int, ending_index: int) -> int:
    """
    Compute declension form_id matching C# Declension.ResolveId().
    Format: lemma_id(5) + case(1) + gender(1) + number(1) + ending_index(1)
    Example: 10789_3_1_2_2 → 107893122
    """
    return lemma_id * 10_000 + case * 1_000 + gender * 100 + number * 10 + ending_index


def compute_conjugation_form_id(lemma_id: int, tense: int, person: int, number: int, reflexive: int, ending_index: int) -> int:
    """
    Compute conjugation form_id matching C# Conjugation.ResolveId().
    Format: lemma_id(5) + tense(1) + person(1) + number(1) + reflexive(1) + ending_index(1)
    Example: 70683_2_3_1_0_3 → 7068323103
    """
    return lemma_id * 100_000 + tense * 10_000 + person * 1_000 + number * 100 + reflexive * 10 + ending_index


sys.path.append('../dpd-db')

from db.db_helpers import get_db_session
from db.models import DpdHeadword, InflectionTemplates
from tools.pos import CONJUGATIONS, DECLENSIONS


def populate_all_lemmas_to_registry():
    """
    One-time operation: Populate registry with ALL lemmas from DPD.

    This ensures all lemma IDs are assigned upfront, ordered by ebt_count.
    After this, the registry should only ever have NEW lemmas appended.

    Run this once, then commit lemma_registry.json to version control.
    """
    print("=" * 60)
    print("POPULATING REGISTRY WITH ALL DPD LEMMAS")
    print("=" * 60)

    # Check if registry already has data
    if REGISTRY_PATH.exists():
        existing = json.loads(REGISTRY_PATH.read_text(encoding='utf-8'))
        if existing.get("nouns") or existing.get("verbs"):
            print(f"WARNING: Registry already contains {len(existing.get('nouns', {}))} nouns and {len(existing.get('verbs', {}))} verbs.")
            response = input("This will ADD new lemmas only. Continue? (yes/no): ")
            if response.lower() != 'yes':
                print("Aborted.")
                return

    registry = load_registry()
    original_registry = deep_copy_registry(registry)

    db_session = get_db_session(Path("../dpd-db/dpd.db"))

    # Noun POS types
    noun_pos = ['noun', 'masc', 'fem', 'neut', 'nt', 'abstr', 'act', 'agent', 'dimin']

    # Verb POS types (excluding participles)
    verb_pos = ['vb', 'pr', 'aor', 'fut', 'opt', 'imp', 'cond',
                'caus', 'pass', 'reflx', 'deno', 'desid', 'intens', 'trans',
                'intrans', 'ditrans', 'impers', 'inf', 'abs', 'ger', 'comp vb']

    # lemma_clean is a Python @property, not a DB column
    # We need to fetch all rows and group in Python

    print("\nQuerying all nouns...")
    all_nouns = db_session.query(DpdHeadword).filter(
        DpdHeadword.pos.in_(noun_pos),
        DpdHeadword.pattern.isnot(None),
        DpdHeadword.pattern != '',
        DpdHeadword.stem.isnot(None),
        DpdHeadword.stem != '-',
    ).all()

    # Group by lemma_clean and get max ebt_count for ordering
    noun_by_lemma: Dict[str, int] = {}
    for word in all_nouns:
        lc = word.lemma_clean
        ebt = word.ebt_count or 0
        if lc not in noun_by_lemma or ebt > noun_by_lemma[lc]:
            noun_by_lemma[lc] = ebt

    # Sort by max ebt_count descending
    noun_lemmas = sorted(noun_by_lemma.items(), key=lambda x: -x[1])
    print(f"Found {len(noun_lemmas)} unique noun lemmas")

    print("Querying all verbs...")
    all_verbs = db_session.query(DpdHeadword).filter(
        DpdHeadword.pos.in_(verb_pos),
        DpdHeadword.pattern.isnot(None),
        DpdHeadword.pattern != '',
        DpdHeadword.stem.isnot(None),
        DpdHeadword.stem != '-',
    ).all()

    # Group by lemma_clean and get max ebt_count for ordering
    verb_by_lemma: Dict[str, int] = {}
    for word in all_verbs:
        lc = word.lemma_clean
        ebt = word.ebt_count or 0
        if lc not in verb_by_lemma or ebt > verb_by_lemma[lc]:
            verb_by_lemma[lc] = ebt

    # Sort by max ebt_count descending
    verb_lemmas = sorted(verb_by_lemma.items(), key=lambda x: -x[1])
    print(f"Found {len(verb_lemmas)} unique verb lemmas")

    # Assign IDs to all noun lemmas (in ebt_count order)
    new_nouns = 0
    for lemma_clean, max_ebt in noun_lemmas:
        if lemma_clean not in registry["nouns"]:
            get_noun_lemma_id(registry, lemma_clean)
            new_nouns += 1

    # Assign IDs to all verb lemmas (in ebt_count order)
    new_verbs = 0
    for lemma_clean, max_ebt in verb_lemmas:
        if lemma_clean not in registry["verbs"]:
            get_verb_lemma_id(registry, lemma_clean)
            new_verbs += 1

    print(f"\nAdded {new_nouns} new noun lemmas (total: {len(registry['nouns'])})")
    print(f"Added {new_verbs} new verb lemmas (total: {len(registry['verbs'])})")

    # Save with safety checks
    save_registry(registry, original_registry)
    print(f"\nRegistry saved to {REGISTRY_PATH}")
    print("=" * 60)


# Enum mappings matching C# Enums.cs
class GrammarEnums:
    """Enum values matching the C# Models/Enums.cs definitions."""

    # NounCase enum
    CASE_NONE = 0
    CASE_NOMINATIVE = 1
    CASE_ACCUSATIVE = 2
    CASE_INSTRUMENTAL = 3
    CASE_DATIVE = 4
    CASE_ABLATIVE = 5
    CASE_GENITIVE = 6
    CASE_LOCATIVE = 7
    CASE_VOCATIVE = 8

    # Number enum
    NUMBER_NONE = 0
    NUMBER_SINGULAR = 1
    NUMBER_PLURAL = 2

    # Gender enum
    GENDER_NONE = 0
    GENDER_MASCULINE = 1
    GENDER_NEUTER = 2
    GENDER_FEMININE = 3

    # Person enum
    PERSON_NONE = 0
    PERSON_FIRST = 1
    PERSON_SECOND = 2
    PERSON_THIRD = 3

    # Reflexive (boolean as int)
    REFLEXIVE_NO = 0
    REFLEXIVE_YES = 1

    # Tense enum (includes traditional moods: imperative, optative)
    TENSE_NONE = 0
    TENSE_PRESENT = 1
    TENSE_IMPERATIVE = 2
    TENSE_OPTATIVE = 3
    TENSE_FUTURE = 4
    TENSE_AORIST = 5

    # String to enum mappings
    CASE_MAP = {
        'nominative': CASE_NOMINATIVE,
        'accusative': CASE_ACCUSATIVE,
        'instrumental': CASE_INSTRUMENTAL,
        'dative': CASE_DATIVE,
        'ablative': CASE_ABLATIVE,
        'genitive': CASE_GENITIVE,
        'locative': CASE_LOCATIVE,
        'vocative': CASE_VOCATIVE
    }

    NUMBER_MAP = {
        'singular': NUMBER_SINGULAR,
        'plural': NUMBER_PLURAL
    }

    GENDER_MAP = {
        'masculine': GENDER_MASCULINE,
        'neuter': GENDER_NEUTER,
        'feminine': GENDER_FEMININE
    }

    PERSON_MAP = {
        'first': PERSON_FIRST,
        'second': PERSON_SECOND,
        'third': PERSON_THIRD
    }

    TENSE_MAP = {
        'present': TENSE_PRESENT,
        'imperative': TENSE_IMPERATIVE,
        'optative': TENSE_OPTATIVE,
        'future': TENSE_FUTURE,
        'aorist': TENSE_AORIST
    }


def clean_stem(stem: Optional[str]) -> str:
    """Remove DPD marker characters from stem for consistent form generation.

    DPD uses markers like ! and * in stems, but these shouldn't appear in
    actual inflected forms. We clean them here so both Python (for corpus matching)
    and C# (for form display) use the same cleaned stem.
    """
    if not stem:
        return ""
    return re.sub(r"[!*]", "", stem)


class NounVerbExtractor:
    """Extract nouns and verbs with grammatical categorization."""

    def __init__(self, output_db_path: str = "../PaliPractice/PaliPractice/Data/training.db",
                 noun_limit: int = 3000, verb_limit: int = 2000):
        self.output_db_path = output_db_path
        self.noun_limit = noun_limit
        self.verb_limit = verb_limit
        self.db_session = get_db_session(Path("../dpd-db/dpd.db"))

        # Load all words found in the Pali Tipitaka corpus
        # This is used to filter out inflections that don't appear in actual texts
        print("Loading Tipitaka word corpus from JSON wordlists...")
        self.all_tipitaka_words: Set[str] = self._load_tipitaka_words()
        print(f"Loaded {len(self.all_tipitaka_words)} words from Tipitaka corpus")

    def _load_tipitaka_words(self) -> Set[str]:
        """Load all words from the Tipitaka corpus JSON wordlists."""
        freq_dir = Path("../dpd-db/shared_data/frequency")
        all_words: Set[str] = set()

        wordlist_files = [
            "cst_wordlist.json",
            "bjt_wordlist.json",
            "sya_wordlist.json",
            "sc_wordlist.json",
        ]

        for filename in wordlist_files:
            filepath = freq_dir / filename
            if filepath.exists():
                with open(filepath) as f:
                    words = json.load(f)
                    all_words.update(words)
                    print(f"  Loaded {len(words)} words from {filename}")
            else:
                print(f"  Warning: {filename} not found, skipping")

        return all_words
        
    def extract_word_variant(self, lemma_1: str, lemma_clean: str) -> str:
        """Extract the variant identifier from DPD lemma_1.

        Examples:
            lemma_1="dhamma", lemma_clean="dhamma" -> ""
            lemma_1="dhamma 1", lemma_clean="dhamma" -> "1"
            lemma_1="aññāti 1.1", lemma_clean="aññāti" -> "1.1"
        """
        if lemma_1 == lemma_clean:
            return ""
        # The variant is everything after the lemma_clean + space
        prefix = lemma_clean + " "
        if lemma_1.startswith(prefix):
            return lemma_1[len(prefix):]
        # Fallback: try splitting on last space
        parts = lemma_1.rsplit(" ", 1)
        return parts[1] if len(parts) > 1 else ""

    def create_schema(self):
        """Create a normalized database schema for nouns and verbs."""
        # Delete old database if it exists
        if os.path.exists(self.output_db_path):
            os.remove(self.output_db_path)
            print(f"Deleted old database: {self.output_db_path}")

        conn = sqlite3.connect(self.output_db_path)
        cursor = conn.cursor()

        # Nouns table - column order: id, ebt_count, lemma_id, lemma, word, ...
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS nouns (
                id INTEGER PRIMARY KEY,
                ebt_count INTEGER DEFAULT 0,
                lemma_id INTEGER NOT NULL,
                lemma TEXT NOT NULL,
                word TEXT NOT NULL DEFAULT '',
                gender INTEGER NOT NULL DEFAULT 0,
                stem TEXT,
                pattern TEXT,
                derived_from TEXT DEFAULT '',
                family_root TEXT DEFAULT '',
                meaning TEXT,
                plus_case TEXT DEFAULT '',
                source_1 TEXT DEFAULT '',
                sutta_1 TEXT DEFAULT '',
                example_1 TEXT DEFAULT '',
                source_2 TEXT DEFAULT '',
                sutta_2 TEXT DEFAULT '',
                example_2 TEXT DEFAULT ''
            )
        """)

        # Verbs table - column order: id, ebt_count, lemma_id, lemma, word, ...
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS verbs (
                id INTEGER PRIMARY KEY,
                ebt_count INTEGER DEFAULT 0,
                lemma_id INTEGER NOT NULL,
                lemma TEXT NOT NULL,
                word TEXT NOT NULL DEFAULT '',
                has_reflexive INTEGER NOT NULL DEFAULT 0,
                type TEXT DEFAULT '',
                trans TEXT DEFAULT '',
                stem TEXT,
                pattern TEXT,
                derived_from TEXT DEFAULT '',
                family_root TEXT DEFAULT '',
                meaning TEXT,
                plus_case TEXT DEFAULT '',
                source_1 TEXT DEFAULT '',
                sutta_1 TEXT DEFAULT '',
                example_1 TEXT DEFAULT '',
                source_2 TEXT DEFAULT '',
                sutta_2 TEXT DEFAULT '',
                example_2 TEXT DEFAULT ''
            )
        """)
        
        # Corpus attestation for noun declensions (only stores corpus-attested forms)
        # form_id encodes: lemma_id(5) + case(1) + gender(1) + number(1) + ending_index(1)
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS corpus_declensions (
                form_id INTEGER PRIMARY KEY
            )
        """)

        # Corpus attestation for verb conjugations (only stores corpus-attested forms)
        # form_id encodes: lemma_id(5) + tense(1) + person(1) + number(1) + voice(1) + ending_index(1)
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS corpus_conjugations (
                form_id INTEGER PRIMARY KEY
            )
        """)
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_nouns_gender ON nouns(gender)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_nouns_lemma_id ON nouns(lemma_id)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_verbs_lemma_id ON verbs(lemma_id)")
        
        conn.commit()
        conn.close()
        print(f"Created database schema at {self.output_db_path}")
    
    def get_noun_pos_list(self) -> List[str]:
        """Get list of noun POS categories (exact matches only)."""
        return ['masc', 'fem', 'nt']
    
    def get_verb_pos_list(self) -> List[str]:
        """Get list of verb POS categories (exact matches only).

        Only 'pr' (present tense regular verbs) for now.
        Future work: add irregular tense forms (aor, fut, opt, imp, cond) as separate trainer.
        Future work: add ger, abs forms.
        Future work: pp, prp, ptp, imperf, perf.
        """
        return ['pr']
    
    def get_training_nouns(self) -> List[DpdHeadword]:
        """Get most frequent nouns suitable for training, limited by unique lemma_clean count."""
        noun_pos = self.get_noun_pos_list()

        # lemma_clean is a Python @property, so we fetch all and filter in Python
        all_words = self.db_session.query(DpdHeadword).filter(
            DpdHeadword.pos.in_(noun_pos),
            DpdHeadword.pattern.isnot(None),
            DpdHeadword.pattern != '',
            DpdHeadword.stem.isnot(None),
            DpdHeadword.stem != '-',
            DpdHeadword.ebt_count > 0,
            DpdHeadword.meaning_1.isnot(None),
            DpdHeadword.meaning_1 != '',
            DpdHeadword.sutta_1.isnot(None),
            DpdHeadword.sutta_1 != '',
            ~DpdHeadword.meaning_1.contains('(gram)'),
            ~DpdHeadword.meaning_1.contains('(abhi)'),
            ~DpdHeadword.meaning_1.contains('(comm)'),
            ~DpdHeadword.meaning_1.contains('in reference to'),
            ~DpdHeadword.meaning_1.contains('name of'),
            ~DpdHeadword.meaning_1.contains('family name')
        ).all()

        # Filter to words with inflection templates
        words_with_templates = [w for w in all_words if w.it is not None]

        # Group by lemma_clean and get max ebt_count for ordering
        lemma_max_ebt: Dict[str, int] = {}
        lemma_words: Dict[str, List[DpdHeadword]] = {}
        for word in words_with_templates:
            lc = word.lemma_clean
            ebt = word.ebt_count or 0
            if lc not in lemma_max_ebt or ebt > lemma_max_ebt[lc]:
                lemma_max_ebt[lc] = ebt
            if lc not in lemma_words:
                lemma_words[lc] = []
            lemma_words[lc].append(word)

        # Get top N lemmas by max ebt_count
        top_lemmas = sorted(lemma_max_ebt.keys(), key=lambda lc: -lemma_max_ebt[lc])[:self.noun_limit]
        top_lemmas_set = set(top_lemmas)
        print(f"Selected {len(top_lemmas)} unique noun lemmas")

        # Collect all words from selected lemmas, ordered by ebt_count
        result = []
        for lc in top_lemmas:
            result.extend(lemma_words[lc])
        result.sort(key=lambda w: -(w.ebt_count or 0))

        # Report lemmas with frequency variance across variants
        print(f"\nNoun lemmas with frequency variance across senses:")
        variance_count = 0
        for lc in top_lemmas:
            words = lemma_words[lc]
            if len(words) > 1:
                freqs = [w.ebt_count or 0 for w in words]
                if min(freqs) != max(freqs):
                    variance_count += 1
                    if variance_count <= 10:  # Show first 10
                        variants = ", ".join(f"{w.lemma_1}={w.ebt_count}" for w in sorted(words, key=lambda x: x.lemma_1))
                        print(f"  {lc}: [{variants}]")
        print(f"  ... {variance_count} lemmas total with frequency variance")

        print(f"\nFound {len(result)} noun rows ({len(top_lemmas)} unique lemmas) with inflection templates")
        if top_lemmas:
            # Report frequency range based on lemma group's ranking frequency (max among variants)
            first_lemma = top_lemmas[0]
            last_lemma = top_lemmas[-1]
            print(f"Lemma ranking frequency range: {lemma_max_ebt[first_lemma]} (highest: {first_lemma}) to {lemma_max_ebt[last_lemma]} (lowest: {last_lemma})")
        return result
    
    def get_training_verbs(self) -> List[DpdHeadword]:
        """Get most frequent verbs suitable for training, limited by unique lemma_clean count."""
        verb_pos = self.get_verb_pos_list()

        # lemma_clean is a Python @property, so we fetch all and filter in Python
        all_words = self.db_session.query(DpdHeadword).filter(
            DpdHeadword.pos.in_(verb_pos),
            DpdHeadword.pattern.isnot(None),
            DpdHeadword.pattern != '',
            DpdHeadword.stem.isnot(None),
            DpdHeadword.stem != '-',
            DpdHeadword.ebt_count > 0,
            DpdHeadword.meaning_1.isnot(None),
            DpdHeadword.meaning_1 != '',
            DpdHeadword.sutta_1.isnot(None),
            DpdHeadword.sutta_1 != '',
            ~DpdHeadword.meaning_1.contains('(gram)'),
            ~DpdHeadword.meaning_1.contains('(abhi)'),
            ~DpdHeadword.meaning_1.contains('(comm)'),
            ~DpdHeadword.meaning_1.contains('in reference to'),
            ~DpdHeadword.meaning_1.contains('name of'),
            ~DpdHeadword.meaning_1.contains('names of'),
            ~DpdHeadword.meaning_1.contains('family name'),
            # Exclude verbs with reflexive-only forms (these are special cases)
            ~DpdHeadword.grammar.contains('reflx')
        ).all()

        # Filter to words with inflection templates
        words_with_templates = [w for w in all_words if w.it is not None]

        # Group by lemma_clean and get max ebt_count for ordering
        lemma_max_ebt: Dict[str, int] = {}
        lemma_words: Dict[str, List[DpdHeadword]] = {}
        for word in words_with_templates:
            lc = word.lemma_clean
            ebt = word.ebt_count or 0
            if lc not in lemma_max_ebt or ebt > lemma_max_ebt[lc]:
                lemma_max_ebt[lc] = ebt
            if lc not in lemma_words:
                lemma_words[lc] = []
            lemma_words[lc].append(word)

        # Get top N lemmas by max ebt_count
        top_lemmas = sorted(lemma_max_ebt.keys(), key=lambda lc: -lemma_max_ebt[lc])[:self.verb_limit]
        print(f"Selected {len(top_lemmas)} unique verb lemmas")

        # Collect all words from selected lemmas, ordered by ebt_count
        result = []
        for lc in top_lemmas:
            result.extend(lemma_words[lc])
        result.sort(key=lambda w: -(w.ebt_count or 0))

        # Report lemmas with frequency variance across variants
        print(f"\nVerb lemmas with frequency variance across senses:")
        variance_count = 0
        for lc in top_lemmas:
            words = lemma_words[lc]
            if len(words) > 1:
                freqs = [w.ebt_count or 0 for w in words]
                if min(freqs) != max(freqs):
                    variance_count += 1
                    if variance_count <= 10:  # Show first 10
                        variants = ", ".join(f"{w.lemma_1}={w.ebt_count}" for w in sorted(words, key=lambda x: x.lemma_1))
                        print(f"  {lc}: [{variants}]")
        print(f"  ... {variance_count} lemmas total with frequency variance")

        print(f"\nFound {len(result)} verb rows ({len(top_lemmas)} unique lemmas) with inflection templates")
        if top_lemmas:
            # Report frequency range based on lemma group's ranking frequency (max among variants)
            first_lemma = top_lemmas[0]
            last_lemma = top_lemmas[-1]
            print(f"Lemma ranking frequency range: {lemma_max_ebt[first_lemma]} (highest: {first_lemma}) to {lemma_max_ebt[last_lemma]} (lowest: {last_lemma})")
        return result
    
    def parse_inflection_template(self, word: DpdHeadword, word_type: str) -> Tuple[List[Dict[str, Any]], int, int]:
        """Parse inflection/conjugation template to extract individual forms with grammar info.
        Returns: (forms list, total_forms_generated, forms_not_in_corpus)
        """
        if not word.it or not word.it.data:
            return [], 0, 0

        try:
            template_data = json.loads(word.it.data)
        except json.JSONDecodeError:
            return [], 0, 0

        forms = []
        total_generated = 0
        not_in_corpus = 0
        stem = clean_stem(word.stem)
        
        # Template structure:
        # Row 0: Headers
        # Column 0: Grammar labels (case names for nouns, tense for verbs)
        # Odd columns: declension/conjugation endings
        # Even columns: grammar info
        
        for row_idx, row in enumerate(template_data[1:], 1):  # Skip header row
            if len(row) < 2:
                continue
                
            grammar_label = row[0][0] if row[0] else ""
            
            # Process columns in pairs
            col_idx = 1
            while col_idx < len(row):
                if col_idx >= len(row) or not row[col_idx]:
                    col_idx += 2
                    continue
                
                # Get endings (can be a list)
                endings = row[col_idx]
                if not isinstance(endings, list):
                    endings = [endings]
                
                # Get grammar info
                grammar_info = ""
                if col_idx + 1 < len(row) and row[col_idx + 1]:
                    grammar_data = row[col_idx + 1]
                    grammar_info = grammar_data[0] if isinstance(grammar_data, list) else grammar_data
                
                # Create form entries (track ending_index for multiple endings)
                for ending_index, ending in enumerate(endings):
                    if ending:
                        inflected_form = f"{stem}{ending}" if ending != "-" else stem
                        total_generated += 1

                        # Check if form appears in the Pali Tipitaka corpus
                        in_corpus = 1 if inflected_form in self.all_tipitaka_words else 0
                        if in_corpus == 0:
                            not_in_corpus += 1

                        # Parse grammar information based on word type
                        if word_type == 'noun':
                            parsed_grammar = self.parse_noun_grammar(grammar_info, grammar_label, word.pos)
                        else:  # verb
                            parsed_grammar = self.parse_verb_grammar(grammar_info, grammar_label, word.pos)

                        form_data = {
                            'form': inflected_form,
                            'in_corpus': in_corpus,
                            'ending_index': ending_index,
                            **parsed_grammar
                        }
                        forms.append(form_data)

                col_idx += 2

        return forms, total_generated, not_in_corpus
    
    def pos_to_gender(self, pos: str) -> int:
        """Map noun POS to Gender enum value."""
        pos_lower = pos.lower()
        if pos_lower in ['masc', 'masculine']:
            return GrammarEnums.GENDER_MASCULINE
        elif pos_lower in ['fem', 'feminine']:
            return GrammarEnums.GENDER_FEMININE
        elif pos_lower in ['nt', 'neut', 'neuter']:
            return GrammarEnums.GENDER_NEUTER
        else:
            # Default for other noun types (abstr, act, agent, dimin, etc.)
            # Try to infer from word patterns, default to None
            return GrammarEnums.GENDER_NONE

    def parse_noun_grammar(self, grammar_str: str, label: str, pos: str) -> Dict[str, int]:
        """Parse grammar string for nouns, returning enum integer values. Defaults to 0 (None) if not found."""
        result = {
            'case_name': GrammarEnums.CASE_NONE,
            'number': GrammarEnums.NUMBER_NONE,
            'gender': GrammarEnums.GENDER_NONE
        }
        # Normalize once for consistent matching
        grammar_lower = grammar_str.lower()
        parts = grammar_lower.split()

        # Gender - return enum integer
        if 'masc' in grammar_lower:
            result['gender'] = GrammarEnums.GENDER_MASCULINE
        elif 'fem' in grammar_lower:
            result['gender'] = GrammarEnums.GENDER_FEMININE
        elif 'nt' in grammar_lower:
            result['gender'] = GrammarEnums.GENDER_NEUTER

        # Number - return enum integer
        if 'sg' in parts or 'singular' in grammar_lower:
            result['number'] = GrammarEnums.NUMBER_SINGULAR
        elif 'pl' in parts or 'plural' in grammar_lower:
            result['number'] = GrammarEnums.NUMBER_PLURAL

        # Case - map abbreviation to enum integer
        case_abbr_map = {
            'nom': GrammarEnums.CASE_NOMINATIVE,
            'acc': GrammarEnums.CASE_ACCUSATIVE,
            'instr': GrammarEnums.CASE_INSTRUMENTAL,
            'dat': GrammarEnums.CASE_DATIVE,
            'abl': GrammarEnums.CASE_ABLATIVE,
            'gen': GrammarEnums.CASE_GENITIVE,
            'loc': GrammarEnums.CASE_LOCATIVE,
            'voc': GrammarEnums.CASE_VOCATIVE
        }

        # Check grammar string first
        for abbr, enum_value in case_abbr_map.items():
            if abbr in parts:
                result['case_name'] = enum_value
                break

        # Check label if not found
        if result['case_name'] == GrammarEnums.CASE_NONE and label:
            for abbr, enum_value in case_abbr_map.items():
                if abbr in label.lower():
                    result['case_name'] = enum_value
                    break

        return result
    
    def parse_verb_grammar(self, grammar_str: str, label: str, pos: str) -> Dict[str, int]:
        """Parse grammar string for verbs, returning enum integer values. Defaults to 0 (None) if not found.
        Note: Tense enum now includes traditional moods (imperative, optative)."""
        result = {
            'person': GrammarEnums.PERSON_NONE,
            'number': GrammarEnums.NUMBER_NONE,
            'tense': GrammarEnums.TENSE_NONE,
            'reflexive': GrammarEnums.REFLEXIVE_NO
        }

        # Normalize once for consistent matching
        grammar_lower = grammar_str.lower()
        parts = grammar_lower.split()
        pos_lower = pos.lower()

        # Person - return enum integer
        if '1st' in grammar_lower or 'first' in grammar_lower:
            result['person'] = GrammarEnums.PERSON_FIRST
        elif '2nd' in grammar_lower or 'second' in grammar_lower:
            result['person'] = GrammarEnums.PERSON_SECOND
        elif '3rd' in grammar_lower or 'third' in grammar_lower:
            result['person'] = GrammarEnums.PERSON_THIRD

        # Number - return enum integer
        if 'sg' in parts or 'singular' in grammar_lower:
            result['number'] = GrammarEnums.NUMBER_SINGULAR
        elif 'pl' in parts or 'plural' in grammar_lower:
            result['number'] = GrammarEnums.NUMBER_PLURAL

        # Tense - includes traditional moods (imperative, optative)
        # For now we only extract 'pr' verbs, but template may contain other tenses
        if 'opt' in grammar_lower:
            result['tense'] = GrammarEnums.TENSE_OPTATIVE
        elif 'imp' in grammar_lower:
            result['tense'] = GrammarEnums.TENSE_IMPERATIVE
        elif 'fut' in grammar_lower:
            result['tense'] = GrammarEnums.TENSE_FUTURE
        elif 'aor' in grammar_lower:
            result['tense'] = GrammarEnums.TENSE_AORIST
        elif 'pr' in grammar_lower or 'pres' in grammar_lower:
            result['tense'] = GrammarEnums.TENSE_PRESENT

        # Reflexive - detected from grammar string (template columns 5-8 have 'reflx' in grammar info)
        if 'reflx' in grammar_lower:
            result['reflexive'] = GrammarEnums.REFLEXIVE_YES

        return result
    
    def extract_and_save(self):
        """Main extraction process."""
        print("Starting noun and verb extraction...")

        # Load lemma registry for stable IDs
        registry = load_registry()
        original_registry = deep_copy_registry(registry)  # Keep copy for safety checks
        print(f"Loaded lemma registry: {len(registry['nouns'])} nouns, {len(registry['verbs'])} verbs")

        # Create schema
        self.create_schema()

        # Get words
        nouns = self.get_training_nouns()
        verbs = self.get_training_verbs()

        conn = sqlite3.connect(self.output_db_path)
        cursor = conn.cursor()
        
        # Process nouns
        total_declensions = 0
        nouns_processed = 0
        nouns_discarded: List[str] = []  # Track nouns without nom sg
        total_noun_forms_generated = 0
        total_noun_forms_filtered = 0

        print(f"\nProcessing {len(nouns)} nouns...")
        for i, word in enumerate(nouns, 1):
            if i % 100 == 0:
                print(f"Processing noun {i}/{len(nouns)}: {word.lemma_1}")

            # Insert noun
            gender = self.pos_to_gender(word.pos)
            lemma_id = get_noun_lemma_id(registry, word.lemma_clean)
            word_variant = self.extract_word_variant(word.lemma_1, word.lemma_clean)
            cursor.execute("""
                INSERT INTO nouns (
                    id, ebt_count, lemma_id, lemma, word, gender, stem, pattern, derived_from, family_root,
                    meaning, plus_case, source_1, sutta_1, example_1, source_2, sutta_2, example_2
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                word.id, word.ebt_count or 0, lemma_id, word.lemma_clean, word_variant, gender,
                clean_stem(word.stem), word.pattern, word.derived_from or '', word.family_root or '',
                word.meaning_1, word.plus_case or '',
                word.source_1 or '', word.sutta_1 or '', word.example_1 or '',
                word.source_2 or '', word.sutta_2 or '', word.example_2 or ''
            ))

            # Extract declensions
            forms, generated, filtered = self.parse_inflection_template(word, 'noun')
            total_noun_forms_generated += generated
            total_noun_forms_filtered += filtered

            if forms:
                # For nouns, check if we have a nominative singular form
                has_nom_sg = any(
                    f.get('case_name') == GrammarEnums.CASE_NOMINATIVE and f.get('number') == GrammarEnums.NUMBER_SINGULAR
                    for f in forms
                )

                if has_nom_sg:
                    nouns_processed += 1

                    # Insert corpus-attested declensions only
                    for form in forms:
                        # Only insert forms that appear in the corpus
                        if form.get('in_corpus', 0) == 1:
                            # Compute form_id: ending_index is 1-based for actual forms
                            form_id = compute_declension_form_id(
                                lemma_id=lemma_id,
                                case=form.get('case_name', GrammarEnums.CASE_NONE),
                                gender=gender,  # Use noun's gender, not form's
                                number=form.get('number', GrammarEnums.NUMBER_NONE),
                                ending_index=form.get('ending_index', 0) + 1  # Convert to 1-based
                            )
                            try:
                                cursor.execute(
                                    "INSERT INTO corpus_declensions (form_id) VALUES (?)",
                                    (form_id,)
                                )
                                total_declensions += 1
                            except sqlite3.IntegrityError:
                                # Skip duplicates
                                pass
                else:
                    nouns_discarded.append(word.lemma_1)
            else:
                nouns_discarded.append(word.lemma_1)
        
        # Process verbs
        total_conjugations = 0
        verbs_processed = 0
        total_verb_forms_generated = 0
        total_verb_forms_filtered = 0

        print(f"\nProcessing {len(verbs)} verbs...")
        for i, word in enumerate(verbs, 1):
            if i % 100 == 0:
                print(f"Processing verb {i}/{len(verbs)}: {word.lemma_1}")

            lemma_id = get_verb_lemma_id(registry, word.lemma_clean)

            # Extract conjugations first to determine has_reflexive
            forms, generated, filtered = self.parse_inflection_template(word, 'verb')
            total_verb_forms_generated += generated
            total_verb_forms_filtered += filtered

            # Check if any form has reflexive=1
            has_reflexive = 1 if any(f.get('reflexive', 0) == GrammarEnums.REFLEXIVE_YES for f in forms) else 0

            # Insert verb
            word_variant = self.extract_word_variant(word.lemma_1, word.lemma_clean)
            cursor.execute("""
                INSERT INTO verbs (
                    id, ebt_count, lemma_id, lemma, word, has_reflexive, type, trans, stem, pattern,
                    derived_from, family_root, meaning, plus_case,
                    source_1, sutta_1, example_1, source_2, sutta_2, example_2
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                word.id, word.ebt_count or 0, lemma_id, word.lemma_clean, word_variant, has_reflexive,
                word.verb or '', word.trans or '', clean_stem(word.stem), word.pattern,
                word.derived_from or '', word.family_root or '', word.meaning_1, word.plus_case or '',
                word.source_1 or '', word.sutta_1 or '', word.example_1 or '',
                word.source_2 or '', word.sutta_2 or '', word.example_2 or ''
            ))

            if forms:
                verbs_processed += 1

                # Insert corpus-attested conjugations only
                for form in forms:
                    # Only insert forms that appear in the corpus
                    if form.get('in_corpus', 0) == 1:
                        # Compute form_id: ending_index is 1-based for actual forms
                        form_id = compute_conjugation_form_id(
                            lemma_id=lemma_id,
                            tense=form.get('tense', GrammarEnums.TENSE_NONE),
                            person=form.get('person', GrammarEnums.PERSON_NONE),
                            number=form.get('number', GrammarEnums.NUMBER_NONE),
                            reflexive=form.get('reflexive', GrammarEnums.REFLEXIVE_NO),
                            ending_index=form.get('ending_index', 0) + 1  # Convert to 1-based
                        )
                        try:
                            cursor.execute(
                                "INSERT INTO corpus_conjugations (form_id) VALUES (?)",
                                (form_id,)
                            )
                            total_conjugations += 1
                        except sqlite3.IntegrityError:
                            # Skip duplicates
                            pass
        
        conn.commit()
        conn.close()

        # Save updated lemma registry (with safety checks against original)
        new_nouns = len(registry['nouns']) - len(original_registry['nouns'])
        new_verbs = len(registry['verbs']) - len(original_registry['verbs'])
        if new_nouns > 0 or new_verbs > 0:
            save_registry(registry, original_registry)
            print(f"\nSaved lemma registry: {len(registry['nouns'])} nouns (+{new_nouns}), {len(registry['verbs'])} verbs (+{new_verbs})")
        else:
            print(f"\nNo new lemmas added to registry (unchanged)")

        print(f"\n=== EXTRACTION COMPLETE ===")
        print(f"Database: {self.output_db_path}")
        print(f"Total headwords: {len(nouns) + len(verbs)}")
        print(f"Nouns processed: {nouns_processed}/{len(nouns)}")
        if nouns_discarded:
            print(f"  Discarded (no nom sg): {', '.join(nouns_discarded)}")
        print(f"Verbs processed: {verbs_processed}/{len(verbs)}")
        print(f"Total declensions: {total_declensions}")
        print(f"Total conjugations: {total_conjugations}")

        # Show corpus attestation statistics
        total_forms_generated = total_noun_forms_generated + total_verb_forms_generated
        total_forms_not_in_corpus = total_noun_forms_filtered + total_verb_forms_filtered
        total_forms_in_corpus = total_forms_generated - total_forms_not_in_corpus
        not_in_corpus_percentage = (total_forms_not_in_corpus / total_forms_generated * 100) if total_forms_generated > 0 else 0

        print(f"\n=== CORPUS ATTESTATION STATISTICS ===")
        print(f"Noun forms: {total_noun_forms_generated} total, {total_noun_forms_generated - total_noun_forms_filtered} in corpus, {total_noun_forms_filtered} theoretical ({total_noun_forms_filtered / total_noun_forms_generated * 100:.1f}%)")
        print(f"Verb forms: {total_verb_forms_generated} total, {total_verb_forms_generated - total_verb_forms_filtered} in corpus, {total_verb_forms_filtered} theoretical ({total_verb_forms_filtered / total_verb_forms_generated * 100:.1f}%)")
        print(f"Total: {total_forms_generated} forms")
        print(f"  - In Tipitaka corpus (in_corpus=1): {total_forms_in_corpus} ({100-not_in_corpus_percentage:.1f}%)")
        print(f"  - Theoretical only (in_corpus=0): {total_forms_not_in_corpus} ({not_in_corpus_percentage:.1f}%)")
        
        self.print_summary_stats()
    
    def print_summary_stats(self):
        """Print summary statistics of extracted data."""
        conn = sqlite3.connect(self.output_db_path)
        cursor = conn.cursor()

        print("\n=== SUMMARY STATISTICS ===")

        # Words by type
        cursor.execute("SELECT COUNT(*) FROM nouns")
        noun_count = cursor.fetchone()[0]
        cursor.execute("SELECT COUNT(*) FROM verbs")
        verb_count = cursor.fetchone()[0]
        print("\nWords by Type:")
        print(f"  nouns: {noun_count}")
        print(f"  verbs: {verb_count}")

        # Nouns by gender
        cursor.execute("SELECT gender, COUNT(*) FROM nouns GROUP BY gender ORDER BY COUNT(*) DESC")
        print("\nNouns by Gender:")
        gender_names = {1: 'masculine', 2: 'neuter', 3: 'feminine', 0: 'none'}
        for gender, count in cursor.fetchall():
            print(f"  {gender_names.get(gender, f'unknown({gender})')}: {count}")

        # Verbs by reflexive capability
        cursor.execute("SELECT has_reflexive, COUNT(*) FROM verbs GROUP BY has_reflexive")
        print("\nVerbs by Reflexive Capability:")
        for has_reflex, count in cursor.fetchall():
            label = "with reflexive forms" if has_reflex else "active only"
            print(f"  {label}: {count}")

        # Corpus attestation counts
        cursor.execute("SELECT COUNT(*) FROM corpus_declensions")
        decl_count = cursor.fetchone()[0]
        cursor.execute("SELECT COUNT(*) FROM corpus_conjugations")
        conj_count = cursor.fetchone()[0]
        print("\nCorpus Attestation Records:")
        print(f"  Noun forms in corpus: {decl_count}")
        print(f"  Verb forms in corpus: {conj_count}")
        print(f"  Total: {decl_count + conj_count}")

        conn.close()


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(
        description="Extract nouns and verbs from DPD database",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # First time: populate registry with ALL lemmas (run once, commit result)
  python extract_nouns_and_verbs.py --populate-registry

  # Normal extraction: create training.db with top N lemmas
  python extract_nouns_and_verbs.py --nouns 3000 --verbs 2000
        """
    )
    parser.add_argument(
        "--populate-registry",
        action="store_true",
        help="Populate lemma_registry.json with ALL lemmas from DPD (one-time operation)"
    )
    parser.add_argument(
        "--nouns",
        type=int,
        default=3000,
        help="Number of unique noun lemmas to extract (default: 3000)"
    )
    parser.add_argument(
        "--verbs",
        type=int,
        default=2000,
        help="Number of unique verb lemmas to extract (default: 2000)"
    )

    args = parser.parse_args()

    if args.populate_registry:
        populate_all_lemmas_to_registry()
    else:
        extractor = NounVerbExtractor(noun_limit=args.nouns, verb_limit=args.verbs)
        extractor.extract_and_save()

"""
Configuration constants for PaliPractice extraction.
"""

from pathlib import Path

# Lemma registry paths
REGISTRY_PATH = Path(__file__).parent.parent / "lemma_registry.json"
REGISTRY_BACKUP_PATH = Path(__file__).parent.parent / "lemma_registry.backup.json"

# Maximum lemma length (characters) - filters out very long compounds
# that would break UI layouts
MAX_LEMMA_LENGTH = 18

# Lemma ID ranges
# Nouns: 10001-69999
# Verbs: 70001-99999
NOUN_ID_START = 10001
NOUN_ID_MAX = 69999
VERB_ID_START = 70001
VERB_ID_MAX = 99999

# POS lists for filtering
NOUN_POS_LIST = ['masc', 'fem', 'nt']
VERB_POS_LIST = ['pr']

# All noun POS types (for registry population)
ALL_NOUN_POS = ['noun', 'masc', 'fem', 'neut', 'nt', 'abstr', 'act', 'agent', 'dimin']

# All verb POS types (for registry population)
ALL_VERB_POS = ['vb', 'pr', 'aor', 'fut', 'opt', 'imp', 'cond',
                'caus', 'pass', 'reflx', 'deno', 'desid', 'intens', 'trans',
                'intrans', 'ditrans', 'impers', 'inf', 'abs', 'ger', 'comp vb']

# Irregular patterns that need HTML parsing instead of template-based extraction
# These match the C# NounPattern and VerbPattern enum irregular values
IRREGULAR_NOUN_PATTERNS = {
    "rāja masc", "brahma masc", "kamma nt", "addha masc",
    "a masc east", "a masc pl", "a2 masc", "go masc", "yuva masc",
    "ī masc pl", "jantu masc", "u masc pl", "ar2 masc",
    "anta masc", "arahant masc", "bhavant masc", "santa masc",
    "parisā fem", "jāti fem", "ratti fem", "nadī fem", "pokkharaṇī fem",
    "mātar fem", "a nt east", "a nt irreg", "a nt pl"
}

IRREGULAR_VERB_PATTERNS = {
    "hoti pr", "atthi pr", "karoti pr", "brūti pr",
    "dakkhati pr", "dammi pr", "hanati pr", "kubbati pr",
    "natthi pr", "eti pr 2"
}

# Plural-only noun patterns (pattern ends with ' pl')
# These lack singular forms by definition
def is_plural_only_pattern(pattern: str) -> bool:
    """Check if pattern is a plural-only pattern (ends in ' pl')."""
    return pattern.strip().endswith(' pl')

# DPD database path (relative to scripts directory)
DPD_DB_PATH = Path(__file__).parent.parent.parent / "dpd-db" / "dpd.db"

# Output training database path
TRAINING_DB_PATH = Path(__file__).parent.parent.parent / "PaliPractice" / "PaliPractice" / "Data" / "training.db"

# Tipitaka wordlist paths for corpus attestation
TIPITAKA_FREQ_PATH = Path(__file__).parent.parent.parent / "dpd-db" / "shared_data" / "frequency"
TIPITAKA_WORDLIST_FILES = [
    "cst_wordlist.json",
    "bjt_wordlist.json",
    "sya_wordlist.json",
    "sc_wordlist.json",
]

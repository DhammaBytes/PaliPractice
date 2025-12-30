"""
Configuration constants for PaliPractice extraction.
"""

from pathlib import Path

# Lemma registry paths (in configs folder)
REGISTRY_PATH = Path(__file__).parent.parent / "configs" / "lemma_registry.json"
REGISTRY_BACKUP_PATH = Path(__file__).parent.parent / "configs" / "lemma_registry.backup.json"

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

# Truly irregular patterns - forms must be read from HTML (DPD like='irreg')
# These match the C# NounPattern enum values after _Irregular breakpoint
IRREGULAR_NOUN_PATTERNS = {
    # Irregular Masculine (9)
    "addha masc", "arahant masc", "bhavant masc", "brahma masc",
    "go masc", "jantu masc", "rāja masc", "santa masc", "yuva masc",
    # Irregular Feminine (6)
    "jāti fem", "mātar fem", "nadī fem", "parisā fem",
    "pokkharaṇī fem", "ratti fem",
    # Irregular Neuter (1)
    "kamma nt"
}

# Variant patterns - use stem+ending but with alternate ending tables
# These are grouped with their parent base pattern but have different endings
# Forms extracted via templates (like base patterns), not HTML
VARIANT_NOUN_PATTERNS = {
    # Variant Masculine → AMasc, AntMasc, ArMasc, ĪMasc, UMasc
    "a masc east", "a masc pl", "a2 masc",
    "anta masc",
    "ar2 masc",
    "ī masc pl",
    "u masc pl",
    # Variant Neuter → ANeut
    "a nt east", "a nt irreg", "a nt pl"
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
TRAINING_DB_PATH = Path(__file__).parent.parent.parent / "PaliPractice" / "PaliPractice" / "Data" / "pali.db"

# Tipitaka wordlist paths for corpus attestation
TIPITAKA_FREQ_PATH = Path(__file__).parent.parent.parent / "dpd-db" / "shared_data" / "frequency"
TIPITAKA_WORDLIST_FILES = [
    "cst_wordlist.json",
    "bjt_wordlist.json",
    "sya_wordlist.json",
    "sc_wordlist.json",
]

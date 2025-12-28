"""
Extraction modules for PaliPractice training data.

This package contains modular components for extracting nouns and verbs
from the DPD (Digital Pali Dictionary) database.
"""

from .config import (
    REGISTRY_PATH,
    REGISTRY_BACKUP_PATH,
    NOUN_ID_START,
    NOUN_ID_MAX,
    VERB_ID_START,
    VERB_ID_MAX,
    IRREGULAR_NOUN_PATTERNS,
    IRREGULAR_VERB_PATTERNS,
    NOUN_POS_LIST,
    VERB_POS_LIST,
)
from .grammar import GrammarEnums, parse_noun_grammar, parse_verb_grammar
from .forms import (
    clean_stem,
    compute_declension_form_id,
    compute_conjugation_form_id,
)
from .registry import (
    RegistryError,
    validate_registry,
    load_registry,
    save_registry,
    get_noun_lemma_id,
    get_verb_lemma_id,
    deep_copy_registry,
)
from .html_parser import (
    parse_inflections_html,
    parse_noun_title,
    parse_verb_title,
)
from .plural_dedup import PluralOnlyDeduplicator

__all__ = [
    # Config
    'REGISTRY_PATH',
    'REGISTRY_BACKUP_PATH',
    'NOUN_ID_START',
    'NOUN_ID_MAX',
    'VERB_ID_START',
    'VERB_ID_MAX',
    'IRREGULAR_NOUN_PATTERNS',
    'IRREGULAR_VERB_PATTERNS',
    'NOUN_POS_LIST',
    'VERB_POS_LIST',
    # Grammar
    'GrammarEnums',
    'parse_noun_grammar',
    'parse_verb_grammar',
    # Forms
    'clean_stem',
    'compute_declension_form_id',
    'compute_conjugation_form_id',
    # Registry
    'RegistryError',
    'validate_registry',
    'load_registry',
    'save_registry',
    'get_noun_lemma_id',
    'get_verb_lemma_id',
    'deep_copy_registry',
    # HTML Parser
    'parse_inflections_html',
    'parse_noun_title',
    'parse_verb_title',
    # Plural Deduplication
    'PluralOnlyDeduplicator',
]

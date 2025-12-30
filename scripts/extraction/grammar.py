"""
Grammar enums and parsing functions for PaliPractice extraction.

Enum values match the C# Models/Enums.cs definitions.
"""

from typing import Dict


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
    GENDER_FEMININE = 2
    GENDER_NEUTER = 3

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


def pos_to_gender(pos: str) -> int:
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
        return GrammarEnums.GENDER_NONE


def parse_noun_grammar(grammar_str: str, label: str, pos: str) -> Dict[str, int]:
    """Parse grammar string for nouns, returning enum integer values.

    Args:
        grammar_str: Grammar info string from template (e.g., "masc nom sg")
        label: Row label from template (e.g., "nominative")
        pos: Part of speech from DPD headword

    Returns:
        Dict with keys 'case_name', 'number', 'gender' as enum integers.
        Defaults to 0 (None) if not found.
    """
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


def parse_verb_grammar(grammar_str: str, label: str, pos: str) -> Dict[str, int]:
    """Parse grammar string for verbs, returning enum integer values.

    Note: Tense enum now includes traditional moods (imperative, optative).

    Args:
        grammar_str: Grammar info string from template
        label: Row label from template
        pos: Part of speech from DPD headword

    Returns:
        Dict with keys 'person', 'number', 'tense', 'reflexive' as enum integers.
        Defaults to 0 (None) if not found.
    """
    result = {
        'person': GrammarEnums.PERSON_NONE,
        'number': GrammarEnums.NUMBER_NONE,
        'tense': GrammarEnums.TENSE_NONE,
        'reflexive': GrammarEnums.REFLEXIVE_NO
    }

    # Normalize once for consistent matching
    grammar_lower = grammar_str.lower()
    parts = grammar_lower.split()

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

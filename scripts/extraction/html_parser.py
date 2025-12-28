"""
HTML parsing functions for DPD inflections_html field.

Used to extract irregular forms from the pre-rendered HTML when
template-based extraction isn't sufficient.
"""

import re
from typing import Dict, List, Tuple

from .grammar import GrammarEnums


def parse_inflections_html(html: str) -> Dict[str, List[str]]:
    """Parse DPD inflections_html to extract forms by grammatical combination.

    Returns dict mapping title (e.g., "masc nom sg") to list of full forms.
    Only non-gray forms are included (gray = not in corpus).

    Args:
        html: Raw HTML from DPD inflections_html field

    Returns:
        Dict mapping grammar title to list of inflected forms
    """
    if not html:
        return {}

    results: Dict[str, List[str]] = {}

    # Find all <td title='...'>...</td> elements
    cell_pattern = r"<td\s+title='([^']+)'[^>]*>(.*?)</td>"
    for match in re.finditer(cell_pattern, html, re.DOTALL):
        title = match.group(1)
        content = match.group(2)

        # Skip empty cells or "in comps"
        if not content.strip() or title == "in comps" or not title:
            continue

        forms = []

        # Split by <br> for multiple forms
        parts = re.split(r"<br\s*/?>", content, flags=re.IGNORECASE)

        for part in parts:
            part = part.strip()
            if not part:
                continue

            # Skip gray forms (not in corpus)
            if "<span class='gray'>" in part:
                continue

            # Extract full form: combine non-bold stem + bold ending
            # Pattern: optional_stem<b>ending</b>
            # Example: "anga<b>raja</b>" -> "angaraja"
            form_match = re.search(r"^([^<]*)<b>([^<]+)</b>", part)
            if form_match:
                stem_part = form_match.group(1)
                ending_part = form_match.group(2)
                full_form = stem_part + ending_part
                forms.append(full_form)

        if forms:
            results[title] = forms

    return results


def parse_noun_title(title: str) -> Tuple[int, int, int]:
    """Parse noun HTML title to grammar components.

    Title format: "masc nom sg", "fem acc pl", "nt gen sg", etc.

    Args:
        title: Title attribute from HTML td element

    Returns:
        Tuple of (case, gender, number) as enum integers.
    """
    parts = title.lower().split()

    # Gender
    gender = GrammarEnums.GENDER_NONE
    if 'masc' in parts:
        gender = GrammarEnums.GENDER_MASCULINE
    elif 'fem' in parts:
        gender = GrammarEnums.GENDER_FEMININE
    elif 'nt' in parts:
        gender = GrammarEnums.GENDER_NEUTER

    # Case
    case = GrammarEnums.CASE_NONE
    case_map = {
        'nom': GrammarEnums.CASE_NOMINATIVE,
        'acc': GrammarEnums.CASE_ACCUSATIVE,
        'instr': GrammarEnums.CASE_INSTRUMENTAL,
        'dat': GrammarEnums.CASE_DATIVE,
        'abl': GrammarEnums.CASE_ABLATIVE,
        'gen': GrammarEnums.CASE_GENITIVE,
        'loc': GrammarEnums.CASE_LOCATIVE,
        'voc': GrammarEnums.CASE_VOCATIVE
    }
    for abbr, val in case_map.items():
        if abbr in parts:
            case = val
            break

    # Number
    number = GrammarEnums.NUMBER_NONE
    if 'sg' in parts:
        number = GrammarEnums.NUMBER_SINGULAR
    elif 'pl' in parts:
        number = GrammarEnums.NUMBER_PLURAL

    return (case, gender, number)


def parse_verb_title(title: str) -> Tuple[int, int, int, int]:
    """Parse verb HTML title to grammar components.

    Title format: "pr 3rd sg", "imp 1st pl", "reflx opt 2nd sg", etc.

    Args:
        title: Title attribute from HTML td element

    Returns:
        Tuple of (tense, person, number, reflexive) as enum integers.
    """
    parts = title.lower().split()

    # Reflexive
    reflexive = GrammarEnums.REFLEXIVE_YES if 'reflx' in parts else GrammarEnums.REFLEXIVE_NO

    # Tense
    tense = GrammarEnums.TENSE_NONE
    tense_map = {
        'pr': GrammarEnums.TENSE_PRESENT,
        'imp': GrammarEnums.TENSE_IMPERATIVE,
        'opt': GrammarEnums.TENSE_OPTATIVE,
        'fut': GrammarEnums.TENSE_FUTURE,
        'aor': GrammarEnums.TENSE_AORIST
    }
    for abbr, val in tense_map.items():
        if abbr in parts:
            tense = val
            break

    # Person
    person = GrammarEnums.PERSON_NONE
    if '1st' in parts:
        person = GrammarEnums.PERSON_FIRST
    elif '2nd' in parts:
        person = GrammarEnums.PERSON_SECOND
    elif '3rd' in parts:
        person = GrammarEnums.PERSON_THIRD

    # Number
    number = GrammarEnums.NUMBER_NONE
    if 'sg' in parts:
        number = GrammarEnums.NUMBER_SINGULAR
    elif 'pl' in parts:
        number = GrammarEnums.NUMBER_PLURAL

    return (tense, person, number, reflexive)

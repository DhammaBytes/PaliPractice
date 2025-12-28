"""
Form ID computation and utility functions for PaliPractice extraction.
"""

import re
from typing import Optional


def clean_stem(stem: Optional[str]) -> str:
    """Remove DPD marker characters from stem for consistent form generation.

    DPD uses markers like ! and * in stems, but these shouldn't appear in
    actual inflected forms. We clean them here so both Python (for corpus matching)
    and C# (for form display) use the same cleaned stem.

    Args:
        stem: Raw stem from DPD database

    Returns:
        Cleaned stem with markers removed
    """
    if not stem:
        return ""
    return re.sub(r"[!*]", "", stem)


def compute_declension_form_id(lemma_id: int, case: int, gender: int, number: int, ending_index: int) -> int:
    """
    Compute declension form_id matching C# Declension.ResolveId().

    Format: lemma_id(5) + case(1) + gender(1) + number(1) + ending_index(1)
    Example: 10789_3_1_2_2 -> 107893122

    Args:
        lemma_id: Stable lemma ID (5 digits, 10001-69999)
        case: NounCase enum value (1-8)
        gender: Gender enum value (1-3)
        number: Number enum value (1-2)
        ending_index: 1-based index for multiple endings

    Returns:
        9-digit form_id encoding all parameters
    """
    return lemma_id * 10_000 + case * 1_000 + gender * 100 + number * 10 + ending_index


def compute_conjugation_form_id(lemma_id: int, tense: int, person: int, number: int,
                                  reflexive: int, ending_index: int) -> int:
    """
    Compute conjugation form_id matching C# Conjugation.ResolveId().

    Format: lemma_id(5) + tense(1) + person(1) + number(1) + reflexive(1) + ending_index(1)
    Example: 70683_2_3_1_0_3 -> 7068323103

    Args:
        lemma_id: Stable lemma ID (5 digits, 70001-99999)
        tense: Tense enum value (1-5)
        person: Person enum value (1-3)
        number: Number enum value (1-2)
        reflexive: 0 (active) or 1 (reflexive)
        ending_index: 1-based index for multiple endings

    Returns:
        10-digit form_id encoding all parameters
    """
    return lemma_id * 100_000 + tense * 10_000 + person * 1_000 + number * 100 + reflexive * 10 + ending_index

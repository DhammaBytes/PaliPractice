"""
Plural-only noun deduplication logic for PaliPractice extraction.

Detects redundant plural-only lemmas (e.g., "kusala" with pattern "a masc pl")
that are just the plural forms of an existing singular lemma (e.g., "kusala 3"
with pattern "a masc"). When the plural-only lemma's forms exactly match
another lemma's plural column, we skip it to avoid redundancy.

Algorithm:
1. Build an index of all singular lemmas grouped by (stem_clean, gender)
2. For each plural-only lemma (pattern ends with ' pl'):
   a. Look up lemmas with same (stem_clean, gender)
   b. For each candidate, compare plural-only forms to candidate's plural column
   c. If 100% match found, mark the plural-only lemma as redundant

True plural-only nouns (pluralia tantum) that DON'T match any singular's
plural forms are kept and marked specially in the Noun model.
"""

import json
import re
from typing import Dict, List, Set, Tuple, Optional, Any
from dataclasses import dataclass

from .config import is_plural_only_pattern
from .forms import clean_stem
from .grammar import GrammarEnums


@dataclass
class PluralMatchResult:
    """Result of checking if a plural-only lemma matches a singular's plurals."""
    is_redundant: bool
    matched_lemma: Optional[str] = None  # lemma_1 of the singular that matches
    matched_pattern: Optional[str] = None
    match_ratio: float = 0.0  # Proportion of forms that matched (0.0-1.0)
    plural_only_forms: int = 0
    matching_forms: int = 0


class PluralOnlyDeduplicator:
    """
    Detects and filters redundant plural-only noun lemmas.

    Usage:
        dedup = PluralOnlyDeduplicator(db_session)
        dedup.build_singular_index(all_nouns)

        for word in nouns:
            if is_plural_only_pattern(word.pattern):
                result = dedup.check_redundant(word)
                if result.is_redundant:
                    print(f"Skipping {word.lemma_1}: matches plural of {result.matched_lemma}")
                    continue
    """

    def __init__(self, db_session):
        """
        Initialize deduplicator with database session.

        Args:
            db_session: SQLAlchemy session for DPD database
        """
        self.db_session = db_session
        # Index: (stem_clean, gender) -> list of (lemma_1, pattern, plural_forms)
        self._singular_index: Dict[Tuple[str, str], List[Tuple[str, str, Set[str]]]] = {}
        self._redundant_count = 0
        self._true_plural_only_count = 0

    def _extract_gender_from_pattern(self, pattern: str) -> str:
        """Extract gender from pattern string."""
        pattern_lower = pattern.lower()
        if 'masc' in pattern_lower:
            return 'masc'
        elif 'fem' in pattern_lower:
            return 'fem'
        elif 'nt' in pattern_lower:
            return 'nt'
        return ''

    def _get_plural_forms_from_template(self, word) -> Set[str]:
        """Extract all plural forms from a word's inflection template.

        Args:
            word: DpdHeadword object with .it (inflection template)

        Returns:
            Set of plural forms (full inflected words)
        """
        if not word.it or not word.it.data:
            return set()

        try:
            template_data = json.loads(word.it.data)
        except json.JSONDecodeError:
            return set()

        plural_forms = set()
        stem = clean_stem(word.stem)

        # Template structure: rows with grammar info, columns for sg/pl
        # Plural forms are typically in column pairs 3-4 (indices 3, 4)
        for row in template_data[1:]:  # Skip header
            if len(row) < 4:
                continue

            # Check columns for plural forms
            for col_idx in range(1, len(row), 2):  # Odd columns have endings
                if col_idx + 1 >= len(row):
                    continue

                grammar_info = row[col_idx + 1]
                if not grammar_info:
                    continue

                grammar_str = grammar_info[0] if isinstance(grammar_info, list) else str(grammar_info)

                # Check if this is a plural form
                if 'pl' in grammar_str.lower().split():
                    endings = row[col_idx]
                    if not isinstance(endings, list):
                        endings = [endings] if endings else []

                    for ending in endings:
                        if ending and ending != "-":
                            form = f"{stem}{ending}"
                            plural_forms.add(form)

        return plural_forms

    def _get_all_forms_from_template(self, word) -> Set[str]:
        """Extract all forms from a word's inflection template.

        Used for plural-only patterns where all forms are plural.

        Args:
            word: DpdHeadword object with .it (inflection template)

        Returns:
            Set of all inflected forms
        """
        if not word.it or not word.it.data:
            return set()

        try:
            template_data = json.loads(word.it.data)
        except json.JSONDecodeError:
            return set()

        all_forms = set()
        stem = clean_stem(word.stem)

        for row in template_data[1:]:  # Skip header
            for col_idx in range(1, len(row), 2):  # Odd columns have endings
                endings = row[col_idx] if col_idx < len(row) else None
                if not endings:
                    continue

                if not isinstance(endings, list):
                    endings = [endings]

                for ending in endings:
                    if ending and ending != "-":
                        form = f"{stem}{ending}"
                        all_forms.add(form)

        return all_forms

    def build_singular_index(self, all_nouns: List[Any]) -> None:
        """
        Build index of singular lemmas for matching against plural-only.

        Args:
            all_nouns: List of DpdHeadword objects (nouns with inflection templates)
        """
        print("Building singular noun index for plural deduplication...")

        for word in all_nouns:
            pattern = word.pattern or ""

            # Skip plural-only patterns - we're indexing singulars
            if is_plural_only_pattern(pattern):
                continue

            gender = self._extract_gender_from_pattern(pattern)
            if not gender:
                continue

            stem = clean_stem(word.stem)
            key = (stem, gender)

            # Get plural forms from this singular lemma's template
            plural_forms = self._get_plural_forms_from_template(word)
            if not plural_forms:
                continue

            if key not in self._singular_index:
                self._singular_index[key] = []

            self._singular_index[key].append((word.lemma_1, pattern, plural_forms))

        total_entries = sum(len(v) for v in self._singular_index.values())
        print(f"  Indexed {total_entries} singular lemmas across {len(self._singular_index)} (stem, gender) keys")

    def check_redundant(self, word) -> PluralMatchResult:
        """
        Check if a plural-only lemma is redundant (matches a singular's plurals).

        Args:
            word: DpdHeadword object with plural-only pattern

        Returns:
            PluralMatchResult indicating if redundant and what it matches
        """
        pattern = word.pattern or ""
        if not is_plural_only_pattern(pattern):
            return PluralMatchResult(is_redundant=False)

        gender = self._extract_gender_from_pattern(pattern)
        stem = clean_stem(word.stem)
        key = (stem, gender)

        # Get all forms from plural-only (all forms are plural)
        plural_only_forms = self._get_all_forms_from_template(word)
        if not plural_only_forms:
            return PluralMatchResult(is_redundant=False, plural_only_forms=0)

        # Look for matching singulars
        candidates = self._singular_index.get(key, [])

        best_match = PluralMatchResult(
            is_redundant=False,
            plural_only_forms=len(plural_only_forms)
        )

        for lemma_1, sing_pattern, sing_plural_forms in candidates:
            # Skip if no plural forms in singular
            if not sing_plural_forms:
                continue

            # Check overlap
            matching = plural_only_forms & sing_plural_forms
            match_ratio = len(matching) / len(plural_only_forms) if plural_only_forms else 0

            if match_ratio > best_match.match_ratio:
                best_match = PluralMatchResult(
                    is_redundant=(match_ratio == 1.0),
                    matched_lemma=lemma_1,
                    matched_pattern=sing_pattern,
                    match_ratio=match_ratio,
                    plural_only_forms=len(plural_only_forms),
                    matching_forms=len(matching)
                )

            # Early exit on perfect match
            if match_ratio == 1.0:
                break

        if best_match.is_redundant:
            self._redundant_count += 1
        elif is_plural_only_pattern(pattern):
            self._true_plural_only_count += 1

        return best_match

    def get_stats(self) -> Dict[str, int]:
        """Get deduplication statistics."""
        return {
            'singular_lemmas_indexed': sum(len(v) for v in self._singular_index.values()),
            'unique_stem_gender_keys': len(self._singular_index),
            'redundant_plural_only': self._redundant_count,
            'true_plural_only': self._true_plural_only_count,
        }

    def print_summary(self) -> None:
        """Print summary of deduplication results."""
        stats = self.get_stats()
        print(f"\n=== PLURAL-ONLY DEDUPLICATION SUMMARY ===")
        print(f"Singular lemmas indexed: {stats['singular_lemmas_indexed']}")
        print(f"Unique (stem, gender) keys: {stats['unique_stem_gender_keys']}")
        print(f"Redundant plural-only (skipped): {stats['redundant_plural_only']}")
        print(f"True plural-only (kept): {stats['true_plural_only']}")

"""
Custom translation adjustments for PaliPractice.

Allows specifying preferred translations that should appear first
in the meaning string for specific lemmas.
"""

import json
from pathlib import Path

# Path to custom translations file (in configs folder)
TRANSLATIONS_PATH = Path(__file__).parent.parent / "configs" / "custom_translations.json"


class TranslationAdjustments:
    """Handles custom translation preferences for lemmas."""

    def __init__(self, translations_path: Path = TRANSLATIONS_PATH):
        self._adjustments: dict[int, tuple[str, str]] = {}  # id -> (lemma_1, preferred)
        self._load(translations_path)

    def _load(self, path: Path) -> None:
        """Load custom translations from JSON file."""
        if not path.exists():
            print(f"  [translations] No custom translations file at {path}")
            return

        with open(path, 'r', encoding='utf-8') as f:
            data = json.load(f)

        for id_str, entry in data.items():
            lemma_id = int(id_str)
            lemma_1 = entry.get("lemma_1", "")
            preferred = entry.get("preferred", "")
            if lemma_1 and preferred:
                self._adjustments[lemma_id] = (lemma_1, preferred)

        print(f"  [translations] Loaded {len(self._adjustments)} custom translation adjustments")

    def apply(self, lemma_id: int, lemma_1: str, meaning: str) -> str:
        """
        Apply custom translation adjustment if one exists for this lemma.

        Validates that both ID and lemma_1 match before applying.

        Args:
            lemma_id: The DPD headword ID
            lemma_1: The DPD lemma_1 value (e.g., "paññā 1")
            meaning: The original meaning string (semicolon-separated)

        Returns:
            Modified meaning string with preferred translation first,
            or original meaning if no adjustment applies.
        """
        if lemma_id not in self._adjustments:
            return meaning

        expected_lemma, preferred = self._adjustments[lemma_id]

        # Validate lemma_1 matches
        if lemma_1 != expected_lemma:
            print(f"  [translations] WARNING: ID {lemma_id} has lemma_1 '{lemma_1}' "
                  f"but expected '{expected_lemma}' - skipping adjustment")
            return meaning

        if not meaning:
            return preferred

        # Parse meanings (split by "; ")
        parts = [p.strip() for p in meaning.split(";")]
        parts = [p for p in parts if p]  # Remove empty strings

        # Check if preferred already exists (case-insensitive search)
        preferred_lower = preferred.lower()
        existing_index = None
        for i, part in enumerate(parts):
            if part.lower() == preferred_lower:
                existing_index = i
                break

        if existing_index is not None:
            if existing_index == 0:
                # Already first, nothing to do
                return meaning
            else:
                # Remove from current position and prepend
                actual_preferred = parts.pop(existing_index)  # Keep original casing
                parts.insert(0, actual_preferred)
                print(f"  [translations] {lemma_1}: moved '{actual_preferred}' to first position")
        else:
            # Doesn't exist, prepend
            parts.insert(0, preferred)
            print(f"  [translations] {lemma_1}: prepended '{preferred}'")

        return "; ".join(parts)

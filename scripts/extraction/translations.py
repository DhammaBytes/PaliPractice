"""
Custom translation adjustments for PaliPractice.

Two types of adjustments:
1. Primary: Move or prepend preferred translation to first position
2. Replace: Substitute a specific term with another (keeps position)
"""

import json
from pathlib import Path

# Path to custom translations file (in configs folder)
TRANSLATIONS_PATH = Path(__file__).parent.parent / "configs" / "custom_translations.json"


class TranslationAdjustments:
    """Handles custom translation preferences for lemmas."""

    def __init__(self, translations_path: Path = TRANSLATIONS_PATH):
        # id -> (lemma_1, preferred)
        self._primary: dict[int, tuple[str, str]] = {}
        # id -> (lemma_1, target, preferred)
        self._replace: dict[int, tuple[str, str, str]] = {}
        self._load(translations_path)

    def _load(self, path: Path) -> None:
        """Load custom translations from JSON file."""
        if not path.exists():
            print(f"  [translations] No custom translations file at {path}")
            return

        with open(path, 'r', encoding='utf-8') as f:
            data = json.load(f)

        # Load primary adjustments
        primary_data = data.get("primary", {})
        for id_str, entry in primary_data.items():
            lemma_id = int(id_str)
            lemma_1 = entry.get("lemma_1", "")
            preferred = entry.get("preferred", "")
            if lemma_1 and preferred:
                self._primary[lemma_id] = (lemma_1, preferred)

        # Load replace adjustments
        replace_data = data.get("replace", {})
        for id_str, entry in replace_data.items():
            lemma_id = int(id_str)
            lemma_1 = entry.get("lemma_1", "")
            target = entry.get("target", "")
            preferred = entry.get("preferred", "")
            if lemma_1 and target and preferred:
                self._replace[lemma_id] = (lemma_1, target, preferred)

        total = len(self._primary) + len(self._replace)
        print(f"  [translations] Loaded {total} custom adjustments ({len(self._primary)} primary, {len(self._replace)} replace)")

    def apply(self, lemma_id: int, lemma_1: str, meaning: str) -> str:
        """
        Apply custom translation adjustment if one exists for this lemma.

        Validates that both ID and lemma_1 match before applying.

        Args:
            lemma_id: The DPD headword ID
            lemma_1: The DPD lemma_1 value (e.g., "paññā 1")
            meaning: The original meaning string (semicolon-separated)

        Returns:
            Modified meaning string, or original if no adjustment applies.
        """
        result = meaning

        # Apply primary adjustment (move/prepend to first)
        if lemma_id in self._primary:
            result = self._apply_primary(lemma_id, lemma_1, result)

        # Apply replace adjustment (substitute term)
        if lemma_id in self._replace:
            result = self._apply_replace(lemma_id, lemma_1, result)

        return result

    def _apply_primary(self, lemma_id: int, lemma_1: str, meaning: str) -> str:
        """Move or prepend preferred translation to first position."""
        expected_lemma, preferred = self._primary[lemma_id]

        # Validate lemma_1 matches
        if lemma_1 != expected_lemma:
            print(f"  [translations] WARNING: ID {lemma_id} has lemma_1 '{lemma_1}' "
                  f"but expected '{expected_lemma}' - skipping primary adjustment")
            return meaning

        if not meaning:
            return preferred

        # Parse meanings (split by ";")
        parts = [p.strip() for p in meaning.split(";")]
        parts = [p for p in parts if p]  # Remove empty strings

        # Check if preferred already exists (case-insensitive, startswith match)
        preferred_lower = preferred.lower()
        existing_index = None
        for i, part in enumerate(parts):
            if part.lower().startswith(preferred_lower):
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
                action = "moved"
        else:
            # Doesn't exist, prepend
            parts.insert(0, preferred)
            action = "added"

        # Check for duplicates (DPD may have added the same meaning)
        parts_lower = [p.lower() for p in parts]
        seen = set()
        duplicates = []
        for p in parts_lower:
            if p in seen:
                duplicates.append(p)
            seen.add(p)

        result = "; ".join(parts)

        if duplicates:
            print(f"  [translations] ALERT: [{lemma_id}] {lemma_1}: DUPLICATE MEANINGS DETECTED after primary!")
            print(f"                 Duplicates: {duplicates}")
            print(f"                 Result: {result}")
            print(f"                 Consider removing this entry from custom_translations.json")
        else:
            print(f"  [translations] [{lemma_id}] {lemma_1}: {result} {{{action}}}")

        return result

    def _apply_replace(self, lemma_id: int, lemma_1: str, meaning: str) -> str:
        """Replace a specific term with another (keeps position)."""
        expected_lemma, target, preferred = self._replace[lemma_id]

        # Validate lemma_1 matches
        if lemma_1 != expected_lemma:
            print(f"  [translations] WARNING: ID {lemma_id} has lemma_1 '{lemma_1}' "
                  f"but expected '{expected_lemma}' - skipping replace adjustment")
            return meaning

        if not meaning:
            return meaning

        # Parse meanings (split by ";")
        parts = [p.strip() for p in meaning.split(";")]
        parts = [p for p in parts if p]  # Remove empty strings

        # Find and replace target term (case-insensitive match, preserve structure)
        target_lower = target.lower()
        replaced = False
        for i, part in enumerate(parts):
            if part.lower() == target_lower:
                parts[i] = preferred
                replaced = True
                break
            elif target_lower in part.lower():
                # Partial match - replace within the part
                # Case-insensitive replacement
                import re
                parts[i] = re.sub(re.escape(target), preferred, part, flags=re.IGNORECASE)
                replaced = True
                break

        if replaced:
            # Check for duplicates after replacement (DPD may have added the same meaning)
            parts_lower = [p.lower() for p in parts]
            seen = set()
            duplicates = []
            for p in parts_lower:
                if p in seen:
                    duplicates.append(p)
                seen.add(p)

            result = "; ".join(parts)

            if duplicates:
                print(f"  [translations] ALERT: [{lemma_id}] {lemma_1}: DUPLICATE MEANINGS DETECTED after replace!")
                print(f"                 Duplicates: {duplicates}")
                print(f"                 Result: {result}")
                print(f"                 Consider removing this entry from custom_translations.json")
            else:
                print(f"  [translations] [{lemma_id}] {lemma_1}: {result} {{replaced '{target}' -> '{preferred}'}}")

            return result

        # Target not found
        print(f"  [translations] WARNING: [{lemma_id}] {lemma_1}: target '{target}' not found in meaning")
        return meaning

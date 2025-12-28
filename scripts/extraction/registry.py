"""
Lemma registry management for stable IDs across rebuilds.

The registry ensures lemma IDs remain stable when the training database
is regenerated. New lemmas are appended, but existing IDs never change.
"""

import json
import shutil
from typing import Dict, Any

from .config import (
    REGISTRY_PATH,
    REGISTRY_BACKUP_PATH,
    NOUN_ID_START,
    NOUN_ID_MAX,
    VERB_ID_START,
    VERB_ID_MAX,
)


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

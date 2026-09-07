"""
Lemma registry management for stable IDs across rebuilds.

The registry ensures lemma IDs remain stable when the training database
is regenerated. New lemmas are appended, but existing IDs never change.
"""

import json
from typing import Dict, Any
from pathlib import Path

from .inputs import read_json, InputError

from .config import (
    REGISTRY_PATH,
    NOUN_ID_START,
    NOUN_ID_MAX,
    VERB_ID_START,
    VERB_ID_MAX,
)


class RegistryError(Exception):
    """Raised when registry validation fails."""
    pass


def validate_mapping(mapping, next_id, start, maximum, kind):
    if not isinstance(mapping, dict):
        raise RegistryError(f"{kind} must be a lemma-to-ID object")
    if type(next_id) is not int or not start <= next_id <= maximum + 1:
        raise RegistryError(f"Invalid next {kind} ID")
    for lemma, identifier in mapping.items():
        if not isinstance(lemma, str) or not lemma.strip():
            raise RegistryError(f"Invalid {kind} lemma")
        if type(identifier) is not int or not start <= identifier <= maximum:
            raise RegistryError(f"Invalid {kind} ID for {lemma}")
    ids = list(mapping.values())
    if len(ids) != len(set(ids)):
        raise RegistryError(f"Duplicate {kind} IDs")
    if ids and next_id <= max(ids):
        raise RegistryError(f"Next {kind} ID must exceed all assigned IDs")


def validate_registry(registry: Dict[str, Any]) -> None:
    """Validate registry structure, value types, unique IDs, and allocation bounds."""
    keys = {"version", "next_noun_id", "next_verb_id", "nouns", "verbs"}
    if not isinstance(registry, dict) or set(registry) != keys:
        raise RegistryError("Invalid lemma registry fields")
    if type(registry["version"]) is not int or registry["version"] != 1:
        raise RegistryError("Unsupported lemma registry version")
    validate_mapping(registry["nouns"], registry["next_noun_id"], NOUN_ID_START, NOUN_ID_MAX, "noun")
    validate_mapping(registry["verbs"], registry["next_verb_id"], VERB_ID_START, VERB_ID_MAX, "verb")


def load_registry(path: Path = REGISTRY_PATH) -> Dict[str, Any]:
    """Require and validate an existing registry; initialization is a separate operation."""
    try:
        registry = read_json(path)
    except InputError as error:
        raise RegistryError(str(error)) from error
    validate_registry(registry)
    return registry


def save_registry(registry: Dict[str, Any], original_registry: Dict[str, Any],
                  *, output_path: Path) -> None:
    """
    Save lemma registry with safety checks.
    - Validates registry before saving
    - Writes only the explicitly supplied candidate path
    - Ensures no existing IDs were modified or removed
    - Requires a new candidate file; incomplete builds have no completion manifest
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

    # Exclusive creation prevents overwriting any existing registry.
    with output_path.open('x', encoding='utf-8') as stream:
        stream.write(json.dumps(registry, indent=2, ensure_ascii=False) + "\n")


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

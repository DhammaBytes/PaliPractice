"""Pinned, offline inputs for an isolated English candidate."""

import hashlib
import json
import re
from pathlib import Path

from .config import EXCLUDED_NOUN_LEMMAS, MAX_LEMMA_LENGTH

CORPORA = ("cst", "bjt", "sya", "sc")
INPUT_NAMES = {"dpd", "registry", "adjustments", *CORPORA}


class InputError(ValueError):
    """The candidate cannot be built from the supplied inputs."""


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def read_json(path: Path):
    def unique_keys(pairs):
        result = {}
        for key, value in pairs:
            if key in result:
                raise InputError(f"Duplicate JSON key: {key}")
            result[key] = value
        return result

    try:
        return json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=unique_keys)
    except (OSError, UnicodeError, json.JSONDecodeError) as error:
        raise InputError(f"Cannot read JSON input {path}: {error}") from error


def configuration(noun_limit: int, verb_limit: int) -> dict:
    return {
        "noun_limit": noun_limit,
        "verb_limit": verb_limit,
        "max_lemma_length": MAX_LEMMA_LENGTH,
        "excluded_noun_lemmas": sorted(EXCLUDED_NOUN_LEMMAS),
    }


def positive_integer(value, name: str, maximum: int = 2_147_483_647) -> int:
    if type(value) is not int or not 0 < value <= maximum:
        raise InputError(f"{name} must be an integer in 1..{maximum}")
    return value


def verify_file(entry: dict, parent: Path) -> Path:
    if not isinstance(entry, dict) or set(entry) != {"path", "sha256", "source"}:
        raise InputError("Each input requires path, sha256, and source")
    if not isinstance(entry["path"], str) or not entry["path"]:
        raise InputError("Input path must be nonempty")
    source = entry["source"]
    if not isinstance(source, dict):
        raise InputError("Each input requires source origin and immutable revision")
    if not isinstance(source.get("origin"), str) or not source["origin"].strip():
        raise InputError("Source origin must be nonempty")
    revision = source.get("revision")
    if not isinstance(revision, str) or not re.fullmatch(
        r"[0-9a-f]{40}|sha256:[0-9a-f]{64}|release:[A-Za-z0-9_.-]+", revision
    ):
        raise InputError("Source revision must identify a commit, checksum, or hashed release")
    path = (parent / entry["path"]).resolve()
    if not path.is_file() or path.stat().st_size == 0:
        raise InputError(f"Missing or empty input: {path}")
    if sha256(path) != entry["sha256"]:
        raise InputError(f"Checksum mismatch: {path}")
    return path


def load_manifest(path: Path) -> tuple[dict, dict[str, Path]]:
    manifest = read_json(path)
    if not isinstance(manifest, dict) or set(manifest) != {
        "schema", "database_version", "configuration", "inputs", "corpus_generation"
    }:
        raise InputError("Invalid input manifest fields")
    if type(manifest["schema"]) is not int or manifest["schema"] != 1:
        raise InputError("Unsupported input manifest schema")
    positive_integer(manifest["database_version"], "database_version")
    config = manifest["configuration"]
    if not isinstance(config, dict):
        raise InputError("Invalid extraction configuration")
    nouns = positive_integer(config.get("noun_limit"), "noun_limit", 50_000)
    verbs = positive_integer(config.get("verb_limit"), "verb_limit", 40_000)
    if config != configuration(nouns, verbs):
        raise InputError("Manifest configuration does not match extraction policy")
    if not isinstance(manifest["inputs"], dict) or set(manifest["inputs"]) != INPUT_NAMES:
        raise InputError("DPD, registry, adjustments, and all four corpora are required")
    generation = manifest["corpus_generation"]
    if not isinstance(generation, dict) or not generation.get("recipe") or not generation.get("revisions"):
        raise InputError("Corpus generation recipe and source revisions are required")
    paths = {name: verify_file(entry, path.parent)
             for name, entry in manifest["inputs"].items()}
    wal = Path(str(paths["dpd"]) + "-wal")
    if wal.exists() and wal.stat().st_size:
        raise InputError("DPD has a nonempty WAL; supply a checkpointed immutable database")
    return manifest, paths


def load_corpus_words(paths: list[Path]) -> set[str]:
    if len(paths) != len(CORPORA) or len(set(paths)) != len(CORPORA):
        raise InputError("Four distinct corpus wordlists are required")
    result = set()
    for path in paths:
        words = read_json(path)
        if not isinstance(words, list) or not words:
            raise InputError(f"Corpus must be a nonempty JSON list: {path}")
        if any(not isinstance(word, str) or not word.strip() for word in words):
            raise InputError(f"Corpus contains an invalid word: {path}")
        result.update(words)
    return result

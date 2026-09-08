"""Immutable validation for the generated application database."""

from __future__ import annotations

import json
import re
import sqlite3
from pathlib import Path
from typing import Any
from urllib.parse import quote

SCHEMA = {
    "nouns": {"id", "ebt_count", "lemma_id", "lemma", "gender", "stem", "pattern"},
    "nouns_details": {"id", "lemma_id", "word", "root", "meaning"},
    "nouns_corpus_forms": {"form_id"},
    "nouns_irregular_forms": {"form_id", "form"},
    "verbs": {"id", "ebt_count", "lemma_id", "lemma", "stem", "pattern"},
    "verbs_details": {
        "id",
        "lemma_id",
        "word",
        "root",
        "type",
        "trans",
        "meaning",
    },
    "verbs_corpus_forms": {"form_id"},
    "verbs_irregular_forms": {"form_id", "form"},
    "verbs_nonreflexive": {"lemma_id"},
}


class DuplicateKeyError(ValueError):
    """Raised when JSON contains a duplicate object key."""


def _unique_object(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    result: dict[str, Any] = {}
    for key, value in pairs:
        if key in result:
            raise DuplicateKeyError(f"duplicate JSON key: {key}")
        result[key] = value
    return result


def _load_registry(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=_unique_object)


def _immutable_connection(path: Path) -> sqlite3.Connection:
    uri = f"file:{quote(str(path.resolve()), safe='/')}?mode=ro&immutable=1"
    connection = sqlite3.connect(uri, uri=True)
    connection.row_factory = sqlite3.Row
    return connection


def validate_data(
    database: Path, version_file: Path, registry_file: Path
) -> tuple[list[str], dict[str, int]]:
    """Return contract errors and useful, non-threshold metrics."""
    errors: list[str] = []
    metrics: dict[str, int] = {}

    if not database.is_file():
        return [f"database is missing: {database}"], metrics
    if not version_file.is_file():
        return [f"version file is missing: {version_file}"], metrics
    if not registry_file.is_file():
        return [f"lemma registry is missing: {registry_file}"], metrics

    version_text = version_file.read_text(encoding="utf-8").strip()
    if not re.fullmatch(r"[1-9][0-9]*", version_text):
        errors.append("version file must contain one positive decimal integer")
        expected_version = None
    else:
        expected_version = int(version_text)

    try:
        registry = _load_registry(registry_file)
    except (OSError, UnicodeError, json.JSONDecodeError, DuplicateKeyError) as error:
        errors.append(f"invalid lemma registry: {error}")
        registry = {}

    try:
        connection = _immutable_connection(database)
    except sqlite3.Error as error:
        return errors + [f"cannot open database read-only: {error}"], metrics

    try:
        integrity = [row[0] for row in connection.execute("PRAGMA integrity_check")]
        if integrity != ["ok"]:
            errors.append(f"SQLite integrity_check failed: {integrity[:3]}")

        user_version = int(connection.execute("PRAGMA user_version").fetchone()[0])
        metrics["user_version"] = user_version
        if expected_version is not None and user_version != expected_version:
            errors.append(
                f"version mismatch: file={expected_version}, PRAGMA user_version={user_version}"
            )

        actual_tables = {
            row[0]
            for row in connection.execute(
                "SELECT name FROM sqlite_master WHERE type='table'"
            )
        }
        for table, required_columns in SCHEMA.items():
            if table not in actual_tables:
                errors.append(f"missing table: {table}")
                continue
            actual_columns = {
                row[1] for row in connection.execute(f'PRAGMA table_info("{table}")')
            }
            missing = sorted(required_columns - actual_columns)
            if missing:
                errors.append(f"{table} missing columns: {', '.join(missing)}")

        if any(table not in actual_tables for table in SCHEMA):
            return errors, metrics
        if any(
            not columns.issubset(
                {
                    row[1]
                    for row in connection.execute(f'PRAGMA table_info("{table}")')
                }
            )
            for table, columns in SCHEMA.items()
        ):
            return errors, metrics

        for table in SCHEMA:
            count = int(connection.execute(f'SELECT COUNT(*) FROM "{table}"').fetchone()[0])
            metrics[f"{table}_rows"] = count
            if count == 0:
                errors.append(f"{table} must not be empty")

        _validate_lemma_ranges(connection, errors)
        _validate_form_ids(connection, errors)

        for primary, details in (("nouns", "nouns_details"), ("verbs", "verbs_details")):
            missing_details = int(
                connection.execute(
                    f"""
                    SELECT COUNT(*) FROM "{primary}" p
                    LEFT JOIN "{details}" d ON d.id=p.id AND d.lemma_id=p.lemma_id
                    WHERE d.id IS NULL
                    """
                ).fetchone()[0]
            )
            orphan_details = int(
                connection.execute(
                    f"""
                    SELECT COUNT(*) FROM "{details}" d
                    LEFT JOIN "{primary}" p ON p.id=d.id AND p.lemma_id=d.lemma_id
                    WHERE p.id IS NULL
                    """
                ).fetchone()[0]
            )
            if missing_details or orphan_details:
                errors.append(
                    f"{primary}/{details} parity failed: "
                    f"{missing_details} missing, {orphan_details} orphan"
                )

        _validate_registry(connection, registry, errors, metrics)
    except sqlite3.Error as error:
        errors.append(f"SQLite contract query failed: {error}")
    finally:
        connection.close()

    return errors, metrics


def _validate_lemma_ranges(
    connection: sqlite3.Connection, errors: list[str]
) -> None:
    for table, lower, upper in (
        ("nouns", 10001, 69999),
        ("nouns_details", 10001, 69999),
        ("verbs", 70001, 99999),
        ("verbs_details", 70001, 99999),
        ("verbs_nonreflexive", 70001, 99999),
    ):
        count = int(
            connection.execute(
                f'SELECT COUNT(*) FROM "{table}" '
                "WHERE lemma_id NOT BETWEEN ? AND ?",
                (lower, upper),
            ).fetchone()[0]
        )
        if count:
            errors.append(
                f"{table} has {count} lemma IDs outside {lower}-{upper}"
            )
    orphaned_nonreflexive = int(
        connection.execute(
            """
            SELECT COUNT(*) FROM verbs_nonreflexive n
            WHERE NOT EXISTS (
                SELECT 1 FROM verbs v WHERE v.lemma_id = n.lemma_id
            )
            """
        ).fetchone()[0]
    )
    if orphaned_nonreflexive:
        errors.append(
            "verbs_nonreflexive has "
            f"{orphaned_nonreflexive} unresolved lemma IDs"
        )


def _validate_form_ids(connection: sqlite3.Connection, errors: list[str]) -> None:
    noun_invalid = """
        form_id / 10000 NOT BETWEEN 10001 AND 69999
        OR (form_id / 1000) % 10 NOT BETWEEN 1 AND 8
        OR (form_id / 100) % 10 NOT BETWEEN 1 AND 3
        OR (form_id / 10) % 10 NOT BETWEEN 1 AND 2
        OR form_id % 10 NOT BETWEEN 1 AND 6
    """
    verb_invalid = """
        form_id / 100000 NOT BETWEEN 70001 AND 99999
        OR (form_id / 10000) % 10 NOT BETWEEN 1 AND 4
        OR (form_id / 1000) % 10 NOT BETWEEN 1 AND 3
        OR (form_id / 100) % 10 NOT BETWEEN 1 AND 2
        OR (form_id / 10) % 10 NOT BETWEEN 1 AND 2
        OR form_id % 10 NOT BETWEEN 1 AND 7
    """
    specifications = (
        ("nouns_corpus_forms", "nouns", 10000, noun_invalid),
        ("nouns_irregular_forms", "nouns", 10000, noun_invalid),
        ("verbs_corpus_forms", "verbs", 100000, verb_invalid),
        ("verbs_irregular_forms", "verbs", 100000, verb_invalid),
    )
    for table, lemma_table, divisor, invalid in specifications:
        malformed = int(
            connection.execute(
                f'SELECT COUNT(*) FROM "{table}" WHERE {invalid}'
            ).fetchone()[0]
        )
        if malformed:
            errors.append(f"{table} has {malformed} malformed form IDs")
        orphaned = int(
            connection.execute(
                f"""
                SELECT COUNT(*) FROM "{table}" f
                WHERE NOT EXISTS (
                    SELECT 1 FROM "{lemma_table}" l
                    WHERE l.lemma_id = f.form_id / {divisor}
                )
                """
            ).fetchone()[0]
        )
        if orphaned:
            errors.append(f"{table} has {orphaned} unresolved lemma prefixes")


def _validate_registry(
    connection: sqlite3.Connection,
    registry: dict[str, Any],
    errors: list[str],
    metrics: dict[str, int],
) -> None:
    required = {"version", "next_noun_id", "next_verb_id", "nouns", "verbs"}
    missing = required - registry.keys()
    if missing:
        errors.append(f"lemma registry missing keys: {', '.join(sorted(missing))}")
        return

    if isinstance(registry["version"], bool) or registry["version"] != 1:
        errors.append("lemma registry version must be 1")

    maps: dict[str, dict[str, int]] = {}
    for kind, lower, upper in (("nouns", 10001, 69999), ("verbs", 70001, 99999)):
        raw = registry[kind]
        if not isinstance(raw, dict):
            errors.append(f"registry {kind} must be an object")
            continue
        invalid = [
            lemma
            for lemma, lemma_id in raw.items()
            if not isinstance(lemma, str)
            or not lemma
            or isinstance(lemma_id, bool)
            or not isinstance(lemma_id, int)
            or not lower <= lemma_id <= upper
        ]
        if invalid:
            errors.append(f"registry {kind} has invalid entries: {invalid[:3]}")
            continue
        ids = list(raw.values())
        if len(ids) != len(set(ids)):
            errors.append(f"registry {kind} has duplicate IDs")
        maps[kind] = raw
        metrics[f"registry_{kind}"] = len(raw)

        next_key = f"next_{kind[:-1]}_id"
        expected_next = max(ids, default=lower - 1) + 1
        next_value = registry[next_key]
        if (
            isinstance(next_value, bool)
            or not isinstance(next_value, int)
            or next_value != expected_next
        ):
            errors.append(
                f"{next_key} must be max assigned ID + 1 "
                f"({expected_next}, got {next_value!r})"
            )

    for table, kind in (("nouns", "nouns"), ("verbs", "verbs")):
        mapping = maps.get(kind)
        if mapping is None:
            continue
        rows = connection.execute(
            f'SELECT DISTINCT lemma_id, lemma FROM "{table}"'
        ).fetchall()
        bad = [
            (int(row["lemma_id"]), str(row["lemma"]))
            for row in rows
            if mapping.get(str(row["lemma"])) != int(row["lemma_id"])
        ]
        if bad:
            errors.append(f"{table} disagrees with lemma registry: {bad[:3]}")

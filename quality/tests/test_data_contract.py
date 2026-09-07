from __future__ import annotations

import json
import sqlite3
import tempfile
import unittest
from pathlib import Path

from quality.checks.data_contract import validate_data


class DataFixture:
    def __init__(self, root: Path) -> None:
        root.mkdir(parents=True, exist_ok=True)
        self.database = root / "pali.db"
        self.version = root / "pali.version.txt"
        self.registry = root / "lemma_registry.json"
        self.version.write_text("123\n", encoding="utf-8")
        self.registry.write_text(
            json.dumps(
                {
                    "version": 1,
                    "next_noun_id": 10002,
                    "next_verb_id": 70002,
                    "nouns": {"n": 10001},
                    "verbs": {"v": 70001},
                }
            ),
            encoding="utf-8",
        )
        connection = sqlite3.connect(self.database)
        connection.executescript(
            """
            PRAGMA user_version=123;
            CREATE TABLE nouns (
                id INTEGER PRIMARY KEY, ebt_count INTEGER, lemma_id INTEGER,
                lemma TEXT, gender INTEGER, stem TEXT, pattern TEXT
            );
            CREATE TABLE nouns_details (
                id INTEGER PRIMARY KEY, lemma_id INTEGER, word TEXT, root TEXT,
                meaning TEXT, meaning_ru TEXT
            );
            CREATE TABLE nouns_corpus_forms (form_id INTEGER PRIMARY KEY);
            CREATE TABLE nouns_irregular_forms (
                form_id INTEGER PRIMARY KEY, form TEXT
            );
            CREATE TABLE verbs (
                id INTEGER PRIMARY KEY, ebt_count INTEGER, lemma_id INTEGER,
                lemma TEXT, stem TEXT, pattern TEXT
            );
            CREATE TABLE verbs_details (
                id INTEGER PRIMARY KEY, lemma_id INTEGER, word TEXT, root TEXT,
                type TEXT, trans TEXT, meaning TEXT, meaning_ru TEXT
            );
            CREATE TABLE verbs_corpus_forms (form_id INTEGER PRIMARY KEY);
            CREATE TABLE verbs_irregular_forms (
                form_id INTEGER PRIMARY KEY, form TEXT
            );
            CREATE TABLE verbs_nonreflexive (lemma_id INTEGER PRIMARY KEY);
            INSERT INTO nouns VALUES (1, 2, 10001, 'n', 1, 'n', 'n');
            INSERT INTO nouns_details VALUES (1, 10001, 'n', '', '', '');
            INSERT INTO nouns_corpus_forms VALUES (100010101);
            INSERT INTO nouns_irregular_forms VALUES (100010101, 'n');
            INSERT INTO verbs VALUES (2, 3, 70001, 'v', 'v', 'v');
            INSERT INTO verbs_details
                VALUES (2, 70001, 'v', '', '', '', '', '');
            INSERT INTO verbs_corpus_forms VALUES (7000111111);
            INSERT INTO verbs_irregular_forms VALUES (7000111111, 'v');
            INSERT INTO verbs_nonreflexive VALUES (70001);
            """
        )
        connection.commit()
        connection.close()

    def execute(self, sql: str) -> None:
        connection = sqlite3.connect(self.database)
        connection.executescript(sql)
        connection.commit()
        connection.close()

    def errors(self) -> list[str]:
        return validate_data(self.database, self.version, self.registry)[0]


class DataContractTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary = tempfile.TemporaryDirectory()
        self.fixture = DataFixture(Path(self.temporary.name))

    def tearDown(self) -> None:
        self.temporary.cleanup()

    def test_valid_fixture_passes_without_mutation(self) -> None:
        before = self.fixture.database.read_bytes()
        errors, metrics = validate_data(
            self.fixture.database, self.fixture.version, self.fixture.registry
        )
        self.assertEqual([], errors)
        self.assertEqual(1, metrics["nouns_rows"])
        self.assertEqual(before, self.fixture.database.read_bytes())

    def test_rejects_version_mismatch(self) -> None:
        self.fixture.version.write_text("124\n", encoding="utf-8")
        self.assertTrue(any("version mismatch" in error for error in self.fixture.errors()))

    def test_rejects_missing_schema_and_empty_table(self) -> None:
        self.fixture.execute("DROP TABLE verbs_nonreflexive;")
        self.assertTrue(any("missing table" in error for error in self.fixture.errors()))

    def test_rejects_empty_table(self) -> None:
        self.fixture.execute("DELETE FROM verbs_irregular_forms;")
        self.assertTrue(any("must not be empty" in error for error in self.fixture.errors()))

    def test_rejects_detail_parity_failure(self) -> None:
        self.fixture.execute("DELETE FROM nouns_details;")
        errors = self.fixture.errors()
        self.assertTrue(any("parity failed" in error for error in errors))

    def test_rejects_registry_disagreement_and_bad_range(self) -> None:
        registry = json.loads(self.fixture.registry.read_text(encoding="utf-8"))
        registry["nouns"] = {"n": 70001}
        registry["next_noun_id"] = 70002
        self.fixture.registry.write_text(json.dumps(registry), encoding="utf-8")
        self.assertTrue(any("invalid entries" in error for error in self.fixture.errors()))

    def test_rejects_registry_only_ids_just_below_exact_bounds(self) -> None:
        registry = json.loads(self.fixture.registry.read_text(encoding="utf-8"))
        registry["nouns"]["registry-only-noun"] = 10000
        registry["verbs"]["registry-only-verb"] = 70000
        self.fixture.registry.write_text(json.dumps(registry), encoding="utf-8")
        errors = self.fixture.errors()
        self.assertTrue(
            any(
                "registry nouns has invalid entries" in error
                and "registry-only-noun" in error
                for error in errors
            )
        )
        self.assertTrue(
            any(
                "registry verbs has invalid entries" in error
                and "registry-only-verb" in error
                for error in errors
            )
        )

    def test_zero_noun_case_and_number_remain_valid_none_values(self) -> None:
        # The base fixture's 0101 suffix encodes case=None and number=None.
        self.assertEqual([], self.fixture.errors())

    def test_accepts_documented_maximum_ids_and_form_components(self) -> None:
        registry = {
            "version": 1,
            "next_noun_id": 70000,
            "next_verb_id": 100000,
            "nouns": {"n": 69999},
            "verbs": {"v": 99999},
        }
        self.fixture.registry.write_text(json.dumps(registry), encoding="utf-8")
        self.fixture.execute(
            """
            UPDATE nouns SET lemma_id=69999;
            UPDATE nouns_details SET lemma_id=69999;
            UPDATE nouns_corpus_forms SET form_id=699998329;
            UPDATE nouns_irregular_forms SET form_id=699998329;
            UPDATE verbs SET lemma_id=99999;
            UPDATE verbs_details SET lemma_id=99999;
            UPDATE verbs_corpus_forms SET form_id=9999953229;
            UPDATE verbs_irregular_forms SET form_id=9999953229;
            UPDATE verbs_nonreflexive SET lemma_id=99999;
            """
        )
        self.assertEqual([], self.fixture.errors())

    def test_rejects_lemma_ids_just_below_documented_ranges(self) -> None:
        self.fixture.execute(
            """
            UPDATE nouns SET lemma_id=10000;
            UPDATE nouns_details SET lemma_id=10000;
            UPDATE verbs SET lemma_id=70000;
            UPDATE verbs_details SET lemma_id=70000;
            UPDATE verbs_nonreflexive SET lemma_id=70000;
            """
        )
        errors = self.fixture.errors()
        self.assertTrue(any("nouns has" in error and "10001-69999" in error for error in errors))
        self.assertTrue(any("verbs has" in error and "70001-99999" in error for error in errors))

    def test_rejects_out_of_range_noun_form_components(self) -> None:
        suffixes = {
            "case": 9111,
            "gender": 411,
            "number": 131,
            "ending": 110,
        }
        for component, suffix in suffixes.items():
            with self.subTest(component=component):
                fixture = DataFixture(Path(self.temporary.name) / f"noun-{component}")
                fixture.execute(
                    f"UPDATE nouns_corpus_forms SET form_id={10001 * 10000 + suffix};"
                )
                self.assertTrue(
                    any("malformed form IDs" in error for error in fixture.errors())
                )

    def test_rejects_out_of_range_verb_form_components(self) -> None:
        suffixes = {
            "tense": 61111,
            "person": 14111,
            "number": 11311,
            "voice": 11131,
            "ending": 11110,
        }
        for component, suffix in suffixes.items():
            with self.subTest(component=component):
                fixture = DataFixture(Path(self.temporary.name) / f"verb-{component}")
                fixture.execute(
                    f"UPDATE verbs_corpus_forms SET form_id={70001 * 100000 + suffix};"
                )
                self.assertTrue(
                    any("malformed form IDs" in error for error in fixture.errors())
                )

    def test_checks_irregular_form_components_and_lemma_prefixes(self) -> None:
        self.fixture.execute(
            """
            UPDATE nouns_irregular_forms SET form_id=100019111;
            UPDATE verbs_irregular_forms SET form_id=7000211111;
            """
        )
        errors = self.fixture.errors()
        self.assertTrue(any("nouns_irregular_forms has 1 malformed" in error for error in errors))
        self.assertTrue(
            any("verbs_irregular_forms has 1 unresolved lemma prefixes" in error for error in errors)
        )

    def test_rejects_duplicate_registry_json_key(self) -> None:
        self.fixture.registry.write_text(
            '{"version":1,"version":1,"next_noun_id":10002,'
            '"next_verb_id":70002,"nouns":{"n":10001},"verbs":{"v":70001}}',
            encoding="utf-8",
        )
        self.assertTrue(any("duplicate JSON key" in error for error in self.fixture.errors()))

    def test_rejects_unresolved_corpus_prefix(self) -> None:
        self.fixture.execute("INSERT INTO nouns_corpus_forms VALUES (200010101);")
        self.assertTrue(
            any("unresolved lemma prefixes" in error for error in self.fixture.errors())
        )

    def test_rejects_corrupt_database(self) -> None:
        self.fixture.database.write_bytes(b"not a sqlite database")
        self.assertTrue(self.fixture.errors())


if __name__ == "__main__":
    unittest.main()

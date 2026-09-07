"""Semantic comparison detects content changes independently of SQLite page layout."""

import sqlite3
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from check_repeatability import semantic_digest


class RepeatabilityTests(unittest.TestCase):
    def test_row_order_is_irrelevant_but_content_schema_and_version_are_not(self):
        with tempfile.TemporaryDirectory() as directory:
            paths = [Path(directory) / f'{index}.db' for index in range(2)]
            for path, rows in zip(paths, [[('rūpa', b'a'), ('sati', b'b')], [('sati', b'b'), ('rūpa', b'a')]]):
                with sqlite3.connect(path) as db:
                    db.execute('CREATE TABLE words (text TEXT, data BLOB)')
                    db.executemany('INSERT INTO words VALUES (?, ?)', rows)
            expected = semantic_digest(paths[0])
            self.assertEqual(expected, semantic_digest(paths[1]))
            with sqlite3.connect(paths[1]) as db:
                db.execute("UPDATE words SET text='atthi' WHERE text='sati'")
            self.assertNotEqual(expected, semantic_digest(paths[1]))
            with sqlite3.connect(paths[1]) as db:
                db.execute("UPDATE words SET text='sati' WHERE text='atthi'")
                db.execute('PRAGMA user_version=2')
            self.assertNotEqual(expected, semantic_digest(paths[1]))
            with sqlite3.connect(paths[1]) as db:
                db.execute('PRAGMA user_version=0')
                db.execute('CREATE INDEX words_text ON words(text)')
            self.assertNotEqual(expected, semantic_digest(paths[1]))


if __name__ == '__main__':
    unittest.main()

"""The experimental projection preserves scoped identities and irregular spellings."""

from contextlib import closing
import sqlite3
import tempfile
import unittest
from pathlib import Path

from extraction.compact import compact


class CompactAttestationTests(unittest.TestCase):
    def test_collision_identity_and_spelling_are_preserved(self):
        with tempfile.TemporaryDirectory() as directory:
            source, output = (Path(directory) / name for name in ('source.db', 'compact.db'))
            with closing(sqlite3.connect(source)) as db:
                for kind in ('nouns', 'verbs'):
                    db.execute(f'CREATE TABLE {kind} (id INTEGER PRIMARY KEY)')
                    db.executemany(f'INSERT INTO {kind} VALUES (?)', [(1,), (2,)])
                    for suffix in ('corpus_forms', 'irregular_forms'):
                        db.execute(f'CREATE TABLE {kind}_{suffix} (headword_id INTEGER NOT NULL REFERENCES {kind}(id), form_id INTEGER NOT NULL, form TEXT NOT NULL, PRIMARY KEY(headword_id,form_id))')
                        db.executemany(f'INSERT INTO {kind}_{suffix} VALUES (?,?,?)', [(1, 123, 'first'), (2, 123, 'second')])
                db.commit()
            original_bytes = source.read_bytes()
            compact(source, output)
            self.assertEqual(original_bytes, source.read_bytes())
            with closing(sqlite3.connect(output)) as db:
                for kind in ('nouns', 'verbs'):
                    self.assertEqual([(1, 123), (2, 123)], db.execute(f'SELECT * FROM {kind}_corpus_forms').fetchall())
                    self.assertEqual([(1, 123, 'first'), (2, 123, 'second')], db.execute(f'SELECT * FROM {kind}_irregular_forms').fetchall())
                    with self.assertRaises(sqlite3.IntegrityError):
                        db.execute(f'INSERT INTO {kind}_corpus_forms VALUES (1,123)')
                    with self.assertRaises(sqlite3.OperationalError):
                        db.execute(f'SELECT rowid FROM {kind}_corpus_forms')
            with self.assertRaises(FileExistsError):
                compact(source, output)
            with self.assertRaises(FileExistsError):
                compact(source, source)

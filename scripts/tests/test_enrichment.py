"""Translation source integrity and immutable English enrichment contracts."""

from contextlib import closing
import csv
import io
import json
from pathlib import Path
import sqlite3
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from extraction.candidate import OUTPUTS
from extraction.enrichment import build_enrichment, validate_enrichment, verify_english_unchanged
from extraction.inputs import InputError, sha256
from extraction.russian_meanings import parse_russian_meanings


def tsv(rows):
    stream = io.StringIO(newline='')
    csv.writer(stream, delimiter='\t').writerows(rows)
    return stream.getvalue().encode('utf-8')


class RussianParserTests(unittest.TestCase):
    def test_curated_raw_empty_and_quoted_text(self):
        data = tsv([['id', 'ru_meaning', 'ru_meaning_raw'],
                    ['1', ' curated "text"\nsecond line ', 'raw'], ['2', ' ', ' raw '], ['3', '', '']])
        self.assertEqual({1: 'curated "text"\nsecond line', 2: 'raw', 3: ''}, parse_russian_meanings(data))

    def test_bad_input_is_not_reported_as_missing_translation(self):
        malformed = [b'', b'\xff', b'id\tru_meaning\n1\t"unterminated',
                     b'id\tru_meaning\n1\ta\n1\tb', b'id\tid\tru_meaning\n1\t1\ta',
                     b'id\tru_meaning\n1\ta\textra', b'id\tru_meaning\n1',
                     b'id\tru_meaning\n0\ta', b'id\tru_meaning\n-1\ta',
                     b'id\tru_meaning\n2147483648\ta', b'id\tru_meaning\n1.0\ta',
                     b'id\tru_meaning\n1\t\x00', b'id\tru_meaning\n']
        for data in malformed:
            with self.subTest(data=data), self.assertRaises(InputError):
                parse_russian_meanings(data)


class EnrichmentTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.english = self.root / 'english'
        self.english.mkdir()
        for name in OUTPUTS:
            if name != 'pali.db':
                (self.english / name).write_text('frozen English artifact')
        with closing(sqlite3.connect(self.english / 'pali.db')) as db, db:
            for kind in ('nouns', 'verbs'):
                db.execute(f'CREATE TABLE {kind}(id INTEGER PRIMARY KEY, lemma_id INTEGER, practice_primary INTEGER)')
            db.executemany('INSERT INTO nouns VALUES (?, ?, ?)', [(1, 10001, 1), (2, 10001, 0), (3, 10002, 1)])
            db.execute('INSERT INTO verbs VALUES (4, 20001, 1)')
            db.execute('PRAGMA user_version=17')
        self.source = self.root / 'ru.tsv'
        self.source.write_bytes(tsv([['id', 'ru_meaning', 'ru_meaning_raw'], ['1', 'слово', 'raw'],
                                    ['2', '', 'сырой'], ['3', '', ''], ['90', 'not selected', ''],
                                    ['99', 'unknown', '']]))
        dpd = self.root / 'dpd.db'
        with closing(sqlite3.connect(dpd)) as db, db:
            db.execute('CREATE TABLE dpd_headwords(id INTEGER PRIMARY KEY)')
            db.executemany('INSERT INTO dpd_headwords VALUES (?)', [(i,) for i in (1, 2, 3, 4, 90)])
        entry = lambda path: {'path': str(path), 'sha256': sha256(path),
                              'source': {'origin': 'https://example.invalid/pinned', 'revision': 'a' * 40}}
        self.manifest = self.root / 'manifest.json'
        self.spec = dict(schema=1, english_sha256=sha256(self.english / 'pali.db'), dpd=entry(dpd), sources={'ru': entry(self.source)})
        self.manifest.write_text(json.dumps(self.spec))
        (self.english / 'candidate.json').write_text(json.dumps({'inputs': {'dpd': entry(dpd)}}))
        # English structural/grammar validation has its own exhaustive real-candidate tests.
        validator = patch('extraction.enrichment.validate_candidate')
        validator.start()
        self.addCleanup(validator.stop)

    def build(self, name='output'):
        return build_enrichment(self.english, self.manifest, self.root / name)

    def test_exact_source_values_coverage_and_repeatability(self):
        before = sha256(self.english / 'pali.db')
        first, second = self.build('one'), self.build('two')
        self.assertEqual(sha256(first / 'pali.db'), sha256(second / 'pali.db'))
        self.assertEqual((first / 'enrichment.json').read_bytes(), (second / 'enrichment.json').read_bytes())
        self.assertEqual(before, sha256(self.english / 'pali.db'))
        layer = validate_enrichment(self.english, self.manifest, first)['layers']['ru']
        self.assertEqual(['accepted', 'accepted', 'empty', 'missing'], [r['status'] for r in layer['records']])
        self.assertEqual([99], layer['unknown_source_ids'])
        self.assertEqual([90], layer['unselected_source_ids'])
        self.assertEqual(1, layer['coverage']['nouns']['translated_primary_lemmas'])
        with closing(sqlite3.connect(first / 'pali.db')) as db:
            self.assertEqual([(1, 'ru', 'слово'), (2, 'ru', 'сырой')], db.execute('SELECT * FROM localized_meanings').fetchall())

    def test_missing_or_changed_source_fails_before_output(self):
        self.source.write_bytes(b'corrupt')
        with self.assertRaises(InputError):
            self.build()
        self.assertFalse((self.root / 'output').exists())
        self.source.unlink()
        with self.assertRaises(InputError):
            self.build()

    def test_wrong_english_checkpoint_and_failed_insert_do_not_change_source(self):
        before = sha256(self.english / 'pali.db')
        self.spec['english_sha256'] = '0' * 64
        self.manifest.write_text(json.dumps(self.spec))
        with self.assertRaises(InputError):
            self.build()
        self.spec['english_sha256'] = before
        self.manifest.write_text(json.dumps(self.spec))
        with patch('extraction.enrichment.SCHEMA', 'not SQL'), self.assertRaises(sqlite3.Error):
            self.build()
        self.assertTrue((self.root / 'output/BUILDING').exists())
        self.assertEqual(before, sha256(self.english / 'pali.db'))
        with self.assertRaises(InputError):
            validate_enrichment(self.english, self.manifest, self.root / 'output')

    def test_consistently_modified_english_or_translation_is_rejected(self):
        output = self.build()
        with closing(sqlite3.connect(output / 'pali.db')) as db, db:
            db.execute("UPDATE localized_meanings SET meaning='incorrect'")
        report = json.loads((output / 'enrichment.json').read_text())
        report['database_sha256'] = sha256(output / 'pali.db')
        (output / 'enrichment.json').write_text(json.dumps(report))
        with self.assertRaisesRegex(InputError, 'Stored meanings'):
            validate_enrichment(self.english, self.manifest, output)
        with closing(sqlite3.connect(output / 'pali.db')) as db, db:
            db.execute('UPDATE nouns SET practice_primary=0')
        with self.assertRaisesRegex(InputError, 'English table'):
            verify_english_unchanged(self.english / 'pali.db', output / 'pali.db')

    def test_spanish_layer_preserves_russian_and_rejects_unpaired_revisions(self):
        dpd = Path(self.spec['dpd']['path'])
        with closing(sqlite3.connect(dpd)) as db, db:
            for column in ('lemma_1', 'pos', 'meaning_1', 'meaning_2'):
                db.execute(f"ALTER TABLE dpd_headwords ADD COLUMN {column} TEXT DEFAULT ''")
            db.execute("UPDATE dpd_headwords SET lemma_1='word ' || id, pos='nt', meaning_1='meaning ' || id")
        self.spec['dpd']['sha256'] = sha256(dpd)
        (self.english / 'candidate.json').write_text(json.dumps({'inputs': {'dpd': self.spec['dpd']}}))
        for language, variable, definition in [('es', 'dpd_ebts_es', 'nt. <b>palabra</b>'),
                                                ('es_english', 'dpd_ebts', 'nt. <b>meaning 1</b>')]:
            path = self.root / (language + '.js')
            path.write_text('let ' + variable + ' = ' + json.dumps({'word 1': definition}) + ';')
            self.spec['sources'][language] = {'path': str(path), 'sha256': sha256(path),
                                             'source': {'origin': 'https://example.invalid/export', 'revision': 'b' * 40}}
        self.manifest.write_text(json.dumps(self.spec))
        result = self.build()
        with closing(sqlite3.connect(result / 'pali.db')) as db:
            self.assertEqual([(1, 'es', 'palabra'), (1, 'ru', 'слово'), (2, 'ru', 'сырой')],
                             db.execute('SELECT * FROM localized_meanings ORDER BY headword_id,language').fetchall())
        self.spec['sources']['es_english']['source']['revision'] = 'c' * 40
        self.manifest.write_text(json.dumps(self.spec))
        with self.assertRaisesRegex(InputError, 'same revision'):
            self.build('unpaired')
        self.assertFalse((self.root / 'unpaired').exists())

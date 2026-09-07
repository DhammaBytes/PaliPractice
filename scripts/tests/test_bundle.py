"""Multilingual promotion requires exact source, gate and artifact identities."""

import contextlib
import io
import json
import shutil
import sqlite3
import tempfile
import unittest
from pathlib import Path

from test_candidate import fixture
from extraction.candidate import build_candidate, validate_candidate
from extraction.enrichment import build_enrichment, dump_json
from extraction.inputs import InputError, read_json, sha256
from check_repeatability import semantic_digest
from semantic_evidence import issue as issue_english, source_identity
from bundle_evidence import issue, verify
from promote_candidate import TARGETS, promote, recover


class BundleTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temp = tempfile.TemporaryDirectory()
        cls.root = Path(cls.temp.name)
        cls.inputs, paths = fixture(cls.root)
        with contextlib.redirect_stdout(io.StringIO()):
            cls.english = build_candidate(cls.inputs, cls.root / 'english')
        ru = cls.root / 'ru.tsv'
        ru.write_text('id\tru_meaning\n2736\tсуществует\n2987\tдорога\n48511\tпробуждённый\n')
        en, es = cls.root / 'en.js', cls.root / 'es.js'
        en.write_text('let dpd_ebts = {"atthi":"pr. exists", "addha":"masc. road", "buddha":"masc. meaning"};')
        es.write_text('let dpd_ebts_es = {"atthi":"pr. existe", "addha":"masc. camino", "buddha":"masc. despierto"};')
        entry = lambda path: {'path': str(path), 'sha256': sha256(path),
                              'source': {'origin': 'test fixture', 'revision': 'a' * 40}}
        cls.translations = cls.root / 'translations.json'
        dump_json(cls.translations, {'schema': 1, 'english_sha256': sha256(cls.english / 'pali.db'),
                                    'dpd': entry(paths['dpd']), 'sources': {'ru': entry(ru), 'es': entry(es), 'es_english': entry(en)}})
        cls.gate = cls.root / 'gate'
        (cls.gate / 'english-repeatability').mkdir(parents=True)
        cls.candidate = cls.gate / 'translation-repeatability/run-1'
        build_enrichment(cls.english, cls.translations, cls.candidate)
        dump_json(cls.gate / 'completion.json', {'status': 'pass', 'findings': 0})
        dump_json(cls.gate / 'manifest.json', {'groups': ['data', 'dotnet', 'desktop']})
        dump_json(cls.gate / 'semantic-sources.json', source_identity())
        dump_json(cls.gate / 'english-repeatability/repeatability.json', {
            'outputs': validate_candidate(cls.english)['outputs'], 'semantic_sha256': semantic_digest(cls.english / 'pali.db')})
        issue_english(cls.english, cls.inputs, cls.gate)
        dump_json(cls.gate / 'translation-repeatability/repeatability.json', {
            'outputs': {p.name: sha256(p) for p in cls.candidate.iterdir()}})
        dump_json(cls.gate / 'dotnet-test.command.json', {'candidate_database': str(cls.candidate / 'pali.db')})
        issue(cls.candidate, cls.english, cls.inputs, cls.translations, cls.gate)
        cls.proof = cls.gate / 'bundle-verification.json'

    @classmethod
    def tearDownClass(cls):
        cls.temp.cleanup()

    def promote(self, destination, after_write=lambda _: None, candidate=None):
        promote(candidate or self.candidate, destination, self.inputs, self.proof, after_write,
                english=self.english, translations=self.translations)

    def test_promotes_exact_database_manifest_and_registries(self):
        target = self.root / 'success'
        self.promote(target)
        recover(target)
        for name, path in TARGETS.items():
            source = 'bundle.json' if name == 'candidate.json' else name
            self.assertEqual((self.candidate / source).read_bytes(), (target / path).read_bytes())
        with contextlib.closing(sqlite3.connect(target / TARGETS['pali.db'])) as db:
            self.assertGreater(db.execute('SELECT count(*) FROM localized_meanings').fetchone()[0], 0)

    def test_every_interrupted_boundary_restores_original_set(self):
        for index, boundary in enumerate(TARGETS):
            with self.subTest(boundary=boundary):
                target = self.root / f'interrupted-{index}'
                before = {}
                for number, (name, path) in enumerate(TARGETS.items()):
                    (target / path).parent.mkdir(parents=True, exist_ok=True)
                    before[path] = f'original {name}'.encode() if number % 2 else None
                    if before[path] is not None:
                        (target / path).write_bytes(before[path])
                def interrupt(name):
                    if name == boundary:
                        raise RuntimeError('interrupted')
                with self.assertRaisesRegex(RuntimeError, 'interrupted'):
                    self.promote(target, interrupt)
                recover(target)
                recover(target)
                for path, data in before.items():
                    self.assertEqual(data, (target / path).read_bytes() if (target / path).exists() else None)

    def test_english_test_run_cannot_issue_multilingual_receipt(self):
        command = self.gate / 'dotnet-test.command.json'
        original = command.read_bytes()
        try:
            dump_json(command, {'candidate_database': str(self.english / 'pali.db')})
            with self.assertRaisesRegex(InputError, 'did not consume'):
                issue(self.candidate, self.english, self.inputs, self.translations, self.gate)
        finally:
            command.write_bytes(original)

    def test_corrupt_translation_or_sidecar_prevents_any_target_write(self):
        for name in ('pali.db', 'practice_registry.json', 'bundle.json'):
            candidate = self.root / ('changed-' + name)
            shutil.copytree(self.candidate, candidate)
            (candidate / name).write_bytes(b'changed artifact')
            target = self.root / ('rejected-' + name)
            with self.assertRaises((InputError, sqlite3.DatabaseError)):
                self.promote(target, candidate=candidate)
            self.assertFalse(target.exists())

    def test_stale_bundle_and_failed_gate_reject_receipt(self):
        completion = self.gate / 'completion.json'
        original = completion.read_bytes()
        try:
            dump_json(completion, {'status': 'fail', 'findings': 1})
            with self.assertRaisesRegex(InputError, 'did not pass'):
                verify(self.candidate, self.english, self.inputs, self.translations, self.proof)
        finally:
            completion.write_bytes(original)
        altered = read_json(self.proof)
        altered['bundle_sha256'] = '0' * 64
        stale = self.root / 'stale.json'
        dump_json(stale, altered)
        with self.assertRaisesRegex(InputError, 'stale'):
            verify(self.candidate, self.english, self.inputs, self.translations, stale)

"""Real candidate promotion interrupted at each file boundary must roll back as a set."""

import contextlib
import io
import json
import shutil
import sqlite3
import tempfile
import unittest
from pathlib import Path

from test_candidate import fixture
from extraction.candidate import ROOT, build_candidate, validate_candidate
from extraction.inputs import InputError, sha256
from promote_candidate import TARGETS, promote, recover
from semantic_evidence import identity
from check_repeatability import semantic_digest


class PromotionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temporary = tempfile.TemporaryDirectory()
        cls.root = Path(cls.temporary.name)
        cls.manifest, _ = fixture(cls.root)
        with contextlib.redirect_stdout(io.StringIO()):
            cls.candidate = build_candidate(cls.manifest, cls.root / 'candidate')
        cls.evidence = cls.root / 'semantic-verification.json'
        cls.evidence.write_text(json.dumps(dict(schema=1, gate_run='test semantic verifier fixture',
            semantic_sha256=semantic_digest(cls.candidate / 'pali.db'), **identity(cls.candidate, cls.manifest))))

    @classmethod
    def tearDownClass(cls):
        cls.temporary.cleanup()

    def test_every_interrupted_file_boundary_restores_old_set_including_missing_files(self):
        for index, boundary in enumerate(TARGETS):
            with self.subTest(boundary=boundary):
                repository = self.root / f'rollback-{index}'
                before = {}
                for number, (name, relative) in enumerate(TARGETS.items()):
                    path = repository / relative
                    path.parent.mkdir(parents=True, exist_ok=True)
                    before[name] = f'old {name}'.encode() if number % 2 == 0 else None
                    if before[name] is not None:
                        path.write_bytes(before[name])

                def interrupt(name):
                    if name == boundary:
                        raise RuntimeError('injected process interruption')

                with self.assertRaisesRegex(RuntimeError, 'interruption'):
                    promote(self.candidate, repository, self.manifest, self.evidence, interrupt)
                recover(repository)
                recover(repository)
                for name, relative in TARGETS.items():
                    path = repository / relative
                    self.assertEqual(before[name], path.read_bytes() if path.exists() else None)

    def test_committed_set_is_retained_on_recovery(self):
        repository = self.root / 'committed'
        promote(self.candidate, repository, self.manifest, self.evidence)
        recover(repository)
        for name, relative in TARGETS.items():
            self.assertEqual((self.candidate / name).read_bytes(), (repository / relative).read_bytes())

    def test_english_production_promotion_is_rejected_before_writes(self):
        with self.assertRaisesRegex(InputError, 'complete multilingual bundle'):
            promote(self.candidate, ROOT, self.manifest, self.evidence)

    def test_missing_or_stale_evidence_prevents_staging(self):
        with self.assertRaises((InputError, OSError)):
            promote(self.candidate, self.root / 'missing', self.manifest, self.root / 'absent.json')
        stale = json.loads(self.evidence.read_text())
        stale['sources'] = {}
        path = self.root / 'stale.json'
        path.write_text(json.dumps(stale))
        with self.assertRaisesRegex(InputError, 'stale'):
            promote(self.candidate, self.root / 'stale', self.manifest, path)
        self.assertFalse((self.root / 'stale').exists())

    def test_consistently_wrong_form_cannot_reuse_semantic_proof(self):
        candidate = self.root / 'wrong-form'
        shutil.copytree(self.candidate, candidate)
        forms = json.loads((candidate / 'primary_forms.json').read_text())
        headword, identifier, form, _ = forms[0]
        forms[0][2] = 'incorrect' + form
        (candidate / 'primary_forms.json').write_text(json.dumps(forms))
        with sqlite3.connect(candidate / 'pali.db') as db:
            for table in ('nouns_irregular_forms', 'verbs_irregular_forms'):
                db.execute(f'UPDATE {table} SET form=? WHERE headword_id=? AND form_id=?',
                           (forms[0][2], headword, identifier))
        corpus = json.loads((candidate / 'corpus_forms.json').read_text())
        for rows in corpus.values():
            for row in rows:
                if row[:2] == [headword, identifier]:
                    row[2] = forms[0][2]
        (candidate / 'corpus_forms.json').write_text(json.dumps(corpus))
        manifest = json.loads((candidate / 'candidate.json').read_text())
        for name in manifest['outputs']:
            manifest['outputs'][name] = sha256(candidate / name)
        (candidate / 'candidate.json').write_text(json.dumps(manifest))
        validate_candidate(candidate)  # Producer consistency alone accepts the wrong spelling.
        with self.assertRaisesRegex(InputError, 'stale'):
            promote(candidate, self.root / 'wrong-target', self.manifest, self.evidence)
        self.assertFalse((self.root / 'wrong-target').exists())

    def test_external_edits_block_recovery_without_overwriting_them(self):
        repository = self.root / 'external'

        def interrupt(_):
            raise RuntimeError('interrupted')

        with self.assertRaises(RuntimeError):
            promote(self.candidate, repository, self.manifest, self.evidence, interrupt)
        target = repository / TARGETS['pali.db']
        target.write_bytes(b'user edit')
        with self.assertRaisesRegex(InputError, 'edited externally'):
            recover(repository)
        self.assertEqual(b'user edit', target.read_bytes())

    def test_previous_six_file_journal_remains_recoverable(self):
        repository = self.root / 'previous-journal'

        def interrupt(_):
            raise RuntimeError('interrupted')

        with self.assertRaises(RuntimeError):
            promote(self.candidate, repository, self.manifest, self.evidence, interrupt)
        path = repository / '.local/promotion/journal.json'
        journal = json.loads(path.read_text())
        del journal['files']['primary_forms.json']
        del journal['files']['corpus_forms.json']
        path.write_text(json.dumps(journal))
        recover(repository)
        self.assertEqual('rolled_back', json.loads(path.read_text())['phase'])
        self.assertFalse((repository / TARGETS['pali.db']).exists())

    def test_promotion_cannot_overwrite_its_pinned_registry_input(self):
        repository = self.root / 'overlap'
        target = repository / TARGETS['lemma_registry.json']
        target.parent.mkdir(parents=True)
        manifest = json.loads(self.manifest.read_text())
        original = Path(manifest['inputs']['registry']['path']).read_bytes()
        target.write_bytes(original)
        manifest['inputs']['registry']['path'] = str(target)
        inputs = self.root / 'overlap-inputs.json'
        inputs.write_text(json.dumps(manifest))
        with self.assertRaisesRegex(InputError, 'overlap output targets'):
            promote(self.candidate, repository, inputs, self.evidence)
        self.assertEqual(original, target.read_bytes())
        self.assertFalse((repository / '.local/promotion').exists())


if __name__ == '__main__':
    unittest.main()

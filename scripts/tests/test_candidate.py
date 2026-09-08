"""Candidate tests run explicitly; the gate does not generate databases until M5."""

import contextlib
import io
import json
import sqlite3
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from extraction.candidate import build_candidate, validate_candidate, OUTPUTS
from extraction.identity import BASELINE, historical_practice_registry, load_baseline
from extraction.inputs import InputError, configuration, load_manifest, sha256
from extract_nouns_and_verbs import NounVerbExtractor
from db.models import Base, DpdHeadword, InflectionTemplates
from sqlalchemy import create_engine
from sqlalchemy.orm import Session


def fixture(root):
    database = root / 'dpd.db'
    engine = create_engine('sqlite:///' + str(database))
    Base.metadata.create_all(engine)
    with Session(engine) as session:
        session.add_all([
            InflectionTemplates(pattern='a masc', data=json.dumps([
                [[''], ['masc sg'], [''], ['masc pl'], ['']],
                [['nom'], ['o'], ['masc nom sg'], ['ā'], ['masc nom pl']],
            ])),
            InflectionTemplates(pattern='ati pr', data=json.dumps([
                [[''], ['sg'], [''], ['pl'], ['']],
                [['pr 3rd'], ['ati'], ['pr 3rd sg'], ['anti'], ['pr 3rd pl']],
            ])),
        ])
        session.add_all([
            InflectionTemplates(pattern='addha masc', data=json.dumps([
                [[''], ['masc sg'], ['']],
                [['nom'], ['addhā'], ['masc nom sg']],
            ])),
            InflectionTemplates(pattern='atthi pr', data=json.dumps([
                [[''], ['sg'], ['']],
                [['pr 3rd'], ['atthi'], ['pr 3rd sg']],
            ])),
            DpdHeadword(id=2987, lemma_1='addha', stem='!', pos='masc',
                        pattern='addha masc', meaning_1='road', sutta_1='DN1', ebt_count=200,
                        inflections_html="<td title='masc nom sg'>addh<b>ā</b></td>"),
            DpdHeadword(id=2736, lemma_1='atthi', stem='!', pos='pr',
                        pattern='atthi pr', meaning_1='exists', sutta_1='DN1', ebt_count=200,
                        inflections_html="<td title='pr 3rd sg'><b>atthi</b></td>"),
        ])
        for identifier, lemma, stem, pos, pattern in [
            (34626, 'dhamma', 'dhamm', 'masc', 'a masc'),
            (48511, 'buddha', 'buddh', 'masc', 'a masc'),
            (49534, 'bhavati', 'bhav', 'pr', 'ati pr'),
        ]:
            session.add(DpdHeadword(id=identifier, lemma_1=lemma, stem=stem,
                                    pos=pos, pattern=pattern, meaning_1='meaning',
                                    sutta_1='DN1', ebt_count=100))
        session.commit()
    engine.dispose()
    (root / 'registry.json').write_bytes((BASELINE / 'lemma_registry.json').read_bytes())
    configs = Path(__file__).resolve().parents[1] / 'configs'
    (root / 'practice_registry.json').write_text(json.dumps(historical_practice_registry(load_baseline()[1])))
    (root / 'paradigm_corrections.json').write_bytes((configs / 'paradigm_corrections.json').read_bytes())
    (root / 'adjustments.json').write_text('{}')
    paths = {'dpd': database, 'registry': root / 'registry.json',
             'adjustments': root / 'adjustments.json',
             'practice_registry': root / 'practice_registry.json',
             'corrections': root / 'paradigm_corrections.json'}
    for name in ('cst', 'bjt', 'sya', 'sc'):
        path = root / f'{name}.json'
        path.write_text(json.dumps(['buddho', 'buddhā', 'dhammo', 'bhavati', 'bhavanti', 'addhā', 'atthi']))
        paths[name] = path
    manifest = {'schema': 2, 'database_version': 2026090701,
                'configuration': configuration(2, 2),
                'corpus_generation': {'recipe': 'explicit test fixture', 'revisions': {'fixture': '1'}},
                'inputs': {name: {'path': str(path), 'sha256': sha256(path),
                                  'source': {'origin': 'test fixture', 'revision': 'sha256:' + sha256(path)}}
                           for name, path in paths.items()}}
    manifest_path = root / 'inputs.json'
    manifest_path.write_text(json.dumps(manifest))
    return manifest_path, paths


class CandidateTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.manifest, self.paths = fixture(self.root)

    def build(self, name):
        with contextlib.redirect_stdout(io.StringIO()):
            return build_candidate(self.manifest, self.root / name)

    def test_offline_repeatable_build_preserves_inputs_and_uses_lexical_ties(self):
        before = {name: sha256(path) for name, path in self.paths.items()}
        with mock.patch('urllib.request.urlopen', side_effect=AssertionError('Network forbidden')):
            first = self.build('first')
            second = self.build('second')
        self.assertEqual(before, {name: sha256(path) for name, path in self.paths.items()})
        for name in (*OUTPUTS, 'candidate.json'):
            self.assertEqual((first / name).read_bytes(), (second / name).read_bytes(), name)
        validate_candidate(first)
        with contextlib.closing(sqlite3.connect(first / 'pali.db')) as connection:
            self.assertEqual([('buddha', 10060)], connection.execute('select lemma,lemma_id from nouns where lemma != \'addha\'').fetchall())
            self.assertEqual([('',)], connection.execute('select distinct meaning_ru from nouns_details').fetchall())
        self.assertEqual((BASELINE / 'lemma_registry.json').read_bytes(), self.paths['registry'].read_bytes())

    def test_existing_candidate_is_never_overwritten(self):
        first = self.build('first')
        digest = sha256(first / 'pali.db')
        with self.assertRaises(FileExistsError):
            self.build('first')
        self.assertEqual(digest, sha256(first / 'pali.db'))

    def test_interruption_leaves_no_completed_candidate_or_input_mutation(self):
        before = sha256(self.paths['registry'])
        with mock.patch.object(NounVerbExtractor, 'extract_and_save', side_effect=KeyboardInterrupt):
            with self.assertRaises(KeyboardInterrupt):
                self.build('failed')
        with self.assertRaisesRegex(InputError, 'interrupted'):
            validate_candidate(self.root / 'failed')
        self.assertFalse((self.root / 'failed' / 'candidate.json').exists())
        self.assertEqual(before, sha256(self.paths['registry']))

    def test_modified_candidate_fails_validation(self):
        first = self.build('first')
        (first / 'pali.version.txt').write_text('1')
        with self.assertRaisesRegex(InputError, 'checksum'):
            validate_candidate(first)

    def test_rehashed_selection_tampering_still_fails_identity_validation(self):
        candidate = self.build('tampered')
        with contextlib.closing(sqlite3.connect(candidate / 'pali.db')) as db:
            db.execute('UPDATE nouns SET practice_primary = 0')
            db.commit()
        manifest = json.loads((candidate / 'candidate.json').read_text())
        manifest['outputs']['pali.db'] = sha256(candidate / 'pali.db')
        (candidate / 'candidate.json').write_text(json.dumps(manifest))
        with self.assertRaisesRegex(InputError, 'Exactly one'):
            validate_candidate(candidate)

    def test_rehashed_headword_scope_corruption_is_rejected(self):
        candidate = self.build('bad-scope')
        with contextlib.closing(sqlite3.connect(candidate / 'pali.db')) as db:
            db.execute('UPDATE nouns_corpus_forms SET headword_id = 999999')
            db.commit()
        manifest = json.loads((candidate / 'candidate.json').read_text())
        manifest['outputs']['pali.db'] = sha256(candidate / 'pali.db')
        (candidate / 'candidate.json').write_text(json.dumps(manifest))
        with self.assertRaisesRegex(InputError, 'headword/grammar'):
            validate_candidate(candidate)

    def test_rehashed_primary_contract_with_invalid_ending_is_rejected(self):
        candidate = self.build('bad-ending')
        primary = json.loads((candidate / 'primary_forms.json').read_text())
        primary[0][1] = primary[0][1] - primary[0][1] % 10 + 9
        (candidate / 'primary_forms.json').write_text(json.dumps(primary))
        manifest = json.loads((candidate / 'candidate.json').read_text())
        manifest['outputs']['primary_forms.json'] = sha256(candidate / 'primary_forms.json')
        (candidate / 'candidate.json').write_text(json.dumps(manifest))
        with self.assertRaisesRegex(InputError, 'primary grammar'):
            validate_candidate(candidate)

    def test_compact_evidence_and_database_must_have_identical_keys(self):
        candidate = self.build('missing-corpus-evidence')
        path = candidate / 'corpus_forms.json'
        corpus = json.loads(path.read_text())
        corpus['nouns'].pop()
        path.write_text(json.dumps(corpus))
        manifest = json.loads((candidate / 'candidate.json').read_text())
        manifest['outputs']['corpus_forms.json'] = sha256(path)
        (candidate / 'candidate.json').write_text(json.dumps(manifest))
        with self.assertRaisesRegex(InputError, 'differs from spelling evidence'):
            validate_candidate(candidate)

    def test_compact_evidence_spelling_must_match_primary_template(self):
        candidate = self.build('wrong-corpus-spelling')
        path = candidate / 'corpus_forms.json'
        corpus = json.loads(path.read_text())
        primary = {(row[0], row[1]) for row in json.loads((candidate / 'primary_forms.json').read_text())}
        row = next(row for rows in corpus.values() for row in rows if tuple(row[:2]) in primary)
        row[2] += 'wrong'
        path.write_text(json.dumps(corpus))
        manifest = json.loads((candidate / 'candidate.json').read_text())
        manifest['outputs']['corpus_forms.json'] = sha256(path)
        (candidate / 'candidate.json').write_text(json.dumps(manifest))
        with self.assertRaisesRegex(InputError, 'disagree with template contract'):
            validate_candidate(candidate)

    def test_bad_checksum_fails_before_candidate_directory_creation(self):
        self.paths['cst'].write_text('[]')
        with self.assertRaisesRegex(InputError, 'Checksum'):
            self.build('bad')
        self.assertFalse((self.root / 'bad').exists())

    def test_missing_and_invalid_corpora_fail(self):
        for content in ('[]', '{}', '[null]', '[""]', '{broken'):
            with self.subTest(content=content):
                self.paths['cst'].write_text(content)
                manifest = json.loads(self.manifest.read_text())
                manifest['inputs']['cst']['sha256'] = sha256(self.paths['cst'])
                self.manifest.write_text(json.dumps(manifest))
                with self.assertRaises(InputError):
                    self.build('bad-' + str(len(list(self.root.iterdir()))))
        self.paths['cst'].unlink()
        with self.assertRaisesRegex(InputError, 'Missing'):
            load_manifest(self.manifest)

    def test_missing_registry_and_malformed_manifest_are_rejected(self):
        self.paths['registry'].unlink()
        with self.assertRaisesRegex(InputError, 'Missing'):
            self.build('missing-registry')
        self.manifest.write_text('{"schema":1,"schema":1}')
        with self.assertRaisesRegex(InputError, 'Duplicate'):
            load_manifest(self.manifest)
        self.manifest.write_text('[]')
        with self.assertRaisesRegex(InputError, 'fields'):
            load_manifest(self.manifest)

    def test_manifest_replacement_during_build_cannot_issue_completion(self):
        original = NounVerbExtractor.extract_and_save

        def replace_manifest(extractor):
            original(extractor)
            manifest = json.loads(self.manifest.read_text())
            manifest['database_version'] += 1
            self.manifest.write_text(json.dumps(manifest))

        with mock.patch.object(NounVerbExtractor, 'extract_and_save', replace_manifest):
            with self.assertRaisesRegex(InputError, 'changed during'):
                self.build('raced')
        self.assertFalse((self.root / 'raced' / 'candidate.json').exists())

    def test_fresh_process_does_not_read_the_bundled_version(self):
        scripts = Path(__file__).resolve().parents[1]
        code = """
import sys
from pathlib import Path
scripts, manifest, output = map(Path, sys.argv[1:])
blocked = scripts.parent / 'PaliPractice/PaliPractice/Data/pali.version.txt'
original = Path.read_text
def guarded(path, *args, **kwargs):
    if path.resolve() == blocked.resolve():
        raise AssertionError('Undeclared production input')
    return original(path, *args, **kwargs)
Path.read_text = guarded
sys.path.insert(0, str(scripts))
from extraction.candidate import build_candidate
build_candidate(manifest, output)
"""
        completed = subprocess.run(
            [sys.executable, '-c', code, str(scripts), str(self.manifest), str(self.root / 'fresh')],
            cwd=self.root, text=True, capture_output=True,
        )
        self.assertEqual(0, completed.returncode, completed.stderr)

    def test_build_is_independent_of_working_directory(self):
        original = Path.cwd()
        try:
            import os
            os.chdir(self.root)
            candidate = self.build('outside')
            validate_candidate(candidate)
        finally:
            os.chdir(original)

    def test_nonempty_wal_and_boolean_versions_are_rejected(self):
        wal = Path(str(self.paths['dpd']) + '-wal')
        wal.write_bytes(b'not checkpointed')
        with self.assertRaisesRegex(InputError, 'WAL'):
            load_manifest(self.manifest)
        wal.unlink()
        manifest = json.loads(self.manifest.read_text())
        manifest['database_version'] = True
        self.manifest.write_text(json.dumps(manifest))
        with self.assertRaisesRegex(InputError, 'integer'):
            load_manifest(self.manifest)


if __name__ == '__main__':
    unittest.main()

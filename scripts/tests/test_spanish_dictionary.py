"""Historical identity joins, explicit adaptations, and stale-review rejection."""

import csv
import io
import json
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from build_spanish_identity import build
from extraction.inputs import InputError, sha256
from extraction.spanish_dictionary import dictionary_records


class SpanishDictionaryTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.en, self.es = self.root / 'en.js', self.root / 'es.js'
        self.en.write_text('let dpd_ebts = ' + json.dumps({'word 1': 'pr. <b>says; speaks</b>',
                                                       'word 2': 'pr. <b>calls</b>'}))
        self.es.write_text('let dpd_ebts_es = ' + json.dumps({'word 1': 'pr. <b>dice; habla</b>',
                                                          'word 2': 'pr. <b>llama</b>'}))
        stream = io.StringIO(newline='')
        csv.writer(stream, delimiter='\t').writerows([
            ['id', 'lemma_1', 'pos', 'meaning_1', 'meaning_2'],
            [10, 'word 1', 'pr', 'says; speaks', ''], [20, 'word 2', 'pr', 'calls', '']])
        # Split mid-row, as in the historical DPD's chunked TSV backups.
        text = stream.getvalue()
        self.parts = [self.root / 'part1.tsv', self.root / 'part2.tsv']
        cut = text.index('speaks') + 2
        for path, data in zip(self.parts, (text[:cut], text[cut:])):
            path.write_bytes(data.encode())
        self.manifest = self.root / 'inputs.json'
        self.spec = {'schema': 1, 'english': self.entry(self.en), 'spanish': self.entry(self.es),
                     'headwords': [self.entry(p) for p in self.parts]}
        self.manifest.write_text(json.dumps(self.spec))
        self.identity = self.root / 'identity.json'
        build(self.manifest, self.identity)
        self.reviews = self.root / 'reviews.json'
        self.review_spec = {'schema': 1, 'identity_sha256': sha256(self.identity),
                            'dpd_sha256': 'd' * 64, 'entries': []}
        self.targets = [(10, 'word 2', 'pr', 'says; speaks', ''),
                        (20, 'word 3', 'pr', 'calls', ''), (30, 'word 4', 'pr', 'speaks', '')]
        self.words = [dict(headword_id=i, kind='verbs', lemma_id=1, primary=i == 10) for i in (10, 20, 30)]

    def entry(self, path):
        return {'path': str(path), 'sha256': sha256(path),
                'source': {'origin': 'https://example.invalid/pinned', 'revision': 'a' * 40}}

    def run_mapping(self):
        self.reviews.write_text(json.dumps(self.review_spec))
        return dictionary_records(self.words, self.targets, self.en, self.es,
                                  self.identity, self.reviews, 'd' * 64)[0]

    def adaptation(self):
        return {'headword_id': 30, 'target_key': 'word 4', 'target_pos': 'pr',
                'target_english': 'speaks', 'source_keys': ['word 1'], 'meaning': 'habla',
                'mode': 'adapted', 'reason': 'Select the speaking gloss for the new split sense.',
                'reviewer': 'reviewer-a; verified by reviewer-b'}

    def test_renumbered_ids_do_not_follow_reused_current_keys(self):
        rows = self.run_mapping()
        self.assertEqual(['dice; habla', 'llama', ''], [r['meaning'] for r in rows])
        self.assertEqual('word 1', rows[0]['source_key'])
        self.assertEqual('word 2', rows[0]['current_key'])
        self.assertEqual('missing_historical_translation', rows[2]['status'])

    def test_changed_meaning_requires_review_and_new_split_is_explicit(self):
        self.targets[0] = (10, 'word 2', 'pr', 'describes', '')
        self.review_spec['entries'] = [self.adaptation()]
        rows = self.run_mapping()
        self.assertEqual('meaning_drift', rows[0]['status'])
        self.assertEqual('', rows[0]['meaning'])
        self.assertEqual('accepted', rows[2]['status'])
        self.assertEqual('habla', rows[2]['meaning'])
        self.assertEqual('adapted', rows[2]['mapping'])

    def test_stale_identity_dpd_and_target_are_rejected(self):
        for key in ('identity_sha256', 'dpd_sha256'):
            before = self.review_spec[key]
            self.review_spec[key] = '0' * 64
            with self.assertRaisesRegex(InputError, 'different dictionary inputs'):
                self.run_mapping()
            self.review_spec[key] = before
        self.review_spec['entries'] = [self.adaptation() | {'target_english': 'says'}]
        with self.assertRaisesRegex(InputError, 'Stale Spanish review target'):
            self.run_mapping()
        self.es.write_text(self.es.read_text().replace('habla', 'conversa'))
        with self.assertRaisesRegex(InputError, 'different source exports'):
            self.run_mapping()

    def test_invalid_review_provenance_duplicates_and_false_verbatim_fail(self):
        changes = [{'mode': 'verbatim'}, {'source_keys': []}, {'source_keys': ['unknown']},
                   {'source_keys': ['word 1', 'word 1']}, {'reason': ''}, {'reviewer': ''},
                   {'meaning': '<b>habla</b>'}, {'mode': 'unresolved'}, {'headword_id': True}]
        for change in changes:
            self.review_spec['entries'] = [self.adaptation() | change]
            with self.subTest(change=change), self.assertRaises(InputError):
                self.run_mapping()
        self.review_spec['entries'] = [self.adaptation(), self.adaptation()]
        with self.assertRaisesRegex(InputError, 'Duplicate Spanish review target'):
            self.run_mapping()

    def test_unresolved_decisions_remain_visible(self):
        self.review_spec['entries'] = [self.adaptation() | {'mode': 'unresolved', 'meaning': ''}]
        row = self.run_mapping()[2]
        self.assertEqual('unresolved_review', row['status'])
        self.assertEqual(['word 1'], row['source_keys'])
        self.assertTrue(row['reason'])

    def test_local_translation_is_explicit_and_does_not_claim_source_authorship(self):
        self.review_spec['entries'] = [self.adaptation() | {'mode': 'translated', 'source_keys': []}]
        row = self.run_mapping()[2]
        self.assertEqual('accepted', row['status'])
        self.assertEqual('translated', row['mapping'])
        self.assertEqual([], row['source_keys'])
        self.review_spec['entries'][0]['source_keys'] = ['word 1']
        with self.assertRaisesRegex(InputError, 'must not claim upstream'):
            self.run_mapping()

    def test_spanish_pos_errors_stay_local_to_the_affected_source_sense(self):
        self.es.write_text(self.es.read_text().replace('pr. <b>llama', 'nt. <b>llama'))
        identity = json.loads(self.identity.read_text())
        identity['spanish_sha256'] = sha256(self.es)
        self.identity.write_text(json.dumps(identity))
        self.review_spec['identity_sha256'] = sha256(self.identity)
        rows = self.run_mapping()
        self.assertEqual('dice; habla', rows[0]['meaning'])
        self.assertEqual('pos_mismatch', rows[1]['status'])
        self.assertEqual('', rows[1]['meaning'])

    def test_bridge_reproduces_and_rejects_changed_or_ambiguous_history(self):
        other = self.root / 'identity-again.json'
        build(self.manifest, other)
        self.assertEqual(self.identity.read_bytes(), other.read_bytes())
        with self.assertRaises(FileExistsError):
            build(self.manifest, other)
        self.parts[0].write_bytes(self.parts[0].read_bytes() + b'changed')
        with self.assertRaisesRegex(InputError, 'Checksum mismatch'):
            build(self.manifest, self.root / 'bad.json')

    def test_bridge_validates_historical_english_not_just_keys(self):
        self.en.write_text(self.en.read_text().replace('says; speaks', 'describes'))
        self.spec['english'] = self.entry(self.en)
        self.manifest.write_text(json.dumps(self.spec))
        with self.assertRaisesRegex(InputError, 'Historical English correspondence failed'):
            build(self.manifest, self.root / 'bad.json')

    def test_identity_cannot_silently_omit_or_reassign_multiple_keys_to_one_id(self):
        original = json.loads(self.identity.read_text())
        for ids in ({'word 1': 10}, {'word 1': 10, 'word 2': 10}):
            self.identity.write_text(json.dumps(original | {'headword_ids': ids}))
            with self.assertRaises(InputError):
                self.run_mapping()

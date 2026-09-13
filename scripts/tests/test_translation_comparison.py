"""Promotion must surface translation regressions even when enrichment is valid."""

import contextlib
import copy
import json
import sqlite3
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from compare_translations import compare, report_digest, sense_changes, source_changes, verify_decisions
from extraction.inputs import InputError, sha256
from promote_candidate import promote


def state():
    return {'meanings': {1: {'kind': 'verbs', 'word': 'vadeti', 'english': 'speaks'}},
            'translations': {(1, 'es'): 'habla', (1, 'ru'): 'говорит'},
            'manifest': {'english': {'inputs': {'dpd': {'sha256': 'old'}}},
                         'translations': {lang: {'source': {'sha256': 'source'},
                                                'review_source': {'sha256': 'review'},
                                                'override_source': {'sha256': 'override'}}
                                          for lang in ('es', 'ru')}}}


class TranslationComparisonTests(unittest.TestCase):
    def test_losses_cannot_be_hidden_by_added_translations(self):
        old, new = state(), state()
        del new['translations'][1, 'es']
        new['meanings'][2] = dict(new['meanings'][1], word='new')
        new['translations'][2, 'es'] = 'nuevo'
        changes = sense_changes(old, new)
        self.assertEqual({'translation_lost:es:1', 'new_gap:ru:2'},
                         {row['key'] for row in changes if row['requires_review']})

    def test_english_change_requires_both_language_reviews_even_when_translation_changes(self):
        old, new = state(), state()
        new['meanings'][1]['english'] = 'describes'
        new['translations'][1, 'es'] = 'describe'
        self.assertEqual({'english_target_changed:es:1', 'english_target_changed:ru:1'},
                         {r['key'] for r in sense_changes(old, new) if r['requires_review']})

    def test_unchanged_gaps_and_removed_selection_are_not_translation_losses(self):
        old, new = state(), state()
        old['translations'].clear()
        new['translations'].clear()
        self.assertEqual([], sense_changes(old, new))
        new['meanings'].clear()
        self.assertFalse(any(r['requires_review'] for r in sense_changes(old, new)))

    def test_mapping_omission_and_source_drift_require_review(self):
        old, new = state(), state()
        del new['manifest']['translations']['es']['review_source']
        new['manifest']['english']['inputs']['dpd']['sha256'] = 'new'
        new['manifest']['translations']['ru']['source']['sha256'] = 'updated'
        keys = {r['key'] for r in source_changes(old, new)}
        self.assertEqual({'mapping_input_removed:es:review_source', 'dpd_baseline_changed:es:layer',
                          'dpd_baseline_changed:ru:layer', 'translation_source_changed:ru:layer',
                          'overrides_need_resync:ru:layer'}, keys)

    def test_decisions_are_complete_unique_and_bound_to_both_bundles(self):
        report = {'baseline': 'old', 'candidate': 'new', 'changes': [
            {'key': 'translation_lost:es:1', 'requires_review': True}]}
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'decisions.json'
            good = {'schema': 1, 'comparison_sha256': report_digest(report), 'decisions': [
                {'key': 'translation_lost:es:1', 'reason': 'Source sense withdrawn after review', 'reviewer': 'reviewer'}]}
            with self.assertRaisesRegex(InputError, 'requires 1 decisions'):
                verify_decisions(report, None)
            for mutation in ('missing', 'duplicate', 'unknown', 'blank', 'stale'):
                bad = copy.deepcopy(good)
                if mutation == 'missing': bad['decisions'] = []
                if mutation == 'duplicate': bad['decisions'] *= 2
                if mutation == 'unknown': bad['decisions'][0]['key'] = 'other'
                if mutation == 'blank': bad['decisions'][0]['reason'] = ' '
                if mutation == 'stale': bad['comparison_sha256'] = 'old'
                path.write_text(json.dumps(bad))
                with self.subTest(mutation=mutation), self.assertRaises(InputError):
                    verify_decisions(report, path)
            path.write_text(json.dumps(good))
            self.assertEqual(good, verify_decisions(report, path))
            with self.assertRaisesRegex(InputError, 'Stale'):
                verify_decisions(dict(report, baseline='changed'), path)
            with self.assertRaisesRegex(InputError, 'Stale'):
                verify_decisions(dict(report, candidate='changed'), path)
        self.assertEqual([], verify_decisions({'changes': []}, None)['decisions'])

    def test_real_comparison_and_promotion_rejection_before_staging(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            old = root / 'PaliPractice/PaliPractice/Data'
            new = root / 'candidate'
            for folder, name, translated in ((old, 'pali.manifest.json', True), (new, 'bundle.json', False)):
                folder.mkdir(parents=True)
                with contextlib.closing(sqlite3.connect(folder / 'pali.db')) as db, db:
                    for kind in ('nouns', 'verbs'):
                        db.execute(f'CREATE TABLE {kind}_details (id INTEGER, word TEXT, meaning TEXT)')
                    db.execute("INSERT INTO verbs_details VALUES (1, 'vadeti', 'speaks')")
                    db.execute('CREATE TABLE localized_meanings (headword_id INTEGER, language TEXT, meaning TEXT)')
                    if translated: db.execute("INSERT INTO localized_meanings VALUES (1, 'es', 'habla')")
                manifest = state()['manifest']
                manifest['outputs'] = {'pali.db': sha256(folder / 'pali.db')}
                (folder / name).write_text(json.dumps(manifest))
            report = compare(root, new)
            self.assertEqual(['translation_lost:es:1'], [r['key'] for r in report['changes']])
            before = (old / 'pali.db').read_bytes()
            # Isolate pre-existing receipt/registry/version policies; exercise the real
            # destination lock and new comparison in the actual promotion entrypoint.
            with patch('promote_candidate.promotion_inputs', return_value=({}, 'bundle.json')), \
                 patch('promote_candidate.protect_input_paths'), \
                 patch('promote_candidate.protect_destination_identities'), \
                 patch('promote_candidate.protect_destination_version'):
                with self.assertRaisesRegex(InputError, 'requires 1 decisions'):
                    promote(new, root, root / 'inputs', root / 'receipt')
            self.assertEqual(before, (old / 'pali.db').read_bytes())
            self.assertEqual(['lock'], [p.name for p in (root / '.local/promotion').iterdir()])
            decisions = root / 'decisions.json'
            decisions.write_text(json.dumps({'schema': 1, 'comparison_sha256': report_digest(report),
                'decisions': [{'key': 'translation_lost:es:1', 'reason': 'Reviewed withdrawn translation',
                               'reviewer': 'test reviewer'}]}))
            targets = {'pali.db': 'PaliPractice/PaliPractice/Data/pali.db',
                       'candidate.json': 'PaliPractice/PaliPractice/Data/pali.manifest.json'}
            with patch('promote_candidate.promotion_inputs', return_value=({'pali.db': sha256(new / 'pali.db')}, 'bundle.json')), \
                 patch('promote_candidate.protect_input_paths'), \
                 patch('promote_candidate.protect_destination_identities'), \
                 patch('promote_candidate.protect_destination_version'), \
                 patch('promote_candidate.TARGETS', targets):
                promote(new, root, root / 'inputs', root / 'receipt', translation_decisions=decisions)
            self.assertEqual((new / 'pali.db').read_bytes(), (old / 'pali.db').read_bytes())
            audit = root / '.local/promotion/translation-reviews' / (report_digest(report) + '.json')
            self.assertEqual(report, json.loads(audit.read_text())['comparison'])
            self.assertEqual([], compare(root, new)['changes'])
            (old / 'pali.manifest.json').write_text('{}')
            with self.assertRaisesRegex(InputError, 'manifest matching'):
                compare(root, new)

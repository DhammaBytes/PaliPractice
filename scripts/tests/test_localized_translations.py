"""Localized ordering, missing rows, and pinned headword identity."""

import json
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from extraction.inputs import InputError
from extraction.localized_translations import apply_overrides, load_overrides, make_first


class LocalizedTranslationTests(unittest.TestCase):
    def test_move_prepend_order_and_idempotence(self):
        cases = [
            ('sufrimiento; malestar; estrés', ['malestar'], 'malestar; sufrimiento; estrés'),
            ('origen; fuente', ['surgimiento', 'origen'], 'surgimiento; origen; fuente'),
            ('стресс; боль; напряжение', ['напряжение', 'стресс'], 'напряжение; стресс; боль'),
            ('Conciencia; mente; conciencia', ['conciencia'], 'conciencia; mente'),
            ('', ['ecuanimidad'], 'ecuanimidad'),
        ]
        for source, preferred, expected in cases:
            with self.subTest(source=source):
                self.assertEqual(expected, make_first(source, preferred))
                self.assertEqual(expected, make_first(expected, preferred))

    def test_missing_and_rejected_sources_get_only_explicit_translation(self):
        for status in ('missing', 'empty', 'meaning_drift', 'unresolved_review'):
            with self.subTest(status=status):
                original = {'headword_id': 59985, 'status': status, 'meaning': 'untrusted meaning'}
                untouched = {'headword_id': 2, 'status': 'accepted', 'meaning': 'otra'}
                result = apply_overrides([original, untouched], {59985: {'preferred': ['surgimiento', 'origen']}})
                self.assertEqual('surgimiento; origen', result[0]['meaning'])
                self.assertEqual('accepted', result[0]['status'])
                self.assertEqual(status, result[0]['override']['source_status'])
                self.assertEqual('untrusted meaning', result[0]['override']['source_meaning'])
                self.assertEqual(untouched, result[1])
                self.assertEqual(status, original['status'])

    def test_invalid_and_stale_configuration_fails(self):
        valid = {'schema': 1, 'primary': {'es': {'59985': {
            'lemma_1': 'samudaya 1', 'preferred': ['surgimiento', 'origen']}}}}
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'overrides.json'
            def read(data, words=None, languages=None):
                path.write_text(json.dumps(data))
                return load_overrides(path, words if words is not None else {59985: ('samudaya 1', 'origin')},
                                      languages if languages is not None else {'es'})
            self.assertEqual(valid['primary']['es']['59985'], read(valid)['es'][59985])
            for words, languages in [({}, {'es'}), ({59985: ('samudaya 2', 'origin')}, {'es'}),
                                      ({59985: ('samudaya 1', 'origin')}, {'ru'})]:
                with self.subTest(words=words, languages=languages), self.assertRaises(InputError):
                    read(valid, words, languages)
            for preferred in ([], 'word', [''], ['a; b'], [' word'], ['a', 'A'], [None], ['<b>x</b>']):
                invalid = json.loads(json.dumps(valid))
                invalid['primary']['es']['59985']['preferred'] = preferred
                with self.subTest(preferred=preferred), self.assertRaises(InputError):
                    read(invalid)
            path.write_text('{"schema":1,"schema":1,"primary":{}}')
            with self.assertRaises(InputError):
                load_overrides(path, {}, {'es'})

    def test_reviewed_replacement_removes_obsolete_gloss_and_guards_resync(self):
        entry = {'lemma_1': 'rasa 5', 'meaning_1': 'nutrients',
                 'source_meaning': 'питательные вещества; удобрение',
                 'replacement': 'питательные вещества', 'reason': 'Fertilizer removed from this sense.'}
        data = {'schema': 1, 'primary': {}, 'replace': {'ru': {'54689': entry}}}
        record = {'headword_id': 54689, 'status': 'accepted', 'meaning': entry['source_meaning']}
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'overrides.json'
            path.write_text(json.dumps(data))
            overrides = load_overrides(path, {54689: ('rasa 5', 'nutrients')}, {'ru'})
            result = apply_overrides([record], overrides['ru'])[0]
            self.assertEqual('питательные вещества', result['meaning'])
            self.assertEqual(entry['source_meaning'], result['override']['source_meaning'])
            self.assertEqual(entry['source_meaning'], record['meaning'])
            with self.assertRaisesRegex(InputError, 'English target'):
                load_overrides(path, {54689: ('rasa 5', 'flavour')}, {'ru'})
            with self.assertRaisesRegex(InputError, 'replacement source'):
                apply_overrides([{**record, 'meaning': 'changed upstream'}], overrides['ru'])
            data['primary'] = {'ru': {'54689': {'lemma_1': 'rasa 5', 'preferred': ['другое']}}}
            path.write_text(json.dumps(data))
            with self.assertRaisesRegex(InputError, 'Conflicting'):
                load_overrides(path, {54689: ('rasa 5', 'nutrients')}, {'ru'})

    def test_reviewed_missing_translation_becomes_accepted(self):
        entry = {'replacement': 'возникающее явление', 'source_meaning': ''}
        result = apply_overrides([{'headword_id': 89243, 'status': 'missing', 'meaning': ''}], {89243: entry})
        self.assertEqual('accepted', result[0]['status'])
        self.assertEqual('возникающее явление', result[0]['meaning'])
        self.assertEqual('missing', result[0]['override']['source_status'])

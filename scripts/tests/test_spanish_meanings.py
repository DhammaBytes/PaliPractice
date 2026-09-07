"""Known export grammar and exact Spanish/English/DPD sense correspondence."""

from pathlib import Path
import tempfile
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from extraction.inputs import InputError
from extraction.spanish_meanings import load_assignment, map_sense, parse_definition, source_index, spanish_records


class SpanishParserTests(unittest.TestCase):
    def test_actual_bold_unbold_literal_and_unicode_formats(self):
        cases = {
            'nt. <b>acción; acto; hacer</b> [√kar + ma]': ('nt', 'acción; acto; hacer'),
            'adj. no duro []': ('adj', 'no duro'),
            'fem. suavidad; lisura; lit. no dureza []': ('fem', 'suavidad; lisura'),
            'pr. <b>hay; existe</b> [√as + ti]': ('pr', 'hay; existe'),
            'pron. <b>nada</b>; lit. no algo [na + ka + ci]': ('pron', 'nada'),
            'masc. <b>uno &amp; dos; pa\u0301li</b>': ('masc', 'uno & dos; páli'),
        }
        for source, expected in cases.items():
            self.assertEqual(expected, parse_definition(source))

    def test_unknown_markup_and_blank_meanings_are_rejected(self):
        for value in ('<script>bad</script>', 'nt. <b>one</b><b>two</b>',
                      'nt. <i>one</i>', 'nt. []', 'nt. <b>&lt;script&gt;</b>',
                      'nt. <b>one</b> unexplained suffix'):
            with self.subTest(value=value), self.assertRaises(InputError):
                parse_definition(value)

    def test_json_assignment_only_no_execution_or_duplicate_keys(self):
        with tempfile.TemporaryDirectory() as root:
            path = Path(root) / 'export.js'
            path.write_text('let dpd_ebts_es = {"atthi 1.1": "pr. <b>hay</b>"};')
            self.assertEqual({'atthi 1.1': 'pr. <b>hay</b>'}, load_assignment(path, 'dpd_ebts_es'))
            for text in ('let dpd_ebts_es = {}; alert(1)', 'let other = {"a":"b"}',
                         'let dpd_ebts_es = {"a":"one","a":"two"}',
                         'let dpd_ebts_es = {"a":42}', 'let dpd_ebts_es = {"a":function(){}}'):
                path.write_text(text)
                with self.assertRaises(InputError):
                    load_assignment(path, 'dpd_ebts_es')


class SpanishMappingTests(unittest.TestCase):
    def setUp(self):
        self.headword = (2736, 'atthi 1.1', 'pr', 'there is; there exists', 'is, exists')
        self.english = {'atthi 1.1': 'pr. <b>there is; there exists</b> [√as + ti]'}
        self.spanish = {'atthi 1.1': 'pr. <b>hay; existe</b> [√as + ti]'}
        self.keys = {'atthi 1.1': [2736]}

    def mapped(self):
        return map_sense(self.headword, self.english, self.spanish, source_index(self.english), self.keys)

    def test_accepts_only_exact_verified_sense(self):
        result = self.mapped()
        self.assertEqual('accepted', result['status'])
        self.assertEqual('hay; existe', result['meaning'])
        self.assertEqual(2736, result['headword_id'])
        self.english['atthi 1.1'] = 'pr. <b>different sense</b>'
        self.assertEqual('meaning_drift', self.mapped()['status'])
        self.assertEqual('', self.mapped()['meaning'])

    def test_renumbering_is_reported_not_automatically_joined(self):
        self.english = {'atthi 1.2': self.english['atthi 1.1']}
        self.spanish = {'atthi 1.2': self.spanish['atthi 1.1']}
        result = self.mapped()
        self.assertEqual('renamed_or_renumbered', result['status'])
        self.assertEqual(['atthi 1.2'], result['suggested_keys'])
        self.assertEqual('', result['meaning'])

    def test_ambiguous_pos_missing_and_unparseable_cases_stay_untranslated(self):
        self.keys['atthi 1.1'].append(999)
        self.assertEqual('ambiguous_headword', self.mapped()['status'])
        self.keys['atthi 1.1'] = [2736]
        self.spanish['atthi 1.1'] = 'nt. <b>existencia</b>'
        self.assertEqual('pos_mismatch', self.mapped()['status'])
        self.spanish['atthi 1.1'] = '<script>bad</script>'
        self.assertEqual('unparseable_definition', self.mapped()['status'])
        self.spanish = {}
        self.assertEqual('missing_translation', self.mapped()['status'])
        self.english = {}
        self.assertEqual('missing_key', self.mapped()['status'])

    def test_unbold_secondary_meaning_and_full_key_are_supported(self):
        with tempfile.TemporaryDirectory() as root:
            en, es = Path(root) / 'en.js', Path(root) / 'es.js'
            en.write_text('let dpd_ebts = {"akakkhala": "adj. not hard []"}')
            es.write_text('let dpd_ebts_es = {"akakkhala": "adj. no duro []"};')
            records, diagnostics = spanish_records([dict(headword_id=72481, kind='nouns', lemma_id=123, primary=True)],
                [(72481, 'akakkhala', 'adj', '', 'not hard')], en, es)
            self.assertEqual('no duro', records[0]['meaning'])
            self.assertEqual('accepted', records[0]['status'])
            self.assertEqual([], diagnostics['unpaired_spanish_keys'])

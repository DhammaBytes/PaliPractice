"""Reject unsafe template encodings and conflicting internal form identities."""

import json
import sqlite3
import sys
import unittest
from pathlib import Path
from types import SimpleNamespace

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from extraction.inputs import InputError
from extraction.templates import parse_template
from extraction.form_storage import insert_form


def word(rows, pattern='a masc', pos='masc', stem='dhamm'):
    return SimpleNamespace(id=1, pattern=pattern, pos=pos, stem=stem,
                           it=SimpleNamespace(data=json.dumps([[['header']], *rows])))


def noun_row(endings=None, grammar='masc nom sg', label='nom'):
    return [[label], endings if endings is not None else ['o'], [grammar]]


class FormTests(unittest.TestCase):
    def test_compounds_are_not_grammatical_forms(self):
        source = word([noun_row(), noun_row(['a'], 'in comps', 'in comps')])
        forms, count, missing = parse_template(source, 'noun', {'dhammo', 'dhamma'})
        self.assertEqual((1, 0), (count, missing))
        self.assertEqual('dhammo', forms[0]['form'])
        self.assertEqual((1, 1, 1), tuple(forms[0][key] for key in ('case_name', 'gender', 'number')))

    def test_unknown_or_contradictory_grammar_fails(self):
        for grammar in ('masc nom', 'masc nom sg surprise', 'nt nom sg', 'masc acc sg', ''):
            with self.subTest(grammar=grammar), self.assertRaises(InputError):
                parse_template(word([noun_row(grammar=grammar)]), 'noun', set())
        with self.assertRaises(InputError):
            parse_template(word([noun_row(label='unknown')]), 'noun', set())

    def test_malformed_empty_and_missing_templates_fail(self):
        for value in ('{', '{}', '[]', '[[]]'):
            source = word([])
            source.it.data = value
            with self.subTest(value=value), self.assertRaises(InputError):
                parse_template(source, 'noun', set())
        for row in ([['nom'], ['o']], [['nom'], 'o', ['masc nom sg']], noun_row([''])):
            with self.subTest(row=row), self.assertRaises(InputError):
                parse_template(word([row]), 'noun', set())

    def test_supported_bounds_preserve_indices_and_overflow_fails(self):
        forms, _, _ = parse_template(word([noun_row(list('abcdef'))]), 'noun', set())
        self.assertEqual(list(range(6)), [form['ending_index'] for form in forms])
        for endings in (list('abcdefg'), ['o', '', 'ā']):
            with self.assertRaises(InputError):
                parse_template(word([noun_row(endings)]), 'noun', set())
        row = [['pr 3rd'], list('abcdefg'), ['pr 3rd sg']]
        forms, _, _ = parse_template(word([row], 'ati pr', 'pr', 'bhav'), 'verb', set())
        self.assertEqual(7, len(forms))
        row[1].append('h')
        with self.assertRaises(InputError):
            parse_template(word([row], 'ati pr', 'pr', 'bhav'), 'verb', set())

    def test_unknown_verb_tense_and_voice_fail(self):
        for grammar in ('aor 3rd sg', 'pass pr 3rd sg', 'pr 4th sg', 'pr 3rd dual'):
            with self.subTest(grammar=grammar), self.assertRaises(InputError):
                parse_template(word([[['pr 3rd'], ['ati'], [grammar]]], 'ati pr', 'pr'), 'verb', set())

    def test_defective_paradigm_can_omit_slots_without_creating_none_forms(self):
        source = word([noun_row([''], ''), [['voc'], ['ā'], ['masc voc pl']]])
        forms, _, _ = parse_template(source, 'noun', set())
        self.assertEqual((8, 2), (forms[0]['case_name'], forms[0]['number']))

    def test_headword_scope_separates_collisions_and_conflicting_duplicates_fail(self):
        connection = sqlite3.connect(':memory:')
        self.addCleanup(connection.close)
        connection.execute('CREATE TABLE forms (headword_id INTEGER, form_id INTEGER, form TEXT, PRIMARY KEY(headword_id, form_id))')
        cursor = connection.cursor()
        self.assertTrue(insert_form(cursor, 'forms', 1, 103357122, 'addhanesu'))
        self.assertTrue(insert_form(cursor, 'forms', 2, 103357122, 'addhesu'))
        self.assertFalse(insert_form(cursor, 'forms', 1, 103357122, 'addhanesu'))
        with self.assertRaisesRegex(InputError, 'Conflicting'):
            insert_form(cursor, 'forms', 1, 103357122, 'addhesu')


if __name__ == '__main__':
    unittest.main()

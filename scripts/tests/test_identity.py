"""Practice history remains meaningful when source senses or ranks change."""

import copy
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from bootstrap_registry import bootstrap
from extraction.identity import (historical_practice_registry, plan_practice,
                                 require_historical_registry, require_practice_registry)
from extraction.inputs import InputError
from extraction.registry import RegistryError, get_noun_lemma_id, validate_registry


def sense(identifier=1, pattern='a masc', count=1):
    return {'id': identifier, 'lemma_id': 10001, 'lemma': 'vassa', 'stem': 'vass',
            'pattern': pattern, 'gender': 1, 'ebt_count': count}


class IdentityTests(unittest.TestCase):
    def setUp(self):
        self.registry = {'version': 1, 'next_noun_id': 10003, 'next_verb_id': 70001,
                         'nouns': {'vassa': 10001, 'dormant': 10002}, 'verbs': {}}
        self.words = {'nouns': [sense(), sense(2), sense(3, 'a nt', 100)], 'verbs': []}
        self.choices = historical_practice_registry(self.words)

    def test_explicit_bootstrap_cannot_overwrite_or_replace_production_history(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'new.json'
            bootstrap(path)
            with self.assertRaises(FileExistsError):
                bootstrap(path)
            from extraction.inputs import read_json
            with self.assertRaises(InputError):
                require_historical_registry(read_json(path), self.registry)

    def test_all_historical_mappings_including_dormant_are_required(self):
        for lemma in self.registry['nouns']:
            changed = copy.deepcopy(self.registry)
            del changed['nouns'][lemma]
            with self.assertRaisesRegex(InputError, 'removed or reassigned'):
                require_historical_registry(changed, self.registry)
        changed = copy.deepcopy(self.registry)
        changed['nouns'] = {'vassa': 10002, 'dormant': 10001}
        with self.assertRaises(InputError):
            require_historical_registry(changed, self.registry)

    def test_new_lemma_appends_without_reusing_a_historical_gap(self):
        current = copy.deepcopy(self.registry)
        self.assertEqual(10003, get_noun_lemma_id(current, 'new'))
        require_historical_registry(current, self.registry)
        self.assertEqual(self.registry['nouns']['dormant'], current['nouns']['dormant'])

    def test_malformed_registry_cannot_allocate(self):
        for value in (True, '10001', 9999, 70000):
            changed = copy.deepcopy(self.registry)
            changed['nouns']['vassa'] = value
            with self.assertRaises(RegistryError):
                validate_registry(changed)
        changed = copy.deepcopy(self.registry)
        changed['nouns']['dormant'] = 10001
        with self.assertRaises(RegistryError):
            validate_registry(changed)

    def test_frequency_and_sense_count_changes_cannot_switch_paradigms(self):
        changed = {'nouns': [sense(5, 'a nt', 1000), sense(4, 'a nt', 900),
                             sense(3, 'a nt', 100), sense()], 'verbs': []}
        selected, proposed, _ = plan_practice(changed, self.choices, {})
        self.assertEqual({10001: 1}, selected)
        self.assertEqual(self.choices, proposed)
        changed['nouns'].reverse()
        self.assertEqual(selected, plan_practice(changed, self.choices, {})[0])

    def test_compatible_sense_change_preserves_anchor(self):
        changed = {'nouns': [sense(7, count=30), sense()], 'verbs': []}
        selected, proposed, changes = plan_practice(changed, self.choices, {})
        self.assertEqual(7, selected[10001])
        self.assertEqual(self.choices, proposed)
        self.assertEqual('compatible_primary_sense', changes[0]['classification'])

    def test_incompatible_paradigm_fails_without_an_exact_correction(self):
        changed = {'nouns': [sense(1, 'vī fem')], 'verbs': []}
        with self.assertRaisesRegex(InputError, 'No compatible'):
            plan_practice(changed, self.choices, {})
        old = self.choices['choices']['10001']
        correction = {'10001': {'anchor_headword_id': 1,
                                'from': {k: old[k] for k in ('stem', 'pattern', 'gender')},
                                'to': {'stem': 'vass', 'pattern': 'vī fem', 'gender': 1}}}
        _, proposed, changes = plan_practice(changed, self.choices, correction)
        require_practice_registry(proposed, self.choices, self.registry, correction)
        self.assertEqual('source_correction', changes[0]['classification'])
        proposed['choices']['10001']['anchor_headword_id'] = 7
        with self.assertRaises(InputError):
            require_practice_registry(proposed, self.choices, self.registry, correction)

    def test_new_practice_choice_is_frequency_selected_and_dormant_choice_kept(self):
        empty = {'schema': 1, 'choices': {}}
        selected, proposed, _ = plan_practice(self.words, empty, {})
        self.assertEqual(3, selected[10001])
        _, dormant, _ = plan_practice({'nouns': [], 'verbs': []}, proposed, {})
        self.assertEqual(proposed, dormant)


if __name__ == '__main__':
    unittest.main()

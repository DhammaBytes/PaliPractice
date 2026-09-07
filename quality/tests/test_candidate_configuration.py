"""A supplied candidate must be explicit and shared by the integration lanes."""

import os
import unittest
from pathlib import Path
from unittest.mock import patch

from quality.gate import GateError, candidate_spec


class CandidateConfigurationTests(unittest.TestCase):
    def test_no_candidate_is_the_bundle_contract(self):
        with patch.dict(os.environ, {}, clear=True):
            self.assertIsNone(candidate_spec())

    def test_partial_candidate_configuration_fails(self):
        for key in ('PALIPRACTICE_CANDIDATE_DIRECTORY', 'PALIPRACTICE_INPUT_MANIFEST'):
            with self.subTest(key=key), patch.dict(os.environ, {key: '/tmp/example'}, clear=True):
                with self.assertRaises(GateError):
                    candidate_spec()

    def test_candidate_and_input_paths_are_resolved_together(self):
        with patch.dict(os.environ, {'PALIPRACTICE_CANDIDATE_DIRECTORY': 'candidate',
                                     'PALIPRACTICE_INPUT_MANIFEST': 'inputs.json'}, clear=True):
            self.assertEqual((Path('candidate').resolve(), Path('inputs.json').resolve()), candidate_spec())

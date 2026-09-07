"""Only a complete gate for the same bytes and consumer sources can issue evidence."""

import contextlib
import io
import json
import tempfile
import unittest
from pathlib import Path

from test_candidate import fixture
from extraction.candidate import build_candidate, validate_candidate
from extraction.inputs import InputError
from semantic_evidence import issue, source_identity, verify
from check_repeatability import semantic_digest


class SemanticEvidenceTests(unittest.TestCase):
    def test_issue_requires_complete_gate_and_unchanged_consumers(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            inputs, _ = fixture(root)
            with contextlib.redirect_stdout(io.StringIO()):
                candidate = build_candidate(inputs, root / 'candidate')
            gate = root / 'gate'
            (gate / 'english-repeatability').mkdir(parents=True)
            (gate / 'completion.json').write_text(json.dumps({'status': 'pass', 'findings': 0}))
            (gate / 'manifest.json').write_text(json.dumps({'groups': ['data']}))
            with self.assertRaisesRegex(InputError, 'complete successful'):
                issue(candidate, inputs, gate)
            (gate / 'manifest.json').write_text(json.dumps({'groups': ['data', 'dotnet', 'desktop']}))
            (gate / 'semantic-sources.json').write_text('{}')
            with self.assertRaisesRegex(InputError, 'sources changed'):
                issue(candidate, inputs, gate)
            (gate / 'semantic-sources.json').write_text(json.dumps(source_identity()))
            (gate / 'english-repeatability/repeatability.json').write_text(json.dumps({
                'outputs': validate_candidate(candidate)['outputs'],
                'semantic_sha256': semantic_digest(candidate / 'pali.db')}))
            issue(candidate, inputs, gate)
            verify(candidate, inputs, gate / 'semantic-verification.json')


if __name__ == '__main__':
    unittest.main()

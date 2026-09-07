"""Verify a candidate against its exact inputs and current extraction sources."""

import argparse
from pathlib import Path

from extraction.candidate import code_identity, validate_candidate
from extraction.inputs import InputError, load_manifest


def verify(candidate: Path, inputs: Path):
    manifest = validate_candidate(candidate.resolve())
    pinned, _ = load_manifest(inputs.resolve())
    expected = {name: {key: value for key, value in entry.items() if key != 'path'}
                for name, entry in pinned['inputs'].items()}
    if manifest['inputs'] != expected or manifest['configuration'] != pinned['configuration']:
        raise InputError('Candidate does not match the supplied input manifest')
    if manifest['database_version'] != pinned['database_version'] or manifest['corpus_generation'] != pinned['corpus_generation']:
        raise InputError('Candidate version/corpus provenance differs from supplied inputs')
    if manifest['code'] != code_identity():
        raise InputError('Candidate is stale: extraction source/environment identity changed')
    print('PASS exact candidate identities, grammar, scoped records, and pinned inputs')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--candidate', type=Path, required=True)
    parser.add_argument('--inputs', type=Path, required=True)
    args = parser.parse_args()
    verify(args.candidate, args.inputs)

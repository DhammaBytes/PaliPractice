"""Bind a complete successful gate to candidate bytes, pinned inputs, and consumer source."""

import argparse
import json
import subprocess
from pathlib import Path

from extraction.candidate import ROOT, validate_candidate
from extraction.inputs import InputError, load_manifest, read_json, sha256
from check_repeatability import semantic_digest


def source_identity() -> dict:
    names = subprocess.check_output(['git', 'ls-files', '-co', '--exclude-standard', '--',
                                      'PaliPractice', 'scripts', 'quality'], cwd=ROOT, text=True).splitlines()
    relevant = {name for name in names if Path(name).suffix in ('.cs', '.csproj', '.props', '.targets', '.py', '.gz')
                or name.endswith(('packages.lock.json', 'global.json'))
                or name.startswith('quality/config/')}
    return {name: sha256(ROOT / name) for name in sorted(relevant) if (ROOT / name).is_file()}


def identity(candidate: Path, inputs: Path) -> dict:
    manifest = validate_candidate(candidate)
    pinned, _ = load_manifest(inputs)
    expected = {name: {key: value for key, value in entry.items() if key != 'path'}
                for name, entry in pinned['inputs'].items()}
    if manifest['inputs'] != expected:
        raise InputError('Semantic evidence input identity differs from candidate')
    for key in ('configuration', 'database_version', 'corpus_generation'):
        if manifest[key] != pinned[key]:
            raise InputError(f'Semantic evidence {key} differs from candidate')
    return {'candidate_sha256': sha256(candidate / 'candidate.json'),
            'outputs': manifest['outputs'], 'inputs': expected, 'sources': source_identity()}


def issue(candidate: Path, inputs: Path, gate_run: Path):
    completion = read_json(gate_run / 'completion.json')
    groups = set(read_json(gate_run / 'manifest.json')['groups'])
    if completion != {'status': 'pass', 'findings': 0} or not {'data', 'dotnet', 'desktop'} <= groups:
        raise InputError('Semantic evidence requires the complete successful candidate gate')
    proof = identity(candidate, inputs)
    if proof['sources'] != read_json(gate_run / 'semantic-sources.json'):
        raise InputError('Consumer sources changed since the successful gate began')
    repeated = read_json(gate_run / 'english-repeatability/repeatability.json')
    if repeated['outputs'] != proof['outputs']:
        raise InputError('Gate repeatability outputs differ from candidate')
    proof.update(schema=1, gate_run=str(gate_run), semantic_sha256=repeated['semantic_sha256'])
    with (gate_run / 'semantic-verification.json').open('x') as stream:
        json.dump(proof, stream, indent=2)
        stream.write('\n')


def verify(candidate: Path, inputs: Path, evidence: Path):
    proof = read_json(evidence)
    keys = {'schema', 'gate_run', 'semantic_sha256', 'candidate_sha256', 'outputs', 'inputs', 'sources'}
    if not isinstance(proof, dict) or set(proof) != keys or proof.get('schema') != 1:
        raise InputError('Missing or unsupported semantic verification evidence')
    if not isinstance(proof['gate_run'], str) or not proof['gate_run']:
        raise InputError('Semantic verification evidence has no gate provenance')
    if proof['semantic_sha256'] != semantic_digest(candidate / 'pali.db'):
        raise InputError('Semantic verification evidence is stale for these database contents')
    current = identity(candidate, inputs)
    if any(proof.get(key) != value for key, value in current.items()):
        raise InputError('Semantic verification evidence is stale or belongs to another candidate')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--candidate', type=Path)
    parser.add_argument('--inputs', type=Path)
    parser.add_argument('--gate-run', type=Path)
    parser.add_argument('--capture-sources', type=Path)
    args = parser.parse_args()
    if args.capture_sources:
        args.capture_sources.write_text(json.dumps(source_identity(), indent=2) + '\n')
    elif args.candidate and args.inputs and args.gate_run:
        issue(args.candidate.resolve(), args.inputs.resolve(), args.gate_run.resolve())
    else:
        parser.error('Require either --capture-sources or candidate, inputs, and gate-run')

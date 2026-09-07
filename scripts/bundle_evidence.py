"""Bind the multilingual bundle to the exact database exercised by the full gate."""

from pathlib import Path

from extraction.enrichment import dump_json, validate_enrichment
from extraction.inputs import InputError, read_json, sha256
from semantic_evidence import verify as verify_english


def identity(candidate: Path, english: Path, translations: Path) -> dict:
    validate_enrichment(english, translations, candidate)
    manifest = read_json(candidate / 'bundle.json')
    return {'bundle_sha256': sha256(candidate / 'bundle.json'), 'outputs': manifest['outputs']}


def issue(candidate: Path, english: Path, inputs: Path, translations: Path, gate: Path):
    proof = identity(candidate, english, translations)
    verify_english(english, inputs, gate / 'semantic-verification.json')
    repeated = read_json(gate / 'translation-repeatability/repeatability.json')['outputs']
    expected = dict(proof['outputs'], **{'bundle.json': proof['bundle_sha256']})
    if repeated != expected:
        raise InputError('Multilingual gate outputs differ from the requested bundle')
    # The gate's .NET lane must have consumed this exact multilingual database.
    command = read_json(gate / 'dotnet-test.command.json')
    tested = command.get('candidate_database')
    if tested != str(candidate / 'pali.db'):
        raise InputError('The .NET gate did not consume the multilingual database')
    dump_json(gate / 'bundle-verification.json', {
        'schema': 1, 'gate_run': str(gate), 'english': read_json(gate / 'semantic-verification.json'), **proof})


def verify(candidate: Path, english: Path, inputs: Path, translations: Path, evidence: Path):
    proof = read_json(evidence)
    if not isinstance(proof, dict) or set(proof) != {'schema', 'gate_run', 'english', 'bundle_sha256', 'outputs'} or proof['schema'] != 1:
        raise InputError('Missing or unsupported multilingual verification evidence')
    gate = Path(proof['gate_run'])
    if read_json(gate / 'completion.json') != {'status': 'pass', 'findings': 0}:
        raise InputError('Multilingual verification gate did not pass')
    if read_json(gate / 'semantic-verification.json') != proof['english']:
        raise InputError('English verification receipt differs from the multilingual gate')
    verify_english(english, inputs, gate / 'semantic-verification.json')
    current = identity(candidate, english, translations)
    if any(proof[key] != value for key, value in current.items()):
        raise InputError('Multilingual verification evidence is stale for this bundle')

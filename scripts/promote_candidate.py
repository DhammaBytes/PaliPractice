"""Recoverable promotion of a validated candidate's database and identity files.

English-only production promotion remains prohibited. Multilingual promotion
requires exact source validation and the receipt from a complete bundle gate.
"""

import argparse
import contextlib
import fcntl
import json
import os
from pathlib import Path

from extraction.candidate import ROOT, validate_candidate
from extraction.inputs import InputError, load_manifest, read_json, sha256
from semantic_evidence import verify as verify_semantics

TARGETS = {
    'pali.db': 'PaliPractice/PaliPractice/Data/pali.db',
    'pali.version.txt': 'PaliPractice/PaliPractice/Data/pali.version.txt',
    'candidate.json': 'PaliPractice/PaliPractice/Data/pali.manifest.json',
    'lemma_registry.json': 'scripts/configs/lemma_registry.json',
    'practice_registry.json': 'scripts/configs/practice_registry.json',
    'paradigm_corrections.json': 'scripts/configs/paradigm_corrections.json',
    'primary_forms.json': 'scripts/generated/primary_forms.json',
}


def durable_write(path: Path, content: bytes):
    temporary = path.with_name(path.name + '.promotion-tmp')
    with temporary.open('wb') as stream:
        stream.write(content)
        stream.flush()
        os.fsync(stream.fileno())
    os.replace(temporary, path)
    sync_directory(path.parent)


def sync_directory(path: Path):
    descriptor = os.open(path, os.O_RDONLY)
    try:
        os.fsync(descriptor)
    finally:
        os.close(descriptor)


def journal_write(directory: Path, journal: dict):
    durable_write(directory / 'journal.json', (json.dumps(journal, sort_keys=True, indent=2) + '\n').encode())


def target_path(repository: Path, name: str) -> Path:
    path = repository / TARGETS[name]
    if path.resolve() != path:
        raise InputError(f'Promotion target contains a symlink: {path}')
    return path


@contextlib.contextmanager
def locked(repository: Path):
    state = repository / '.local/promotion'
    if state.resolve() != state:
        raise InputError('Promotion state must not be a symlink')
    state.mkdir(parents=True, exist_ok=True)
    with (state / 'lock').open('a') as lock:
        fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        yield state


def recover_locked(repository: Path, state: Path):
    path = state / 'journal.json'
    if not path.exists():
        return
    journal = read_json(path)
    if journal.get('phase') in ('committed', 'rolled_back'):
        return
    supported_sets = (set(TARGETS), set(TARGETS) - {'primary_forms.json'})
    if journal.get('phase') != 'prepared' or set(journal.get('files', {})) not in supported_sets:
        raise InputError('Unrecognized promotion journal; manual investigation required')
    # Verify every backup and current target before touching any target.
    for name, entry in journal['files'].items():
        backup = state / ('old-' + name)
        if entry['old'] is not None and sha256(backup) != entry['old']:
            raise InputError(f'Promotion backup is damaged: {name}')
        target = target_path(repository, name)
        current = sha256(target) if target.exists() else None
        if current not in (entry['old'], entry['new']):
            raise InputError(f'Promotion target was edited externally: {name}')
    for name, entry in journal['files'].items():
        target = target_path(repository, name)
        if entry['old'] is None:
            target.unlink(missing_ok=True)
            sync_directory(target.parent)
        else:
            durable_write(target, (state / ('old-' + name)).read_bytes())
    journal['phase'] = 'rolled_back'
    journal_write(state, journal)


def promotion_inputs(candidate: Path, repository: Path, inputs: Path, evidence: Path,
                     english: Path | None, translations: Path | None):
    if (candidate / 'bundle.json').exists():
        from bundle_evidence import verify as verify_bundle
        if english is None or translations is None:
            raise InputError('Multilingual promotion requires English checkpoint and translation inputs')
        verify_bundle(candidate, english.resolve(), inputs.resolve(), translations.resolve(), evidence.resolve())
        manifest = read_json(candidate / 'bundle.json')
        if set(manifest['languages']) != {'en', 'ru', 'es'}:
            raise InputError('Database readiness requires English, Russian and Spanish layers')
        return manifest['outputs'], 'bundle.json'
    manifest = validate_candidate(candidate)
    if repository == ROOT and manifest['language_layer'] == 'en':
        raise InputError('English-only candidates cannot replace the Russian-capable production bundle')
    verify_semantics(candidate, inputs.resolve(), evidence.resolve())
    return manifest['outputs'], 'candidate.json'


def protect_input_paths(repository: Path, inputs: Path, english: Path | None, translations: Path | None):
    protected = {inputs.resolve(), *load_manifest(inputs.resolve())[1].values()}
    if english is not None and translations is not None:
        from extraction.enrichment import read_sources
        protected.update(read_sources(translations.resolve(), english.resolve())[1].values())
        protected.add(translations.resolve())
    targets = {target_path(repository, name) for name in TARGETS}
    if protected & targets:
        raise InputError('Promotion inputs overlap output targets; snapshot pinned inputs first')


def promote(candidate: Path, repository: Path, inputs: Path, evidence: Path, after_write=lambda _: None,
            *, english: Path | None = None, translations: Path | None = None):
    candidate, repository = candidate.resolve(), repository.resolve()
    outputs, manifest_name = promotion_inputs(candidate, repository, inputs, evidence, english, translations)
    protect_input_paths(repository, inputs, english, translations)
    expected = dict(outputs, **{'candidate.json': sha256(candidate / manifest_name)})
    with locked(repository) as state:
        recover_locked(repository, state)
        files = {}
        for name in TARGETS:
            target = target_path(repository, name)
            target.parent.mkdir(parents=True, exist_ok=True)
            old = target.read_bytes() if target.exists() else None
            if old is not None:
                durable_write(state / ('old-' + name), old)
            source = manifest_name if name == 'candidate.json' else name
            durable_write(state / ('new-' + name), (candidate / source).read_bytes())
            if sha256(state / ('new-' + name)) != expected[name]:
                raise InputError(f'Candidate changed during promotion staging: {name}')
            files[name] = {'old': sha256(target) if old is not None else None,
                           'new': sha256(state / ('new-' + name))}
        journal = {'phase': 'prepared', 'files': files}
        journal_write(state, journal)
        for name in TARGETS:
            durable_write(target_path(repository, name), (state / ('new-' + name)).read_bytes())
            after_write(name)
        for name, entry in files.items():
            if sha256(target_path(repository, name)) != entry['new']:
                raise InputError(f'Promoted file differs from candidate: {name}')
        journal['phase'] = 'committed'
        journal_write(state, journal)


def recover(repository: Path):
    repository = repository.resolve()
    with locked(repository) as state:
        recover_locked(repository, state)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=('promote', 'recover'))
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--candidate', type=Path)
    parser.add_argument('--inputs', type=Path)
    parser.add_argument('--evidence', type=Path)
    parser.add_argument('--english', type=Path)
    parser.add_argument('--translations', type=Path)
    args = parser.parse_args()
    if args.command == 'recover':
        recover(args.repository.resolve())
    elif args.candidate and args.inputs and args.evidence:
        promote(args.candidate.resolve(), args.repository.resolve(), args.inputs, args.evidence,
                english=args.english, translations=args.translations)
    else:
        parser.error('promote requires --candidate, --inputs, and --evidence')

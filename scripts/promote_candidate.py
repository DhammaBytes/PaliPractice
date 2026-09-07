"""Recoverable promotion of a validated candidate's database and identity files.

English-only production promotion remains prohibited. A scratch repository can
exercise the same file protocol before multilingual promotion is enabled.
"""

import argparse
import contextlib
import fcntl
import json
import os
from pathlib import Path

from extraction.candidate import ROOT, validate_candidate
from extraction.inputs import InputError, read_json, sha256
from semantic_evidence import verify as verify_semantics

TARGETS = {
    'pali.db': 'PaliPractice/PaliPractice/Data/pali.db',
    'pali.version.txt': 'PaliPractice/PaliPractice/Data/pali.version.txt',
    'candidate.json': 'PaliPractice/PaliPractice/Data/pali.manifest.json',
    'lemma_registry.json': 'scripts/configs/lemma_registry.json',
    'practice_registry.json': 'scripts/configs/practice_registry.json',
    'paradigm_corrections.json': 'scripts/configs/paradigm_corrections.json',
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
    if journal.get('phase') != 'prepared' or set(journal.get('files', {})) != set(TARGETS):
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


def promote(candidate: Path, repository: Path, inputs: Path, evidence: Path, after_write=lambda _: None):
    candidate, repository = candidate.resolve(), repository.resolve()
    manifest = validate_candidate(candidate)
    expected = dict(manifest['outputs'], **{'candidate.json': sha256(candidate / 'candidate.json')})
    if repository == ROOT and manifest['language_layer'] == 'en':
        raise InputError('English-only candidates cannot replace the Russian-capable production bundle')
    verify_semantics(candidate, inputs.resolve(), evidence.resolve())
    with locked(repository) as state:
        recover_locked(repository, state)
        files = {}
        for name in TARGETS:
            target = target_path(repository, name)
            target.parent.mkdir(parents=True, exist_ok=True)
            old = target.read_bytes() if target.exists() else None
            if old is not None:
                durable_write(state / ('old-' + name), old)
            durable_write(state / ('new-' + name), (candidate / name).read_bytes())
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
    args = parser.parse_args()
    if args.command == 'recover':
        recover(args.repository.resolve())
    elif args.candidate and args.inputs and args.evidence:
        promote(args.candidate.resolve(), args.repository.resolve(), args.inputs, args.evidence)
    else:
        parser.error('promote requires --candidate, --inputs, and --evidence')

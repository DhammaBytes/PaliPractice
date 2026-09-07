"""Build two isolated English candidates and verify bytes, semantics, and source preservation."""

import argparse
from contextlib import closing
import hashlib
import json
import sqlite3
import subprocess
import sys
from pathlib import Path

from extraction.candidate import OUTPUTS, ROOT, validate_candidate
from extraction.inputs import InputError, load_manifest, sha256


def semantic_digest(database: Path) -> str:
    digest = hashlib.sha256()
    with closing(sqlite3.connect(database.resolve().as_uri() + '?mode=ro&immutable=1', uri=True)) as connection:
        objects = connection.execute(
            "SELECT type, name, tbl_name, sql FROM sqlite_master ORDER BY type, name").fetchall()
        digest.update(json.dumps(objects, ensure_ascii=False).encode())
        digest.update(str(connection.execute('PRAGMA user_version').fetchone()[0]).encode())
        for kind, name, _, _ in objects:
            if kind != 'table':
                continue
            quoted = '"' + name.replace('"', '""') + '"'
            rows = [json.dumps(row, ensure_ascii=False, default=lambda value: {'blob': value.hex()})
                    for row in connection.execute(f'SELECT * FROM {quoted}')]
            digest.update(json.dumps(sorted(rows), ensure_ascii=False).encode())
    return digest.hexdigest()


def preserved_paths(inputs: Path) -> list[Path]:
    _, paths = load_manifest(inputs)
    source = [ROOT / 'PaliPractice/PaliPractice/Data/pali.db',
              ROOT / 'PaliPractice/PaliPractice/Data/pali.version.txt']
    source.extend((ROOT / 'scripts/configs').glob('*.json'))
    return sorted(set(source + list(paths.values()) + [inputs]))


def compare(first: Path, second: Path):
    validate_candidate(first)
    validate_candidate(second)
    mismatches = [name for name in (*OUTPUTS, 'candidate.json')
                  if sha256(first / name) != sha256(second / name)]
    semantics = [semantic_digest(path / 'pali.db') for path in (first, second)]
    if mismatches or semantics[0] != semantics[1]:
        raise InputError(f'Candidate repeatability failed: bytes={mismatches}, semantics={semantics}')
    return semantics[0]


def check(inputs: Path, output: Path, supplied: Path | None = None):
    paths = preserved_paths(inputs)
    before = {str(path): sha256(path) for path in paths}
    output.mkdir(parents=True, exist_ok=False)
    try:
        for name in ('run-1', 'run-2'):
            with (output / f'{name}.log').open('w') as log:
                subprocess.run([sys.executable, '-B', str(ROOT / 'scripts/extract_nouns_and_verbs.py'),
                                'build', '--manifest', str(inputs), '--output', str(output / name)],
                               cwd=output, stdout=log, stderr=subprocess.STDOUT, check=True)
        digest = compare(output / 'run-1', output / 'run-2')
        if supplied:
            compare(output / 'run-1', supplied)
    finally:
        if before != {str(path): sha256(path) for path in paths}:
            raise InputError('Candidate regeneration modified a source input or production artifact')
    report = {'semantic_sha256': digest, 'source_sha256': before,
              'outputs': {name: sha256(output / 'run-1' / name) for name in OUTPUTS},
              'supplied_candidate': str(supplied) if supplied else None}
    (output / 'repeatability.json').write_text(json.dumps(report, indent=2) + '\n')
    print(f'PASS two isolated builds, identical bytes and semantics: {digest}')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--inputs', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--compare', type=Path)
    args = parser.parse_args()
    check(args.inputs.resolve(), args.output.resolve(), args.compare.resolve() if args.compare else None)

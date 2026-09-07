"""Prove deterministic offline translation enrichment and English preservation."""

import argparse
from pathlib import Path

from extraction.enrichment import build_enrichment, dump_json, read_sources, validate_enrichment
from extraction.inputs import InputError, sha256


def check(english: Path, manifest: Path, output: Path):
    _, sources = read_sources(manifest, english)
    preserved = [manifest, *sources.values(), *english.iterdir()]
    before = {str(path): sha256(path) for path in preserved if path.is_file()}
    output.mkdir(parents=True, exist_ok=False)
    for name in ('run-1', 'run-2'):
        build_enrichment(english, manifest, output / name)
    first, second = output / 'run-1', output / 'run-2'
    hashes = {path.name: sha256(path) for path in first.iterdir()}
    if hashes != {path.name: sha256(path) for path in second.iterdir()}:
        raise InputError('Translation builds are not byte-identical')
    if before != {name: sha256(Path(name)) for name in before}:
        raise InputError('Translation build changed its inputs or English checkpoint')
    report = validate_enrichment(english, manifest, first)
    dump_json(output / 'repeatability.json', {'outputs': hashes, 'preserved': before,
                                            'coverage': {k: v['coverage'] for k, v in report['layers'].items()}})


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--english', type=Path, required=True)
    parser.add_argument('--manifest', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    check(args.english.resolve(), args.manifest.resolve(), args.output.resolve())
    print('PASS translation repeatability, source equality, and English preservation')

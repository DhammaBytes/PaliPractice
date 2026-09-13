"""Derive source keys → stable DPD IDs from pinned historical TSV parts, offline."""

import argparse
import csv
import io
import json
from pathlib import Path

from extraction.inputs import InputError, positive_integer, read_json, sha256, verify_file
from extraction.spanish_meanings import load_assignment, normalize, parse_definition


def read_inputs(manifest_path: Path):
    manifest = read_json(manifest_path)
    if not isinstance(manifest, dict) or set(manifest) != {'schema', 'english', 'spanish', 'headwords'}:
        raise InputError('Historical identity manifest requires schema, english, spanish and headwords')
    if type(manifest['schema']) is not int or manifest['schema'] != 1:
        raise InputError('Unsupported historical identity manifest')
    parts = manifest['headwords']
    if not isinstance(parts, list) or not parts:
        raise InputError('Historical headword TSV parts are required in source order')
    paths = [verify_file(entry, manifest_path.parent) for entry in parts]
    if len(set(paths)) != len(paths) or len({entry['source']['revision'] for entry in parts}) != 1:
        raise InputError('Historical parts must be distinct and from one revision')
    english_path = verify_file(manifest['english'], manifest_path.parent)
    spanish_path = verify_file(manifest['spanish'], manifest_path.parent)
    english = load_assignment(english_path, 'dpd_ebts')
    spanish = load_assignment(spanish_path, 'dpd_ebts_es')
    if set(english) != set(spanish):
        raise InputError('Historical bridge requires identical English and Spanish key sets')
    return manifest, paths, english_path, spanish_path, english


def build(manifest_path: Path, output: Path):
    manifest, paths, english_path, spanish_path, english = read_inputs(manifest_path)
    # Historical split TSVs are byte chunks of one file, not independently headed tables.
    data = b''.join(path.read_bytes() for path in paths).decode('utf-8-sig')
    reader = csv.DictReader(io.StringIO(data, newline=''), delimiter='\t', strict=True)
    by_key = historical_keys(reader)
    identities = {}
    for key, value in english.items():
        row = by_key.get(key)
        if row is None or parse_definition(value) != (normalize(row['pos']), normalize(row['meaning_1'] or row['meaning_2'])):
            raise InputError(f'Historical English correspondence failed: {key}')
        identities[key] = int(row['id'])
    result = {'schema': 1, 'english_sha256': sha256(english_path), 'spanish_sha256': sha256(spanish_path),
              'historical_sources': [{k: v for k, v in entry.items() if k != 'path'} for entry in manifest['headwords']],
              'headword_ids': identities}
    # Recheck acquisition pins before publishing any derived identity dictionary.
    for entry in [manifest['english'], manifest['spanish'], *manifest['headwords']]:
        verify_file(entry, manifest_path.parent)
    with output.open('x', encoding='utf-8') as stream:
        stream.write(json.dumps(result, ensure_ascii=False, sort_keys=True, indent=2) + '\n')
    return result


def historical_keys(reader):
    required = {'id', 'lemma_1', 'pos', 'meaning_1', 'meaning_2'}
    if not reader.fieldnames or not required <= set(reader.fieldnames):
        raise InputError('Historical headwords lack required columns')
    result, ids = {}, set()
    for row in reader:
        if None in row or any(row.get(k) is None for k in required):
            raise InputError('Malformed historical headword row')
        identifier = positive_integer(int(row['id']), 'historical headword ID')
        key = row['lemma_1']
        if not key or key in result or identifier in ids:
            raise InputError('Duplicate or empty historical identity')
        result[key] = {column: row[column] for column in required}
        ids.add(identifier)
    return result


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--manifest', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    result = build(args.manifest.resolve(), args.output.resolve())
    print(f'PASS: {len(result["headword_ids"])} unique historical Spanish identities')

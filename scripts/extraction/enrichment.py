"""Pinned translation layers over an immutable English candidate."""

from contextlib import closing
import json
from pathlib import Path
import shutil
import sqlite3

from .candidate import OUTPUTS, validate_candidate
from .inputs import InputError, read_json, sha256, verify_file
from .russian_meanings import load_russian_meanings
from .spanish_meanings import spanish_records

TABLE = 'localized_meanings'
SCHEMA = '''CREATE TABLE localized_meanings (
    headword_id INTEGER NOT NULL,
    language TEXT NOT NULL,
    meaning TEXT NOT NULL CHECK(length(trim(meaning)) > 0),
    PRIMARY KEY (headword_id, language)
) WITHOUT ROWID'''


def connect(path: Path):
    return closing(sqlite3.connect(path.resolve().as_uri() + '?mode=ro&immutable=1', uri=True))


def dump_json(path: Path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, sort_keys=True, indent=2) + '\n', encoding='utf-8')


def selected_words(database: Path) -> list[dict]:
    result = []
    with connect(database) as db:
        for kind in ('nouns', 'verbs'):
            result.extend(dict(kind=kind, headword_id=row[0], lemma_id=row[1], primary=bool(row[2]))
                          for row in db.execute(f'SELECT id, lemma_id, practice_primary FROM {kind} ORDER BY id'))
    return result


def read_sources(manifest_path: Path, english: Path):
    manifest = read_json(manifest_path)
    if not isinstance(manifest, dict) or set(manifest) != {'schema', 'english_sha256', 'dpd', 'sources'}:
        raise InputError('Translation manifest requires schema, english_sha256, dpd, and sources')
    if type(manifest['schema']) is not int or manifest['schema'] != 1:
        raise InputError('Unsupported translation manifest')
    if manifest['english_sha256'] != sha256(english / 'pali.db'):
        raise InputError('Translation manifest belongs to another English checkpoint')
    sources = manifest['sources']
    if not isinstance(sources, dict) or not sources or set(sources) - {'ru', 'es', 'es_english'}:
        raise InputError('Unsupported translation languages')
    if ('es' in sources) != ('es_english' in sources):
        raise InputError('Spanish requires paired English and Spanish exports')
    paths = {language: verify_file(entry, manifest_path.parent) for language, entry in sources.items()}
    if 'es' in paths and sources['es']['source']['revision'] != sources['es_english']['source']['revision']:
        raise InputError('Spanish paired exports must use the same revision')
    paths['_dpd'] = verify_file(manifest['dpd'], manifest_path.parent)
    if manifest['dpd']['sha256'] != read_json(english / 'candidate.json')['inputs']['dpd']['sha256']:
        raise InputError('Translation identity reference differs from the English DPD source')
    return manifest, paths


def translation_records(words: list[dict], meanings: dict[int, str], language: str) -> list[dict]:
    records = []
    for word in words:
        identifier = word['headword_id']
        meaning = meanings.get(identifier, '')
        status = 'accepted' if meaning else ('empty' if identifier in meanings else 'missing')
        records.append({**word, 'language': language, 'meaning': meaning,
                        'source_key': str(identifier), 'status': status})
    return records


def coverage(records: list[dict]) -> dict:
    result = {}
    for kind in ('nouns', 'verbs'):
        selected = [row for row in records if row['kind'] == kind]
        primary = [row for row in selected if row['primary']]
        result[kind] = {
            'selected_senses': len(selected),
            'translated_senses': sum(row['status'] == 'accepted' for row in selected),
            'primary_lemmas': len(primary),
            'translated_primary_lemmas': sum(row['status'] == 'accepted' for row in primary),
        }
    return result


def source_evidence(entry: dict) -> dict:
    return {key: value for key, value in entry.items() if key != 'path'}


def expected_layer(manifest: dict, paths: dict, english: Path) -> dict:
    words = selected_words(english / 'pali.db')
    selected_ids = {row['headword_id'] for row in words}
    with connect(paths['_dpd']) as dpd:
        known_ids = {row[0] for row in dpd.execute('SELECT id FROM dpd_headwords')}
        headwords = (dpd.execute('SELECT id, lemma_1, pos, meaning_1, meaning_2 FROM dpd_headwords').fetchall()
                     if 'es' in paths else [])
    layers = {}
    for language in sorted(set(manifest['sources']) - {'es_english'}):
        if language == 'ru':
            meanings = load_russian_meanings(paths[language])
            records = translation_records(words, meanings, language)
            extra = {'unselected_source_ids': sorted(set(meanings) & known_ids - selected_ids),
                     'unknown_source_ids': sorted(set(meanings) - known_ids)}
        else:
            records, extra = spanish_records(words, headwords, paths['es_english'], paths['es'])
            extra['reference_source'] = source_evidence(manifest['sources']['es_english'])
        layers[language] = {'source': source_evidence(manifest['sources'][language]),
                            'records': records, 'coverage': coverage(records), **extra}
    return layers


def layer_rows(layers: dict) -> list[tuple]:
    rows = {}
    for language, layer in layers.items():
        for record in layer['records']:
            if record['status'] == 'accepted':
                key = (record['headword_id'], language)
                if key in rows and rows[key] != record['meaning']:
                    raise InputError(f'Conflicting translation: {key}')
                rows[key] = record['meaning']
    return [(identifier, language, meaning) for (identifier, language), meaning in sorted(rows.items())]


def verify_english_unchanged(english_db: Path, enriched_db: Path):
    with connect(english_db) as original, connect(enriched_db) as translated:
        objects = original.execute('SELECT type, name, tbl_name, sql FROM sqlite_master ORDER BY type, name').fetchall()
        actual = translated.execute('SELECT type, name, tbl_name, sql FROM sqlite_master WHERE tbl_name != ? ORDER BY type, name', (TABLE,)).fetchall()
        if objects != actual:
            raise InputError('English schema changed during enrichment')
        if original.execute('PRAGMA user_version').fetchone() != translated.execute('PRAGMA user_version').fetchone():
            raise InputError('English database version changed during enrichment')
        for kind, name, _, _ in objects:
            if kind == 'table':
                quoted = '"' + name.replace('"', '""') + '"'
                before = sorted(original.execute(f'SELECT * FROM {quoted}').fetchall(), key=repr)
                after = sorted(translated.execute(f'SELECT * FROM {quoted}').fetchall(), key=repr)
                if before != after:
                    raise InputError(f'English table changed during enrichment: {name}')


def bundle_manifest(english: Path, output: Path, report: dict) -> dict:
    return {'schema': 1, 'kind': 'multilingual-database',
            'english': read_json(english / 'candidate.json'),
            'languages': ['en', *sorted(report['layers'])],
            'translations': {language: {key: layer[key] for key in ('source', 'coverage', 'reference_source') if key in layer}
                             for language, layer in report['layers'].items()},
            'outputs': {name: sha256(output / name) for name in (*OUTPUTS, 'enrichment.json')}}


def validate_enrichment(english: Path, manifest_path: Path, output: Path):
    if (output / 'BUILDING').exists():
        raise InputError('Translation build is incomplete')
    validate_candidate(english)
    manifest, paths = read_sources(manifest_path, english)
    layers = expected_layer(manifest, paths, english)
    report = read_json(output / 'enrichment.json')
    if report != {'schema': 1, 'english_sha256': manifest['english_sha256'],
                  'database_sha256': sha256(output / 'pali.db'), 'layers': layers}:
        raise InputError('Translation evidence differs from pinned source or output')
    verify_english_unchanged(english / 'pali.db', output / 'pali.db')
    with connect(output / 'pali.db') as db:
        if db.execute('PRAGMA integrity_check').fetchall() != [('ok',)]:
            raise InputError('Invalid enriched SQLite database')
        actual = db.execute('SELECT headword_id, language, meaning FROM localized_meanings ORDER BY headword_id, language').fetchall()
        if actual != layer_rows(layers):
            raise InputError('Stored meanings differ from pinned source')
    for name in OUTPUTS:
        if name != 'pali.db' and sha256(output / name) != sha256(english / name):
            raise InputError(f'English artifact changed: {name}')
    if read_json(output / 'bundle.json') != bundle_manifest(english, output, report):
        raise InputError('Multilingual bundle manifest differs from verified artifacts')
    return report


def build_enrichment(english: Path, manifest_path: Path, output: Path):
    validate_candidate(english)
    manifest, paths = read_sources(manifest_path, english)
    layers = expected_layer(manifest, paths, english)
    output.mkdir(parents=True, exist_ok=False)
    (output / 'BUILDING').write_text('Incomplete translation candidate; do not promote.\n')
    for name in OUTPUTS:
        shutil.copyfile(english / name, output / name)
    with closing(sqlite3.connect(output / 'pali.db')) as db, db:
        db.execute(SCHEMA)
        db.executemany('INSERT INTO localized_meanings VALUES (?, ?, ?)', layer_rows(layers))
    if read_sources(manifest_path, english) != (manifest, paths):
        raise InputError('Translation inputs changed during enrichment')
    verify_english_unchanged(english / 'pali.db', output / 'pali.db')
    dump_json(output / 'enrichment.json', {'schema': 1, 'english_sha256': manifest['english_sha256'],
                                         'database_sha256': sha256(output / 'pali.db'), 'layers': layers})
    dump_json(output / 'bundle.json', bundle_manifest(english, output, read_json(output / 'enrichment.json')))
    (output / 'BUILDING').unlink()
    validate_enrichment(english, manifest_path, output)
    return output

"""Released practice identities and explicit single-paradigm selection."""

from contextlib import closing
import json
import sqlite3
from pathlib import Path

from .inputs import InputError, read_json, sha256
from .registry import validate_registry

BASELINE = Path(__file__).resolve().parents[1] / 'baselines/v1.1'


def load_baseline() -> tuple[dict, dict[str, list[dict]]]:
    manifest = read_json(BASELINE / 'manifest.json')
    for name, entry in manifest['files'].items():
        if sha256(BASELINE / name) != entry['sha256']:
            raise InputError(f'Released baseline checksum mismatch: {name}')
    registry = read_json(BASELINE / 'lemma_registry.json')
    with closing(sqlite3.connect((BASELINE / 'pali.db').as_uri() + '?mode=ro&immutable=1', uri=True)) as db:
        db.row_factory = sqlite3.Row
        words = {table: [dict(row) for row in db.execute(f'SELECT * FROM {table} ORDER BY id')]
                 for table in ('nouns', 'verbs')}
    return registry, words


def require_historical_registry(registry: dict, historical: dict):
    validate_registry(registry)
    for table, counter in (('nouns', 'next_noun_id'), ('verbs', 'next_verb_id')):
        for lemma, identifier in historical[table].items():
            if registry[table].get(lemma) != identifier:
                raise InputError(f'Historical {table} mapping removed or reassigned: {lemma}')
        for lemma, identifier in registry[table].items():
            if lemma not in historical[table] and identifier < historical[counter]:
                raise InputError(f'New {table} identity is not append-only: {lemma}')
        if registry[counter] < historical[counter]:
            raise InputError(f'Historical allocation counter decreased: {counter}')


def groups(rows: list[dict]) -> dict[int, list[dict]]:
    result = {}
    for row in rows:
        result.setdefault(row['lemma_id'], []).append(row)
    return result


def legacy_primary(rows: list[dict]) -> dict:
    """Exact v1.1 Lemma selection, used only to establish released identities."""
    patterns = {}
    for row in rows:
        patterns.setdefault(row['pattern'], []).append(row)
    pattern = min(patterns, key=lambda key: (-len(patterns[key]), min(r['id'] for r in patterns[key])))
    return min(patterns[pattern], key=lambda row: (-row['ebt_count'], row['id']))


def paradigm(row: dict) -> dict:
    return {name: row.get(name, 0) for name in ('pattern', 'stem', 'gender')}


def anchor(row: dict, kind: str) -> dict:
    return {'lemma': row['lemma'], 'kind': kind, 'anchor_headword_id': row['id'],
            **paradigm(row)}


def historical_practice_registry(words: dict[str, list[dict]]) -> dict:
    return {'schema': 1, 'choices': {
        str(identifier): anchor(legacy_primary(rows), table)
        for table, table_rows in words.items() for identifier, rows in sorted(groups(table_rows).items())
    }}


def permitted_correction(identifier: str, old: dict, new: dict, corrections: dict) -> bool:
    correction = corrections.get(identifier)
    return (isinstance(correction, dict) and correction.get('from') == paradigm(old)
            and correction.get('to') == paradigm(new)
            and correction.get('anchor_headword_id') == old['anchor_headword_id'])


def validate_choice(identifier, choice, registry):
    fields = {'lemma', 'kind', 'anchor_headword_id', 'pattern', 'stem', 'gender'}
    if not isinstance(identifier, str) or not identifier.isdecimal():
        raise InputError('Invalid practice ID')
    if not isinstance(choice, dict) or set(choice) != fields:
        raise InputError(f'Invalid practice choice: {identifier}')
    if choice['kind'] not in ('nouns', 'verbs') or type(choice['anchor_headword_id']) is not int:
        raise InputError(f'Invalid practice kind/headword: {identifier}')
    if not all(isinstance(choice[key], str) for key in ('lemma', 'pattern', 'stem')):
        raise InputError(f'Invalid practice text: {identifier}')
    genders = (1, 2, 3) if choice['kind'] == 'nouns' else (0,)
    if type(choice['gender']) is not int or choice['gender'] not in genders:
        raise InputError(f'Invalid practice gender: {identifier}')
    if registry[choice['kind']].get(choice['lemma']) != int(identifier):
        raise InputError(f'Practice choice is not in lemma registry: {identifier}')


def require_practice_registry(current: dict, historical: dict, registry: dict, corrections: dict):
    if not isinstance(current, dict) or current.get('schema') != 1 or not isinstance(current.get('choices'), dict):
        raise InputError('Invalid practice registry')
    for identifier, old in historical['choices'].items():
        new = current['choices'].get(identifier)
        if new == old:
            continue
        if not isinstance(new, dict) or not permitted_correction(identifier, old, new, corrections):
            raise InputError(f'Historical practice paradigm removed or reassigned: {identifier}')
        if (new['lemma'], new['kind'], new['anchor_headword_id']) != (old['lemma'], old['kind'], old['anchor_headword_id']):
            raise InputError(f'Correction changed practice identity: {identifier}')
    for identifier, choice in current['choices'].items():
        validate_choice(identifier, choice, registry)


def select_primary(identifier: str, rows: list[dict], old: dict | None, corrections: dict) -> tuple[dict, str]:
    if old is None:
        return min(rows, key=lambda row: (-row['ebt_count'], row['id'])), 'new_practice_identity'
    matching = [row for row in rows if paradigm(row) == paradigm(old)]
    change = 'same_paradigm'
    if not matching:
        correction = corrections.get(identifier, {})
        if correction.get('from') != paradigm(old) or correction.get('anchor_headword_id') != old['anchor_headword_id']:
            raise InputError(f'No compatible practice paradigm for {old["lemma"]} ({identifier})')
        matching = [row for row in rows if paradigm(row) == correction.get('to')]
        change = 'source_correction'
    if not matching:
        raise InputError(f'Approved correction has no matching source rows: {identifier}')
    primary = min(matching, key=lambda row: (-row['ebt_count'], row['id']))
    if change == 'same_paradigm' and primary['id'] != old['anchor_headword_id']:
        change = 'compatible_primary_sense'
    return primary, change


def plan_practice(words: dict[str, list[dict]], current: dict, corrections: dict) -> tuple[dict, dict, list[dict]]:
    proposed = json.loads(json.dumps(current))
    selected, changes = {}, []
    for table, rows in words.items():
        for identifier, senses in sorted(groups(rows).items()):
            key = str(identifier)
            old = current['choices'].get(key)
            primary, change = select_primary(key, senses, old, corrections)
            selected[identifier] = primary['id']
            if old is None:
                proposed['choices'][key] = anchor(primary, table)
            elif change == 'source_correction':
                proposed['choices'][key] = {**old, **paradigm(primary)}
            changes.append({'lemma_id': identifier, 'lemma': primary['lemma'], 'kind': table,
                            'classification': change, 'old': old, 'selected': anchor(primary, table)})
    return selected, proposed, changes

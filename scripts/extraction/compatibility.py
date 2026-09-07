"""Read-only comparison against released practice identities and content."""

from contextlib import closing
import sqlite3
from pathlib import Path

from .identity import (BASELINE, groups, legacy_primary, load_baseline, paradigm,
                       historical_practice_registry, require_historical_registry,
                       require_practice_registry, select_primary)
from .inputs import InputError, read_json


def connect(path: Path):
    db = sqlite3.connect(path.resolve().as_uri() + '?mode=ro&immutable=1', uri=True)
    db.row_factory = sqlite3.Row
    return closing(db)


def words_from(db):
    return {kind: [dict(row) for row in db.execute(f'SELECT * FROM {kind} ORDER BY id')]
            for kind in ('nouns', 'verbs')}


def validate_identities(directory: Path):
    historical, old_words = load_baseline()
    registry = read_json(directory / 'lemma_registry.json')
    require_historical_registry(registry, historical)
    choices = read_json(directory / 'practice_registry.json')
    corrections = read_json(directory / 'paradigm_corrections.json')
    require_practice_registry(choices, historical_practice_registry(old_words), registry, corrections)
    with connect(directory / 'pali.db') as db:
        words = words_from(db)
    for kind, rows in words.items():
        for identifier, senses in groups(rows).items():
            if any(row.get('practice_primary') not in (0, 1) for row in senses):
                raise InputError(f'Invalid practice selection flag: {identifier}')
            selected = [row for row in senses if row['practice_primary'] == 1]
            choice = choices['choices'].get(str(identifier))
            if choice is None or len(selected) != 1:
                raise InputError(f'Exactly one practice sense is required: {identifier}')
            expected, _ = select_primary(str(identifier), senses, choice, corrections)
            if selected[0]['id'] != expected['id']:
                raise InputError(f'Practice sense disagrees with registry: {identifier}')
            if any(registry[kind].get(row['lemma']) != identifier for row in senses):
                raise InputError(f'Word disagrees with historical lemma registry: {identifier}')


def noun_eligible(form_id, primary):
    lemma, grammar = divmod(form_id, 10000)
    case, grammar = divmod(grammar, 1000)
    gender, grammar = divmod(grammar, 100)
    number, ending = divmod(grammar, 10)
    return (lemma in primary and 1 <= case <= 8 and gender == primary[lemma]['gender']
            and number in (1, 2) and 1 <= ending <= 6)


def verb_eligible(form_id, primary, nonreflexive):
    lemma, grammar = divmod(form_id, 100000)
    tense, grammar = divmod(grammar, 10000)
    person, grammar = divmod(grammar, 1000)
    number, grammar = divmod(grammar, 100)
    voice, ending = divmod(grammar, 10)
    valid = (lemma in primary and 1 <= tense <= 4 and person in (1, 2, 3)
             and number in (1, 2) and voice in (1, 2) and 1 <= ending <= 7)
    citation = (tense, person, number, voice) == (1, 3, 1, 1)
    return valid and not citation and not (voice == 2 and lemma in nonreflexive)


def eligible(db, kind, primary):
    nonreflexive = {row[0] for row in db.execute('SELECT lemma_id FROM verbs_nonreflexive')}
    result = set()
    columns = {row[1] for row in db.execute(f'PRAGMA table_info({kind}_corpus_forms)')}
    scoped = 'headword_id' in columns
    for row in db.execute(f'SELECT * FROM {kind}_corpus_forms'):
        if scoped and row['headword_id'] not in {word['id'] for word in primary.values()}:
            continue
        form_id = row['form_id']
        valid = (noun_eligible(form_id, primary) if kind == 'nouns'
                 else verb_eligible(form_id, primary, nonreflexive))
        if valid:
            result.add(form_id - form_id % 10)
    return result


def delta(old, new):
    return {'retained_count': len(old & new), 'added': sorted(new - old), 'removed': sorted(old - new)}


def content_changes(old_db, new_db, kind, old_primary, new_primary):
    old_details = {row['id']: dict(row) for row in old_db.execute(f'SELECT * FROM {kind}_details')}
    new_details = {row['id']: dict(row) for row in new_db.execute(f'SELECT * FROM {kind}_details')}
    result = []
    for identifier in sorted(old_primary.keys() & new_primary.keys()):
        old = old_details[old_primary[identifier]['id']]
        new = new_details[new_primary[identifier]['id']]
        changes = {key: {'old': old[key], 'new': new[key]} for key in sorted(old.keys() & new.keys())
                   if key not in ('id', 'meaning_ru') and old[key] != new[key]}
        if changes:
            result.append({'lemma_id': identifier, 'fields': changes})
    return result


def comparison(directory: Path, selection_changes: list[dict]):
    result = {'schema': 1, 'baseline': 'v1.1', 'selection': selection_changes, 'kinds': {},
              'eligibility_contract': 'All ranks/patterns/grammar enabled; active verb citation excluded. '
              'Uses stored corpus flags and repository ending bounds; M4 must verify attestation.'}
    with connect(BASELINE / 'pali.db') as old_db, connect(directory / 'pali.db') as new_db:
        old_words, new_words = words_from(old_db), words_from(new_db)
        for kind in ('nouns', 'verbs'):
            old = {key: legacy_primary(rows) for key, rows in groups(old_words[kind]).items()}
            new = {row['lemma_id']: row for row in new_words[kind] if row['practice_primary']}
            eligible_delta = delta(eligible(old_db, kind, old), eligible(new_db, kind, new))
            divisor = 10000 if kind == 'nouns' else 100000
            eligible_delta['removed_with_cutoff_lemma'] = [identifier for identifier in eligible_delta['removed']
                                                         if identifier // divisor not in new]
            result['kinds'][kind] = {
                'cutoff': delta(set(old), set(new)), 'eligibility': eligible_delta,
                'content_changes': content_changes(old_db, new_db, kind, old, new),
                'paradigm_changes': [{'lemma_id': identifier, 'old': paradigm(old[identifier]),
                                      'new': paradigm(new[identifier])}
                                     for identifier in sorted(old.keys() & new.keys())
                                     if paradigm(old[identifier]) != paradigm(new[identifier])],
            }
    return result

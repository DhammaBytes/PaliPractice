"""Validate scoped records and the primary template contract without generation."""

from .compatibility import connect, words_from
from .config import IRREGULAR_NOUN_PATTERNS, IRREGULAR_VERB_PATTERNS
from .inputs import InputError, read_json


def valid_id(identifier, word, kind):
    if type(identifier) is not int:
        return False
    divisor = 10000 if kind == 'nouns' else 100000
    if identifier // divisor != word['lemma_id']:
        return False
    digits = [identifier // (10 ** power) % 10 for power in range(4 if kind == 'nouns' else 5)]
    if kind == 'nouns':
        ending, number, gender, case = digits
        return 1 <= ending <= 6 and number in (1, 2) and gender == word['gender'] and 1 <= case <= 8
    ending, voice, number, person, tense = digits
    return 1 <= ending <= 7 and voice in (1, 2) and number in (1, 2) and 1 <= person <= 3 and 1 <= tense <= 4


def read_scoped(db, table, words, kind):
    columns = {row[1] for row in db.execute(f'PRAGMA table_info({table})')}
    if not {'headword_id', 'form_id', 'form'} <= columns:
        raise InputError(f'Missing scoped form columns: {table}')
    result = {}
    for row in db.execute(f'SELECT headword_id, form_id, form FROM {table}'):
        headword, identifier, form = row
        word = words.get(headword)
        if word is None or not valid_id(identifier, word, kind):
            raise InputError(f'Invalid headword/grammar in {table}: {headword}/{identifier}')
        if not isinstance(form, str) or not form or form != form.strip():
            raise InputError(f'Invalid rendered string in {table}: {headword}/{identifier}')
        key = (headword, identifier)
        if key in result:
            raise InputError(f'Duplicate scoped form identity in {table}: {key}')
        result[key] = form
    return result


def primary_row(row, words):
    if not isinstance(row, list) or len(row) != 4 or type(row[0]) is not int:
        raise InputError('Malformed primary form contract')
    headword, identifier, form, attested = row
    word = words.get(headword)
    if word is None or not word['practice_primary']:
        raise InputError(f'Primary form refers to an unselected headword: {headword}')
    kind = 'nouns' if word['lemma_id'] < 70000 else 'verbs'
    if not valid_id(identifier, word, kind) or type(attested) is not int or attested not in (0, 1):
        raise InputError(f'Invalid primary grammar/attestation: {headword}/{identifier}')
    if not isinstance(form, str) or not form or form != form.strip():
        raise InputError('Invalid primary rendered string')
    return (headword, identifier), (form, attested)


def read_primary(directory, words):
    expected = read_json(directory / 'primary_forms.json')
    if not isinstance(expected, list) or not expected:
        raise InputError('Missing primary form contract')
    result = {}
    for row in expected:
        key, value = primary_row(row, words)
        if key in result:
            raise InputError(f'Duplicate primary form identity: {key}')
        result[key] = value
    if {key[0] for key in result} != {word['id'] for word in words.values() if word['practice_primary']}:
        raise InputError('Selected headword has no primary forms')
    return result


def primary_records(records, words):
    return {key: value for key, value in records.items() if words[key[0]]['practice_primary']}


def validate_kind(db, kind, patterns, kind_words, expected):
    corpus = read_scoped(db, kind + '_corpus_forms', kind_words, kind)
    irregular = read_scoped(db, kind + '_irregular_forms', kind_words, kind)
    primary = {key: value for key, value in expected.items() if key[0] in kind_words}
    required = {key: form for key, (form, attested) in primary.items() if attested}
    if primary_records(corpus, kind_words) != required:
        raise InputError(f'{kind} primary corpus records disagree with template contract')
    required_irregular = {key: form for key, (form, _) in primary.items() if kind_words[key[0]]['pattern'] in patterns}
    if primary_records(irregular, kind_words) != required_irregular:
        raise InputError(f'{kind} primary irregular records disagree with template contract')
    if any(kind_words[key[0]]['pattern'] not in patterns for key in irregular):
        raise InputError(f'{kind} regular headword has irregular form records')


def validate_forms(directory):
    with connect(directory / 'pali.db') as db:
        by_kind = words_from(db)
        words = {word['id']: word for rows in by_kind.values() for word in rows}
        expected = read_primary(directory, words)
        for kind, patterns in (('nouns', IRREGULAR_NOUN_PATTERNS), ('verbs', IRREGULAR_VERB_PATTERNS)):
            kind_words = {word['id']: word for word in by_kind[kind]}
            validate_kind(db, kind, patterns, kind_words, expected)

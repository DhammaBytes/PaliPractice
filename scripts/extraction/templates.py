"""Strict selected-template parsing with explicit grammar and ending bounds."""

import json

from .forms import clean_stem
from .grammar import pos_to_gender
from .inputs import InputError

CASES = {'nom': 1, 'acc': 2, 'instr': 3, 'dat': 4, 'abl': 5, 'gen': 6, 'loc': 7, 'voc': 8}
GENDERS = {'masc': 1, 'fem': 2, 'nt': 3}
NUMBERS = {'sg': 1, 'pl': 2}
TENSES = {'pr': 1, 'imp': 2, 'opt': 3, 'fut': 4}
PERSONS = {'1st': 1, '2nd': 2, '3rd': 3}
MAX_ENDINGS = {'noun': 6, 'verb': 7}


def noun_grammar(text, label, pos):
    parts = text.split()
    if len(parts) != 3 or parts[0] not in GENDERS or parts[1] not in CASES or parts[2] not in NUMBERS:
        raise InputError(f'Unknown noun grammar: {text!r}')
    if parts[1] != label or GENDERS[parts[0]] != pos_to_gender(pos):
        raise InputError(f'Contradictory noun grammar: {text!r}, {label!r}, {pos!r}')
    return {'case_name': CASES[parts[1]], 'gender': GENDERS[parts[0]], 'number': NUMBERS[parts[2]]}


def verb_grammar(text, label):
    parts = text.split()
    reflexive = parts[:1] == ['reflx']
    if reflexive:
        parts = parts[1:]
    if len(parts) != 3 or parts[0] not in TENSES or parts[1] not in PERSONS or parts[2] not in NUMBERS:
        raise InputError(f'Unknown verb grammar: {text!r}')
    if ' '.join(parts[:2]) != label:
        raise InputError(f'Contradictory verb grammar: {text!r}, {label!r}')
    return {'tense': TENSES[parts[0]], 'person': PERSONS[parts[1]],
            'number': NUMBERS[parts[2]], 'reflexive': int(reflexive)}


def string_list(value):
    if not isinstance(value, list) or not all(isinstance(item, str) for item in value):
        raise InputError('Template cells must be lists of strings')
    return value


def cell_forms(endings, grammar, label, word, kind, corpus):
    endings = string_list(endings)
    grammar = string_list(grammar)
    if not any(endings):
        return []
    if len(grammar) != 1 or not grammar[0]:
        raise InputError(f'Missing grammar for selected template {word.pattern}')
    if len(endings) > MAX_ENDINGS[kind] or not all(endings):
        raise InputError(f'Unsupported ending count or gap in {word.pattern}: {endings}')
    parsed = (noun_grammar(grammar[0], label, word.pos) if kind == 'noun'
              else verb_grammar(grammar[0], label))
    stem = clean_stem(word.stem)
    forms = []
    for index, ending in enumerate(endings):
        form = stem + ('' if ending == '-' else ending)
        if not form or form != form.strip():
            raise InputError(f'Empty or untrimmed rendered form in {word.pattern}')
        forms.append({'form': form, 'in_corpus': int(form in corpus), 'ending_index': index, **parsed})
    return forms


def row_forms(row, word, kind, corpus):
    if not isinstance(row, list) or len(row) < 3 or len(row) % 2 != 1:
        raise InputError(f'Malformed selected template row: {word.pattern}')
    labels = string_list(row[0])
    if len(labels) != 1:
        raise InputError(f'Malformed template label: {word.pattern}')
    label = labels[0]
    if kind == 'noun' and label == 'in comps':
        return []
    verb_labels = {f'{t} {p}' for t in TENSES for p in PERSONS}
    valid_label = label in CASES if kind == 'noun' else label in verb_labels
    if not valid_label:
        raise InputError(f'Unknown template row label: {label!r}')
    forms = []
    for index in range(1, len(row), 2):
        forms.extend(cell_forms(row[index], row[index + 1], label, word, kind, corpus))
    return forms


def parse_template(word, kind, corpus):
    if kind not in MAX_ENDINGS or not word.it or not word.it.data:
        raise InputError(f'Missing selected inflection template for headword {word.id}')
    try:
        data = json.loads(word.it.data)
    except (json.JSONDecodeError, TypeError) as error:
        raise InputError(f'Malformed selected template: {word.pattern}') from error
    if not isinstance(data, list) or len(data) < 2:
        raise InputError(f'Empty selected template: {word.pattern}')
    forms = []
    for row in data[1:]:
        forms.extend(row_forms(row, word, kind, corpus))
    if not forms:
        raise InputError(f'No usable forms for selected headword {word.id}')
    return forms, len(forms), sum(not form['in_corpus'] for form in forms)

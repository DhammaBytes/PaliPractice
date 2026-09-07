"""Headword-scoped rendered forms; public combination IDs remain unchanged."""

from .forms import compute_declension_form_id, compute_conjugation_form_id
from .inputs import InputError


def insert_form(cursor, table, headword_id, form_id, form):
    old = cursor.execute(f'SELECT form FROM {table} WHERE headword_id=? AND form_id=?',
                         (headword_id, form_id)).fetchone()
    if old is not None:
        if old[0] != form:
            raise InputError(f'Conflicting rendered form: {table} {headword_id}/{form_id}: {old[0]!r} vs {form!r}')
        return False
    cursor.execute(f'INSERT INTO {table} (headword_id, form_id, form) VALUES (?, ?, ?)',
                   (headword_id, form_id, form))
    return True


def public_form_id(kind, lemma_id, form):
    ending = form['ending_index'] + 1
    if kind == 'nouns':
        return compute_declension_form_id(lemma_id, form['case_name'], form['gender'], form['number'], ending)
    return compute_conjugation_form_id(lemma_id, form['tense'], form['person'], form['number'], form['reflexive'], ending)


def store_forms(cursor, kind, word, lemma_id, forms, irregular):
    count = 0
    # Validate all internal identities, including theoretical regular forms.
    seen = {}
    for form in forms:
        identifier = public_form_id(kind, lemma_id, form)
        previous = seen.setdefault(identifier, form['form'])
        if previous != form['form']:
            raise InputError(f'Conflicting template endings for headword {word.id}/{identifier}')
        if form['in_corpus']:
            count += insert_form(cursor, kind + '_corpus_forms', word.id, identifier, form['form'])
        if irregular:
            insert_form(cursor, kind + '_irregular_forms', word.id, identifier, form['form'])
    return count

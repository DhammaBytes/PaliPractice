"""Pinned terminology preferences and reviewed full translation replacements."""

from pathlib import Path

from .inputs import InputError, positive_integer, read_json


def load_overrides(path: Path, headwords: dict[int, tuple[str, str]], languages: set[str]) -> dict:
    data = read_json(path)
    if (not isinstance(data, dict) or set(data) - {'schema', 'primary', 'replace'}
            or not {'schema', 'primary'} <= set(data) or type(data['schema']) is not int or data['schema'] != 1):
        raise InputError('Invalid localized overrides schema')
    result = {}
    for mode in ('primary', 'replace'):
        load_section(data.get(mode, {}), mode, headwords, languages, result)
    return result


def load_section(section, mode, headwords, languages, result):
    if not isinstance(section, dict) or set(section) - {'es', 'ru'}:
        raise InputError('Localized overrides support only ES and RU')
    if set(section) - languages:
        raise InputError('Localized overrides require the corresponding translation layer')
    for language, entries in section.items():
        if not isinstance(entries, dict):
            raise InputError('Localized overrides must be keyed by headword ID')
        targets = result.setdefault(language, {})
        for key, entry in entries.items():
            identifier = validate_target(key, entry, headwords, mode)
            if identifier in targets:
                raise InputError(f'Conflicting localized overrides: {language}:{identifier}')
            if mode == 'primary':
                validate_preferred(entry['preferred'])
            else:
                validate_replacement(entry, headwords[identifier][1])
            targets[identifier] = entry


def validate_target(key: str, entry, headwords, mode: str) -> int:
    if not key.isascii() or not key.isdecimal() or str(int(key)) != key:
        raise InputError('Invalid localized override headword ID')
    identifier = positive_integer(int(key), 'Localized override headword ID')
    fields = ({'lemma_1', 'preferred'} if mode == 'primary' else
              {'lemma_1', 'meaning_1', 'source_meaning', 'replacement', 'reason'})
    if not isinstance(entry, dict) or set(entry) != fields:
        raise InputError('Invalid localized override fields')
    if identifier not in headwords or entry['lemma_1'] != headwords[identifier][0]:
        raise InputError(f'Stale localized override target: {identifier}')
    return identifier


def validate_replacement(entry, english):
    if entry['meaning_1'] != english:
        raise InputError('Stale localized replacement English target')
    if not isinstance(entry['source_meaning'], str):
        raise InputError('Localized replacement requires its original source meaning')
    if not isinstance(entry['replacement'], str):
        raise InputError('Localized replacement requires plain-text meaning')
    validate_preferred(entry['replacement'].split('; '))
    if not isinstance(entry['reason'], str) or not entry['reason'].strip():
        raise InputError('Localized replacement requires a review reason')


def validate_preferred(preferred):
    if not isinstance(preferred, list) or not preferred:
        raise InputError('Localized preferred meanings must be a nonempty list')
    if any(not isinstance(term, str) or not term.strip() or term != term.strip()
           or any(char in term for char in (';', '\x00', '\n', '\r', '<', '>')) for term in preferred):
        raise InputError('Localized preferred meanings must be individual plain-text terms')
    if len({term.casefold() for term in preferred}) != len(preferred):
        raise InputError('Duplicate localized preferred meaning')


def make_first(meaning: str, preferred: list[str]) -> str:
    """Move matching terms or prepend new ones, preserving all other meanings."""
    leading = {term.casefold() for term in preferred}
    remainder = [part.strip() for part in meaning.split(';')
                 if part.strip() and part.strip().casefold() not in leading]
    return '; '.join([*preferred, *remainder])


def apply_overrides(records: list[dict], overrides: dict[int, dict]) -> list[dict]:
    result = []
    for record in records:
        entry = overrides.get(record['headword_id'])
        if entry is None:
            result.append(record)
            continue
        if 'replacement' in entry:
            if record['meaning'] != entry['source_meaning']:
                raise InputError(f'Stale localized replacement source: {record["headword_id"]}')
            meaning = entry['replacement']
        else:
            # Rejected mappings must not leak into an explicitly supplied translation.
            source = record['meaning'] if record['status'] == 'accepted' else ''
            meaning = make_first(source, entry['preferred'])
        result.append({**record, 'status': 'accepted', 'meaning': meaning,
                       'override': {**entry, 'source_status': record['status'],
                                    'source_meaning': record['meaning']}})
    return result

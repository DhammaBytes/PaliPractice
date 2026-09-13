"""Pinned, language-specific preferences applied after source identity mapping."""

from pathlib import Path

from .inputs import InputError, positive_integer, read_json


def load_overrides(path: Path, headwords: dict[int, str], languages: set[str]) -> dict:
    data = read_json(path)
    if not isinstance(data, dict) or set(data) != {'schema', 'primary'} or type(data['schema']) is not int or data['schema'] != 1:
        raise InputError('Invalid localized overrides schema')
    primary = data['primary']
    if not isinstance(primary, dict) or set(primary) - {'es', 'ru'}:
        raise InputError('Localized overrides support only ES and RU')
    if set(primary) - languages:
        raise InputError('Localized overrides require the corresponding translation layer')
    result = {}
    for language, entries in primary.items():
        if not isinstance(entries, dict):
            raise InputError('Localized overrides must be keyed by headword ID')
        result[language] = {}
        for key, entry in entries.items():
            identifier = validate_target(key, entry, headwords)
            validate_preferred(entry['preferred'])
            result[language][identifier] = entry['preferred']
    return result


def validate_target(key: str, entry, headwords: dict[int, str]) -> int:
    if not key.isascii() or not key.isdecimal() or str(int(key)) != key:
        raise InputError('Invalid localized override headword ID')
    identifier = positive_integer(int(key), 'Localized override headword ID')
    if not isinstance(entry, dict) or set(entry) != {'lemma_1', 'preferred'}:
        raise InputError('Invalid localized override fields')
    if identifier not in headwords or entry['lemma_1'] != headwords[identifier]:
        raise InputError(f'Stale localized override target: {identifier}')
    return identifier


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


def apply_overrides(records: list[dict], overrides: dict[int, list[str]]) -> list[dict]:
    result = []
    for record in records:
        preferred = overrides.get(record['headword_id'])
        if preferred is None:
            result.append(record)
            continue
        # Rejected source mappings must not leak into an explicitly supplied translation.
        meaning = record['meaning'] if record['status'] == 'accepted' else ''
        result.append({**record, 'status': 'accepted', 'meaning': make_first(meaning, preferred),
                       'override': {'preferred': preferred, 'source_status': record['status'],
                                    'source_meaning': record['meaning']}})
    return result

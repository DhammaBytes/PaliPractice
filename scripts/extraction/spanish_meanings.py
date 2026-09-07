"""Read the known DPD Spanish export without executing JavaScript or guessing senses."""

import html
import json
import re
import unicodedata
from pathlib import Path

from .inputs import InputError


def normalize(text: str) -> str:
    return ' '.join(unicodedata.normalize('NFC', html.unescape(text)).split())


def load_assignment(path: Path, variable: str) -> dict[str, str]:
    def unique(pairs):
        result = {}
        for key, value in pairs:
            if key in result:
                raise InputError(f'Duplicate export headword: {key}')
            result[key] = value
        return result
    try:
        text = path.read_text(encoding='utf-8-sig')
        match = re.fullmatch(r'\s*let\s+' + re.escape(variable) + r'\s*=\s*(\{.*\})\s*;?\s*', text, re.S)
        if not match:
            raise InputError(f'Unsupported JavaScript export assignment: {variable}')
        result = json.loads(match[1], object_pairs_hook=unique)
    except (OSError, UnicodeError, json.JSONDecodeError) as error:
        raise InputError(f'Cannot parse pinned export {path}: {error}') from error
    if not result or any(not key.strip() or not isinstance(value, str) or not value.strip()
                         for key, value in result.items()):
        raise InputError('Export requires nonempty headword/definition strings')
    return result


def parse_definition(value: str) -> tuple[str, str]:
    """Extract only the meaning from the observed POS/bold/literal/etymology format."""
    pos, separator, body = value.partition('. ')
    if not separator or not pos or '<' in pos or '\x00' in value:
        raise InputError('Definition has no supported POS prefix')
    body = body.strip()
    # Only the final square-bracket block is etymology; unknown brackets fail below.
    body = re.sub(r'\s*\[[^\[\]<>]*\]\s*$', '', body)
    if body.startswith('<b>'):
        match = re.fullmatch(r'<b>([^<>]+)</b>(?:; lit\. [^<>]*)?', body)
        if not match:
            raise InputError('Unsupported bold definition structure')
        meaning = match[1]
    else:
        meaning = body.split('; lit. ', 1)[0]
    if any(token in meaning for token in ('<', '>', '[', ']')):
        raise InputError('Unsupported definition markup')
    meaning = normalize(meaning)
    if not meaning or any(token in meaning for token in ('<', '>')):
        raise InputError('Definition has no plain meaning text')
    return normalize(pos), meaning


def source_index(english: dict[str, str]) -> dict[tuple[str, str], list[str]]:
    index = {}
    for key, value in english.items():
        try:
            parsed = parse_definition(value)
        except InputError:
            continue
        index.setdefault(parsed, []).append(key)
    return index


def map_sense(headword: tuple, english: dict, spanish: dict, by_meaning: dict, keys: dict) -> dict:
    identifier, key, pos, meaning_1, meaning_2 = headword
    expected = (normalize(pos), normalize(meaning_1 or meaning_2))
    base = {'headword_id': identifier, 'source_key': key, 'meaning': ''}
    if len(keys[key]) != 1:
        return {**base, 'status': 'ambiguous_headword'}
    if key in english and key not in spanish:
        return {**base, 'status': 'missing_translation'}
    if key not in english:
        suggestions = sorted(by_meaning.get(expected, []))
        status = 'renamed_or_renumbered' if suggestions else 'missing_key'
        return {**base, 'status': status, 'suggested_keys': suggestions}
    try:
        paired = parse_definition(english[key])
        translated_pos, meaning = parse_definition(spanish[key])
    except InputError as error:
        return {**base, 'status': 'unparseable_definition', 'reason': str(error)}
    if paired != expected:
        return {**base, 'status': 'meaning_drift', 'expected_english': expected[1], 'source_english': paired[1]}
    if translated_pos != paired[0]:
        return {**base, 'status': 'pos_mismatch'}
    return {**base, 'status': 'accepted', 'meaning': meaning}


def spanish_records(words: list[dict], headwords: list[tuple], english_path: Path, spanish_path: Path):
    english = load_assignment(english_path, 'dpd_ebts')
    spanish = load_assignment(spanish_path, 'dpd_ebts_es')
    by_id = {row[0]: row for row in headwords}
    keys = {}
    for identifier, key, *_ in headwords:
        keys.setdefault(key, []).append(identifier)
    by_meaning = source_index(english)
    records = []
    for word in words:
        if word['headword_id'] not in by_id:
            raise InputError('Selected headword absent from pinned DPD reference')
        mapped = map_sense(by_id[word['headword_id']], english, spanish, by_meaning, keys)
        records.append({**word, **mapped, 'language': 'es'})
    return records, {'unpaired_english_keys': sorted(set(english) - set(spanish)),
                     'unpaired_spanish_keys': sorted(set(spanish) - set(english)),
                     'source_keys_absent_from_dpd': sorted(set(spanish) - set(keys))}

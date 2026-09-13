"""Historical Spanish identities and explicitly reviewed English–Spanish mappings."""

from pathlib import Path

from .inputs import InputError, positive_integer, read_json, sha256
from .spanish_meanings import load_assignment, normalize, parse_definition


def require_fields(value, fields: set[str], label: str):
    if not isinstance(value, dict) or set(value) != fields:
        raise InputError(f'Invalid {label} fields')


def require_text(value, label: str):
    if not isinstance(value, str) or not value.strip() or '\x00' in value:
        raise InputError(f'Invalid {label}')


def load_identity(path: Path, english_path: Path, spanish_path: Path):
    identity = read_json(path)
    require_fields(identity, {'schema', 'historical_sources', 'english_sha256', 'spanish_sha256',
                              'headword_ids'}, 'Spanish identity')
    if type(identity['schema']) is not int or identity['schema'] != 1:
        raise InputError('Unsupported Spanish identity schema')
    if identity['english_sha256'] != sha256(english_path) or identity['spanish_sha256'] != sha256(spanish_path):
        raise InputError('Spanish identity belongs to different source exports; rebuild it before resync')
    if not isinstance(identity['historical_sources'], list) or not identity['historical_sources']:
        raise InputError('Spanish identity requires historical provenance')
    english = load_assignment(english_path, 'dpd_ebts')
    spanish = load_assignment(spanish_path, 'dpd_ebts_es')
    ids = identity['headword_ids']
    if not isinstance(ids, dict) or set(ids) != set(english) or set(ids) != set(spanish):
        raise InputError('Spanish identity must cover both exports exactly')
    by_id = {}
    for key, identifier in ids.items():
        positive_integer(identifier, 'Spanish historical headword ID')
        if identifier in by_id:
            raise InputError('Duplicate Spanish historical headword ID')
        source_pos, source_meaning = parse_definition(english[key])
        by_id[identifier] = (key, source_pos, source_meaning)
    return by_id, spanish


def validate_review(entry: dict, headwords: dict, spanish: dict):
    require_fields(entry, {'headword_id', 'target_key', 'target_pos', 'target_english',
                           'source_keys', 'meaning', 'mode', 'reason', 'reviewer'}, 'Spanish review')
    identifier = positive_integer(entry['headword_id'], 'review headword ID')
    target = headwords.get(identifier)
    if target is None or (entry['target_key'], entry['target_pos'], entry['target_english']) != target:
        raise InputError(f'Stale Spanish review target: {identifier}')
    require_text(entry['reason'], 'Spanish review reason')
    require_text(entry['reviewer'], 'Spanish reviewer')
    keys = entry['source_keys']
    if not isinstance(keys, list) or any(not isinstance(k, str) or k not in spanish for k in keys):
        raise InputError(f'Unknown Spanish review source: {identifier}')
    if len(keys) != len(set(keys)):
        raise InputError('Duplicate Spanish review source key')
    validate_review_meaning(entry, spanish)


def validate_review_meaning(entry: dict, spanish: dict):
    identifier, keys = entry['headword_id'], entry['source_keys']
    mode, meaning = entry['mode'], entry['meaning']
    if mode == 'unresolved':
        if meaning != '':
            raise InputError('Unresolved Spanish review must have no translation')
        return
    if mode == 'translated':
        if keys:
            raise InputError('Local Spanish translation must not claim upstream source keys')
    elif mode not in ('verbatim', 'adapted') or not keys:
        raise InputError('Spanish review requires a supported mode and source evidence')
    require_text(meaning, 'reviewed Spanish meaning')
    if any(token in meaning for token in ('<', '>', '[', ']')):
        raise InputError('Reviewed Spanish meaning must be plain text')
    if mode == 'verbatim' and (len(keys) != 1 or parse_definition(spanish[keys[0]])[1] != meaning):
        raise InputError(f'Verbatim Spanish review differs from source: {identifier}')


def load_reviews(path: Path, identity_path: Path, dpd_sha256: str, headwords: dict, spanish: dict):
    reviews = read_json(path)
    require_fields(reviews, {'schema', 'identity_sha256', 'dpd_sha256', 'entries'}, 'Spanish reviews')
    if type(reviews['schema']) is not int or reviews['schema'] != 1:
        raise InputError('Unsupported Spanish review schema')
    if reviews['identity_sha256'] != sha256(identity_path) or reviews['dpd_sha256'] != dpd_sha256:
        raise InputError('Spanish reviews belong to different dictionary inputs; review again before resync')
    if not isinstance(reviews['entries'], list):
        raise InputError('Spanish reviews must be a list')
    by_id = {}
    for entry in reviews['entries']:
        validate_review(entry, headwords, spanish)
        identifier = entry['headword_id']
        if identifier in by_id:
            raise InputError('Duplicate Spanish review target')
        by_id[identifier] = entry
    return by_id


def map_historical(identifier: int, target: tuple, source: tuple | None, review: dict | None, spanish: dict):
    base = {'headword_id': identifier, 'current_key': target[0], 'meaning': ''}
    if review is not None:
        return {**base, 'source_key': review['source_keys'][0] if review['source_keys'] else '',
                'source_keys': review['source_keys'], 'mapping': review['mode'],
                'status': 'unresolved_review' if review['mode'] == 'unresolved' else 'accepted',
                'meaning': review['meaning'], 'reason': review['reason'], 'reviewer': review['reviewer']}
    if source is None:
        return {**base, 'source_key': '', 'status': 'missing_historical_translation'}
    key, pos, english = source
    base = {**base, 'source_key': key, 'mapping': 'historical_id', 'source_english': english}
    if (pos, english) != target[1:]:
        return {**base, 'status': 'meaning_drift', 'expected_english': target[2]}
    try:
        translated_pos, meaning = parse_definition(spanish[key])
    except InputError as error:
        return {**base, 'status': 'unparseable_definition', 'reason': str(error)}
    if translated_pos != pos:
        return {**base, 'status': 'pos_mismatch'}
    return {**base, 'status': 'accepted', 'meaning': meaning}


def dictionary_records(words: list[dict], headwords: list[tuple], english_path: Path,
                       spanish_path: Path, identity_path: Path, review_path: Path, dpd_sha256: str):
    historical, spanish = load_identity(identity_path, english_path, spanish_path)
    targets = {identifier: (key, normalize(pos), normalize(meaning_1 or meaning_2))
               for identifier, key, pos, meaning_1, meaning_2 in headwords}
    if len(targets) != len(headwords):
        raise InputError('Duplicate current DPD headword ID')
    reviews = load_reviews(review_path, identity_path, dpd_sha256, targets, spanish)
    records = []
    for word in words:
        identifier = word['headword_id']
        if identifier not in targets:
            raise InputError('Selected headword absent from pinned DPD reference')
        mapped = map_historical(identifier, targets[identifier], historical.get(identifier), reviews.get(identifier), spanish)
        records.append({**word, **mapped, 'language': 'es'})
    return records, {'reviewed_headword_ids': sorted(reviews)}

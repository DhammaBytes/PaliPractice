"""Compare a candidate with the shipped dictionary before recoverable promotion."""

import argparse
import hashlib
import json
from pathlib import Path

from extraction.enrichment import connect
from extraction.inputs import InputError, read_json, sha256

LANGUAGES = ('es', 'ru')
MAPPING_SOURCES = ('identity_source', 'review_source', 'override_source')


def snapshot(directory: Path, manifest_name: str) -> dict:
    database = directory / 'pali.db'
    manifest = read_json(directory / manifest_name)
    digest = sha256(database)
    if manifest.get('outputs', {}).get('pali.db') != digest:
        raise InputError('Dictionary comparison requires a manifest matching its database')
    meanings = {}
    with connect(database) as db:
        for kind in ('nouns', 'verbs'):
            for identifier, word, meaning in db.execute(f'SELECT id, word, meaning FROM {kind}_details'):
                meanings[identifier] = {'kind': kind, 'word': word, 'english': meaning}
        translations = {(identifier, language): meaning for identifier, language, meaning in
                        db.execute('SELECT headword_id, language, meaning FROM localized_meanings')}
    return {'identity': {'database_sha256': digest, 'manifest_sha256': sha256(directory / manifest_name)},
            'meanings': meanings, 'translations': translations, 'manifest': manifest}


def event(kind: str, language: str, identifier, before, after, required: bool) -> dict:
    return {'key': f'{kind}:{language}:{identifier}', 'kind': kind, 'language': language,
            'headword_id': identifier if isinstance(identifier, int) else None,
            'before': before, 'after': after, 'requires_review': required}


def sense_event_rows(identifier: int, language: str, before, after, previous, current) -> list[dict]:
    if after is None:
        return [event('selection_removed', language, identifier, before, None, False)]
    result = []
    if previous and not current:
        result.append(event('translation_lost', language, identifier, previous, after, True))
    elif not current and before is None:
        result.append(event('new_gap', language, identifier, None, after, True))
    if before and before != after and (previous or current):
        result.append(event('english_target_changed', language, identifier, before, after, True))
    if current and current != previous:
        result.append(event('translation_changed' if previous else 'translation_added',
                            language, identifier, previous, current, False))
    return result


def sense_changes(old: dict, new: dict) -> list[dict]:
    result = []
    for identifier in sorted(set(old['meanings']) | set(new['meanings'])):
        for language in LANGUAGES:
            result.extend(sense_event_rows(identifier, language, old['meanings'].get(identifier),
                          new['meanings'].get(identifier), old['translations'].get((identifier, language)),
                          new['translations'].get((identifier, language))))
    return result


def source_changes(old: dict, new: dict) -> list[dict]:
    result = []
    before, after = old['manifest']['translations'], new['manifest']['translations']
    old_dpd = old['manifest']['english']['inputs']['dpd']['sha256']
    new_dpd = new['manifest']['english']['inputs']['dpd']['sha256']
    for language in LANGUAGES:
        left, right = before.get(language, {}), after.get(language, {})
        if left and not right:
            result.append(event('language_removed', language, 'layer', left, None, True))
        for name in MAPPING_SOURCES:
            if name in left and name not in right:
                result.append(event('mapping_input_removed', language, name, left[name], None, True))
        if old_dpd != new_dpd:
            # Packaged English may be abbreviated by preferences. Require a source-baseline
            # review too, including for RU, whose numeric IDs cannot establish freshness.
            result.append(event('dpd_baseline_changed', language, 'layer', old_dpd, new_dpd, True))
        if left.get('source') != right.get('source'):
            result.append(event('translation_source_changed', language, 'layer',
                                left.get('source'), right.get('source'), True))
        if left.get('override_source') and right.get('override_source') and left.get('source') != right.get('source'):
            result.append(event('overrides_need_resync', language, 'layer',
                                left['override_source'], right['override_source'], True))
    return result


def compare(repository: Path, candidate: Path) -> dict:
    old = snapshot(repository / 'PaliPractice/PaliPractice/Data', 'pali.manifest.json')
    new = snapshot(candidate, 'bundle.json')
    changes = sorted(sense_changes(old, new) + source_changes(old, new), key=lambda row: row['key'])
    return {'schema': 1, 'baseline': old['identity'], 'candidate': new['identity'], 'changes': changes}


def report_digest(report: dict) -> str:
    return hashlib.sha256(json.dumps(report, ensure_ascii=False, sort_keys=True,
                                     separators=(',', ':')).encode()).hexdigest()


def verify_decisions(report: dict, decisions: Path | None) -> dict:
    required = {row['key'] for row in report['changes'] if row['requires_review']}
    if decisions is None:
        if required:
            raise InputError(f'Translation comparison requires {len(required)} decisions; run compare_translations.py')
        return {'schema': 1, 'comparison_sha256': report_digest(report), 'decisions': []}
    data = read_json(decisions)
    if not isinstance(data, dict) or set(data) != {'schema', 'comparison_sha256', 'decisions'} or data['schema'] != 1:
        raise InputError('Invalid translation comparison decisions')
    if data['comparison_sha256'] != report_digest(report):
        raise InputError('Stale translation comparison decisions; regenerate against the shipped bundle')
    if not isinstance(data['decisions'], list):
        raise InputError('Translation decisions must be a list')
    verify_decision_entries(data['decisions'], required)
    return data


def verify_decision_entries(entries: list, required: set[str]):
    seen = set()
    for entry in entries:
        if not isinstance(entry, dict) or set(entry) != {'key', 'reason', 'reviewer'}:
            raise InputError('Each translation decision requires key, reason and reviewer')
        if any(not isinstance(entry[field], str) or not entry[field].strip() for field in entry):
            raise InputError('Translation decisions require nonempty text')
        key = entry['key']
        if key not in required or key in seen:
            raise InputError('Unknown or duplicate translation decision')
        seen.add(key)
    if seen != required:
        raise InputError('Missing translation comparison decisions')


def write_report(report: dict, output: Path):
    # Exclusive creation keeps a completed review from being overwritten accidentally.
    with output.open('x', encoding='utf-8') as stream:
        json.dump({**report, 'comparison_sha256': report_digest(report)}, stream, ensure_ascii=False, indent=2)
        stream.write('\n')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--candidate', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    comparison = compare(args.repository.resolve(), args.candidate.resolve())
    write_report(comparison, args.output)
    print(f"Comparison written; {sum(row['requires_review'] for row in comparison['changes'])} decisions required")

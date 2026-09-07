"""Read-only validation using the same structural contract as candidate builds."""

import argparse
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT))
from quality.checks.data_contract import validate_data


def validate_database(db_path=None, *, version_path=None, registry_path=None):
    database = Path(db_path) if db_path else ROOT / 'PaliPractice/PaliPractice/Data/pali.db'
    version = Path(version_path) if version_path else database.with_name('pali.version.txt')
    registry = Path(registry_path) if registry_path else ROOT / 'scripts/configs/lemma_registry.json'
    errors, counts = validate_data(database, version, registry)
    for error in errors:
        print(f'FAIL {error}')
    if not errors:
        print(f'PASS structural database checks: {counts}')
        print('Exact candidate form and source checks remain required before release.')
    return not errors


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--database', type=Path)
    parser.add_argument('--version', type=Path)
    parser.add_argument('--registry', type=Path)
    args = parser.parse_args()
    return 0 if validate_database(args.database, version_path=args.version, registry_path=args.registry) else 1


if __name__ == '__main__':
    raise SystemExit(main())

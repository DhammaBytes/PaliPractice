"""Build or verify an offline translation candidate; never acquire or promote data."""

import argparse
from pathlib import Path

from extraction.enrichment import build_enrichment, validate_enrichment

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=('build', 'verify'))
    parser.add_argument('--english', type=Path, required=True)
    parser.add_argument('--manifest', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    action = build_enrichment if args.command == 'build' else validate_enrichment
    action(args.english.resolve(), args.manifest.resolve(), args.output.resolve())
    print('PASS translation source equality and English preservation')

"""Create an empty registry for a new project; never used for production rebuilds."""

import argparse
from pathlib import Path

from extraction.registry import save_registry


def bootstrap(output: Path):
    registry = {'version': 1, 'next_noun_id': 10001, 'next_verb_id': 70001,
                'nouns': {}, 'verbs': {}}
    save_registry(registry, registry, output_path=output)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    bootstrap(parser.parse_args().output)

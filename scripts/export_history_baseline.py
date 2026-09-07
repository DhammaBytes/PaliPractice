"""Reproduce legacy history with released grammar code; output only to a new workspace."""

import argparse
import gzip
import json
import shutil
import subprocess
from pathlib import Path

from extraction.candidate import ROOT
from extraction.inputs import InputError, sha256


def export(output: Path):
    templates = ROOT / 'scripts/history_baseline'
    manifest = json.loads((templates / 'manifest.json').read_text())
    database = ROOT / 'scripts/baselines/v1.1/pali.db'
    if sha256(database) != manifest['source_database_sha256']:
        raise InputError('Released history database checksum differs')
    output.mkdir(parents=True, exist_ok=False)
    base = 'PaliPractice/PaliPractice/'
    paths = [base + path for path in ('Models/Enums.cs', 'Models/Inflection', 'Models/Words',
             'Services/Database/Entities', 'Services/Grammar/InflectionService.cs')]
    paths.extend(base + 'Services/Database/Repositories/' + name + '.cs' for name in
                 ('ILemmaRepository', 'INounRepository', 'IVerbRepository', 'NounRepository', 'VerbRepository'))
    revision = manifest['source_revision']
    names = subprocess.check_output(['git', 'ls-tree', '-r', '--name-only', revision, '--', *paths],
                                    cwd=ROOT, text=True).splitlines()
    for name in names:
        destination = output / 'source' / name
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(subprocess.check_output(['git', 'show', revision + ':' + name], cwd=ROOT))
    for template in templates.glob('*.in'):
        shutil.copyfile(template, output / template.stem)
    subprocess.run(['dotnet', 'run', '--project', str(output / 'HistoryExport.csproj'), '--',
                    str(database), str(output / 'history.json')], check=True)
    if sha256(output / 'history.json') != manifest['json_sha256']:
        raise InputError('Released history reconstruction differs from the frozen export')
    compressed = output / 'history-v1.1.json.gz'
    compressed.write_bytes(gzip.compress((output / 'history.json').read_bytes(), mtime=0))
    print(f'PASS released history JSON identity; compressed export: {compressed}')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    export(parser.parse_args().output.resolve())

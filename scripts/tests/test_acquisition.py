"""Acquisition boundaries; does not run upstream conversion tools."""

import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from acquire_corpora import archive, check_conversion
from extraction.inputs import InputError


class AcquisitionTests(unittest.TestCase):
    def test_archive_uses_commit_bytes_and_excludes_ignored_and_uncommitted_data(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            repository = root / 'source'
            repository.mkdir()
            subprocess.run(['git', 'init', '-q', str(repository)], check=True)
            (repository / '.gitignore').write_text('generated.txt\n')
            (repository / 'tracked.txt').write_text('released bytes')
            subprocess.run(['git', 'add', '.'], cwd=repository, check=True)
            subprocess.run(['git', '-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid',
                            '-c', 'commit.gpgsign=false', 'commit', '-qm', 'fixture'],
                           cwd=repository, check=True)
            revision = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=repository, text=True).strip()
            (repository / 'tracked.txt').write_text('pending user edit')
            (repository / 'generated.txt').write_text('unproven input')
            destination = root / 'archived'
            archive(repository, revision, ['.'], destination)
            self.assertEqual('released bytes', (destination / 'tracked.txt').read_text())
            self.assertFalse((destination / 'generated.txt').exists())
            self.assertEqual('pending user edit', (repository / 'tracked.txt').read_text())
            with self.assertRaisesRegex(InputError, 'full Git'):
                archive(repository, 'HEAD', ['.'], root / 'bad')

    def test_incomplete_conversion_fails(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source, target = root / 'source', root / 'target'
            source.mkdir()
            target.mkdir()
            (source / 'one.xml').write_text('<text/>')
            with self.assertRaisesRegex(InputError, 'Incomplete'):
                check_conversion(source, target, '.xml')
            (target / 'wrong.txt').write_text('pāli')
            with self.assertRaisesRegex(InputError, 'Incomplete'):
                check_conversion(source, target, '.xml')
            (target / 'wrong.txt').unlink()
            (target / 'one.txt').write_text('')
            with self.assertRaisesRegex(InputError, 'Incomplete'):
                check_conversion(source, target, '.xml')
            (target / 'one.txt').write_text('pāli')
            check_conversion(source, target, '.xml')


if __name__ == '__main__':
    unittest.main()

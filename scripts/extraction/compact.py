"""Project validated rendered records into compact runtime storage."""

from contextlib import closing
import sqlite3
from pathlib import Path

from .inputs import InputError


def compact(source: Path, destination: Path):
    # Exclusive creation prevents accidental replacement of a bundle or prior experiment.
    with destination.open('xb'):
        pass
    with closing(sqlite3.connect(source.resolve().as_uri() + '?mode=ro', uri=True)) as original:
        with closing(sqlite3.connect(destination)) as output:
            original.backup(output)
            for kind in ('nouns', 'verbs'):
                for suffix in ('corpus_forms', 'irregular_forms'):
                    table = f'{kind}_{suffix}'
                    columns = 'headword_id, form_id' + (', form' if suffix == 'irregular_forms' else '')
                    spelling = 'form TEXT NOT NULL,' if suffix == 'irregular_forms' else ''
                    output.execute(f'ALTER TABLE {table} RENAME TO old_{table}')
                    # A composite primary key in a rowid table duplicates keys in a
                    # separate index. WITHOUT ROWID stores the records by that pair.
                    output.execute(f'''CREATE TABLE {table} (
                        headword_id INTEGER NOT NULL REFERENCES {kind}(id),
                        form_id INTEGER NOT NULL, {spelling}
                        PRIMARY KEY (headword_id, form_id)) WITHOUT ROWID''')
                    output.execute(f'INSERT INTO {table} SELECT {columns} FROM old_{table}')
                    output.execute(f'DROP TABLE old_{table}')
            output.commit()
            output.execute('VACUUM')
            if output.execute('PRAGMA integrity_check').fetchone() != ('ok',) or output.execute('PRAGMA foreign_key_check').fetchall():
                raise InputError('Compact database integrity failure')
            # Exact logical comparison covers every table, not only the primary forms.
            for table, in original.execute("SELECT name FROM sqlite_master WHERE type='table'"):
                columns = 'headword_id, form_id' if table.endswith('_corpus_forms') else '*'
                before = sorted(original.execute(f'SELECT {columns} FROM {table}').fetchall())
                after = sorted(output.execute(f'SELECT {columns} FROM {table}').fetchall())
                if before != after:
                    raise InputError(f'Compact projection changed table: {table}')
    return {'source_bytes': source.stat().st_size, 'compact_bytes': destination.stat().st_size}



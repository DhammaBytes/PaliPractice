"""Strict offline parser for the SBS Russian TSV; acquisition is a separate step."""

import csv
import io
from pathlib import Path

from .inputs import InputError, positive_integer


class RussianMeaningsError(InputError):
    """The Russian source cannot be used as translation evidence."""


def validate_headers(headers: list[str]):
    if not headers or len(headers) != len(set(headers)) or any(not h.strip() for h in headers):
        raise RussianMeaningsError('Missing or duplicate Russian TSV headers')
    if 'id' not in headers or not {'ru_meaning', 'ru_meaning_raw'} & set(headers):
        raise RussianMeaningsError('Russian TSV requires id and a meaning column')


def parse_russian_meanings(data: bytes) -> dict[int, str]:
    """Preserve curated -> raw -> empty precedence without rewriting definitions."""
    try:
        reader = csv.reader(io.StringIO(data.decode('utf-8-sig'), newline=''),
                            delimiter='\t', strict=True)
        headers = next(reader, [])
        validate_headers(headers)
        meanings = {}
        for fields in reader:
            if not fields:
                continue
            if len(fields) != len(headers):
                raise RussianMeaningsError(f'Russian TSV row width mismatch at line {reader.line_num}')
            row = dict(zip(headers, fields))
            raw_id = row['id']
            if not raw_id.isascii() or not raw_id.isdecimal():
                raise RussianMeaningsError(f'Invalid Russian headword id: {raw_id!r}')
            headword_id = positive_integer(int(raw_id), 'Russian headword id')
            if headword_id in meanings:
                raise RussianMeaningsError(f'Duplicate Russian headword id: {headword_id}')
            meaning = row.get('ru_meaning', '').strip() or row.get('ru_meaning_raw', '').strip()
            if '\x00' in meaning:
                raise RussianMeaningsError('NUL in Russian meaning')
            meanings[headword_id] = meaning
        if not meanings:
            raise RussianMeaningsError('Russian TSV has no data rows')
        return meanings
    except (UnicodeError, csv.Error) as error:
        raise RussianMeaningsError(f'Malformed Russian TSV: {error}') from error


def load_russian_meanings(path: Path) -> dict[int, str]:
    """Read a local source. Callers must verify its pinned checksum first."""
    try:
        return parse_russian_meanings(path.read_bytes())
    except OSError as error:
        raise RussianMeaningsError(f'Cannot read Russian source: {error}') from error

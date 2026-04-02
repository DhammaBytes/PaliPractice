"""
Russian meaning import for PaliPractice extraction.

Downloads the russian.tsv backup from the DPD fork and returns a mapping
from DPD headword ID to Russian meaning text.

Prefer the curated `ru_meaning` column, but fall back to `ru_meaning_raw`
when `ru_meaning` is empty. Some entries in the fork currently only have
the raw field populated.
"""

from __future__ import annotations

import csv
import io
from urllib.error import URLError
from urllib.request import urlopen

from .config import RUSSIAN_MEANINGS_TIMEOUT_SECONDS, RUSSIAN_MEANINGS_URL


class RussianMeaningsError(RuntimeError):
    """Raised when the Russian meaning TSV cannot be fetched or parsed."""


def load_russian_meanings(
    url: str = RUSSIAN_MEANINGS_URL,
    timeout_seconds: int = RUSSIAN_MEANINGS_TIMEOUT_SECONDS,
) -> dict[int, str]:
    """Download and parse Russian meanings keyed by DPD headword ID."""
    try:
        with urlopen(url, timeout=timeout_seconds) as response:
            raw_data = response.read()
    except URLError as ex:
        raise RussianMeaningsError(f"Failed to fetch Russian meanings from {url}: {ex}") from ex

    try:
        text = raw_data.decode("utf-8-sig")
    except UnicodeDecodeError as ex:
        raise RussianMeaningsError(f"Russian meanings are not valid UTF-8: {ex}") from ex

    reader = csv.DictReader(io.StringIO(text), delimiter="\t", quotechar='"')
    if reader.fieldnames is None:
        raise RussianMeaningsError("Russian meanings TSV is missing a header row")

    headers = {field.strip() for field in reader.fieldnames if field}
    if "id" not in headers:
        raise RussianMeaningsError(
            "Russian meanings TSV is missing required columns: id"
        )

    if "ru_meaning" not in headers and "ru_meaning_raw" not in headers:
        raise RussianMeaningsError(
            "Russian meanings TSV must include ru_meaning or ru_meaning_raw"
        )

    meanings: dict[int, str] = {}

    for line_number, row in enumerate(reader, start=2):
        values = [(value or "").strip() for value in row.values()]
        if not any(values):
            continue

        raw_id = (row.get("id") or "").strip()
        if not raw_id:
            raise RussianMeaningsError(f"Russian meanings TSV line {line_number} is missing an id")

        try:
            headword_id = int(raw_id)
        except ValueError as ex:
            raise RussianMeaningsError(
                f"Russian meanings TSV line {line_number} has invalid id '{raw_id}'"
            ) from ex

        if headword_id in meanings:
            raise RussianMeaningsError(
                f"Russian meanings TSV contains duplicate id {headword_id} on line {line_number}"
            )

        primary_meaning = (row.get("ru_meaning") or "").strip()
        raw_meaning = (row.get("ru_meaning_raw") or "").strip()
        meanings[headword_id] = primary_meaning or raw_meaning

    return meanings

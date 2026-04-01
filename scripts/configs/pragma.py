"""
Shared SQLite PRAGMA settings for the bundled pali.db database.

The database version lives in a single text file bundled with the app:
PaliPractice/PaliPractice/Data/pali.version.txt

The persisted value uses the format YYYYMMDDNN:
- YYYYMMDD = local build date
- NN = build number for that day, from 00 to 99

Python extraction reads that file to stamp PRAGMA user_version, bumps it after
each successful DB build, and the app reads the same bundled file to decide
whether a cached local database copy needs replacement.
"""

from datetime import datetime
from pathlib import Path


DATABASE_VERSION_PATH = (
    Path(__file__).resolve().parents[2]
    / "PaliPractice"
    / "PaliPractice"
    / "Data"
    / "pali.version.txt"
)


MAX_DAILY_DATABASE_BUILDS = 100


def _parse_database_version(version: int) -> tuple[str, int]:
    raw_version = str(version)
    if len(raw_version) != 10:
        raise ValueError(f"Database version must use YYYYMMDDNN format: {version}")

    date_part = raw_version[:8]
    build_part = raw_version[8:]

    try:
        datetime.strptime(date_part, "%Y%m%d")
    except ValueError as exc:
        raise ValueError(f"Database version has invalid date component: {version}") from exc

    build_number = int(build_part)
    if not 0 <= build_number < MAX_DAILY_DATABASE_BUILDS:
        raise ValueError(
            f"Database version build must be between 00 and 99: {version}"
        )

    return date_part, build_number


def read_database_version() -> int:
    raw_version = DATABASE_VERSION_PATH.read_text(encoding="utf-8").strip()
    try:
        version = int(raw_version)
    except ValueError as exc:
        raise ValueError(
            f"Invalid database version '{raw_version}' in {DATABASE_VERSION_PATH}"
        ) from exc

    _parse_database_version(version)
    return version


def compute_next_database_version(current_version: int | None = None, now: datetime | None = None) -> int:
    effective_now = now or datetime.now()
    today = effective_now.strftime("%Y%m%d")
    current = read_database_version() if current_version is None else current_version
    current_day, current_build = _parse_database_version(current)

    if current_day > today:
        raise ValueError(
            f"Current database version {current} is from a future date relative to {today}"
        )

    if current_day < today:
        return int(f"{today}00")

    if current_build >= MAX_DAILY_DATABASE_BUILDS - 1:
        raise ValueError(
            f"Exceeded {MAX_DAILY_DATABASE_BUILDS} database builds for {today}"
        )

    return int(f"{today}{current_build + 1:02d}")


def write_database_version(version: int) -> None:
    _parse_database_version(version)
    tmp_path = DATABASE_VERSION_PATH.with_suffix(DATABASE_VERSION_PATH.suffix + ".tmp")
    tmp_path.write_text(f"{version}\n", encoding="utf-8")
    tmp_path.replace(DATABASE_VERSION_PATH)


DATABASE_VERSION = read_database_version()

"""Configuration modules for PaliPractice scripts."""

from .pragma import (
    DATABASE_VERSION,
    compute_next_database_version,
    read_database_version,
    write_database_version,
)

__all__ = [
    "DATABASE_VERSION",
    "compute_next_database_version",
    "read_database_version",
    "write_database_version",
]

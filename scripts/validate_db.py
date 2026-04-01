#!/usr/bin/env python3
"""Compatibility entry point for database validation."""

from extraction.validate_db import validate_database


if __name__ == "__main__":
    raise SystemExit(0 if validate_database() else 1)

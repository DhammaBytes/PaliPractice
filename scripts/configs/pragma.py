"""
SQLite PRAGMA settings for the pali.db database.

These settings are used by:
- scripts/extract_nouns_and_verbs.py: Sets the version when generating pali.db
- PaliPractice/Services/Database/DatabaseService.cs: Reads the version to check for updates

When updating the database schema or data format, increment DATABASE_VERSION here
and ensure BundleVersion in DatabaseService.cs matches or exceeds this value.
"""

# Database version - increment when schema or data format changes
# Must match or exceed BundleVersion in DatabaseService.cs
DATABASE_VERSION = 1

#!/usr/bin/env python3
"""
Analyze lemmas in training.db to find groups with the same lemma_clean
but differences in grammatical properties (not translations/references).
"""

import sqlite3
from collections import defaultdict
from pathlib import Path


# Columns to exclude from comparison (translation/reference related)
EXCLUDED_COLUMNS = {
    'id', 'lemma',  # id and lemma are expected to differ
    'ebt_count', 'meaning',
    'source_1', 'sutta_1', 'example_1',
    'source_2', 'sutta_2', 'example_2',
    'plus_case', 'family_root', 'stem', 'derived_from',
    'trans', 'type',
    'gender', 'pattern'
}


def analyze_table(cursor, table_name):
    """Analyze a table for lemmas with same lemma_clean but different properties."""

    # Get all column names
    cursor.execute(f"PRAGMA table_info({table_name})")
    all_columns = [row[1] for row in cursor.fetchall()]

    # Columns to compare (grammatical properties)
    compare_columns = [col for col in all_columns if col not in EXCLUDED_COLUMNS]

    # Fetch all rows
    cursor.execute(f"SELECT * FROM {table_name}")
    rows = cursor.fetchall()

    # Map column names to indices
    col_to_idx = {col: idx for idx, col in enumerate(all_columns)}

    # Group by lemma_clean
    groups = defaultdict(list)
    for row in rows:
        lemma_clean = row[col_to_idx['lemma_clean']]
        groups[lemma_clean].append(row)

    # Find groups with differences
    differences = []

    for lemma_clean, group_rows in groups.items():
        if len(group_rows) < 2:
            continue  # Skip single-entry groups

        # Compare each row with the first row in the group
        reference_row = group_rows[0]
        group_diffs = []

        for row in group_rows[1:]:
            row_diffs = {}
            for col in compare_columns:
                idx = col_to_idx[col]
                if reference_row[idx] != row[idx]:
                    row_diffs[col] = {
                        'reference': reference_row[idx],
                        'current': row[idx]
                    }

            if row_diffs:
                group_diffs.append({
                    'reference_lemma': reference_row[col_to_idx['lemma']],
                    'current_lemma': row[col_to_idx['lemma']],
                    'differences': row_diffs
                })

        if group_diffs:
            differences.append({
                'lemma_clean': lemma_clean,
                'group_diffs': group_diffs,
                'all_rows': group_rows,
                'col_to_idx': col_to_idx
            })

    return differences, compare_columns, len(groups)


def print_differences(table_name, differences, compare_columns, unique_count):
    """Print differences in a readable format."""

    if not differences:
        print(f"\n{'='*80}")
        print(f"Table: {table_name.upper()}")
        print(f"{'='*80}")
        print(f"Unique lemma_clean count: {unique_count}")
        print("✓ No grammatical differences found for lemmas with same lemma_clean")
        return

    print(f"\n{'='*80}")
    print(f"Table: {table_name.upper()}")
    print(f"{'='*80}")
    print(f"Unique lemma_clean count: {unique_count}")
    print(f"\nFound {len(differences)} lemma groups with grammatical differences:\n")

    for diff_group in differences:
        lemma_clean = diff_group['lemma_clean']
        print(f"\n┌─ Lemma group: '{lemma_clean}' ─────────────────")

        for diff in diff_group['group_diffs']:
            print(f"│")
            print(f"│ Comparing:")
            print(f"│   Reference: {diff['reference_lemma']}")
            print(f"│   Current:   {diff['current_lemma']}")
            print(f"│")
            print(f"│ Differences:")

            for col, values in diff['differences'].items():
                ref_val = values['reference']
                cur_val = values['current']
                print(f"│   • {col:15s}: {ref_val!s:20s} → {cur_val!s}")

        print(f"└{'─'*50}")


def main():
    db_path = Path(__file__).parent.parent / "PaliPractice" / "PaliPractice" / "Data" / "training.db"

    if not db_path.exists():
        print(f"Error: Database not found at {db_path}")
        return

    print(f"Analyzing database: {db_path}")

    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()

    # Analyze nouns
    noun_diffs, noun_cols, noun_unique_count = analyze_table(cursor, 'nouns')
    print_differences('nouns', noun_diffs, noun_cols, noun_unique_count)

    # Analyze verbs
    verb_diffs, verb_cols, verb_unique_count = analyze_table(cursor, 'verbs')
    print_differences('verbs', verb_diffs, verb_cols, verb_unique_count)

    # Summary
    print(f"\n{'='*80}")
    print("SUMMARY")
    print(f"{'='*80}")
    print(f"Nouns: {noun_unique_count} unique lemma_clean, {len(noun_diffs)} groups with differences")
    print(f"Verbs: {verb_unique_count} unique lemma_clean, {len(verb_diffs)} groups with differences")
    print(f"\nColumns compared:")
    print(f"  Nouns: {', '.join(noun_cols)}")
    print(f"  Verbs: {', '.join(verb_cols)}")

    conn.close()


if __name__ == "__main__":
    main()

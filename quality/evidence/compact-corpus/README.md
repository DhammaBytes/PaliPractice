# Compact corpus storage integration

Database version `2026090703` replaces `2026090702` through the existing copied
bundle version check. The database is 3,457,024 bytes (3.30 MiB), down from
6,176,768 bytes (5.89 MiB): 44.03% smaller. All logical table contents match the
prior bundle after projecting corpus records to `(headword_id, form_id)`.
Translations, primary choices, spellings and practice identities are unchanged.

## Build and runtime contract

Generation still writes and validates full rendered forms first. It retains all
attested spellings in `corpus_forms.json`, then verifies the compact projection
before replacing the candidate database. The corpus and irregular tables use
`WITHOUT ROWID`; only irregular tables retain spelling columns. Runtime corpus
caches hold scoped keys. No lemma/mastery ID encoding changes.

The evidence is part of candidate repeatability, translation preservation,
manifest hashes and the eight-file promotion transaction. It is promoted to
`scripts/generated/corpus_forms.json` for repository tests, never bundled in the
app. Compact keys must exactly match the spelling evidence. All primary forms
are checked through real reconstruction against template spellings and corpus
wordlists, including the known alternate-paradigm collisions. Missing, extra,
corrupted or stale evidence cannot reuse a successful promotion receipt.

## Verification

- `auto` gate passed in 233.75 seconds: 2,937 .NET tests and 64 producer tests,
  plus the quality checks and desktop build.
- The gate generated compact English and multilingual candidates twice and
  compared bytes and semantics. .NET tests consumed the compact candidate.
- A separate ordinary full run after promotion passed all 2,937 tests.
- All eight promoted files match the candidate; the promotion journal completed.
- Independent read-only review found no blocking findings.
- Package comparisons are recorded in `package-identities.json`.

See [results.json](results.json), [bundle.json](bundle.json),
[bundle-verification.json](bundle-verification.json) and
[semantic-verification.json](semantic-verification.json).
The prior bundle is checksum-archived in `.local/backups/before-compact-production`.
The candidate is `.local/candidates/compact-production-verified`; pinned input
manifests are under `.local/inputs/compact-production`.

The updated reader and compact bundle must remain paired. Exact build tests
replace the runtime spelling comparison. Existing database-version replacement
handles copied assets; no new upgrade mechanism was added.

Review noted two non-blocking test-helper details: a custom PaliDbLoader path
still uses global TestPaths for evidence (current callers use the default), and
the compact-copy test repeats compaction when given an already compact candidate.
Neither changes the verified production flow.

This change remains uncommitted. Native checks concern package assets, not a
whole-app UI release review.

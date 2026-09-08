# Legacy-history migration memory

The decoded v1.1 reconstruction dictionary is now local to a migration call.
It loads only when a missing snapshot is encountered, serves both noun and verb
backfill, then becomes eligible for garbage collection. No static reference
retains the dictionary after migration.

Backfill queries at most 256 missing snapshots at a time, ordered by history ID.
The cursor advances past unresolved rows too. Existing captured text remains
unchanged. Both histories and the schema version stay in the existing single
transaction. The frozen history resource and database versions are unchanged.

Verification:

- 14 focused migration tests passed, covering no-load paths, several batches,
  unknown rows, preserved snapshots, shared loading and backfill rollback.
- `auto` gate passed in 238.74 seconds, including all 2,941 .NET tests and the
  desktop build. No failures or skipped tests.
- Independent read-only review found no blocking findings.
- Frozen gzip checksum still matches its source manifest.

Run: `20260908T000309.455858Z-34909-e5ea6e`.
Log: `/tmp/agentic-quality-loop-501/runner-logs/run-20260908T000309.373630Z-34893-3eed5640/gate.log`.
Completion and semantic/bundle receipts are preserved beside this record.
The preceding run passed its tests/builds but failed worktree stability because
an external English About-text edit occurred during execution. That edit was
preserved; the successful rerun includes the updated worktree.

Prior uncommitted compact-database work is preserved. No commit was made.

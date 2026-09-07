# M1 verification record

Status: complete. Full selected gate passed; fresh independent review found no blockers.

The original worktree and nested-submodule edits are archived locally at
`.local/backups/m1-start/worktree.tar.gz`. Its checksum and all input identities
are in [initial-inputs.json](initial-inputs.json). The user explicitly approved
restoring the two archived nested submodules to their pinned revisions.
Generated SQLite sidecars and Finder metadata are excluded only in local Git
configuration. No source checkout was advanced and no bundle was regenerated.

The verified production baseline is [v1.1](../../../scripts/baselines/v1.1/manifest.json).
Its database and registry are stored with the manifest. The published macOS and
Windows archive hashes and embedded database were verified; the iOS binary was
not extracted. HEAD's later database is not used as the released baseline.

The analyzer bootstrap anchor moved from `4a85777b8eb633f0507397b020e82403e0ae8104`
to `90330a40e07e399a2d1750068affe0d87e2699e5`. Every one of the eight allowed
methods belongs to a file unchanged between those commits. Fresh Roslyn checks
must verify all eight exact allowances; no allowance was added or increased.

The translation test helper now loads each adjustment section separately.
Existing and new queue forms share the same bounded selection scan and fallback
order. Python template eligibility and validator policy checks have separate
functions. The gate runs independent .NET lanes even if analyzer configuration
is invalid, while retaining a hard failure for the invalid configuration.

## Remaining work and ownership

| Milestone | Known limitation |
| --- | --- |
| M2 | Offline isolated extraction; complete pinned input provenance; no implicit translation download or production writes |
| M3 | Append-only released identities and explicit historical practice-paradigm policy |
| M4 | Colliding noun attestation IDs, invalid None grammar, exact all-form tests, selected-pattern coverage |
| M5 | Repeatability, promotion recovery, history/migrations/statistics/provisioning, package remediation, platform evidence |

The current structural data check and passing legacy tests do not close those
semantic defects. Package paths and advisory references are in
[the gate backlog](../../TODO.md). Coverage percentage remains advisory.

The pending rebuild database, version, registry additions, and static noun
endings remain separately identifiable uncommitted work through M1; they are
not promoted by the gate or accepted as the production baseline.

## Verification

[The complete gate result](gate-result.json) records the selected lanes and test
counts. `auto` selected all local gate groups and passed on 7 September 2026.
The first sandboxed retry failed to create an MSBuild IPC socket; it was stopped
and the same universal runner succeeded with local IPC permission. The Python
self-tests include invalid-analyzer-configuration lane independence. The .NET
suite includes adjustment-section behavior and existing queue fallback tests.
No native-platform or runtime desktop smoke pass is claimed by this milestone.

## Independent review and commit boundary

Fresh read-only reviewer `m1_review` reviewed M1 on 7 September 2026 and reported
no blocking findings. It checked the changed declarations and direct consumers,
queue behavior, regression test, analyzer failure handling, bootstrap boundary,
and the release manifest against Git blobs. It did not rerun passing checks.
No accepted code fixes or second review pass were required.

The M1 commit adopts the pending quality/toolchain/test-path infrastructure and
existing extraction-source adjustments together with the bounded M1 refactors.
The four pending rebuild outputs listed above remain outside that commit. The
gate result describes the exact working snapshot, including those pending
outputs; it must not be described as a clean-checkout release validation.

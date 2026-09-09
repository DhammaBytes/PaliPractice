# PaliPractice quality gate

Run from the repository root:

```text
python3 quality/gate.py auto
```

`auto` unions checks for committed, staged, unstaged, deleted, renamed, and
untracked paths; an unknown path selects the full gate. `fast` runs the
repository self-tests, fatal Ruff rules, the base-ratcheted Lizard check,
read-only semantic data checks, and recursive submodule checks. Data, DPD, and
quality-infrastructure paths route through every affected consumer; unknown
paths widen to full. `full` also restores in
locked mode, runs all .NET tests with fresh Coverlet evidence, and builds the
desktop target. With explicit pinned inputs, the gate regenerates isolated English
candidates in external evidence storage. It never acquires inputs, promotes data,
or invokes native device builds.
`--profile auto` remains equivalent to the positional form. Every external step
has a 1200-second timeout, adjustable with `--step-timeout-seconds`.

The database check is a semantic contract, not a content snapshot. Normal
changes to lemmas, meanings, forms, and row counts are allowed. It blocks
corruption, missing or empty required tables, invalid identifier encodings,
version mismatches, broken table relationships, and disagreement with the
lemma registry. It does not compare the database bytes or current row counts
with a baseline.

The process takes a repository-wide `fcntl` lock before creating evidence and
fingerprints the root and recursive submodule worktrees before and after
execution. Started child commands inherit the lock descriptor, so an
uncatchable gate-process exit cannot expose a still-running build to a second
repository gate. Each run gets a unique external evidence directory. The
current and one prior completed run are retained by default; incomplete crash
directories cannot displace completed evidence. Change retention with
`--keep-previous-runs N`. Set `QUALITY_ARTIFACT_ROOT` to choose another external
location. The evidence base, runs, tool caches, and build candidate must be
current-user-owned private directories; pre-existing symlinks or shared
directories are rejected before the gate writes or prunes anything.

Python tooling is pinned to Ruff 0.16.6 and Lizard 1.24.0. Ruff blocks only
`E9,F63,F7,F82`. Lizard blocks a function above CCN 15 only when it is new or
worse than `--base`; existing debt remains visible without being broadened.
Fresh Lizard CSV is schema-checked and its per-file function inventory is
cross-checked against Python's maintained AST before it can act as hard
evidence.
Both tools normally run offline from the gate's persistent private cache. On a
new machine, opt into network access for one fast run with
`QUALITY_ALLOW_NETWORK=1 python3 quality/gate.py fast --base HEAD`; subsequent
runs stay offline at the same pinned versions.

Roslyn uses `quality/config/CodeMetricsConfig.txt` at threshold 15. A successful
.NET run writes `ca1502-current.json` into its evidence directory. Intentional
existing findings may be copied into `ca1502-baseline.json` as
`path`, `symbol`, and `max_complexity`; baseline edits are ordinary reviewed
source changes, never automatic gate mutations. The gate consumes the baseline
blob from `--base`, and the working-tree copy must exist and byte-match that
blob, so a same-diff replacement or deletion cannot seed a poisoned baseline
for the next task. A legitimate recalibration is therefore a separately
reviewed baseline commit, not an agent-side gate fix. During this initial bootstrap only, the current baseline is
accepted when no baseline exists at `--base`, its `anchor` exactly equals that
commit, every allowance matches fresh Roslyn evidence, and every allowed file
is unchanged from the base.

Invalid analyzer policy or baseline remains a hard failure, but does not prevent
independent tests, coverage collection, or the desktop build from executing.
Fresh diagnostics are retained even when allowances cannot be trusted.

CA1502 activation is proved by Roslyn rather than by a repository-written C#
parser. The gate copies the current .NET source and configuration into a
private external candidate, injects an unpredictable complexity-16 method into
both the real app and test projects, and uses that candidate for the normal
restore, test, coverage, and desktop-build commands. Both probes must appear in
fresh CA1502 output at exactly 16 before their diagnostics are removed from the
product report. This exercises the effective project imports, analyzer config,
rulesets, project references, and source-level global suppressions without
touching the worktree. Repository-root `.editorconfig`, `.globalconfig`,
`global.json`, NuGet config, and root props/targets/rulesets are copied above
the nested solution so the candidate has the same upward repository
configuration.

A random probe cannot expose a suppression scoped to one existing method, so a
small exact inventory reserves those local escape-hatch mechanisms:
`#pragma warning disable`, suppression attributes (apart from the existing
ReSharper-only attribute), generated-code attributes/headers/file names, and
global/category analyzer-disable keys. These spellings are deliberately
reserved even in comments and strings; this keeps the check honest and avoids
implementing a C# parser. Every .NET invocation also receives the same
command-line analyzer properties; `CodeMetricsConfig.txt` remains pinned to
threshold 15.

After a dependency update, regenerate and verify the lock files in both
configurations from `PaliPractice/`:

```text
dotnet restore PaliPractice.sln --use-lock-file --force-evaluate
dotnet restore PaliPractice.sln --use-lock-file --force-evaluate -p:Configuration=Release
```

The reviewed `packages.lock.json` files are repository inputs. Uno resolves the
same package versions in Debug and Release but excludes development-only assets
in Release. The gate restores in Release with `--locked-mode` and uses that same
configuration for tests and builds; it never refreshes locks itself. A Debug
restore cannot be reused for a Release `--no-restore` build. When updating
packages, update `Directory.Packages.props` and the Uno SDK in `global.json`,
regenerate both configurations for each project, then run the full gate. Keep Uno-managed package
families aligned with the SDK defaults. Check current advisories for the app and
test projects with `dotnet list package --vulnerable --include-transitive` from
the solution directory. Keep NuGet security warnings visible and verify native
SQLite loading on release platforms after bundle updates.

The test contract rejects zero tests and every failed, skipped, inconclusive, or
otherwise non-passing TRX result. Fresh Coverlet XML must contain nonempty
first-party coverage from the exact `PaliPractice` package and repository source
inventory. Duplicate collector copies are accepted only when byte-identical;
ambiguous reports fail. The percentage is reported only as an advisory metric.

## Bundled-data comparison inputs

Ordinary .NET tests and the gate resolve DPD, all four corpus wordlists, and
English adjustments through `quality/config/test-inputs.json`. Its paths are
relative to that file. Expected SHA-256 values come from the bundled
`Data/pali.manifest.json`; the location map does not duplicate source identities.
Tests verify the inputs once per process before comparison and reject missing
files, mismatched hashes, and a nonempty DPD WAL. They never fall back to
`dpd-db/dpd.db` or the checkout's generated corpus files.

Provision the sources described in [scripts/SETUP.md](../scripts/SETUP.md) before
running tests. No download or regeneration occurs during ordinary bundled tests.
For direct `dotnet test` runs, `PALIPRACTICE_INPUT_MANIFEST` can supply alternate
locations, but their hashes and bytes must still match the bundle. The gate
reserves that variable for the candidate workflow below. Candidate tests consume
their explicit pinned inputs, including English adjustments, instead of the
bundled location map. `PALIPRACTICE_CORPUS_DIRECTORY` is no longer used.

## Verifying an isolated data candidate

The gate permits candidate-only regeneration from an explicit manifest:

```bash
PALIPRACTICE_INPUT_MANIFEST=/absolute/path/inputs.json \
  python3 quality/gate.py auto --base <commit>
```

For data and .NET lanes, the gate builds twice into separate external evidence
folders using the same pinned inputs and version. All candidate output bytes and
database semantic digests must match. Input and production artifact hashes must
remain unchanged. Integration tests then consume the first verified build.
Optionally supply `PALIPRACTICE_CANDIDATE_DIRECTORY` to also require the rebuilt
outputs to match an existing candidate exactly. Candidate manifests, input hashes,
and extraction source identities are verified before and after the test lanes.
Producer fixture tests run in the Python lane. Acquisition and promotion remain
separate actions. Desktop compilation alone does not prove candidate packaging
or native iOS/Android operation; retain the separate provisioning smoke evidence.

A complete successful candidate gate emits `semantic-verification.json`, binding
the exact candidate, inputs, and producer/consumer source hashes. Promotion requires
this receipt and rejects stale evidence. English-only production promotion remains
blocked; multilingual promotion uses the recovery journal.

With `PALIPRACTICE_TRANSLATION_MANIFEST` set, the gate also creates two isolated
enriched copies and checks exact source mappings and unchanged English data.
The .NET integration lane consumes that verified multilingual database, including
every displayed noun/verb sense in all three languages. Source evidence includes
`.resw` resources so language labels and credits are bound to the checked source.
The gate emits `bundle-verification.json` only after the full gate passes and
the recorded .NET database path matches the enriched candidate. This receipt
binds its complete output hashes, English semantic receipt and source identities.
Multilingual promotion requires it and all three language layers; the compact `bundle.json`
becomes the packaged `Data/pali.manifest.json`. Database readiness does not imply
UI or app-publication readiness.

Compact corpus candidates retain full spellings in the hashed `corpus_forms.json`
build artifact, not in the app asset. Candidate validation checks exact key parity
with that artifact, and the .NET lane independently checks corpus membership and
all primary reconstructed strings. Full spelling validation precedes compaction;
the compact projection and its evidence are repeated, hashed, and promoted together.

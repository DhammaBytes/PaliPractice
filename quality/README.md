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
desktop target. The gate never invokes the database generator or native builds.
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

Python tooling is pinned to Ruff 0.15.22 and Lizard 1.23.0. Ruff blocks only
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

The first setup must create NuGet lock files once, from `PaliPractice/`:

```text
dotnet restore PaliPractice.sln --use-lock-file --force-evaluate
```

The reviewed `packages.lock.json` files are repository inputs. The gate uses
`dotnet restore --locked-mode`; it never refreshes locks itself.

The test contract rejects zero tests and every failed, skipped, inconclusive, or
otherwise non-passing TRX result. Fresh Coverlet XML must contain nonempty
first-party coverage from the exact `PaliPractice` package and repository source
inventory. Duplicate collector copies are accepted only when byte-identical;
ambiguous reports fail. The percentage is reported only as an advisory metric.

## Verifying an isolated data candidate

During M4, the gate can consume an existing candidate without running extraction:

```sh
PALIPRACTICE_CANDIDATE_DIRECTORY=/absolute/path/to/candidate \
PALIPRACTICE_INPUT_MANIFEST=/absolute/path/to/inputs.json \
  /absolute/path/to/run_quality_gate.py --repo /absolute/path/to/PaliPractice \
  --profile auto --base <milestone-base>
```

Both variables are required together. The gate verifies candidate hashes,
identity/scoped-form contracts, pinned inputs, and current extraction code before
and after the checks. Its structural lane and .NET integration tests consume that
candidate and those input paths. An English candidate must have empty translation
fields; the existing bundled database must retain Russian meanings when tested.
The desktop lane compiles the app; it does not claim to package or provision the
candidate. No extraction, acquisition, or promotion runs in this mode. M5 adds
isolated generation and provisioning evidence.

Without a supplied candidate the gate checks the bundled data. The preserved
pending bundle still contains the known invalid grammar/attestation records;
strict checks must fail for that data rather than accept `None` grammar or a
percentage of mismatches. Use a rebuilt candidate to verify the repairs.

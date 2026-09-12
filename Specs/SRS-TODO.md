# Deterministic SRS rework

Branch: `codex/srs-simulation`
Base: `a15f954` (2026-09-11)

## Objective and boundaries

Exercise the production noun and verb queues across time, persisted answers,
partial sessions, restarts, and changing filters. Use reproducible evidence to
fix demonstrated scheduling defects. Keep the implementation small.

Preserve combination IDs, dictionary assets, user history, mastery records,
schema/migration behavior, answer snapshots, and the 5 a.m. local progress day.
Do not rewrite the app, split mastery by ending, invent a memory model, change
cooldown intervals/retirement, or add a general simulation framework.
Pre-existing Spanish/Russian resource edits are outside this task.

## Milestones (commit each after verification)

- [x] M0: Record scope, checklist, branch, and starting revision.
- [x] M1: Add an optional production clock to queue and repository operations;
  preserve system-clock defaults and existing scheduling. Test exact due
  boundaries, recorded timestamps/history, and local progress-day rollover.
- [x] M2: Add a small deterministic runner using production providers and
  SQLite repositories, a synthetic corpus and the read-only bundled dictionary.
  Exercise actual completed-card budgets, silent rebuilds, and persisted
  answers; verify replay, abandoned cards, and file-backed reopen behavior.
- [ ] M3: Reproduce and fix bounded scheduling defects with before/after tests:
  short-session new/review and mastery-bucket service, due admission above 500,
  and small-pool spacing/urgency. Change only policies supported by the evidence.
- [ ] M4: Add representative noun/verb filter timelines and daily, weekly,
  irregular, and long-return scenarios. Report actual eligible pool sizes,
  backlog/service (including unselected due cards), and repetition. Keep exact
  invariants separate from descriptive metrics and distinguish overload from
  starvation. Use independent eligibility checks rather than queue self-checks.
- [ ] M5: Run the repository gate and bounded independent review, address
  blocking findings, record replay commands/evidence and limitations, and commit
  the final milestone. Confirm history/IDs/assets and unrelated edits remain
  intact.

## Verification

Use focused NUnit tests during implementation and the agentic-quality-loop
runner with the milestone base for stable milestone verification. Final gate
uses the task base above. Reports belong in external test/gate evidence, not app
assets. No skipped tests, weakened checks, or rewritten history to get a pass.

Initial environment: system `dotnet` provides SDK 10.0.301; the project pins
10.0.401. Check existing task-local SDK installations before installing anything
or changing project configuration.

## Evidence and decisions

- M0: Created the branch from `a15f954`; two pre-existing localization edits
  remain unstaged. No implementation changes yet.
- M1: Optional `TimeProvider` in the existing queue/repository; explicit due
  cutoffs and a shared pure local-day key calculation. No schema, IDs, cooldown,
  or scheduling-policy changes. All 28 focused clock tests passed; independent
  review found no blockers.
- M1 gate (`auto`, base `eeb9e08`): 3,016 tests passed, one pre-existing Spanish
  resource test failed; desktop build and Roslyn checks passed. The failing test
  passes against temporary copies of the committed resources. The unstaged
  Spanish `Top 100/300/500` labels explain the failure; neither the resources nor
  its assertion was changed for this task. Overall gate remains failed.
  Evidence: `20260911T235449.359576Z-78696-b11d0f` under the gate's external runs.
  Focused TRX and baseline comparison: `/private/tmp/pali-srs-task/test-results`.
- Toolchain: `/private/tmp/pali-srs-toolchain` is a private copy of the installed
  toolchain plus the existing 10.0.401 archive. Microsoft's workload installer
  registered the existing 10.0.303.1 workload set there. Use this directory first
  in `PATH` and as `DOTNET_ROOT` for subsequent gates. System installation and
  project SDK/dependency configuration remain unchanged.
- M2: Eight runner tests passed for both practice types: abandonment, completed
  budgets, silent rebuilds, exhausted/cooldown pools, file reopening, and replay
  on the bundled corpus. Independent SQL supplies expected default eligibility;
  each answer must also have an attested inflection. Independent review found
  no blockers. The auto gate passed desktop/Roslyn checks, with the same
  pre-existing Spanish resource failure (3,024 passed, one failed).
  Evidence: `20260912T000853.928101Z-5462-ec88b5`; focused `m2-simulation.trx`.

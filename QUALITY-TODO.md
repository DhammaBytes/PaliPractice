# Pāli Practice Product Quality TODO

This file tracks product behavior that still needs stronger test protection.
Database generation and data workflow remain in
[scripts/SETUP.md](scripts/SETUP.md). Accessibility work remains in
[ACCESSIBILITY-TODO.md](ACCESSIBILITY-TODO.md). Gate implementation and
calibration belong in [quality/TODO.md](quality/TODO.md); executable policy and
profiles belong in [quality/README.md](quality/README.md).

The sequenced DPD hardening and Russian/Spanish dictionary-meaning work is in
[DATA-REBUILD-ROADMAP.md](DATA-REBUILD-ROADMAP.md), including production identity,
history, statistics, and database-upgrade acceptance criteria.

## Rules

- Coverage is evidence, not the task or a cross-project score. Audit existing
  tests and name the behavior, invariant, and plausible fault first.
- Prefer deterministic queue, repository, grammar, and renderer-free ViewModel
  tests. Use explicit seeds and controllable clocks instead of sleeps or
  wall-clock assertions.
- Protect generated `pali.db` through semantic schema, relationship, version,
  identifier, and grammar contracts. Normal lemma, form, meaning, and row-count
  changes must not require frozen bytes or count baselines.
- Do not add fixture mirrors, percentage-only tests, broad snapshots, or
  implementation-coupled mocks.
- Never weaken thresholds, scope, exclusions, baselines, analyzer policy,
  routing, or existing assertions to make a campaign pass.
- Use maintained Roslyn and Python analyzers. Use a maintained mutation tool on
  one small pure policy only; do not create a custom parser or mutator.

## Priority behavior protection

- [ ] Audit `PracticeQueueBuilder`, `CooldownCalculator`, and both providers for
      missing tests around exact due-time boundaries, deterministic day/type
      seeds, malformed settings, small or exhausted pools, retirement,
      constraint fallback, and stable queue-to-provider handoff.
- [ ] Add direct protection for the practice ViewModel state machine: load,
      empty, error, cancellation, reveal/rate command gating, repeated input,
      exactly-once result/progress updates, one goal notification, exhaustion,
      navigation, disposal, and review-prompt failure isolation.
- [ ] Audit durable user state for mastery, history, daily progress, settings,
      and statistics consistency under partial repository failure. A single
      answer must not leave these records in conflicting states.
- [ ] Audit database provisioning for corrupt or stale copies, version
      mismatch, interrupted copy and cleanup, insufficient storage, permission
      failure, and platform-specific bundled-versus-copied behavior.
- [ ] Audit settings-to-practice and grammar presentation contracts for noun
      and verb filters, lemma range, citation-form exclusion, at-least-one
      constraints, inflection variants, alternative forms, translation
      fallback, Unicode normalization, and example filtering.
- [ ] Audit logical-day history and statistics at the local day boundary, DST
      and timezone changes, streak gaps, ordering, resolved form text,
      repository failure, and preservation of the last valid display state.

## Targeted diagnostics

- [ ] After the current rollout debt in
      [quality/TODO.md](quality/TODO.md) is resolved, use uncovered-file and
      CA1502 evidence to select at most three high-risk gaps per campaign.
- [ ] Refactor current complexity findings during coherent nearby work. Do not
      seed or widen a baseline to excuse changed code.
- [ ] Collect changed-line and changed-branch evidence for representative pure
      queue, grammar, and repository changes before proposing a hard threshold.
- [ ] Run one bounded Stryker.NET pilot on `CooldownCalculator` or a factored
      pure queue policy. Strengthen tests for meaningful survivors and reject
      the pilot if its cost or signal is poor.

## Bounded platform smokes

- [ ] Keep one repeatable desktop journey for launch, database provisioning,
      settings, noun and verb practice, reveal, Easy/Hard, goal completion,
      history/statistics, and relaunch persistence.
- [ ] Run a narrow iOS variant for direct bundled-database behavior and a
      narrow Android variant for copied-database behavior, lifecycle recovery,
      Pāli text, and the same critical practice transition.
- [ ] Keep Windows/Linux packaging, signing, store review, and physical-device
      evidence separate from ordinary local gate claims.
- [ ] Use [ACCESSIBILITY-TODO.md](ACCESSIBILITY-TODO.md) as the detailed
      semantics backlog; platform smokes should report only the accessibility
      behavior actually exercised.

## Regression rule

For every confirmed escaped defect, first add a failing test at the lowest
useful layer. If that is impractical, record why and add the narrowest
renderer or platform smoke that would detect recurrence.

## Completion evidence

Each completed item must report the protected behavior, plausible fault, tests
added or confirmed sufficient, relevant coverage or mutation evidence, the
base-relative quality-gate result, and the independent-review result.

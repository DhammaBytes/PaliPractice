# Deterministic SRS simulations

These tests exercise the production queue, noun/verb providers, inflection
service, and SQLite answer/history transactions. They do not estimate human
retention. Cooldown intervals, grading transitions, and permanent retirement
are unchanged.

## Run and replay

From `PaliPractice/`, use the SDK pinned by `global.json`:

```sh
PALIPRACTICE_SRS_REPORT_DIR=/tmp/pali-srs-run-a \
  dotnet test PaliPractice.Tests/PaliPractice.Tests.csproj -c Release \
  --filter 'FullyQualifiedName~PaliPractice.Tests.Practice.Simulation'
```

Repeat with another report directory to compare independent processes. Each
JSON report contains the scenario, complete filter timeline, seed, dictionary
SHA-256, trace SHA-256, session results, and every answered card. Matching input
and trace hashes provide a replay check. The current runner uses dates derived
from seeds 17 and 83 for production queue ordering; attendance has its own seeded
random generator. The original 24 timelines retain the `Legacy` answer policy
for comparisons with existing reports; that policy depends on the scenario seed.
Eight 365-visit daily timelines cross two ordering seeds with `AlwaysEasy` and
`WeakPlural` learners for both practice types. These explicit learners do not
depend on the ordering seed: `WeakPlural` always answers Hard for plural forms
and Easy for singular forms. It is a persistent-difficulty stress case, not a
model of a student learning. Replaying requires the same code/runtime and dictionary. An alternate
candidate can be supplied through the existing test input configuration.

Reports default to an external temporary directory and are attached to NUnit
results. They are evidence, not app assets. Exact invariants fail the test;
characterization values have no uncalibrated pass/fail thresholds.

## Coverage

- Synthetic sets give exact expected IDs for abandonment, buffered cards,
  queue exhaustion, same-day restarts, sparse mastery buckets, retirement,
  overdue admission above 500 records, and ineligible-record isolation.
- A file-backed practice database is closed and reopened; its answers, full
  historical snapshots, and subsequent selections must match an uninterrupted
  run. Displayed but unanswered cards must not advance progress or scheduling.
- The bundled dictionary is opened read-only. The independent eligibility
  oracle decodes corpus IDs and primary-headword ranks directly. Named pattern
  subsets list their expected raw patterns explicitly, including child patterns.
  Every timeline also compares the complete eligible set against the queue.
- Noun and verb timelines cover daily, one-card, weekly, irregular, one-lemma,
  two-lemma, broad, default, narrow pattern, rank-window, and year-return cases.
  Configured goal and completed budget are separate. Nouns cover all eight
  cases and both numbers; verbs cover all four supported tenses/moods, persons,
  numbers, and both voices. Sparse attestation and unequal corpus sizes remain
  real. Existing filter-contract tests provide exhaustive static filter coverage.
  The one-lemma case uses a valid two-rank window plus a pattern filter; the
  current rank controls do not permit equal minimum and maximum ranks.
- A frozen cohort of 600 due cards must receive service within 800 answer slots
  while new cards remain available. The run finishes before any answered card
  can become due again, distinguishing starvation from insufficient capacity.

## Read the evidence

`MaxOverdueDays` includes all eligible due cards at session start, including
cards never selected. `MaxEligibleVisitsSkipped` counts consecutive visits at
which a due card was eligible but unanswered; it resets when the card leaves
the eligible due set or receives service. These measure opportunity to practice,
not time while a filter is disabled.

`BetweenSessionDueCardDays` integrates outstanding due cards between visits
under the previously active filter, including cards becoming due during a break.
It excludes the seconds spent answering inside sessions. Session records show
eligible pool size, due counts before/after, actual answers, source, mastery
transition, queue builds (including an exhausted rebuild), and the pending card.
Reports also count exposure by case/tense, unique forms, and within-session lemma
repeats. Exposure totals are descriptive; a large grammar cohort naturally has
more opportunities than a sparse one.

Version 2 reports include the queue source hash and five mastery-bucket records
per visit. Each records due counts before/after, reviews served at their previous
level, maximum overdue age, skipped visits, oldest unserved card, and cumulative
between-session due card-days. `EnteredDueSet` counts cards due now that were not
in the previous visit's eligible due set after answering. It includes newly
enabled overdue cards as well as cards whose cooldown expired; it is not a pure
cooldown-arrival rate. Empty buckets report zero maxima and no oldest card.

Use `FullyQualifiedName~SrsTimelineTests.ExtendedTimeline` to run only the eight
year-long cases. Visits 180 and 365 provide paired checkpoints without separate
runs. Compare the same learner, seed, dictionary, and filters across revisions;
trace hashes change when report instrumentation changes even if selections do
not. For the ordering experiment, all original session selections and old
summary metrics were compared before applying the scheduler change.

Always compare answer capacity with due arrivals before interpreting backlog as
starvation. A one-card daily learner with a large enabled set is intentionally
an overload scenario. No simulated result here proves learning efficacy or
justifies changing cooldowns or retirement.

## Scope limits

Spacing remains local to each built queue; restart boundaries can repeat a lemma.
Existing soft fallback priorities and the 100-candidate scan remain unchanged.
The first eligible-review cap was removed, but this is not a new global urgency
policy: mastery buckets still share review turns, with due order within buckets
subject to spacing. No per-ending mastery, adaptive workload forecast, latent
memory model, general event framework, or nightly service is introduced.

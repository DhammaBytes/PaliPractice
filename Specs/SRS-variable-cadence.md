# Continuous variable cadence

## Scope and decision

User request: replace the fixed cadence while keeping regressions minimal in
percentage terms. Task base `8630054`, branch `codex/srs-simulation`, repository
`/Users/ivm/Sources/PaliPractice`. Existing Spanish and Russian edits are excluded.

Keep the variable cadence for its improved mixed-queue variation and restored
pattern exposure, with the workload regressions below disclosed. This is a small
aggregate tradeoff, not a regression-free result: 32 of 40 timelines stay within
+2% overdue debt, and the worst individual increase is +5.23%.

The schedule continues across short sessions without stored fields or migration.
It reads the first recorded history row's UTC date, using the build date before
history exists, and uses completed answers to locate the current position.
Each aligned 30-position block shuffles gaps of 5, 6, 6, 6, and 7 positions. The planned
rate remains five new cards per 30 answers, with 4–6 intervening reviews when
both sources remain available. A purpose-specific seed constant separates this
schedule from word shuffling.

Bucket initialization uses the scheduled review ordinal. As before, this is a
planned phase, not the actual review-source count after fallback. Due admission,
spacing priorities, stable review order, cooldowns, grading, retirement,
dictionary, IDs, and history storage remain unchanged.

## Experiment

Baselines are the fixed-cadence queue at `8630054` and the retained original
queue at `a15f954` (clock adapter `d942bb6`). The primary incremental comparison
is against `8630054`.

Coverage includes 24 attendance/filter timelines, 42 fixed-state cases across
24 date seeds, four persisted restart cases, and 16 year-long timelines. The
latter cross seeds 17, 83, 211, and 397 with nouns/verbs and AlwaysEasy/WeakPlural.
Each year-long run completes 18,250 answers. Seeds 211 and 397 extend the earlier
comparison; they are additional coverage, not a pristine holdout after all
implementation changes.

Two date-independent prototypes preceded this policy. The 5,5,6,7,7 prototype
raised debt about 4.5% in both original weak-plural noun runs; the 5,6,6,6,7
prototype also had mixed changes. Adding the stable first-history date restored
initial date variation. A final review found that its first-block RNG seed
matched word shuffling. The separate-stream fix uses a fixed descriptive
constant, not a seed selected for favorable outcomes. All final tables below
come from the corrected implementation; earlier prototype results are not mixed
into them.

The new noun-pattern regression fails on the fixed-cadence baseline. The RNG
regression also failed before stream separation: both types reused the same
prefix for all 24 dates. Both checks pass on the final implementation.

## Workload against the fixed cadence

Overdue card-days measure due backlog integrated over elapsed time between
sessions. Positive percentages mean more debt.

| Cohort | Change in overdue card-days |
|---|---:|
| Original 24 timelines, aggregated | +0.36% |
| All 16 year-long runs, aggregated | +1.15% |
| Year-long nouns, AlwaysEasy, four seeds | +1.46% |
| Year-long nouns, WeakPlural, four seeds | +0.44% |
| Year-long verbs, AlwaysEasy, four seeds | −0.90% |
| Year-long verbs, WeakPlural, four seeds | +2.76% |

Aggregates weight runs by debt; they are not confidence intervals or predictions
of human learning. The original timelines complete 25,206 answers versus 25,203
before: changing-filter verbs at seed 17 complete two more and changing-filter
nouns at seed 17 complete one more. No timeline completes fewer answers. All
292,000 year-long answers are retained.

| Year-long profile | Seed 17 | Seed 83 | Seed 211 | Seed 397 |
|---|---:|---:|---:|---:|
| Nouns, AlwaysEasy | +1.12% | +2.12% | −2.04% | +4.85% |
| Nouns, WeakPlural | +0.96% | +1.75% | −1.08% | +0.16% |
| Verbs, AlwaysEasy | +1.78% | −4.02% | +4.48% | −5.38% |
| Verbs, WeakPlural | +2.73% | +5.23% | −0.28% | +3.38% |

Two shorter timelines also exceed +2%: changing-filter nouns at seed 83 rise
+3.23%, and weekly-small verbs at seed 83 rise +3.04%. All remaining shorter
cases stay within +1.71%.

The largest increase in maximum eligible visits skipped is three visits:
year-long AlwaysEasy verbs at seed 17 rise from 32 to 35 (+9.38%). The largest
relative increase is changing-filter nouns at seed 83, from 8 to 9 (+12.5%).
Daily verbs at seed 83 rise from 9 to 10 (+11.11%). Other increases are at most
two visits. The worst debt case, year-long WeakPlural verbs at seed 83, has a
lower maximum wait (84 to 82); the metrics describe different aspects of service.

Different introductions alter the number and mix of Hard answers. This can
explain workload variation but does not cancel a raw regression or establish
better learning. A strict +2% cap in every scenario is not met.

## Queue quality and randomization

All 14 all-new and all 14 all-due fixed-state queues are exactly unchanged
across their 24 seeds. Fallback makes cadence irrelevant with only one source.

Mixed-queue new-pattern exposure across the first 50 cards of 24 seeds:

| Cohort | Fixed cadence | Final cadence | Original queue |
|---|---:|---:|---:|
| One-combo nouns: a-masculine | 0 | 49 | 72 |
| Broad verbs: Eti | 8 | 47 | 58 |

Complete exclusion of the dominant new noun pattern is fixed. These counts do
not establish population-proportional exposure or restore every original share;
spacing deliberately balances patterns. No test requires uniform distribution.

Mean cross-seed first-50 set overlap:

| Mixed queue | Fixed cadence | Final cadence |
|---|---:|---:|
| One-combo nouns | 70.94% | 58.76% |
| Broad verbs | 58.15% | 53.23% |
| Rank-window verbs | 56.00% | 45.69% |

Overlap does not increase in any mixed case. Lower overlap means more variation
at a fixed state; it is not automatically a learning gain.

The largest increase in adjacent same-lemma repetition is +0.51 percentage
points in rare verbs (2.04% to 2.55%). Mixed two-noun queues rise +0.27 points
(21.10% to 21.37%). Adjacent combo repetition does not increase in any fixed-state
case; rare nouns improve by 0.94 points. Percentage points avoid misleading
relative changes from near-zero rates.

All four persisted restart probes retain their answer totals, first-new
position, review-bucket totals, and final due count. In 30 one-card visits, both
types introduce five new cards, starting at answer six, and serve five reviews
from each of the five mastery buckets. Three-card visits still drain their
finite cohorts.

## Comparison with the original queue

The earlier refactor's small-pool spacing and short-session service gains are
retained. This cadence change does not resolve its long-run workload differences
from the original scheduler. The +0.36%/+1.15% figures above measure only this
incremental change against the fixed cadence.

On seeds 17 and 83 shared with the original queue, full-refactor overdue debt is:

| Profile | Nouns | Verbs |
|---|---:|---:|
| AlwaysEasy | +27.87% | +21.07% |
| WeakPlural | +0.50% | +7.57% |

These full-refactor differences are not uniformly small. A separate change to
review allocation or admission needs separate evidence. See
`Specs/SRS-pre-refactor-comparison.md` for the preceding comparison.

## Verification and reproduction

The final canonical `auto` gate passes all 3,142 tests, including 126 simulation
tests. The focused queue/schedule run passes 94 tests. All 46 focused queue
reports reproduce byte for byte in the separate gate run. Schedule tests also
cover bounded variable gaps, balanced admission, arbitrary split equivalence,
review ordinal, stable seed lookup, and database reopening.

Final gate run: `20260912T032236.180736Z-82582-7dc919`, elapsed 408.97 seconds.
Evidence root:
`/private/var/folders/cn/1_yc26_j25v239tcn5zw3zzr0000gn/T/pali-practice-quality-837444863b1d/runs/20260912T032236.180736Z-82582-7dc919`.

The gate used `/private/tmp/pali-srs-verification` with the task's exact app/test
sources and committed resources. The user's existing Spanish/Russian edits were
excluded. The previously observed Spanish shared-English-label test failure in
the main working tree remains outside this change; neither resources nor its
assertion were changed.

Raw evidence is under `/private/tmp/pali-srs-cadence/`: `separated/` contains 86
final reports; `fixed-holdout/` contains 16 fixed-cadence year-long reports.
Original fixed reports remain under `/private/tmp/pali-srs-pre-refactor/current/`
and original-queue reports under its `old/` sibling. `separated-comparison.json`,
`separated-summary.txt`, `separated-tables.txt`, `analyze_candidates.py`, and
`summarize_final.py` hold paired calculations. Source fingerprints cover both
queue and cadence files. Earlier `balanced/`, `minimal-jitter-v2/`, and `dated/`
results are exploratory; the aborted `minimal-jitter/` run is not evidence.

The first full gate found a builder complexity increase despite passing tests.
Moving seed fallback and bucket-phase branches into their respective helpers
resolved it. The final corrected RNG policy passed the complete gate above.

A fresh read-only review found no blocking code issues. Its one documentation
verification confirmed the final evidence and disclosed regressions; one table
rounding error was corrected (+1.75% for WeakPlural nouns at seed 83). All nine
changed C# files match the passing verification checkout byte for byte. The
pre-existing Spanish and Russian resource hashes are unchanged.

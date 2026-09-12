# SRS ordering experiment

## Boundary and method

Request: try the proposed queue correction and measure it against the current
simulation report. Repository `/Users/ivm/Sources/PaliPractice`, branch
`codex/srs-simulation`, baseline `c7ab432f802273285b308f69faf7ea43d18ea3ee`.
The pre-existing Spanish and Russian resource edits are excluded from this task.

The production change preserves the due order of reviews skipped for spacing.
Previously, selecting a later review swapped the oldest skipped card behind
newer cards. The selected card now moves to the consumed position while the
skipped cards retain their order. The existing 100-candidate search bounds the
shift to 99 entries. New-card shuffling is unchanged.

Cooldowns, grading, retirement, identities, dictionary, history schema, bucket
rotation, and new-card ratio remain unchanged. The test changes add per-bucket
backlog evidence and fixed learner policies. No latent-memory model is used.

Before changing production, all 24 instrumented legacy timelines reproduced
all selections, sessions, and prior summary metrics from the current report
(floating-point debt tolerance 0.000001 card-days). The new ordering regression
failed for nouns and verbs on the baseline, then passed after the correction.

The paired experiment uses the bundled dictionary SHA-256
`9E951B247E7CF1AD053F9E6D9F4915B0D2F46A8B66B0E0FEAC357F3F1C82FCC9`.
It compares the same 24 legacy timelines plus eight new year-long timelines:
nouns/verbs × seeds 17/83 × AlwaysEasy/WeakPlural. Each year-long run completes
50 cards per day for 365 visits. WeakPlural always answers Hard on plurals and
Easy on singulars; it is an overload stress case, not predicted human behavior.
Fixed learner policies do not depend on ordering seed. The legacy policy is
retained only to preserve the existing comparison inputs.

## Paired results

Debt means cumulative eligible overdue card-days between sessions. Lower is
better, but aggregate totals weight long and overloaded scenarios more heavily.
These two seeds do not support a statistical efficacy claim.

| Measurement | Before | After |
|---|---:|---:|
| Original 24 scenarios: total debt | 310,846 | 310,146 (−0.23%) |
| Original 24: actual answers / requested budgets | 25,206 / 27,320 | 25,203 / 27,320 |
| Year-long 8 scenarios: total debt | 1,511,497 | 1,494,190 (−1.15%) |
| Year-long 8: actual answers / requested budgets | 146,000 / 146,000 | 146,000 / 146,000 |

Maximum consecutive eligible visits skipped improved in 7 of the original
24 runs and 6 of the eight year-long runs; it was unchanged in the others.
No run's maximum became worse. This is not a guarantee for every individual card.

| Year-long scenario | Seed | Debt change | Maximum visits skipped, before → after | Due at final visit, before → after |
|---|---:|---:|---:|---:|
| Nouns, AlwaysEasy | 17 | +1.60% | 33 → 32 | 824 → 835 |
| Nouns, AlwaysEasy | 83 | −2.10% | 32 → 32 | 839 → 826 |
| Verbs, AlwaysEasy | 17 | −3.41% | 36 → 32 | 823 → 814 |
| Verbs, AlwaysEasy | 83 | −2.99% | 39 → 33 | 836 → 795 |
| Nouns, WeakPlural | 17 | −1.18% | 81 → 81 | 1,374 → 1,362 |
| Nouns, WeakPlural | 83 | −4.06% | 82 → 81 | 1,392 → 1,366 |
| Verbs, WeakPlural | 17 | +0.45% | 88 → 83 | 1,291 → 1,296 |
| Verbs, WeakPlural | 83 | +1.80% | 86 → 84 | 1,261 → 1,268 |

The result is mixed at the individual-run level. Changing review order changes
future due dates, available pools, and which forms receive the fixed answer
policy. Three fewer answers occurred across two legacy changing-filter runs:
verbs seed 17 lost two answers and nouns seed 17 lost one. Small-pool lemma
repeats also rose slightly in some runs. No assertion was relaxed to accept
these differences; they remain characterization evidence.

## Remaining workload problem

After the fix, AlwaysEasy runs grow from 501–534 due cards at visit 180 to
795–835 at visit 365. At that final visit, 626–664 cards are in levels 7–8.
During the last 30 visits, levels 5–6, 7–8, and 9–10 each receive approximately
416–417 reviews, while 621–634 cards enter the observed due set at levels 7–8.
Equal turns across buckets do not track unequal demand in these runs.

WeakPlural runs grow from 640–693 due at visit 180 to 1,268–1,366 at visit 365.
All final outstanding due cards are at levels 1–2. Persistent Hard answers keep
that cohort active while the fixed ratio continues to introduce new cards.
The ordering correction cannot resolve this capacity imbalance.

`EnteredDueSet` is a difference between eligible due sets at visit boundaries.
It includes re-enabled cards and does not by itself count all arrivals during
answering. Do not treat it as a complete cooldown-arrival counter, especially
for level-1 cards that become due inside a session. Due counts, service, waiting,
and debt are reported separately.

Keep the ordering correction. The next controlled experiment should compare
backlog-aware new-card admission and review allocation against this baseline,
with an explicit minimum service rule for new cards and higher mastery levels.
Test those policies separately before combining them. Neither a permanent
new-card ban during backlog nor a cooldown/retirement redesign follows from
these results. Human retention remains unmeasured.

## Reproduce and evidence

Run instructions and metric definitions are in
`PaliPractice/PaliPractice.Tests/Practice/Simulation/README.md`. The original
24 reports are preserved in `/private/tmp/pali-srs-order/original-report`;
instrumented baseline and corrected reports are in `before/` and `after/`
under the same external directory. `compare.py` and `comparison.json` there
contain the per-run comparison. Report version 2 includes the scheduler source
hash; trace hashes cover session and bucket evidence.

Focused validation: 32 baseline timelines passed; both new ordering regressions
failed before the fix; all 63 simulation tests passed after it with no skips
(1 minute 45 seconds). TRX evidence is in `/private/tmp/pali-srs-order/results`.

The canonical `auto --base c7ab432` gate passed in the isolated verification
checkout: all 3,079 .NET tests passed with no skips, plus desktop build,
Roslyn/complexity, coverage-report, Python/producer, data, recursive submodule,
and unchanged-worktree checks. Gate runtime was 300.51 seconds. Evidence:
`20260912T013040.853872Z-8259-64b162` under the external quality runs directory.
All 32 corrected JSON reports match the gate's separate-process replay exactly.

The original worktree retains its previously confirmed Spanish resource-test
failure from the unrelated Top 100/300/500 edits; its assertion and the Spanish
and Russian files were left unchanged. The isolated gate uses committed resource
versions. All implementation and test files match the passing checkout; this
verification log was completed after that gate.

Fresh independent read-only review found no blocking issues. It verified the
stable shift, adjacent queue/persistence behavior, metric definitions, fixed
learner independence, and the reported numerical comparisons against retained
JSON. No reviewer fixes or second verification pass were needed.

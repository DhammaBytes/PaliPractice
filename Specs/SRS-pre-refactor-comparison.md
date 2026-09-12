# Queue quality before and after the SRS refactor

## Decision

The current queue improves small-pool spacing and service across short sessions,
but it is not an unconditional improvement. Large all-new pools preserve the
original shuffle. Mixed pools have a new selection bias caused by the fixed
new-card cadence interacting with review order and spacing. The broader
comparison also shows higher long-run overdue debt in several scenarios.

Keep the targeted spacing, due-admission, and skipped-review ordering fixes.
Revise the fixed-cadence approach while preserving progress across abandoned
queues. Do not ship a return to the old reset-on-every-build slot plan: it
reintroduces the demonstrated short-session problem. Workload policy still needs
its own controlled comparison; these results do not identify a best cooldown or
retention policy.

## Boundary and method

User request: compare queue quality and randomization against the pre-refactor
queue under different scenarios. Task base `f9a136a`, branch
`codex/srs-simulation`, repository `/Users/ivm/Sources/PaliPractice`.
Only tests and documentation change in this task. Existing Spanish/Russian
resource edits are excluded. Production queue, persistence, IDs, and dictionary
are unchanged.

- Original policy: `a15f954`. For deterministic execution, use the queue from
  `d942bb6`; its diff from the original adds only the optional clock field,
  constructor parameter, and replacement of `DateTime.UtcNow` for the seed date.
  Shared current persistence contains clock support and an additional read-only
  history-count query; original queue selection does not use that query.
- Current policy: `f9a136a`, including the earlier small stable-order correction.
- Same bundled dictionary, runtime, corpus oracle, simulated times, initial
  state, filters, attendance, and answer policies for each pair.
- 42 fixed-state cases: nouns/verbs × default, broad, two lemmas, one lemma,
  rare pattern/grammar, rank window, one grammatical combination × all-new,
  all-due, mixed. Each uses 24 date seeds, 17 days apart. These are 1,008 queues
  per policy, plus exact same-seed rebuild checks. The simulated clock stays
  fixed; changing the seed does not change due status. Each queue builds 60
  cards, and the reported prefix contains at most 50.
- Initial fixed-state mastery uses a separate seed (2718), levels 1–10, and
  due times in a fixed one-day band about 30 days overdue. Mixed state practices
  approximately half of the eligible forms. No answers are recorded in these
  snapshots. This is a controlled workload, not an estimated user population.
- Four additional persisted restart cases: two practice types × one/three
  answers per visit, 30 visits, synthetic 20-lemma corpus, 60 initially due
  cards distributed evenly across five mastery buckets. All answers finish
  before the shortest cooldown expires.
- 24 existing attendance/filter timelines and eight 365-visit timelines. The
  current timelines reuse the previous exact-replay evidence; their runner,
  queue, persistence and corpus are unchanged. The original queue was run
  against those same timelines. Legacy answers retain the original policy;
  year-long AlwaysEasy/WeakPlural policies are independent of the ordering seed.

## Randomization and content selection

### All-new and all-due queues

All-new queues were exactly identical between policies for six of seven filter
profiles in both practice types, including broad, default, rare, rank-window,
one-lemma, and one-combo. Each profile produced 24 distinct ordered queues.
Only the two-lemma profile changed, due to the tighter small-pool spacing.

Broad all-new first-50 sets had mean pairwise Jaccard overlap of 0.11% for nouns
and 0.33% for verbs, unchanged. Broad eligible pools contain 19,907 noun forms
and 10,678 verb forms; defaults contain 394 and 389. Low overlap in the broad
pools is expected, not a separate learning-quality score.

Every all-due fixed-state scenario produced one ordered queue across all 24
seeds under each policy. Review priority is deterministic in both versions.
Shuffling urgent reviews merely to increase entropy would be the wrong target.

### Mixed queues: fixed cadence reduces variation

When both pools remain available, the original new-card positions were 5, 6,
or 7 cards apart (4–6 intervening reviews). Current positions are always 6
cards apart. Both remain reproducible for a given state and seed.

Mean pairwise Jaccard overlap of first-50 form sets across date seeds:

| Mixed filter | Nouns, old → current | Verbs, old → current |
|---|---:|---:|
| Default | 42.7% → 48.1% | 29.4% → 38.8% |
| Broad | 60.9% → 54.5% | 57.3% → 58.1% |
| Rank window | 44.9% → 52.0% | 39.0% → 56.0% |
| One combo | 45.5% → 70.9% | 47.8% → 53.9% |
| Rare pattern/grammar | 61.9% → 65.4% | 65.4% → 65.4% |

Higher overlap means less selected-set variation, but urgent reviews should
naturally recur across identical-state snapshots. These are not consecutive
practice sessions. Set overlap cannot distinguish permutations of a small pool;
all small-pool new/mixed cases still produced 24 distinct orders.

### A concrete cohort-selection regression

The mixed masculine nominative-singular scenario has 776 eligible noun forms.
Of its 392 unpracticed forms, 314 are a-masculine. Across the first 50 cards of
24 queues:

| New cohort | Original queue | Current queue | Current + original variable slot plan |
|---|---:|---:|---:|
| Noun one-combo: a-masculine | 72 | 0 | 71 |
| Verb broad: Eti | 58 | 8 | 56 |

The third column of results comes from an external ablation: replace only the
current slot-planning region with the original randomized interval logic, while
retaining current due admission, bucket initialization, spacing and stable
review order. Fixed-state runs all start with zero completed answers, making
this a slot-rule comparison without differing history phases. All 42 fixed-state
cases passed the same invariants under this variant.

This supports a causal role for the fixed cadence in these scenarios. It does
not prove permanent cohort starvation during evolving practice, nor validate
reverting the slot plan in production. The old slot plan resets across short
sessions. Parent-pattern exposure need not match population proportions because
spacing intentionally balances patterns; the measured zero exposure and the
isolated restoration of exposure are the relevant findings.

## Spacing and short-session service

Within-session adjacent same-lemma rate in weekly, two-lemma timelines:

| Practice type | Original, two seeds | Current, same seeds |
|---|---:|---:|
| Nouns | 39.7–52.6% | 8.7–9.8% |
| Verbs | 59.8–62.8% | 14.1–16.2% |

The all-new two-lemma fixed-state case improves from 48.9% to 0.8% for nouns,
and 55.8% to 34.7% for verbs. Attested forms per lemma are uneven, so strict
alternation cannot persist after one lemma's available forms are exhausted.

Other spacing axes have trade-offs. In the fixed all-due two-noun case, adjacent
combo repeats rise from 0% to 3.2%. In the mixed rare noun case they rise from
3.0% to 4.3%. One-lemma cases necessarily repeat the lemma; one-combo cases
necessarily repeat the combination. Neither should be counted as avoidable
repetition.

The 90-visit one-answer-per-day, always-Hard timelines introduce only four forms
under the original queue, versus 19 currently. Maximum consecutive eligible
visits skipped falls from 84 to 20 for both types and both seeds. Total debt
rises from 250 to 832 card-days because the current queue admits more work:
lower old debt here was not evidence of better service.

In the frozen synthetic one-answer restart case, both types show:

| First 30 answers | Original | Current |
|---|---:|---:|
| New cards | 0 | 5 |
| Reviews from buckets 1 through 5 | 12 / 12 / 6 / 0 / 0 | 5 / 5 / 5 / 5 / 5 |

With three answers per visit, the first new form appears at answer 61 originally
and answer 6 currently. Both eventually drain the finite due cohort; this test
shows delayed service, not proof of permanent starvation with finite arrivals.

Spacing still does not cross built-queue boundaries. In weekly noun timelines,
the first lemma matches the previous visit's last lemma at 6–10 boundaries
originally and 13–17 currently. Those visits are a week apart, so this is not an
immediate in-session repeat. The refactor does not establish cross-session
spacing guarantees.

## Attendance, changing filters, and long-run debt

Daily broad 90-visit runs already have almost zero adjacent lemma repeats in
both versions. Current verb maximum waits improve from 13–14 to 9–11 visits;
noun waits change from 9–10 to 9. New admissions rise slightly. Year-return and
one-lemma scenarios show little difference. Changing-filter overlap and spacing
are mixed, with no uniform improvement.

Across two seeds per year-long cohort:

| 365 daily visits, 50 answers each | Current overdue debt versus original | Maximum skipped visits, original → current |
|---|---:|---:|
| Nouns, AlwaysEasy | +25.8% | 28–29 → 32 |
| Verbs, AlwaysEasy | +22.5% | 35–36 → 32–33 |
| Nouns, WeakPlural | −0.8% | 54–56 → 81 |
| Verbs, WeakPlural | +3.5% | 56 → 83–84 |

All eight runs complete 18,250 answers under both policies. AlwaysEasy final due
counts rise from 744–763 to 826–835 for nouns and 720–728 to 795–814 for verbs.
WeakPlural final due counts fall, while maximum waiting becomes worse. Neither
final backlog size nor a single aggregate metric captures queue quality.

The original 500-card admission cap also hid parts of large due workloads. In
the fixed broad all-due snapshots, its first 50 reviews are all from levels 9–10,
because old admission sorts by last practice time before limiting. The current
queue serves ten from each mastery bucket. Removing that exclusion changes
service substantially; the long-run debt difference must not be attributed to
the latest small ordering fix alone.

## Evidence and limits

External evidence: `/private/tmp/pali-srs-pre-refactor/`. `old/` and `current/`
contain 78 paired reports: 42 snapshots, four restarted-prefix cases and 32
timelines. `slot-ablation/` contains the 42 isolated comparisons. `analyze.py`
and `comparison.json` record the calculations; the three queue source variants
and task boundary are also preserved. Reports contain scheduler/dictionary
hashes. Jaccard measures intersection divided by union; adjacent-repeat rates
use actual within-session neighboring-card pairs. Timeline debt includes all
eligible due cards, including unselected cards, but excludes answering time.

Focused checks: 46 quality tests passed against each policy; all 32 original
queue timelines passed the shared validity checks. The ablation's 42 tests
passed. The short-case and snapshot probes characterize behavior rather than
hard-coding the current policy as the desired result. Two timeline seeds and
fixed answer policies do not establish statistical or human-learning efficacy.

The final canonical `auto --base f9a136a` gate passed in the isolated checkout:
3,125 .NET tests, no skips, plus desktop build, coverage-report,
Roslyn/complexity, Python/producer, data, recursive submodule, and unchanged-tree
checks. Runtime: 321.3 seconds. Evidence run:
`20260912T021215.025695Z-76853-ef6c7c`. All 78 current reports also match the gate's
separate-process output byte for byte.

The isolated checkout uses committed localization resources. The original
worktree's previously confirmed Spanish resource-test failure remains outside
this task; neither its assertion nor the pre-existing Spanish/Russian edits
was changed. Final implementation/test files match the passing checkout. These
verification notes were completed after the gate.

Fresh independent read-only review found no blockers. It checked the task diff,
producer/consumer boundaries, source identities, metrics, cohort counts, and
isolated slot-plan evidence. No reviewer fixes were required. External raw
reports should be retained if exact evidence inspection is needed later.

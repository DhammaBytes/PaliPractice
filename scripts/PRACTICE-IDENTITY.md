# Practice identity contract

The public lemma registry is append-only. Every mapping in the immutable v1.1
release baseline, including dormant lemmas outside the current frequency cutoff,
must remain. Missing production registries fail ordinary extraction. Candidate
registry additions are proposals, not writes to the production registry.

`configs/practice_registry.json` was bootstrapped from the v1.1 database using
its released selection rule: largest raw-pattern sense group, ties by lowest
DPD ID, then highest EBT sense with lowest ID as tie-breaker. That rule establishes
the historical anchor only. Each entry records lemma ID, noun/verb kind, anchor
headword, raw pattern, stem, and noun gender. Entries remain when a lemma leaves
the cutoff. Do not regenerate this file from a newer dictionary.

For retained identities, extraction selects the highest-EBT sense within the
anchored pattern/stem/gender, breaking ties by DPD ID. Sense-count changes cannot
switch the paradigm. A compatible primary-sense change preserves the historical
anchor; its meaning/example delta is reported. A lemma entering practice for the
first time selects its highest-EBT sense, then appends that paradigm choice.
Selection into the top-N set still uses the maximum EBT count across its senses.
The app ranks and renders using the explicitly selected practice sense.

`practice_primary` marks exactly one row per selected lemma in a candidate.
`Lemma` uses that row as Primary and includes only senses with its pattern, stem,
and gender. Repositories, queue ranking, details, and rendering already consume
`Lemma.Primary`. Old bundles without the column retain their original selection
rule; candidate validation rejects a missing or ambiguous explicit selection.

## Changes that require explicit treatment

- `vassa` retains its released masculine paradigm. The more frequent neuter
  sense does not inherit masculine mastery. Supporting both requires a new
  append-only practice identity and is outside this update.
- `pathavī` and `mahāpathavī` retain lemma and grammatical identities. The reviewed
  corrections file permits exactly the DPD change from `ī fem` with stem ending
  in `v` to `vī fem` with that `v` in the endings. It does not authorize arbitrary
  future pattern changes. Candidate output includes the pinned correction file.
- `musati` may change primary DPD sense if the new row has the same `mus` stem
  and `ati pr` paradigm. Its headword and meaning changes appear in the report;
  existing mastery is not cloned or reassigned to another paradigm.
- A retained lemma with no compatible source paradigm fails generation. A
  linguistic correction needs an exact reviewed from/to entry. A genuinely new
  paradigm needs a new practice identity, not an exception that reinterprets old
  mastery. Never solve this by renumbering the lemma registry.
- Cutoff removals make mastery dormant. Keep the mapping and practice anchor so
  re-entry can restore eligibility under the same identity. M5 covers history
  and statistics behavior.

`compatibility.json` compares the candidate with v1.1: cutoff changes, selected
headwords/paradigms, primary detail changes, and public EndingId=0 eligibility.
The eligibility comparison enables all rank/pattern/grammar filters and excludes
the active present-third-singular verb citation. Until M4 repairs corpus storage,
this report uses existing stored flags and does not certify their truth.

## Internal form identity for M4

Public combination IDs remain unchanged. Internal noun and verb form records
must carry DPD headword ID plus grammatical components, ending index, and actual
rendered string. Headword identity resolves to the stored pattern/stem/gender;
corpus lookup must use the selected primary headword and rendered string.
Irregular lookup must use the same headword scope. Ending indices are local to
that paradigm and cannot identify attestation across a cleaned lemma.

Identical duplicate internal records may deduplicate; a conflicting rendered
form under the same internal key must fail. Queue eligibility and rendering must
use the same verified primary form set. M4 adds the storage and consumer changes
and checks every primary generated form against the pinned corpus.

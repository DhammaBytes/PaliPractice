# M3 verification record

Status: complete. Candidate, producer tests, and full gate passed; independent
review found no blockers.
Base: `214094b8b482446ab7e4f8f0afd171bade4400b0`.

The [practice identity contract](../../../scripts/PRACTICE-IDENTITY.md) preserves
all released lemma mappings and historical practice paradigms. The practice
registry was seeded from the v1.1 baseline and remains append-only for new
practice choices, including dormant entries. Ordinary generation requires both
registries. Explicit empty-registry bootstrap refuses overwrite and cannot pass
production historical validation.

Candidates persist exactly one primary sense per lemma. The app reads that flag
and uses the selected pattern/stem/gender for its included senses. Existing
bundles without the new column retain released behavior. Candidate validation
requires the flag and checks it against the practice registry, even when a
modified database has been rehashed in its output manifest.

The May DPD candidate built under `.local/candidates/english-m3-final` passes
structural and identity validation. Its [input manifest](input-manifest.json),
[candidate manifest](candidate-manifest.json), and full
[compatibility report](compatibility.json) are retained here. M5 selects the final
current DPD release; this candidate is not promoted into the app.

Compared with released v1.1:

| Measure | Nouns | Verbs |
| --- | ---: | ---: |
| Retained lemmas | 1,399 | 714 |
| Cutoff additions / removals | 101 / 101 | 36 / 36 |
| Retained eligible combinations | 18,676 | 10,366 |
| Added eligible combinations | 1,234 | 331 |
| Removed eligible combinations | 1,195 | 352 |
| Primary detail changes | 87 | 34 |

All removed eligible combinations belong to removed lemmas. Exactly two retained
paradigms change under the explicit `pathavī`/`mahāpathavī` source corrections.
These eligibility figures use existing corpus flags; M4 must repair and verify
those flags. They are not proof of grammatical attestation.

Twenty-two Python tests passed, including actual isolated extraction, input
preservation, historical/dormant mapping checks, append-only allocation,
malformed registries, reordered and changed sense counts, incompatible paradigms,
exact corrections, explicit bootstrap refusal, and selection tampering.
Six app tests cover explicit/legacy selection, stem/gender isolation, duplicate
selection failure, verbs, and actual SQLite repository loading.

The complete `auto` gate passed all routed groups with 2,853 .NET tests, no
failed/skipped tests, and a successful desktop build. Coverage remains advisory.
The gate's existing data tests still read the pending bundle; exact candidate
integration and device provisioning checks remain M5 work. Full evidence paths
and counts are in [verification.json](verification.json).

The original pending bundle, version, lemma-registry additions, and static noun
endings remain outside this milestone commit. No production promotion occurred.

Fresh reviewer `m3_review` inspected M3 and the direct repository/queue consumers.
It reported no blocking findings; no code fix or verification pass was required.

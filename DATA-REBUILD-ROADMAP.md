# DPD rebuild and translation roadmap

Created: 7 September 2026. Status: M1 and M2 complete; M3 is next.

The objective is a reproducible, grammatically correct English DPD database with stable production practice identities, followed by verified Russian and Spanish meanings in the next app update. The findings and measured starting state are in [DATA-REBUILD-AUDIT.md](DATA-REBUILD-AUDIT.md).

## Sequence and working rules

| Milestone | Outcome | Depends on |
| --- | --- | --- |
| M1 | A preserved production baseline and an operational quality gate | — |
| M2 | An isolated, pinned, offline English candidate build | M1 |
| M3 | Stable practice identities and explicit paradigm selection | M2 |
| M4 | Correct grammar, corpus attestation, and complete pattern verification | M3 |
| M5 | A stable English candidate with a green gate and verified upgrade behavior | M4 |
| M6 | Russian enrichment through a tested translation contract | M5 |
| M7 | Spanish enrichment with verified sense mapping | M6 |
| M8 | App support for English, Russian, and Spanish meanings | M7 |
| M9 | Verified multilingual release candidate and deliberate promotion | M8 |

Work through one milestone at a time. Each milestone below has bounded steps and an exit condition. Keep a completion record with the task diff, tests, gate evidence, independent review, and unresolved issues. Do not mark a milestone complete because its implementation exists without the required evidence.

Core English work ends at M5. Translation implementation must not begin before that checkpoint. Source discovery for planning has already been done; it does not count as an implemented importer.

- Preserve existing uncommitted changes. Select and record an actual released database/registry baseline; HEAD is a comparison reference, not automatic proof of what users have installed.
- Keep historical lemma IDs and `EndingId=0` SRS combination IDs stable. Keep dormant mastery when lemmas leave the cutoff. Do not clone mastery into additional paradigms.
- Accept justified frequency-cutoff churn. Report semantic and eligibility changes; do not freeze dictionary row counts or expand limits merely to obtain a pass.
- Continue preserving the noun-only exclusion of `atthi` and its verb senses.
- English extraction must work without translation downloads. Intermediate English-only candidates are not shipped over the existing Russian-capable bundle.
- Keep the English core unchanged during translation enrichment: the same headword selection, registry, grammar, forms, corpus flags, rankings, examples, and English meanings. Verify that invariant mechanically.
- Do not widen complexity baselines, skip tests, or retain percentage mismatch allowances to make defects pass. The gate can remain red for explicit known product defects through M4; by M5 it must pass.
- The implementation goal authorizes a commit after each completed milestone. Database promotion, publication, and release remain separate actions.

## M1 — Preserve the baseline and restore useful gate execution

**Purpose:** make subsequent failures trustworthy without losing the pending rebuild or treating it as the released compatibility baseline.

- [x] Inventory pending app, data, registry, and nested-submodule changes. Preserve the current bundles and identify the released database, registry, and application selection behavior used to derive historical practice identities.
- [x] Record DPD database version separately from its source checkout and corpus-input provenance. Preserve the current audit evidence in a durable project-owned record where needed; `/tmp` is not a release archive.
- [x] Resolve the stale analyzer-baseline anchor through the repository's reviewed baseline process. Inspect findings against the intended base; do not just replace the hash or allow changed code.
- [x] Reconcile required dirty submodule states without resetting user changes. Distinguish generated input files from tracked source and accidental local files.
- [x] Restore execution of applicable .NET and desktop gate lanes; refactor the identified quality-helper defects and affected complexity increases in bounded changes. Ensure an unavailable lane reports why, while independent checks still run.
- [x] Triage the recorded package advisories and assign upgrades/validation to the affected dependency paths. Required release remediation is completed by M5.

**Exit:** the preserved baseline is identifiable, all applicable gate lanes execute or report a concrete environment limitation, and remaining failures have an explicit owner/milestone. A passing structural check is not presented as grammatical validation. No hidden skip or baseline relaxation is introduced.

**Evidence:** baseline manifest, worktree inventory, complete gate run, and a small failure inventory linked to M2–M5. This milestone establishes an operational gate; it does not claim a green English database.

## M2 — Build an English candidate without side effects

**Purpose:** make correctness fixes testable against controlled inputs before touching the bundled database.

- [x] Separate acquisition of upstream inputs from extraction. Accept explicit paths for DPD, all four corpus files, registry, custom English adjustments, output directory, and a supplied database version.
- [x] Record immutable source revisions/releases and checksums, relevant extraction configuration, and code revision. Include configuration such as selection limits and exclusions. Verify checksums before extraction.
- [x] Make the core extraction path independent of the Russian importer. Preserve translation-capable application behavior and existing bundled data while the English candidate is under development.
- [x] Require complete, parseable, nonempty corpus inputs. Establish how the corpus files are reproduced from pinned source corpora; a DPD checkout update alone is insufficient.
- [x] Write all proposed outputs—including registry additions and version/manifest files—under a unique candidate directory. Do not update the production registry or bundled version during candidate generation.
- [x] Add a separate validation/promotion interface. A failed build must leave the existing bundle and registry unchanged. M4 expands semantic validation; M5 verifies recovery and final promotion behavior.
- [x] Add deterministic tie-breakers for frequency selection and new-ID allocation. Given the same input registry, candidate contents must not depend on database traversal order, working directory, or wall-clock time.

**Exit:** an English candidate can be built offline into a temporary directory from a manifest. Missing inputs, a wrong checksum, or interrupted generation fail without altering bundled data or the production registry.

**Tests:** unavailable/corrupt corpus input, malformed manifests, deterministic ties, unchanged source/registry fingerprints, and extraction with translation inputs unavailable. Do not run candidate generation from the gate yet under the existing no-extraction policy.

## M3 — Protect production identities and specify the practice paradigm

**Purpose:** prevent a rebuild from silently changing what an existing mastery ID represents.

- [ ] Require the production registry for ordinary extraction. Keep registry initialization as an explicit bootstrap path. Compare every historical mapping against the released baseline, including dormant lemmas; additions are append-only.
- [ ] Define one explicit practice-paradigm selection contract used by generation, repositories, ranking, and rendering. Persist the historical choice where necessary rather than recalculating it from changing sense counts.
- [ ] Classify changes as source corrections within a paradigm, meaning/example changes, compatible primary-sense changes, incompatible paradigm changes, or cutoff additions/removals. Record the chosen treatment for cases such as `vassa`, `pathavī`, and `musati`.
- [ ] Define the internal headword/paradigm identity needed by corpus and irregular-form storage. Preserve public SRS IDs. M4 implements the attestation repair against this contract.
- [ ] Produce a comparison report for old/new eligibility and primary headword, stem, pattern, and gender. Unexplained reassignment fails; expected cutoff movement is reported.

**Exit:** historical IDs cannot be reassigned even if both candidate database and registry are regenerated together. Generation and the app select the same practice paradigm. Incompatible changes have a written policy and tests.

**Tests:** missing registry, deleted/dormant mapping, attempted renumbering, newly appended lemma, reordered senses, changed sense counts, and retained-ID paradigm changes. Full practice support for additional paradigms is outside this release unless required to resolve a specific compatibility defect.

## M4 — Fix grammar and prove exact rendered-form correctness

**Purpose:** close audit points 1, 2, and 5 with behavior tests rather than percentage thresholds.

- [ ] First reproduce the false attestation of `addhanesu`, `sīhanādebhi`, and `sīhanādāna` in regression tests using real repositories and `InflectionService`.
- [ ] Scope corpus attestation and irregular forms to the selected headword/paradigm, and verify the rendered string. Queue eligibility must use the same form set. Allow deduplication of identical records but reject conflicting records instead of swallowing insert errors.
- [ ] Skip recognized non-inflection rows such as `in comps`. Reject malformed selected templates and unknown grammatical encodings. Require valid nonzero grammatical components in corpus/irregular records.
- [ ] Correct both validators and the gate fixture that currently accepts `Case.None`/`Number.None`. Replace 10%/5% mismatch tolerances with exact assertions.
- [ ] Check every extracted raw pattern through production pattern helpers and every selected primary noun/verb through production form generation. Cover regular, variant, irregular, plural-only, and legitimate defective paradigms. Validate static ending order against pinned DPD templates.
- [ ] Remove the unverified fixed ending-count assumption from lookup behavior, or enforce a documented supported bound with a failing overflow test. Never truncate an extra ending silently; keep the public ID encoding valid.

**Exit:** no invalid grammatical corpus IDs, no conflicting internal form identities, and exact agreement between every generated primary form's attestation flag and the pinned corpus. Every selected pattern is supported and usable according to its legitimate paradigm. Existing SRS combination IDs remain compatible under M3's policy.

**Tests:** full corpus/form round trips, queue/render agreement, collision fixtures, invalid rows, malformed templates, unknown patterns, empty usable paradigms, variant ordering, ending bounds, and the noun-only `atthi` exclusion.

## M5 — Establish the stable English checkpoint

**Purpose:** finish audit point 4, the quality-gate improvements, and production upgrade behavior before adding translation data.

- [ ] Run candidate validation before promotion: SQLite integrity, schema, detail parity, registry/identity compatibility, grammar, supported patterns, usable forms, exact attestation, and version/manifest consistency.
- [ ] Make promotion of database, version, registry, and manifest recoverable across failure. Use an explicit staged/recovery protocol; independent file renames do not constitute a transaction. Test interrupted promotion and rollback.
- [ ] After M2's isolation is proven, deliberately update the repository's no-extraction gate policy to permit candidate-only regeneration. Adopt ConjuGato's two isolated builds from identical inputs with a fixed version, semantic comparison, deterministic byte checks, and source-file preservation.
- [ ] Make integration tests consume the exact verified candidate. Route changes to all affected producers/consumers, retain useful external evidence, and require a green complete local gate. Do not copy unrelated coverage targets or platform policies from ConjuGato.
- [ ] Protect user-data upgrades with versioned transactional migrations where required. Store historical form/lemma/grammar snapshots for new history records, and preserve resolvability of existing records through baseline data where possible. Test records for removed lemmas; document any irrecoverable old records honestly.
- [ ] Define lifetime-versus-current statistics semantics. Keep dormant mastery, exclude ineligible records from active review queues, and make displayed totals/due counts consistent with the selected semantics.
- [ ] Complete relevant package remediation and app database-provisioning checks. Verify stale/corrupt copies, interrupted replacement, and compatibility with existing practice data.
- [ ] Select and pin the DPD release current at implementation time. Run the English candidate through all checks and review frequency and semantic deltas. Do not assume the July snapshot remains the desired version indefinitely.

**Exit — translation work may now begin:** the English candidate passes the full gate, isolated repeatability checks, identity/upgrade tests, and bounded desktop plus relevant iOS/Android database-provisioning smoke checks. The English manifest and semantic digest are frozen as the base for M6–M9. All five core audit points are closed with evidence.

If the selected DPD inputs change later, regenerate and revalidate this checkpoint before applying translations. This is a stable candidate checkpoint; the bundled app is not replaced with an English-only build as an intermediate step.

## M6 — Define translation enrichment and harden Russian import

**Purpose:** add translations to a verified English core without affecting grammatical data or selection.

- [ ] Introduce a small common result contract for imported meanings: language, exact DPD headword ID, plain meaning, source key/revision/checksum, and mapping/availability status in build evidence. Keep source-specific parsing in separate RU and ES adapters.
- [ ] Make language enrichment deterministic and offline from pinned inputs. Fail on import corruption; distinguish valid missing translations from download/parse/mapping failures.
- [ ] Harden Russian parsing and pin its TSV. Preserve the existing curated `ru_meaning` → `ru_meaning_raw` → empty rule. Report missing/unknown IDs and coverage by selected sense and primary practice lemma.
- [ ] Compare every stored Russian meaning with the pinned source. Report coverage changes and source revisions without arbitrary frozen row counts.
- [ ] Assert that applying, removing, or refreshing a translation layer changes no part of the frozen English core and no practice identity. A failed language import must not produce a partially promoted bundle.

**Exit:** Russian enrichment passes parser fixtures, exact selected-ID comparisons, English-core immutability checks, and the relevant gate lanes. Existing Russian selection/fallback behavior remains compatible.

**Tests:** curated/raw/empty cases, duplicate and invalid IDs, malformed TSV rows/headers/encoding, missing source, deterministic output, source mismatch, and correct English fallback for valid missing meanings.

## M7 — Map and import Spanish senses correctly

**Purpose:** adapt the Spanish project's actual export format rather than assuming it is a Russian-style DPD fork.

Planning inspection used [`8a634e53d0605bc50f0a9913b221ebc6a40f17b2`](https://github.com/DhammaBytes/dpd-dictionary-es/tree/8a634e53d0605bc50f0a9913b221ebc6a40f17b2), dated 18 July 2026:

| Russian source | Spanish source |
| --- | --- |
| TSV with numeric DPD `id` | `js/dpd_ebts_es.js`, a JavaScript assignment containing JSON-compatible data |
| Dedicated meaning columns | Formatted definitions containing POS, HTML, optional literal meaning, and etymology |
| Direct ID join | Full headword/sense strings, for example `atthi 1.1`; no numeric DPD IDs in the meaning map |
| Curated/raw precedence | Paired `js/dpd_ebts.js` English export available for sense checks |

Both inspected ES and EN maps contain 72,698 unique keys with identical key sets. `dpd_i2h.js` maps forms to headword strings; it is not an ID registry. The README's description of numeric keys does not match the inspected files.

Against the existing pending database, 2,328/2,384 noun rows and 1,268/1,317 verb rows have exact Spanish headword keys. **These are key-availability counts, not verified translation coverage.** Numbered senses or meanings may have changed between source snapshots. Recompute against M5's selected release.

- [ ] Pin and hash the paired Spanish/English files. Prefer an upstream structured source with explicit DPD IDs if one is later found; otherwise parse only the known assignment/data format without executing remote JavaScript.
- [ ] Match complete headword keys to the pinned DPD headword records. Verify sense correspondence using the paired English definition and DPD data. Do not join by cleaned lemma, strip numeric sense suffixes, or infer an ID from iteration order.
- [ ] Classify exact verified matches, renamed/renumbered keys, meaning drift, ambiguous matches, missing keys, and unparseable definitions. Do not accept a key-only match as proof of semantic identity. Maintain explicit reviewed mapping overrides with source fingerprints where necessary; unresolved cases use English fallback.
- [ ] Define and test extraction of the Spanish meaning separately from POS/literal/etymological material, HTML/entity handling, Unicode, and whitespace. Use representative fixtures from the actual export, including unbolded definitions and multiple senses.
- [ ] Validate every accepted selected mapping and report unmatched/missing/ambiguous items by primary lemma and sense. Review terminology and a risk-based sample of accepted definitions; mechanical mapping checks do not prove translation quality.
- [ ] Carry source attribution and translation provenance into release credits. The source README states CC BY-NC-SA 4.0 and describes AI-assisted translation; preserve that provenance without treating its self-reported quality score as validation.

**Exit:** every imported Spanish meaning has a traceable verified DPD identity; no ambiguous mapping is silently imported. Parser and mapping fixtures pass, coverage gaps are explicit, and Spanish enrichment leaves the frozen English core unchanged.

## M8 — Add Spanish meaning selection to the app

**Purpose:** make all three meaning languages usable while preserving existing preferences and fallback behavior.

- [ ] Extend bundled details storage/models and `GetMeaning` for Spanish. Choose the smallest schema change consistent with the three supported languages and M6's contract; keep schema/version checks and provisioning in sync.
- [ ] Append Spanish to the persisted preference enum without changing existing `English=0` and `Russian=1`. Do not bind persistence to a reordered display list. Preserve explicitly selected existing preferences.
- [ ] Add the Spanish option and required labels in the app's existing UI resource languages. Handle Spanish locale variants for new/default preferences. A full Spanish UI localization is separate from Spanish dictionary meanings and is not assumed in this roadmap.
- [ ] Use requested-language → English fallback per sense. Verify mixed coverage, combined translation entries, language changes, persistence across restart, and unsupported/corrupt preference values.
- [ ] Test that switching translation language does not change eligible forms, practice identities, mastery, or English data. Check long meanings, Pāli diacritics, and removal of source HTML/etymology scaffolding in actual practice cards.

**Exit:** English, Russian, and Spanish meanings work in the app, existing preference values retain their meanings, missing translations fall back correctly, and platform smoke checks cover the affected screens. Unit/integration tests and applicable gate lanes pass.

## M9 — Validate and promote the multilingual release candidate

- [ ] Build a fresh final candidate from the M5 English manifest and pinned RU/ES manifests. Repeat offline and verify reproducibility and unchanged English semantics.
- [ ] Produce the release data diff: added/removed lemmas, eligible-combination changes, paradigm/sense changes, language coverage and mapping exceptions, and all source/output identities.
- [ ] Run the complete gate against the final diff and database. Run fresh-install and existing-user upgrade journeys, including dormant mastery/history, all three meaning languages, fallback, settings persistence, and noun/verb practice.
- [ ] Complete independent review, source credits, and user-facing notes for relevant data changes. Resolve remaining blocking findings before promotion.
- [ ] Promote only the validated candidate with its matching version/registry/manifest, using M5's recovery protocol. Verify packaged database identities on supported target builds and retain the prior bundle for rollback.

**Exit:** one traceable multilingual bundle passes semantic, compatibility, reproducibility, and app checks. The exact validated bundle is packaged. App publication and commits remain deliberate actions outside automatic milestone completion.

## Quality-gate responsibilities after M5–M9

| Check group | Required evidence |
| --- | --- |
| Input integrity | Immutable revisions/hashes, complete corpus inputs, explicit versions/configuration; offline verification |
| English semantics | SQLite/schema/detail parity, valid grammar, supported patterns, usable forms, exact attestation |
| Production compatibility | Append-only historical registry, explicit paradigm policy, eligibility delta, migration/history/statistics behavior |
| Generation | Two isolated candidate runs, semantic/deterministic output comparison, no source mutations, validated recoverable promotion |
| Translations | Separate adapter fixtures, exact RU values, verified ES sense mappings, explicit gaps, unchanged English core |
| App | All selected primary forms through real consumers; language persistence/fallback; targeted provisioning and practice smokes |

Early milestone tests can be scoped to the relevant behavior, but failures in required final checks cannot be waived by using a narrower profile. Keep gate implementation policy in [quality/README.md](quality/README.md), its mechanical backlog in [quality/TODO.md](quality/TODO.md), and broader product protection in [QUALITY-TODO.md](QUALITY-TODO.md).

## Completion record

Update this table at the end of each milestone with durable evidence links.

| Milestone | Status | Evidence / remaining blockers |
| --- | --- | --- |
| M1 | Complete | [Baseline, gate, review, and remaining work](quality/evidence/m1/README.md) |
| M2 | Complete | [Inputs, candidate, gate, and review](quality/evidence/m2/README.md) |
| M3 | Next | M2 complete; protect identities and specify practice paradigms |
| M4 | Planned | Requires M3 |
| M5 | Planned | English stability checkpoint |
| M6 | Planned | Blocked on M5 completion |
| M7 | Planned | Spanish source discovery complete; implementation requires M6 |
| M8 | Planned | Requires verified RU/ES enrichment |
| M9 | Planned | Requires all previous milestones |

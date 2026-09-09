# Database rebuild audit — 7 September 2026

> Historical audit of the pre-hardening database. Its findings were addressed by
> [milestones M1–M9](DATA-REBUILD-ROADMAP.md); see the
> [M9 verification checkpoint](quality/evidence/m9/README.md) for the resulting
> data and model evidence. This report preserves the state observed at audit time,
> not the current release status. UI and publication checks remain separate.

At the time of this audit, the rebuild was not ready to release. Ordinary frequency-cutoff churn is expected, but corpus-attestation collisions and invalid grammatical IDs are confirmed defects. A newer published DPD database is available; the Russian TSV used by the importer has not changed since the July rebuild.

This was a read-only audit of application data and source code. No extraction, source-input replacement, registry update, commit, or production migration was performed. The only repository addition is this report. Existing worktree changes were preserved.

## Sources and freshness

| Input | Current local state | Verified upstream state |
| --- | --- | --- |
| Application database | `pali.db`, `user_version=2026071300`, generated 13 July | Not rebuilt during this audit |
| DPD database | `db_info.dpd_release_version=v0.4.20260531`; 89,050 headwords | Latest published release is [`v0.4.20260728`](https://github.com/digitalpalidictionary/dpd-db/releases/tag/v0.4.20260728); 89,280 headwords |
| DPD source checkout | `56fe6d1835d0f8efd1ce079d991bcfb74c423aad`, 3 January 2026; origin uses the old DhammaBytes URL | [`main=b808547dd04a87f468f2ea350e5a8448cc5bfa45`](https://github.com/digitalpalidictionary/dpd-db/commit/b808547dd04a87f468f2ea350e5a8448cc5bfa45), 7 September; newer than the published database |
| Russian fork | Importer reads a moving `sbs-ru` URL; no revision recorded in `pali.db` | Default branch [`sbs-ru=c179f0c044e3e0ca366ac47445f30a1b2ffc732c`](https://github.com/sasanarakkha/dpd-db-sbs/commit/c179f0c044e3e0ca366ac47445f30a1b2ffc732c), 11 July |
| Russian TSV | All 3,701 stored fields exactly match the current source, including empty values | Last file change [`74adf997e47ffbb6eabffb966fb33edfbef24218`](https://github.com/sasanarakkha/dpd-db-sbs/commit/74adf997e47ffbb6eabffb966fb33edfbef24218), 10 July |
| Four corpus wordlists | Present; modification dates 21 December 2025; union has 1,142,215 strings | Generated, ignored inputs: the release Git directory contains only its README, not versioned wordlists. A source-checkout update does not refresh or establish their provenance. |

The Russian repository has September activity on `as_upstream`; that is not a newer Russian TSV on the importer/default branch. The other two feature branches also have older TSV history. There is no evidence that our importer should switch branches.

The downloaded release archive was verified against GitHub's published SHA-256:

```text
DPD dpd.db.tar.xz: cd746f33633c087e514e8eed1f6939d9a8debb1e9dca5d8447fcc32fd343b7fe
Russian TSV:      7e1fb87986944f5dbed9769d2428b7880826e3f6b70b7aec0fade285738b605d
```

The source checkout, downloaded DPD database, corpus files, and Russian translations are separate inputs. A submodule commit alone cannot identify this build.

## What the July DPD release would change

The released database was downloaded and inspected separately at `/private/tmp/pali-audit-20260907/dpd-latest.db`. Production noun/verb selection methods were called against it with a read-only SQLite connection, without invoking extraction or allocating IDs.

- `dpd_headwords` and `inflection_templates` have the same columns as the May release.
- All inflection-template JSON values are semantically unchanged; no templates were added or removed. The newer release does not itself require another static-ending synchronization relative to the current worktree.
- Among currently included rows, 420 noun EBT counts and 282 verb EBT counts change. No current headword IDs disappear or acquire another cleaned lemma.
- `bandhana 5` (47908) changes from `nt` / `a nt` to `adj` / `a adj`, and therefore leaves noun extraction as intended.
- Current filters select 2,394 noun senses under 1,500 lemmas, and 1,325 verb senses under 750 lemmas. Nine noun lemmas and six verb lemmas are exchanged relative to the pending July build. All selected lemmas already exist in the current registry.
- With the current Russian TSV, coverage would be **2,383/2,394 noun rows** and **1,318/1,325 verb rows**. This is a predictable source-coverage change; it is not evidence of failed import.
- No retained noun changes its selected primary headword/paradigm. Retained verb `musati` changes primary from 81032, “deceives; dazzles; confuses,” to 53039, “robs; plunders; steals (from).” The latter gains a `sutta_1` citation and becomes eligible; equal EBT counts then favor the smaller ID. Stem `mus` and pattern `ati pr` remain unchanged. This deserves a semantic-change entry in the release diff, not a lemma-ID renumbering.

These are selection previews, not validation of a newly generated database or refreshed corpus-attestation data.

## Fixes needed before the next rebuild

### 1. Separate attestation identity from stable practice identity

[`forms.py:27`](scripts/extraction/forms.py#L27) encodes a lemma, grammatical combination, and local ending index. [`extract_nouns_and_verbs.py:728`](scripts/extract_nouns_and_verbs.py#L728) merges rows from every sense into that space and swallows `sqlite3.IntegrityError`. [`InflectionService.cs:112`](PaliPractice/PaliPractice/Services/Grammar/InflectionService.cs#L112) renders the selected noun but resolves attestation using that merged ID space.

The current data reproduces **57 IDs associated with distinct template strings**, of which **48 occur in the stored corpus table**. The primary practice forms with false-positive attestation are:

| ID | Rendered form | Source of the error |
| --- | --- | --- |
| 103357121 | `addhanesu` | The regular minority `addha` paradigm supplies an attested slot used by the irregular primary paradigm. |
| 110075122 | `sīhanādebhi` | The eastern minority paradigm assigns another string to this ending slot. |
| 110076121 | `sīhanādāna` | The same cross-paradigm slot collision. |

There were no equivalent verb collisions or primary false-positive/false-negative flags in this audit. No broad irregular-array drift was found.

Preserve public `EndingId=0` combination IDs. Give internal attestation and irregular-form storage a headword/paradigm identity and verify the actual rendered string; have both queue eligibility and rendering use the same selected paradigm. If output is deliberately limited to one canonical practice paradigm, generation and the app must share that explicit selection contract. Identical duplicate data can be deduplicated, but conflicting strings must fail validation rather than be silently discarded.

Required regression test: for every selected primary noun and verb, use real repositories plus `InflectionService` and assert exact equivalence between each rendered form's `InCorpus` flag and membership in the pinned wordlist union. Include the three failures above, alternate patterns, and irregular forms. Verify queue eligibility against those same generated forms.

### 2. Reject invalid grammar instead of counting it as complete enough

The parser processes `in comps` rows as inflections. The current noun corpus table contains **1,665 invalid IDs out of 31,735**, with `Case.None` and `Number.None`. All 12,505 verb corpus IDs have valid grammar.

The legacy validator prints **94.8%** noun grammar completeness and exits successfully. The new gate also passes: [`data_contract.py:214`](quality/checks/data_contract.py#L214) allows zero components, and [`test_data_contract.py:147`](quality/tests/test_data_contract.py#L147) explicitly requires those noun values to pass.

Skip recognized non-inflection rows. Reject unknown/malformed grammatical rows in selected templates. Require nonzero, valid case/gender/number or tense/person/number/voice in corpus and irregular tables. Update the gate fixture that currently accepts invalid grammar. Preserve legitimate defective paradigms and unattested forms; neither requires zero-valued corpus IDs.

Replace the irregular tests' 10% noun / 5% verb mismatch tolerances with exact form-resolution checks. Their explanation that string checks make ID mismatches harmless contradicts the actual ID-based lookup.

### 3. Protect historical identities and make primary selection explicit

[`Lemma.cs:30`](PaliPractice/PaliPractice/Models/Words/Lemma.cs#L30) chooses a pattern by number of senses, then chooses its highest-frequency headword. This affects meanings, examples, ranking, gender, and inflections. **108 current noun lemmas have multiple paradigms; 39 select a paradigm with a lower maximum EBT count than an excluded one.**

This is a product/linguistic policy issue, not proof that the maximum-EBT sense is always the correct replacement. Do not blindly change selection to frequency order for existing production IDs. Record the historical practice paradigm; treat incompatible changes and additional paradigms explicitly. `vassa` is a useful review case: masculine senses outvote the more frequent neuter “year” sense.

The current registry is append-only relative to HEAD: five noun entries added, no removals or remappings, no verb changes. However, [`registry.py:64`](scripts/extraction/registry.py#L64) silently returns an empty registry when the file is absent. The gate only checks the current database against the current registry; rebuilding both can therefore hide reassignment.

Normal extraction must fail if the production registry is missing. Initialization must be an explicit bootstrap operation. Check all historical registry mappings against an explicit release baseline, including lemmas outside the latest cutoff. Report retained primary headword/stem/pattern/gender changes separately from added/removed lemmas. Add deterministic tie-breakers to frequency selection; the current Python sort inherits unspecified query ordering for ties.

### 4. Pin inputs and validate the candidate before promotion

The importer fetches a moving Russian URL. The database records only an application build number. Missing corpus files generate warnings and extraction continues. Malformed template JSON silently becomes an empty form list. Selected noun rows/details are inserted before usable forms are established.

[`extract_nouns_and_verbs.py:1046`](scripts/extract_nouns_and_verbs.py#L1046) promotes after extraction/reporting without enforcing the semantic contract. Registry saving happens earlier, and version-file/database promotion uses separate file operations.

Implement a candidate workflow with explicit DPD, Russian TSV, corpus, registry, output, and version inputs. It should run offline once inputs are acquired. Record source release/commits, input hashes, extraction-code revision, filters, and output identity in a manifest. All four required corpus files must be present, readable, nonempty, and validated.

Before promotion, require SQLite integrity, schema/detail parity, registry compatibility, valid grammatical IDs, supported patterns, exact form/attestation agreement, and usable primary combinations. Preserve legitimate defective/plural-only paradigms. Fail on malformed selected templates. Stage database, version, and registry changes together and define rollback/recovery for interrupted promotion. Test that invalid candidates leave the existing bundle and registry usable.

The Russian parser needs fixture coverage for curated→raw→empty fallback, duplicate/malformed IDs, malformed rows, and bad headers. Compare every imported selected meaning with the exact pinned TSV. Existing noun/verb tests only require more than zero Russian meanings. Current content is correct, but those tests would permit a near-total import failure.

### 5. Cover all actual application patterns

Pattern tests enumerate known C# enums and sample three DPD words per pattern. Some integrity tests filter unknown DPD patterns out of their comparisons. This does not prove every extracted pattern is supported by the app.

Assert every distinct raw pattern in the candidate database parses through the production pattern helpers. Exercise every selected primary lemma through real inflection code, including stem variants and irregulars. The July release introduced no new selected patterns, so this is a preventative rebuild-contract gap, not a newly observed July incompatibility.

## Production compatibility and expected cutoff churn

Rechecked against HEAD's committed `pali.db`, using all valid supported grammatical combinations and the production active-verb citation exclusion:

| Comparison: HEAD → pending database | Nouns | Verbs |
| --- | ---: | ---: |
| Old eligible combination IDs | 19,871 | 10,718 |
| Old IDs still eligible | 18,666 | 10,327 |
| Old IDs no longer eligible | 1,205 | 391 |
| Removed lemmas explaining all those losses | 101 | 38 |

No retained primary headword changed in that comparison. `pathavī` and `mahāpathavī` retain their headwords but have corrected stem/pattern data. The noun `atthi` is absent; two verb senses remain. The registry still retains the historical noun identity.

Accept cutoff additions/removals as an audited delta; do not freeze row counts or expand the cutoff just to make a comparison pass. Keep dormant mastery records. Existing statistics count mastery rows independently of current eligibility, while history reconstructs text from the current database and falls back to `?` ([`HistoryViewModel.cs:43`](PaliPractice/PaliPractice/Presentation/Practice/ViewModels/HistoryViewModel.cs#L43)). Decide whether statistics mean lifetime activity or currently available material, and label/filter accordingly. Historical display needs a stored snapshot or retained historical lookup data. These are release UX concerns distinct from the corpus-ID repair; they do not justify renumbering or deleting mastery.

## Verification and gate state

- SQLite `integrity_check`: **pass** on the current app database.
- Legacy `scripts/validate_db.py`: **exit 0**, despite invalid noun grammar.
- Current .NET suite: **2,846 passed; 0 failed/skipped/inconclusive**. This was a separate existing-suite run, not a successful full gate or platform build.
- `auto` gate against `90330a40e07e399a2d1750068affe0d87e2699e5`: **fail**. Quality self-tests, Ruff, data contract, and worktree preservation pass. Blocking findings: Python complexity (`get_training_nouns` 37 vs base 32; `validate_database` 48 vs 46), dirty nested DPD submodules, and a CA1502 bootstrap baseline anchored to an older commit. The baseline error prevents the .NET/desktop gate lane from running. Review/re-establish the baseline against the intended base; do not relax it simply to get a pass.
- The separate .NET run still emits NU1903 warnings for SQLitePCLRaw 2.1.2/2.1.11, its Android package, and Tmds.DBus.Protocol 0.21.2. These are package-audit warnings from that run, not a fresh complete vulnerability review.
- A fresh read-only reviewer independently confirmed the attestation failure and identified registry-initialization, unknown-pattern, candidate-promotion, and Russian-test gaps. No fixes were made or represented as verified.

Two handoff corrections: absolute-path test helpers have already been improved through `TestPaths` and the repository-root override. Also, the `GROUP BY` sampling criticism was too broad: SQLite's single built-in `MAX` aggregate selects bare columns from a maximizing row. Equal maxima and the ordering among tied groups still need explicit tie-breakers ([SQLite documentation](https://www.sqlite.org/lang_select.html#bareagg)). Sampling only three words remains insufficient for whole-database coverage.

## Useful practices from another app

Another app’s quality gate and generator determinism checks provide useful patterns:

1. Run a generator twice from the same pinned inputs in isolated temporary directories; compare semantic output and, when deterministic, bytes. Use a fixed build version for this check.
2. Make database-backed tests consume the exact verified candidate and verify the packaged artifact against its source/output identity.
3. Keep logs/manifests outside the worktree, preserve source files, explain expensive lane selection, and continue independent checks when another lane cannot run.

PaliPractice policy currently forbids running extraction in the gate. Keep that boundary until the generator supports a fully isolated, offline candidate build; do not copy the other app’s regeneration step into the current gate unchanged. No native-build matrix, coverage percentage, or unrelated analyzer policy needs to be copied to solve these data issues.

## Suggested order

1. Add exact failing regression tests for attestation and zero grammar; implement the internal attestation fix without changing SRS combination IDs.
2. Add historical registry/primary-paradigm comparisons and enforce candidate validation/promotion.
3. Add pinned input manifests, strict corpus input checks, deterministic ordering, and full candidate pattern/translation checks.
4. Resolve the existing gate rollout failures, then preview a candidate based on the verified July DPD release and pinned July Russian TSV. Inspect the frequency and semantic deltas before promotion.
5. Add isolated repeatability checks after the candidate interface exists. Address dormant-history/statistics behavior before shipping the affected update.

## Audit evidence

Temporary evidence directory: `/private/tmp/pali-audit-20260907/`. It contains the verified DPD release, pinned Russian TSV, read-only audit probes, complete collision and mixed-paradigm inventories, latest-selection previews, source metadata, and `test-results/audit-tests.trx`. These audit probes are not substitutes for repository regression tests.

Gate evidence: `/private/var/folders/cn/1_yc26_j25v239tcn5zw3zzr0000gn/T/pali-practice-quality-837444863b1d/runs/20260907T155923.891405Z-89292-4c5143/`.

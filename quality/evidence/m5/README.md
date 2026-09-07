# M5 completion evidence

Base: `a76a278a402155f54c1e875dea826131a3431e7a`. M5 verification is complete.

The selected release is DPD `v0.4.20260728`, source commit
`37912132f30f5023cb5a1c4e2dde26a24b7b3e2e`. Its archive matches the published GitHub
asset digest; the extracted database matches the pinned input hash. Durable local
inputs are under `.local/inputs/dpd-v0.4.20260728` and `.local/inputs/corpora-july`.
The latter reproduces CST/BJT/SC and the July BUDSIR Thai source from the release's
exact gitlinks. `sya` remains the Thai output key.

- [Pinned input manifest](input-manifest.json)
- [Corpus reproduction provenance](corpus-provenance.json)
- [Released v1.1 compatibility comparison](compatibility-report.json)
- [Initial two-build repeatability evidence](repeatability-probe.json)
- [NuGet advisory audit](package-audit.txt)

The initial repeatability probe produced identical output bytes and semantic
digest `6367c1bc86a1ac8fb069e3d1468dca6a636f69746676f28ab68a6e94a3ce4ad9`.
All six targeted primary-form tests passed against that exact candidate.
The complete gate passed: 2,919 .NET tests, 40 producer tests, 48 gate self-tests,
desktop build, repeatability, and semantic verification. Independent review and
its verification pass have no remaining blocking findings. See
[gate completion](gate-completion.json), [semantic evidence](semantic-verification.json),
and [native provisioning](native-provisioning.json). iOS runtime verification also passed.

Compared with released v1.1, 104 noun lemmas and 40 verb lemmas leave the cutoff.
Nouns lose 1,227 eligible combinations: 1,226 follow cutoff removals and one is
the repaired false `addhanesu` attestation. Verbs lose 402 combinations, all with
cutoff removals. Historical lemma and public combination identities remain
unchanged. The reports retain every changed sense/paradigm and content detail.

Lifetime practice totals, SRS distributions, and past activity retain dormant
mastery/history. Due counts use current queue eligibility, including user
filters. Mastery is not deleted when the dictionary or filters change.

Practice schema version 1 transactionally adds history snapshots and backfills
resolvable legacy rows from the released grammar resource. Unknown old rows stay
unresolved. New attempts save the displayed answer, lemma, and grammar alongside
mastery in one transaction. See the [reconstruction contract](../../../scripts/history_baseline/README.md).

Promotion uses a prepared journal plus durable staged files and backups. A
prepared transaction must be recovered before packaging: recovery restores the
whole prior file set, including originally absent files. Committed transactions
retain the new set. External edits or damaged backups stop recovery. The file
set is recoverable across interruptions; concurrent readers must not package a
prepared transaction. English-only promotion into this production repository is
blocked. No production promotion has run.

Native dependencies now use `sqlite-net-base` with the explicit SQLitePCLRaw 3.x
bundle, removing the old `bundle_green` and retired native library. The desktop
D-Bus dependency uses the patched 0.21.3 backport. NuGet reports no known
vulnerabilities in either project; the focused native-version test requires
SQLite >= 3.50.2. Android, desktop, and iOS loaded native SQLite 3.50.4. Android fresh provisioning,
legacy-history migration, and recovery from corrupt database and temporary-copy
files passed with the exact candidate hash. Desktop fresh provisioning and legacy
migration passed through the real DatabaseService in an isolated console probe.
The signed iOS simulator app opened successfully and passed fresh provisioning
and legacy-history migration. Its installed grammar database matches the exact
candidate hash; SQLite writer-version evidence is 3.50.4. The removed noun
`atthi` history was reconstructed with provenance and mastery unchanged.

The Android test uses a separate app identifier and ephemeral emulator. Native
recovery checks exercised Debug copy behavior. Desktop GUI capture was unavailable
because macOS was locked; the console probe establishes database provisioning,
not UI behavior. No production app data or bundled database was replaced.

Independent reviewer `m5_review` found two blocking issues in its initial pass:
promotion needed a full-gate semantic receipt, and a 500-row pre-filter limit could
hide eligible reviews behind dormant rows. Both were fixed with regression tests.
The complete auto gate was rerun and the same reviewer verified both fixes with
no remaining blockers. Current source/input/candidate identities still match the
[semantic receipt](semantic-verification.json).

The full gate completed in 223.66 seconds with 2,919 .NET tests, 40 producer tests,
and 48 gate self-tests passing; no .NET tests failed or skipped. External run:
`20260907T194743.543140Z-67165-e5c9f9`. The stable English manifest and semantic
digest are frozen for M6–M9. The original pending bundle, version, and lemma registry
remain outside the M5 commit. Translation implementation and release promotion
remain future milestones.

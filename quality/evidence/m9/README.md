# M9 — Multilingual database and model readiness

This checkpoint covers database generation, model behavior, recoverable promotion
and packaged data identities. It is not approval to publish the whole app.

## Data checkpoint

- Database version: `2026090702`.
- Database SHA-256: `7ce322fe8064fd7ef81ca36277acace90fc219a6de58a1c4f3f5e4b260b03213`.
- Frozen English database SHA-256: `f4850fe87522595ed82fdc3abf89ae9b5aebd22d7d8d63dc60c40c087863f75f`.
- Selected data: 2,395 noun senses / 1,500 primary lemmas; 1,327 verb senses / 750 primary lemmas.
- Source identities, configuration, code fingerprints, coverage and artifact
  hashes are in [bundle.json](bundle.json), also packaged as `Data/pali.manifest.json`.
- The local reproducible checkpoint is `.local/candidates/m9-verified-final`.
- Its frozen configuration input manifest is `.local/inputs/m9-frozen/english.json`.

| Meanings | Noun senses | Verb senses | Primary noun lemmas | Primary verb lemmas |
| --- | --- | --- | --- | --- |
| Russian | 2,384 / 2,395 | 1,320 / 1,327 | 1,498 / 1,500 | 750 / 750 |
| Spanish | 2,177 / 2,395 | 1,162 / 1,327 | 1,359 / 1,500 | 683 / 750 |

Missing or unverified translations use English. All accepted meanings are
compared with pinned sources; Spanish key matches also require matching English
sense/POS evidence. See [data-changes.json](data-changes.json) for every gap and
status count. No ambiguous sense mapping is silently accepted.

## Compatibility and user data

Against v1.1, 104 noun lemmas and 40 verb lemmas leave the fixed selections, with
an equal number entering. Existing pending lemma-ID mappings are all preserved.
Nouns retain 18,644 eligible combinations, add 1,263 and remove 1,227; 1,226 of
those removals accompany removed lemmas. The other removal is `103357120` for
`addha`, whose false `addhanesu` attestation is corrected. Verbs retain 10,316,
add 362 and remove 402; all removals accompany removed lemmas.

The two explicit noun source corrections remain `pathavī` and `mahāpathavī`.
`musati` retains its practice identity with a compatible primary sense. Complete
selection, eligibility and content changes are in [compatibility.json](compatibility.json).

Real `DatabaseService` tests cover direct-bundle access, copied-bundle fresh
provisioning, and replacement of an older copied database plus legacy user data.
They preserve dormant `atthi` noun mastery and recovered history (`atthiṃ`),
exclude dormant records from current due review, preserve settings across reopen,
and keep eligible IDs unchanged through ES/RU/EN changes. Exhaustive tests cover
all displayed senses and all primary forms against pinned sources.

## Promotion and recovery

Promotion verifies the multilingual receipt, unchanged consumer sources and
exact source/output identities before staging. The database, version, compact
manifest, lemma/practice registries, corrections and primary-form oracle form
one recoverable set. Every interrupted file boundary is tested, including absent
old files, idempotent recovery, external-edit refusal, stale evidence and support
for the previous six-file journal.

The original pending bundle and registries were archived with SHA-256 checksums
in `.local/backups/m9-before-promotion`. `.local/promotion` retains the current
journal and old/new file sets. The v1.1 baseline remains immutable.

A direct test run after initial promotion found that the exact-form oracle was
not yet promoted. Promotion now includes `scripts/generated/primary_forms.json`,
and ordinary tests resolve that file while candidate tests use the candidate's
copy. Producer fixtures now use the frozen historical practice registry, so
promotion cannot change their input identity accidentally.
The post-promotion gate also exposed mutable paths in the older input manifest.
The original configuration bytes were recovered from the checksum-verified
pre-promotion backup into a separate pinned snapshot. Promotion now rejects
input paths that overlap output targets, and a regression test proves rejection
before staging. Source hashes and generated database bytes remain unchanged.

## Verification

The final `auto` gate passed in 228.5 seconds: 2,936 .NET tests, 61 producer
tests and 48 quality tests, with no failures or skips. Its run is
`20260907T223939.450762Z-15312-79e4c9`; [completion.json](completion.json),
[bundle-verification.json](bundle-verification.json) and
[semantic-verification.json](semantic-verification.json) preserve the result and
source/output identities. The fresh read-only reviewer found no blockers in
its initial review or final verification of the handoff fixes.

Promotion completed with a committed seven-file journal. All seven promoted
outputs match the verified candidate. A separate ordinary full .NET run against
the promoted repository database rebuilt the test assembly and passed all 2,936
tests with no failures or skips. [promoted-tests.json](promoted-tests.json)
records that check. The original pending bundle remains checksum-verified in
the pre-promotion backup.

[Package identities](package-identities.json) record byte comparisons for the
actual desktop output, iOS simulator app bundle and Android APK. These are data
packaging checks; no whole-app UI or publication claim is made.

# M4 verification record

Status: complete. Exact candidate tests and complete gate passed; independent
review found no blockers. Base: `c37146116839987ea3414b302952fd6ccd254d42`.

Corpus and irregular records now use `(headword_id, form_id)` plus rendered text.
The app resolves attestation using the actual headword and rendered string;
repository eligibility uses the selected primary headword. Internal ending
indices cannot transfer attestation between paradigms. Public lemma IDs and
EndingId=0 combination encoding remain unchanged.

The three reported errors were reproduced through real repositories and
`InflectionService` before the repair: `addhanesu` locative plural,
`sīhanādebhi` ablative plural, and `sīhanādāna` genitive plural were falsely
attested. Instrumental `sīhanādebhi` was already false and remains covered. All
four cases pass against the repaired candidate.

The strict parser skips the recognized `in comps` row and rejects unknown,
contradictory, malformed, or empty selected templates. It rejects missing grammar,
ending gaps, and more than six noun or seven verb endings. Both structural
validators reject zero-valued grammar components. Identical internal records may
deduplicate; conflicting forms fail. Irregular forms come from the same pinned
DPD templates as other forms, eliminating separate HTML index ordering.

The exhaustive test found missing application support for `vī fem`. M4 adds its
DPD ending table, pattern value 251, and parent classification without changing
existing enum values. It also adopts the previously pending noun-ending sync
verified against the same DPD inputs. The old ambiguous highest-frequency sample
query now selects an exact row with an ID tie-breaker.

Every one of 90,619 primary forms matches the template artifact in text and ending
ID, matches pinned-corpus membership, and agrees with repository eligibility.
Every extracted raw pattern resolves through production helpers. This includes
regular, variant, irregular, and plural-only data. Parser fixtures cover valid
missing slots, unsupported endings, and malformed grammar. The older small HTML
samples are supplemental; they are not the completeness boundary.

Percentage-based HTML/corpus checks were replaced with exact stored-string
membership. Irregular corpus checks now require exact headword, grammar, and text
matches, with no 10%/5% allowance. Upstream HTML gray status is not the pinned
corpus contract and cannot override its word membership.

The May DPD candidate is under `.local/candidates/english-m4-final`.
[Input identities](input-manifest.json), [candidate identities](candidate-manifest.json),
and [compatibility changes](compatibility.json) are retained here. Compared with
v1.1, noun eligibility retains 18,675 combinations, adds 1,234, and removes 1,196.
Of those removals, 1,195 belong to cutoff removals; the remaining ID `103357120`
(`addha` locative plural) loses its false attestation. Verb eligibility retains
10,366, adds 331, and removes 352, all removals belonging to cutoff changes.
Mastery is not deleted or reassigned.

Thirty-one producer tests and 48 quality self-tests passed. The complete `auto`
gate passed all routed groups, 2,903 .NET tests with no failures/skips, and the
desktop build. The gate verified candidate/input/current-code identities before
and after testing. [Verification details](verification.json) retain the exact
external evidence path and counts.

Candidate consumption was added to the gate in M4 to prove the repaired data
without replacing the Russian-capable bundle. It does not generate or promote
outputs. M5 adds automatic isolated builds and upgrade/provisioning evidence.
Without an explicitly supplied candidate, the gate checks the pending bundle and
correctly rejects its known invalid data. No acceptance threshold was weakened.

Fresh reviewer `m4_review` inspected the changed producer/consumer paths, public
identity compatibility, exact tests, and gate wiring. It reported no blockers;
no code fix or second review pass was required. Legacy-bundle lookup retains its
legacy behavior until a new bundle is deliberately promoted. The bundled database,
version, and pending lemma registry remain outside this milestone commit.

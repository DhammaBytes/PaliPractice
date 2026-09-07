# M2 verification record

Status: complete. Isolated candidate and producer tests passed; complete gate passed; independent review found no blockers.
Base: `b2bec14aeb7a49072d59197b9ffdd00d105136b0`.

The acquisition command archives explicit DPD, corpus-text, and SuttaCentral
Git commits. It converts CST XML and BJT Sinhala data inside a new workspace,
then runs upstream Go frequency analysis and canonicalizes all four wordlists.
[Corpus provenance](corpus-provenance.json) records source revisions, recipe
hash, Python/package/Go versions, and output hashes. The generated data and
conversion logs remain in `.local/inputs/corpora-m2-v2`.

Acquisition required the DPD Python environment; the smaller app extraction
venv lacked BeautifulSoup. Go dependencies were downloaded separately from the
archived `go.mod`/`go.sum`, then frequency analysis ran offline with caches under
the acquisition directory. The completed CST/BJT conversions were retained
when retrying Go after sandbox cache denial and a missing output-directory
failure. Conversion file names/counts/nonempty contents were checked before the
successful retry. No source submodule was modified.

The regenerated CST, BJT, and SYA sets exactly match the preserved input sets.
SuttaCentral has two fewer word strings under the pinned source revision.
[Verification details](verification.json) record the set deltas and hashes proving
that all original M1 inputs, including both databases and registry, are unchanged.

The full candidate uses the current local May DPD database, identified by its
checksum. It is an isolation proof, not the final current-release selection
required by M5. [Input manifest](input-manifest.json) and
[candidate manifest](candidate-manifest.json) preserve the exact identities.
Candidate files are under `.local/candidates/english-m2-final`.

Thirteen focused producer/acquisition tests passed. They exercise the real
extractor and DPD model with small regular/irregular fixtures: offline repeated
byte equality, lexical frequency ties, registry/input preservation, existing
output refusal, interruption, candidate tampering, missing/corrupt corpus,
missing registry, duplicate/malformed manifest, nonempty WAL, version typing,
manifest replacement during execution, and working-directory independence.
A fresh process also forbids reading the bundled version file. Acquisition tests
verify committed bytes instead of pending/ignored files and incomplete conversion
failure. These producer tests are run explicitly, not from the gate in M2.

The candidate passes the shared structural database contract and output checksum
validation. That contract still has the documented M3–M5 gaps. Neither this
candidate nor the gate result proves exact grammatical attestation, historical
identity compatibility, or release upgrade behavior. No production promotion
interface is enabled before M5's semantic and recovery checks.

The pending bundle, bundled version, production registry additions, and static
noun endings remain uncommitted. M2 writes only candidate registry/version/data
outputs and cannot replace those production files.

Fresh reviewer `m2_review` inspected the M2 declarations, direct consumers,
source/input boundaries, deterministic output, registry writes, and tests. It
reported no blocking findings and did not rerun passing checks. No verification
pass or code fix was required. The complete gate result in `verification.json`
includes all routed groups. Its .NET data tests still consume the pending bundle;
M5 routes them to the exact semantically verified candidate.

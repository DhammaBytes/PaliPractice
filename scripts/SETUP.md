# DPD inputs and English candidates

The active sequence is in [DATA-REBUILD-ROADMAP.md](../DATA-REBUILD-ROADMAP.md).
M1 preserves the released baseline. M2 isolates English extraction. M3–M5 add
identity, grammatical, upgrade, and promotion guarantees. A structural candidate
pass is not a release approval. Russian and Spanish enrichment starts after M5.

## Inputs and acquisition

Extraction never downloads translations or updates the bundled database. Supply:

- An existing, checkpointed DPD SQLite database. Nonempty WAL files are rejected.
- All four nonempty corpus wordlists: CST, BJT, SYA, and SuttaCentral.
- The current production lemma registry, practice registry, reviewed paradigm corrections,
  and custom English adjustments.
- An explicit version, selection limits, and output directory.

The DPD database version and the DPD source checkout revision are separate
identities. Download/verify an upstream release separately, then pin the exact
local database bytes. `pin_inputs.py` records content identity; it does not claim
that a locally supplied database has been authenticated against a release asset.
M5 records the chosen release and its published download checksum.

Existing corpus JSON files do not establish source provenance. Reproduce them
from explicit Git commits in an isolated acquisition directory. The following
commits are the M1 local source baseline, not a claim about the latest DPD source:

```sh
dpd-db/.venv/bin/python scripts/acquire_corpora.py \
  --repository dpd-db --output .local/inputs/corpora-example \
  --dpd-revision 56fe6d1835d0f8efd1ce079d991bcfb74c423aad \
  --texts-revision 7901f7aa7aae0601f7274542778b64822451f074 \
  --sc-revision 51529c6774a8ca31dcf5b96f89f15f36b10052a2
```

Use the DPD Python environment for acquisition: CST conversion needs
BeautifulSoup/XML support and BJT transliteration needs the upstream dependencies.
The command archives tracked source bytes at each commit; ignored generated text
files are not copied. It runs upstream CST XML conversion, BJT transliteration,
and `go run ./go_modules/frequency/setup`, then writes sorted unique wordlists.
Go dependencies must already be cached: acquisition sets `GOPROXY=off` and
`GOSUMDB=off` and checks dependencies before conversion. Set
`PALIPRACTICE_GO_MODULE_CACHE` to an existing module cache populated separately
with `go mod download` from the pinned DPD `go.mod`/`go.sum`. Without that setting,
it uses `.go-mod-cache` inside the acquisition directory. Compilation uses a
local `.go-cache`; no source checkout cache is modified. It records revisions,
Python package versions, tool versions, recipe hash, and output hashes
in `provenance.json`. Logs and intermediate source copies stay under the new
acquisition directory. Failure never changes the source submodules.

## Pin and build

Run from the repository root, using the Python environment with the dependencies
in `scripts/requirements.txt` and the checked-out DPD model dependencies:

```sh
.venv/bin/python scripts/pin_inputs.py \
  --dpd dpd-db/dpd.db \
  --registry scripts/configs/lemma_registry.json \
  --adjustments scripts/configs/custom_translations.json \
  --practice-registry scripts/configs/practice_registry.json \
  --corrections scripts/configs/paradigm_corrections.json \
  --corpora .local/inputs/corpora-example \
  --output .local/inputs/english-example.json --version 2026090701

.venv/bin/python scripts/extract_nouns_and_verbs.py build \
  --manifest .local/inputs/english-example.json \
  --output .local/candidates/english-example

.venv/bin/python scripts/extract_nouns_and_verbs.py validate \
  .local/candidates/english-example
```

Use a new output directory for each attempt. Existing directories and output
files are not overwritten. Manifest input paths may be absolute or relative to
the manifest; command execution does not depend on the working directory.
Every input is checked against its SHA256 before extraction and again before
completion. The manifest also pins selection limits, exclusions, and maximum
lemma length. Missing inputs, duplicate JSON keys, invalid corpus content, and
configuration drift fail.

A completed candidate contains `pali.db`, `pali.version.txt`, proposed
`lemma_registry.json`, `practice_registry.json`, `paradigm_corrections.json`,
`compatibility.json`, deterministic `inflection_validation.log`, and
`candidate.json` with input, code, environment, and output identities. The
`BUILDING` marker remains after an interrupted or failed build; validation rejects
that directory. The generator uses a read-only SQLite connection and writes
proposed registry additions beside the candidate. It does not initialize a
missing production registry.

The schema retains Russian columns, empty in an English candidate, so current
application language support is preserved. The existing Russian importer is
outside this build path. The bundled Russian-capable database is not replaced.

## Selection and validation

The default limits remain 1,500 noun lemmas and 750 verb lemmas. Selection uses
maximum EBT frequency per cleaned lemma. Ties use cleaned lemma, then DPD ID for
sense order. Noun `atthi` remains excluded; its verb is eligible. Existing
pattern/POS, length, meaning/example, and plural-only deduplication rules remain.
Practice selection is explicit; see [the identity contract](PRACTICE-IDENTITY.md).

`validate` checks output hashes, SQLite/schema relationships, version, and
registry agreement through the same structural contract as the gate. It does
not yet certify corpus attestation. It requires every released lemma mapping,
validates retained practice paradigms, and requires exactly one selected sense. Do not copy
an English candidate into the app manually. M5 supplies the verified, recoverable
promotion interface after semantic and upgrade checks exist.

Run isolated producer tests explicitly during M2–M4:

```sh
.venv/bin/python -B -m unittest discover -s scripts/tests -v
```

The repository gate still does not invoke extraction or acquisition. M5 changes
that policy only after candidate isolation is proven. Run the normal gate via
the repository quality workflow before milestone handoff.

## Exact forms and attestation (M4)

The generator skips only recognized compound rows, rejects unknown or malformed
selected grammar, and supports at most six noun or seven verb ending variants.
An overflow fails; indices are never silently truncated. Corpus and irregular
records use `(headword_id, form_id)` plus rendered text. Conflicting duplicate
keys fail. Irregular forms now come from the same pinned templates as regular
forms, so HTML ordering cannot change their attestation indices.

`primary_forms.json` records every selected primary form from those templates.
The app test compares every rendered form and ending ID with this file, checks
exact pinned-corpus membership, and compares repository eligibility with rendered
attestation. All extracted raw pattern names must resolve through production
pattern helpers. The older small HTML pattern samples remain supplemental;
they no longer define the completeness boundary. Their sense query now chooses
an explicit highest-frequency row with a deterministic ID tie-breaker.

The standalone `validate_db.py` uses the same strict structural contract as the
gate and accepts explicit `--database`, `--version`, and `--registry` paths.
Candidate `validate` additionally checks the scoped records and primary forms.
`verify_candidate.py --candidate <directory> --inputs <manifest>` checks current
code and exact input identities too. The complete candidate gate is described in
[quality/README.md](../quality/README.md).

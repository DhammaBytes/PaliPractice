# DPD inputs and English candidates

The completed hardening milestones are recorded in [DATA-REBUILD-ROADMAP.md](../DATA-REBUILD-ROADMAP.md).
The current pipeline builds a pinned English candidate, verifies it, then adds
Russian and Spanish meanings without changing the English core. A structural
candidate pass alone is not approval to promote or release it.

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

### Sources for the current bundled-data tests

The ordinary quality gate and `dotnet test` use
[`quality/config/test-inputs.json`](../quality/config/test-inputs.json) to locate:

- DPD release `v0.4.20260728` at `.local/inputs/dpd-v0.4.20260728/dpd.db`.
- The four pinned wordlists under `.local/inputs/corpora-july/`.
- English adjustments at `scripts/configs/custom_translations.json`.

The bundled `PaliPractice/PaliPractice/Data/pali.manifest.json` supplies the
authoritative hashes, DPD release URL and archive digest, and corpus generation
recipe and revisions. Provision these inputs separately, then run the gate.
Tests hash the inputs before querying them and reject a different snapshot.
Updating the DPD Git checkout does not update or provision the release database.
The gate never downloads inputs or replaces the bundled database to resolve a
comparison mismatch. If source locations change, update the location map;
when promoting a new source snapshot, provision its inputs and update the map
alongside the normal bundle promotion.

### Acquiring corpus sources

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

Run from the repository root with Python 3.11 or later. Install the reviewed
extraction and test dependencies into a virtual environment:

```sh
python3 -m venv .venv
.venv/bin/python -m pip install -r scripts/requirements.txt
```

The DPD source checkout and its model dependencies are also required for
extraction. Corpus acquisition uses the separate upstream DPD environment
as described above; do not replace its dependency constraints with this app's
requirements.

Create a new configuration snapshot before pinning: production config files are
promotion targets and cannot also be immutable build inputs. Choose a database
version greater than the current `pali.version.txt`; the value below is an example.

```sh
mkdir .local/inputs/config-example
cp scripts/configs/lemma_registry.json scripts/configs/practice_registry.json \
  scripts/configs/paradigm_corrections.json scripts/configs/custom_translations.json \
  .local/inputs/config-example/

.venv/bin/python scripts/pin_inputs.py \
  --dpd .local/inputs/dpd-v0.4.20260728/dpd.db \
  --registry .local/inputs/config-example/lemma_registry.json \
  --adjustments .local/inputs/config-example/custom_translations.json \
  --practice-registry .local/inputs/config-example/practice_registry.json \
  --corrections .local/inputs/config-example/paradigm_corrections.json \
  --corpora .local/inputs/corpora-example \
  --output .local/inputs/english-example.json --version 2026090801

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
`compatibility.json`, `primary_forms.json`, `corpus_forms.json`,
deterministic `inflection_validation.log`, and
`candidate.json` with input, code, environment, and output identities. The
`BUILDING` marker remains after an interrupted or failed build; validation rejects
that directory. The generator uses a read-only SQLite connection and writes
proposed registry additions beside the candidate. It does not initialize a
missing production registry.

Russian and Spanish values are added to `localized_meanings` during enrichment; they are
not populated by English extraction or loaded as fields on each lemma.

## Selection and validation

The default limits remain 1,500 noun lemmas and 750 verb lemmas. Selection uses
maximum EBT frequency per cleaned lemma. Ties use cleaned lemma, then DPD ID for
sense order. Noun `atthi` remains excluded; its verb is eligible. Existing
pattern/POS, length, meaning/example, and plural-only deduplication rules remain.
Practice selection is explicit; see [the identity contract](PRACTICE-IDENTITY.md).

`validate` checks output hashes, SQLite/schema relationships, version, and
registry agreement through the same structural contract as the gate. It does not
replace the independent app reconstruction/corpus checks in the full gate. It
checks scoped corpus evidence, requires every released lemma mapping, validates
retained practice paradigms, and requires exactly one selected sense. Use the
verified promotion interface below; do not copy an English candidate into the app.

Run isolated producer tests:

```sh
.venv/bin/python -B -m unittest discover -s scripts/tests -v
```

The repository gate permits isolated candidate extraction from pinned inputs,
but does not acquire inputs or promote the production bundle. Run the
normal gate via the repository quality workflow before milestone handoff.

## Exact forms and attestation (M4)

The generator skips only recognized compound rows, rejects unknown or malformed
selected grammar, and supports at most six noun or seven verb ending variants.
An overflow fails; indices are never silently truncated. Corpus and irregular
build records use `(headword_id, form_id)` plus rendered text. Conflicting
duplicate keys fail. Compaction removes only the corpus spelling column after
validation and retains those spellings in `corpus_forms.json` as build evidence.
Irregular forms come from the same pinned templates as regular
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

## Russian enrichment (M6)

Acquire `db/backup_tsv/russian.tsv` separately at a full commit from the `sbs-ru`
branch of `sasanarakkha/dpd-db-sbs`. Record its SHA-256 and immutable URL in a
translation manifest. Enrichment itself is offline and never reads the moving
branch URL. The manifest has `schema: 1`, `english_sha256`, a `dpd` input entry
matching the English candidate’s pinned DPD, and `sources.ru`. Each input entry
uses the same `path`, `sha256`, `source.origin`, `source.revision` format as the
English manifest; relative paths resolve against the translation manifest.

```bash
.venv/bin/python scripts/enrich_candidate.py build \
  --english /absolute/verified-english-candidate \
  --manifest /absolute/translation-inputs.json \
  --output /absolute/new-unique-translated-candidate

.venv/bin/python scripts/enrich_candidate.py verify \
  --english /absolute/verified-english-candidate \
  --manifest /absolute/translation-inputs.json \
  --output /absolute/new-unique-translated-candidate
```

Output contains a new `pali.db` with `localized_meanings(headword_id, language,
meaning)`, `enrichment.json`, and a compact `bundle.json`. Original English tables and artifacts are
unchanged. The report carries source pins, every selected sense’s mapping and
availability status, primary/sense coverage, and unselected/unknown source IDs.
Empty and missing translations have no table row; runtime English fallback is
implemented by the app’s selected-language meaning loader. These outputs require a successful full-gate receipt before promotion.

Supply `PALIPRACTICE_TRANSLATION_MANIFEST` alongside
`PALIPRACTICE_INPUT_MANIFEST` to run two isolated, byte-identical enrichments in
the gate. Verification re-parses the pinned source, compares every stored meaning,
and compares all original SQLite schema objects, rows, version, and artifacts.
A failed build leaves only its incomplete candidate directory; never reuse it.

## Spanish sense mapping (M7)

Add `sources.es` (`js/dpd_ebts_es.js`) and `sources.es_english`
(`js/dpd_ebts.js`) to the translation manifest, both at the same pinned commit.
The same offline build/verify commands support RU, ES, or both together. The
parser accepts the observed `let variable = {JSON};` format only and does not
execute JavaScript. Duplicate keys, malformed JSON, and invalid value types fail
acquisition validation; individual unsupported definitions remain explicit gaps.

A full DPD headword key is accepted only when the paired English export’s POS and
meaning equal the pinned DPD `pos` and `meaning_1` (or `meaning_2` when the former
is empty). Comparison normalizes Unicode, entities, and whitespace, but does not
reorder or discard meanings. The Spanish POS must match too. Parsing handles bold
and unbold definitions and removes only recognized literal/etymology scaffolding.

The report classifies meaning drift, missing keys/translations, possible renamed
or renumbered keys, ambiguous DPD keys, unsupported definitions, and POS mismatch.
Suggested keys are diagnostics, never automatic joins. Unresolved mappings stay
absent from `localized_meanings` for English fallback. No mapping overrides were
needed for the accepted M7 subset; any future override requires explicit review
and pins for source bytes, full keys, target ID, and paired English evidence.
Mechanical correspondence does not establish translation quality: retain the
bounded terminology/sample review and upstream AI-assisted translation credits.

## Multilingual database readiness and promotion (M9)

Run the complete gate with both pinned input manifests. It creates two English
and two enriched candidates, runs real repository and provisioning/model tests
against the multilingual database, then issues `bundle-verification.json`.
Keep the printed external gate directory until promotion finishes; it contains
the English checkpoint and both semantic receipts.
Pinned configuration inputs must live in snapshot files outside promotion
targets. Promotion rejects overlapping input/output paths; otherwise replacing
a registry would invalidate the inputs needed to reproduce the checkpoint.

```bash
.venv/bin/python scripts/promote_candidate.py promote \
  --repository /absolute/path/to/PaliPractice \
  --candidate /absolute/gate-run/translation-repeatability/run-1 \
  --english /absolute/gate-run/english-repeatability/run-1 \
  --inputs /absolute/english-inputs.json \
  --translations /absolute/translation-inputs.json \
  --evidence /absolute/gate-run/bundle-verification.json
```

Promotion validates exact input and output identities before staging, requires
EN/RU/ES layers, then journals the database, version, manifest and identity files
as one recoverable set. `bundle.json` becomes `Data/pali.manifest.json`, carrying
English source/configuration/code identities and translation source pins and
coverage. Detailed sense mapping stays in the retained candidate's
`enrichment.json` whose hash is recorded in the packaged manifest.
The exact primary-form oracle and full corpus spelling evidence are promoted to
`scripts/generated/primary_forms.json` and `scripts/generated/corpus_forms.json`.
Ordinary tests use them to verify the checked-in database. Neither is an app asset.

If interrupted, run `promote_candidate.py recover --repository <repository>`.
It restores the whole prior set and refuses to overwrite unrelated external
edits. Preserve `.local/promotion` until the checkpoint is accepted; its old-file
copies retain the prior bundle. The gate never promotes or publishes an app.
This checkpoint establishes data/model readiness only.

### Compact corpus storage

Candidate generation validates full rendered records before projecting corpus
attestation into `(headword_id, form_id)` keys. Both corpus and irregular tables
use `WITHOUT ROWID`; irregular tables keep their `form` strings. Exact projection
checks preserve all other table contents. `corpus_forms.json` retains every
attested spelling as hashed build evidence, alongside `primary_forms.json`.
Neither evidence file is an app asset. Promotion places them in `scripts/generated`
for ordinary repository tests. Compact keys must exactly match the evidence;
all primary reconstructions and corpus membership are checked by the .NET gate.

Bump the pinned input manifest's database version for a changed shipped bundle.
The existing copied-database version check installs the new compact bundle on
upgrade. Keep the matching app reader, version and output manifest together.

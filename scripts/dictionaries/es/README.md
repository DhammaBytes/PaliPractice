# Spanish dictionary: historical identities and reviewed adaptations

This directory supplies the Spanish mappings used with PaliPractice's pinned
DPD `v0.4.20260728`. It recovers translations lost through sense renumbering and
English edits, and records local adaptations for later splits and new senses.
The comparison is English to Spanish; reviewers do not infer meanings from Pali.

## Why the Spanish source needs a historical reference

The app uses the July 28, 2026 DPD database. The Spanish repository is pinned at
`8a634e53d0605bc50f0a9913b221ebc6a40f17b2` (July 18, 2026), but its English and
Spanish definition files have not changed since their March 14 initial commit.
All 72,698 English export keys match unique IDs, parts of speech, and definitions
in DPD snapshot `2f74b9d40bf1f25f9c11fb0446d60e110eeb6d00` (November 7, 2025).
This establishes an identity reference, not the exact day the export was made.

Repository commit dates must not be treated as dictionary data dates. The
Russian source at `c179f0c044e3e0ca366ac47445f30a1b2ffc732c` records a July 8, 2026
upstream sync and already carries numeric IDs; its input is unchanged here.
The Spanish and Russian repositories may catch up later. This directory is a
versioned local dictionary, not a claim that our adaptations are upstream work.

## Files and ownership

- `identity-inputs.json` pins the two historical TSV parts and paired EN/ES
  exports. Paths are relative to this directory; acquired files remain in
  `.local/inputs`, outside the shipped app and outside Git.
- `identity.json` is generated. It contains the complete 72,698-key historical
  ID dictionary and source checksums. Never hand-edit its IDs.
- `reviews.json` contains 347 explicit decisions for current DPD senses. Each
  identifies its current ID, key, POS and English meaning, source Spanish keys,
  final Spanish text, rationale and reviewers. This file is the editable source
  of truth for manual mappings.

The historical ID dictionary recovers 36 unchanged senses automatically, in
addition to the 3,339 previously imported senses. The reviewed entries supply
158 verbatim reuses, 188 adaptations and one new local translation. Together
these cover the selected 2,395 noun senses and 1,327 verb senses. Coverage is a
mechanical measure; it does not certify the quality of every inherited source
translation.

Three agents reviewed disjoint batches of 116, 116 and 115 English–Spanish
pairs. Different agents independently checked each complete batch. The second
pass corrected 27 entries, including the five initially unresolved senses;
a further check refined rehabilitation/demotion and the noon-to-dawn interval.
The remaining `pareti 3` received an explicit local English-to-Spanish
translation, with no claim of upstream authorship. Review was AI-assisted and
bounded to English–Spanish correspondence, not a Pali scholarly review.

## How mapping works

1. Verify the manifest's checksums before reading any source.
2. Match each source Spanish key to its historical DPD ID using `identity.json`.
3. Follow that numeric ID into the current DPD database. If POS and full
   normalized English meaning are unchanged, use the author's Spanish text.
4. Otherwise apply a pinned, explicit entry from `reviews.json`, or report the
   gap. Never automatically join synonyms or another similarly numbered key.

Review modes are deliberately distinct:

| Mode | Meaning |
|---|---|
| `verbatim` | Exactly the parsed authored Spanish definition from one source key. |
| `adapted` | Selected, corrected or composed Spanish based on listed sources and current English; additions are local authorship. |
| `translated` | New local translation of current English. Source keys must be empty. |
| `unresolved` | No accepted translation; an empty value and rationale remain visible in evidence. |

Historical IDs are not enough when the meaning changed. For example, old
`vadeti 1` combined speaking and describing; the new senses get separate
`dice; habla` and `llama; describe (como)` meanings. Conversely, old `gocara 2`
is the ID now called `gocara 3`: matching the current key directly would select
a different sense. Cross-headword English synonyms may supply vocabulary for
an explicitly reviewed adaptation, but are never asserted to be the same ID.

The identity dictionary is bound to the EN/ES source hashes. Reviews are bound
to both its hash and the exact current DPD hash. Every review's target key, POS
and English are checked again. Duplicate IDs, unknown source keys, falsely
labelled verbatim text, and stale review targets fail the build. A translation
manifest must supply `es_identity` and `es_reviews` together. Omitting both
selects the original exact-key importer for unadapted source evaluation.

## Reproduce the historical dictionary

Acquire the two TSV parts from the commit URLs in `identity-inputs.json` into
`.local/inputs/spanish-identity/`. Acquire the paired exports at their pinned
commit into `.local/inputs/spanish-m7/`. Acquisitions must preserve exact bytes.
The older TSV parts are chunks of a single table, including one shared header;
they are concatenated in manifest order.

From the repository root:

```sh
python3 scripts/build_spanish_identity.py \
  --manifest scripts/dictionaries/es/identity-inputs.json \
  --output /tmp/new-spanish-identity.json
cmp scripts/dictionaries/es/identity.json /tmp/new-spanish-identity.json
python3 -m unittest discover -s scripts/tests -p 'test_spanish*.py'
```

The builder verifies all input pins and every source English key/POS/meaning
against the historical TSV, requires unique IDs/keys, and refuses to overwrite
an output. Spanish POS/markup issues are handled per selected entry during
import, so an unrelated source typo cannot invalidate the whole ID dictionary.

Add `es_identity` and `es_reviews` entries to the translation manifest's
`sources`, each with `path`, `sha256` and `source` fields, as with the other
inputs. Use their file hashes as immutable `sha256:<digest>` revisions. The
bundle's `identity_source` and `review_source` retain these provenance pins.
Build and verify an isolated multilingual candidate using the normal commands
in `scripts/SETUP.md`. Run the complete gate against that exact candidate and
promote with the existing promotion tool. Increase the bundled database version
so installed copies are replaced. Do not patch the packaged SQLite file by hand.

## Future Spanish or Russian resync

The [mandatory shipped-dictionary comparison](../README.md) applies to both
languages and is enforced at promotion. It also covers the separate ES/RU
terminology preferences in `scripts/configs/localized_translations.json`, which
run after these Spanish mappings. Review those preferences during resync too.

1. Acquire new sources into a new directory and record repository revision,
   last data-file update, and actual DPD baseline separately. Keep today's
   source files and dictionary available for comparison.
2. Try exact current-DPD correspondence without the old Spanish dictionary.
   For remaining renamed keys, identify a historical DPD snapshot matching the
   new English export; regenerate a new identity dictionary from pinned data.
3. Compare all old reviewed IDs with the newly mapped Spanish values. Prefer
   correct updated upstream definitions and retire redundant local entries.
   Re-review adaptations, split senses, new local translations, and any still
   unresolved cases. Do not merely change the review hashes to bypass staleness.
4. Compare both availability and meaning freshness. Russian's stable IDs avoid
   numbering failures, but an older translated definition can still be stale.
5. Update `reviews.json`, its target checks and source pins explicitly. Report
   added, changed, retired and still-local mappings. Rebuild, verify, run the
   gate, and promote a new database version.

When these dictionary inputs are supplied, a source update invalidates their
pins until that review is done. Omitting them selects exact-key evaluation;
promotion separately requires a decision for their removal and any new losses.
This is intentional: dated manual decisions must not silently override newer
upstream work. Updating DPD itself is not required to update ES/RU translations.

## Sources and license

- [DPD July release](https://github.com/digitalpalidictionary/dpd-db/releases/tag/v0.4.20260728)
- [Historical DPD](https://github.com/digitalpalidictionary/dpd-db/tree/2f74b9d40bf1f25f9c11fb0446d60e110eeb6d00/db/backup_tsv)
- [Spanish source](https://github.com/DhammaBytes/dpd-dictionary-es/tree/8a634e53d0605bc50f0a9913b221ebc6a40f17b2)
- [Spanish initial data commit](https://github.com/DhammaBytes/dpd-dictionary-es/commit/9e204150e901836c8e009240387a53fc13fd6a48)
- [Russian sync record](https://github.com/sasanarakkha/dpd-db-sbs/blob/c179f0c044e3e0ca366ac47445f30a1b2ffc732c/kamma/upstream_sync/accepted_sync.json)

Dictionary data and derived adaptations retain **CC BY-NC-SA 4.0** licensing.
Attribution: Digital Pāḷi Dictionary; Spanish translation coordinated by
Paññābhūmi/DhammaBytes; Russian derivation by SBS/sasanarakkha. Local reviewed
adaptations are by PaliPractice with AI assistance, dated September 12, 2026.

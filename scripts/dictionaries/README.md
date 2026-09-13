# Maintained dictionary mappings and update review

Dictionary meanings and UI resource translations have separate workflows.
[SETUP](../SETUP.md) owns acquisition, candidate builds, the gate and promotion.
[Spanish dictionary](es/README.md) owns the historical ID bridge and reviewed
English–Spanish mappings. UI review records live in `Specs/Localization`.

The September 12 Spanish recovery supplied all 2,395 selected noun senses and
1,327 verb senses. That is a dated checkpoint, not a fixed future selection size.
The packaged `pali.manifest.json` records the currently shipped source hashes and
coverage; historical M6–M9 reports retain their original measurements.

## Required comparison before promotion

`promote_candidate.py` compares the candidate against the actual shipped database
and its matching manifest, under the promotion lock, before staging replacements.
The ordinary full-gate receipt remains required. A passing gate alone does not
approve translation regressions. There is no flag to skip the comparison.

Generate the reviewable report against the exact gate candidate:

```sh
python3 scripts/compare_translations.py \
  --repository /absolute/path/to/PaliPractice \
  --candidate /absolute/gate-run/translation-repeatability/run-1 \
  --output /absolute/new-translation-comparison.json
```

The report lists added/changed translations and removed selections. It requires
an explicit decision for each of these findings:

- Previously translated selected IDs losing an ES or RU meaning.
- Newly selected senses with missing ES or RU meanings.
- Changed packaged English targets with an old or new translation.
- Removed language layers, Spanish identity/review inputs, or terminology inputs.
- A changed DPD baseline, for each language, including Russian.
- Changed translation sources, including review of retained terminology overrides.

Existing untranslated senses do not become new failures on each rebuild.
Removing a sense from practice selection is reported separately from losing its
translation while it remains selected. Additions cannot offset losses.

For findings requiring review, create a version-controlled JSON file under
`scripts/dictionaries/resync/` (create this directory when the first review is
needed). Use the report's `comparison_sha256` and one entry for each required key:

```json
{
  "schema": 1,
  "comparison_sha256": "<exact digest from the report>",
  "decisions": [
    {
      "key": "translation_lost:es:12345",
      "reason": "Describe the inspected change and why accepting this gap is appropriate.",
      "reviewer": "Name or review reference"
    }
  ]
}
```

Prefer fixing accidental losses and rebuilding over accepting them. For a
legitimate source update, review changed definitions and retire obsolete local
mappings. Do not change pins solely to suppress errors. Pass the completed file
to promotion with `--translation-decisions /absolute/review.json`. If no findings
require review, omit that argument. Empty reasons, missing/duplicate/unknown keys,
and decisions for different old/new bundles are rejected. Promotion recomputes
the report; a changed shipped baseline invalidates earlier decisions too.

Promotion retains the report and decisions in
`.local/promotion/translation-reviews/<comparison-sha256>.json` before staging.
Keep authored decisions in Git as well; local evidence is not durable project
memory and a recorded review does not itself prove promotion succeeded.

## Meaning freshness and local terminology

`../configs/localized_translations.json` supplies language-specific preferred
terms and guarded full replacements after source mapping. It is separate from the historical Spanish review
file and applies to RU too. Pin it as `sources.overrides`; see SETUP for its
schema. The comparator detects removal of that input and asks for a review when
upstream changes while overrides remain present.

Russian stable IDs establish identity, not meaning freshness. The comparator
checks packaged English for each sense and requires a language-level review on
any DPD baseline change. Packaged English can contain local preferences and is
not a complete copy of the raw DPD definition. The baseline review must therefore
compare the pinned old/new DPD meanings and the translation source's actual DPD
baseline, including changes hidden by display preferences. It is an explicit
review obligation, not automatic proof of semantic equivalence.

Checks enforce completeness and provenance of review records. They cannot judge
whether a reviewer supplied a sound linguistic rationale. English–Spanish agent
review also does not establish independent Pali accuracy.

# M7 completion evidence

Base: `816e7a5` (M6). Paired ES/EN source: DhammaBytes/dpd-dictionary-es commit
`8a634e53d0605bc50f0a9913b221ebc6a40f17b2` (18 July 2026), rechecked and acquired
at the full revision on 7 September 2026. See [pinned inputs](input-manifest.json).
Both maps have 72,698 keys. They are parsed as a known assignment containing JSON;
no upstream JavaScript is executed.

[Mapping report](mapping-report.json): 2,177/2,395 noun senses and 1,162/1,327 verb
senses pass exact paired-English/POS correspondence. Primary coverage is
1,359/1,500 noun lemmas and 683/750 verb lemmas. The 258 meaning-drift cases,
84 missing keys, and 41 possible renamed/renumbered keys remain untranslated.
Suggested replacements are never joined automatically. There are no overrides.
Every accepted stored value is re-derived from pinned source definitions.

Both enrichment runs are byte-identical, and original English schema, rows,
version, and sidecar artifacts are unchanged. Russian source-derived values are
unchanged when Spanish is added. [Initial repeatability](repeatability-probe.json).

## Bounded terminology review

The [sample](terminology-sample.json) covers common doctrinal vocabulary,
polysemous nouns, common verbs, numbered senses, Pāli diacritics, and every
accepted selected unbold definition. The implementer compared English/Spanish
meaning text and extracted boundaries. No blocking sense mismatch or leftover
POS/literal/etymology markup was found in this sample. Source distinctions are
preserved: `dukkha` includes a bodily-pain sense, `nibbāna` distinguishes fire from
mental defilement, and `kamma` includes work and Vinaya legal-action senses.

This is a bounded review, not expert certification of all Spanish definitions.
The source uses `conciencia` for `viññāṇa` although its README describes a
`consciencia` convention; preserve the source wording and record the inconsistency
rather than claiming uniform terminology or silently editing it. Upstream states
AI-assisted translation/review. About attribution in M8 credits coordination to
the Paññābhūmi team, Jalisco, Mexico, with repository and monastery links.

The complete auto gate passed in 223.7 seconds: 2,919 .NET tests, 54 producer
tests, and 48 quality self-tests. No .NET tests failed or skipped.
[Gate/review verification](verification.json), [completion](gate-completion.json),
and [final repeatability](repeatability.json) preserve the result.

Fresh reviewer `m7_review` reported no blocking findings and independently checked
all 50 sample records against paired exports and DPD identities. No code changes
or second review pass were required. No production
bundle is promoted during M7. The app will consume the language-keyed table in M8.

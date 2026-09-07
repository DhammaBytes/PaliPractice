# M6 completion evidence

Base: `fed12dc748f0918a5c609acb478923e631c8b020`.

Russian source: `sbs-ru` commit `c179f0c044e3e0ca366ac47445f30a1b2ffc732c`
(11 July 2026), fetched at that immutable revision on 7 September 2026.
The [input manifest](input-manifest.json) pins source bytes and the full DPD
identity reference. The [coverage report](coverage.json) distinguishes absent
rows from valid empty meanings; neither is an import error. Unknown source IDs
29083 and 62523 are not in the selected English candidate and are not imported.

Two isolated builds produced byte-identical outputs. Every stored translation
is compared with curated/raw precedence from the pinned TSV, and every original
English schema object, table row, database version, and sidecar artifact is
compared before/after enrichment. The English database remains SHA-256
`f4850fe87522595ed82fdc3abf89ae9b5aebd22d7d8d63dc60c40c087863f75f`.

Current coverage: 2,384/2,395 noun senses, 1,320/1,327 verb senses;
1,498/1,500 primary noun lemmas and 750/750 primary verb lemmas.
These denominators differ from the previous pending rebuild because the English
checkpoint changed. Do not compare raw percentages as if they were the same
selected headword set.

The six added fixture tests cover malformed/corrupt sources, exact source values,
repeatability, missing and stale pins, interrupted writes, and deliberately
modified translations/English content. App consumption of the new language-keyed
table belongs to M8. No production bundle, version, or registry is promoted here.
The full auto gate passed in 219.69 seconds: 2,919 .NET tests, 46 producer
tests, and 48 quality self-tests; no .NET failures or skips. See
[verification](verification.json), [gate completion](gate-completion.json), and
[repeatability](repeatability.json). Fresh reviewer `m6_review` reported no
blocking findings; no code changes or verification review were required.

The reviewer requested a same-ID coverage delta before closing the milestone.
[Coverage delta](coverage-delta.json) compares with the previous pending bundle,
explicitly not a released artifact: 2,373 shared noun senses retain 2,371
translations; 1,311 shared verb senses retain 1,310 translations. No shared
meaning string changed. Newly selected/removed English senses explain the
different overall totals.

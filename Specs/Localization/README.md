# UI localization roadmap

Localize Pāli Practice into Russian and Spanish with verified grammatical
terminology, a small pilot, and independent review in each language. This is a
planning and research deliverable; it does not approve or ship translations.

Baseline: `e27d39be9cfb4b02a56afd4522fbaa7edfc87532`, inspected 2026-09-07.
The worktree was clean at the start of this task.

## Scope and current state

The source is
[`Strings/en/Resources.resw`](../../PaliPractice/PaliPractice/Strings/en/Resources.resw).
It contains **234 keys**, including **92 grammar keys**. The other groups are
Common (11), Start (4), Settings (54), History (5), About (13), Help (13),
Statistics (21), Practice (12), and Feedback (9). Help includes two short
instruction sections and seven FAQ entries. About includes attribution, links,
a quotation, and license information. These are part of UI localization too.

- Russian already has 234 matching keys, but its text was written without a
  localization spec and has no preservation priority. The owner explicitly
  authorizes replacing all Russian UI text from scratch if useful. Existing
  wording is optional reference material, not a required translation baseline.
- Spanish has no UI resource file yet. Its dictionary meanings are a separate,
  already implemented feature.
- `AppText`, `AppTextFormatter`, and `GrammarText` already centralize resource
  access, formatting, and grammar labels. Extend these only where necessary.
- `ResourceCompletenessTests` currently compares English and Russian key sets.
  It does not cover Spanish, duplicate keys, placeholders, or linguistic quality.
- `Settings.Row.Language` means **Translation language**. The corresponding
  setting changes dictionary meanings; it does not select the UI language.
- Resource loading uses `ResourceLoader`; the project declares English as the
  default language. Runtime fallback and platform language selection still need
  verification with the completed resources.

Include labels, hints, short/table variants, dialogs, feedback text, statistics,
dates/counts, Help, About, and exposed accessibility text. Preserve Pāli words,
diacritics, examples, form identities, and practice behavior. Database extraction,
dictionary retranslation, new grammar guides, and store marketing are outside
this roadmap.

## Small document set and locale isolation

This file owns scope, shared Pāli semantics, implementation steps, and acceptance
criteria. [ru.md](ru.md) and [es.md](es.md) each own one locale's terminology,
evidence, style decisions, and unresolved questions. Keep subsequent review
findings and acceptance status in the relevant locale file. Do not add a
generated dashboard, lease system, translation database, or per-string workflow.

Use fresh sessions with **no inherited conversation** for each locale translator
and linguistic reviewer. Each receives this shared brief, the English resources
and relevant code, its assigned locale memo, and target-language sources. It may
read its own existing resource file. It must not read sibling-locale resources,
notes, research, or review results. A coordinator may compare issue identifiers
and implementation needs, but must not write target-language prose using both
locale contexts. The initial RU and ES research agents follow this separation.

The owner-authored English resources are the source of truth for product meaning
and pedagogical scope. Research establishes appropriate target-language terms
and phrasing; it is not a prerequisite for retaining the author's claims.
Preserve explanations, causal relationships, frequency qualifiers, and deliberate
simplifications. Do not remove, weaken, expand, or require revalidation of English
claims as part of localization. If a translation ambiguity cannot be resolved,
ask the owner about that specific ambiguity. Changes to English require an
explicit owner request and are not a localization milestone.

## Shared grammar reference

Use Charles Duroiselle's
[A Practical Grammar of the Pāli Language, Learn Pali Language edition](https://palistudies.blogspot.com/p/a-practical-grammar-of-pali-language.html)
as the preferred shared reference for general Pāli grammar understanding,
as selected by the product owner. Both locale agents should consult its relevant
sections before supplementary summaries. Useful entry points are chapter V,
§116 (nominal categories); chapter X, §§354–369 (verb introduction) and
§§381–403 (present system); and chapter XIV, §§594–602 (case syntax).

The linked edition retains paragraph numbering and distinguishes editorial
annotations. Cite paragraph numbers when recording a finding. This reference
supports understanding of the authored UI; the English resources remain the
content authority, and locale-specific sources establish conventional Russian
or Spanish terminology. Adding this working reference does not change the app's
FAQ links. Page and contents inspected 2026-09-07.

## Evidence policy

For target-language terminology, prefer attributable Pāli teaching material in
that language. If it is unavailable or inconclusive, use academic Sanskrit
teaching material for the term, with an explicit limitation: Sanskrit does not
establish how Pāli forms behave. General linguistic dictionaries and style guides
can establish spelling or grammatical terminology, not Pāli usage. English Pāli
references can help translators understand the intended distinction, not dictate
natural local wording or override the authored explanation.

For each disputed term, record the exact resource family, preferred wording,
source URL with section/page, what the source supports, and what remains
unverified. Distinguish **supported**, **proposed**, and **unresolved** decisions;
an existing translation or an agent's fluent output is not evidence. Catalog
entries and search snippets are leads, not proof of a book's terminology.
Use two independent sources where practical for high-risk disputes. When that
is impossible, keep the limitation visible and request a qualified review of
that specific distinction. Do not infer that Spanish Pāli resources do not exist.

## Shared meaning and translation boundaries

Translate the following families carefully while preserving the English meaning.
No English rewrite or grammar audit is required before translation. Retain enum
values, database identifiers, and resource keys unless a separate implementation
need requires a change.

| Resource family | Translation requirement |
|---|---|
| `Settings.Section.Tenses`; `Grammar.Tense.*` | Preserve the selector's scope: present/future and imperative/optative. Choose conventional local terms for those categories; do not require renaming the English heading or enum. |
| `Grammar.Voice.Reflexive.*`; `Settings.Row.VoiceHint`; `Settings.VoiceOption.*` | Preserve the intended parassapada/attanopada distinction. Select the established local term for that referent rather than assume a literal English cognate has the same use. Retain the author's statement that attanopada mostly appears in poetic verses, including its frequency qualifier. |
| `Settings.CitationFormWarning.Content` | Preserve the explanation that the app uses present third-person singular as its citation form because Pāli does not commonly use infinitives, followed by the stated settings adjustment. Do not strengthen “does not commonly use” into absence or impossibility. The existence of infinitives does not contradict this frequency statement. |
| `Grammar.Case.*.PracticeHint` / `.SettingsHint` | Questions and prepositions are memory aids for the selected Pāli functions, not definitions or one-to-one mappings to target-language cases. Preserve the extra scope of settings hints where present. |
| `Grammar.Tense.Optative.SettingsHint` | Explain the intended functions directly; English modal words must not become literal local equivalents or an equation with a target-language mood. |
| `Settings.Tenses.AoristFooter`; `Help.Faq.MissingForms` | Preserve the authored past-narrative gloss, explanation of form variation and learning needs, and current product limitation. Avoid substituting one modern target-language past tense for aorist, or turning a possible future feature into a promise. |
| `Grammar.Type.*`; table headings | A pattern, its stem, a root, and a citation form are different concepts. “Non-standard” describes a supported variant here; it must not imply an incorrect Pāli form. |
| `Help.Faq.MultipleForms`; `Grammar.Table.NonCorpusHint` | Distinguish attestation in the app's reference corpus from grammatical possibility. A gray or omitted form is not thereby ungrammatical. |

Preserve short factual prose as authored: word counts and corpus scope,
repetition intervals, backup behavior, attribution, and excluded forms.
Review translations for fidelity, not for opportunities to rewrite the source.

## Delivery sequence

| Step | Owner | Deliverable and exit condition |
|---|---|---|
| 1. Record the source inventory | Coordinator | Record the English source revision and confirmed language policy below; inventory remaining visible literals and accessibility labels. |
| 2. Reconcile each terminology memo | Separate RU and ES agents | Source-linked decisions for all high-risk families; one full/short/table convention per concept; unresolved terms explicitly identified. RU may translate from scratch without auditing or reusing existing wording. |
| 3. Translate and review a pilot | One translator and a fresh reviewer per locale | Review the pilot below in screenshots. Resolve semantic and layout findings before bulk translation. |
| 4. Complete UI resources | Same locale owners, disjoint paths | RU revises or fully replaces the text in `Strings/ru/Resources.resw`; ES creates `Strings/es/Resources.resw`. All source keys and authored meanings represented, with placeholders and markup intact. |
| 5. Integrate and verify | Coordinator / implementation owner | Resource tests, language resolution, formatting, and rendered UI checks pass. Add only the code needed by observed failures. |
| 6. Accept each locale | Fresh locale reviewer, then product owner for remaining decisions | Review every value in context, fix blocking findings, and verify the fixes. Record reviewer, source revision, remaining limitations, and platform evidence in that locale memo. |

RU and ES can progress independently from the authored English source. A
blocked term need not stop unrelated buttons or navigation text. A locale cannot
be marked ready while an unresolved term can teach the wrong distinction.

The pilot is a small set of **complete UI contexts**, not a random sample:

- One noun card plus declension table and case settings: all eight cases,
  including separate practice/settings hints, genders and both numbers.
- Conjugation settings and one card with the optional voice badge: four
  tense/mood categories, three persons, both numbers, active/middle labels,
  optative hint, and the citation-form warning.
- Grammar-help and missing-forms FAQ entries, including link text and markup.
- A streak count and history date, one narrow statistics tooltip, and a table
  heading that includes a Pāli pattern example.

Use deliberately difficult labels and combinations. Compare full and compact
variants at actual rendered widths; a character limit is not a fit test.

## Language policy and implementation work

**Confirmed by the product owner:** UI follows the system-wide language selection
with English fallback; users can override the dictionary translation language
through its separate existing setting. This lets users read UI and definitions in
different languages. Use broad `ru` and `es` resource sets initially. Spanish
wording should be broadly intelligible across regions; national conventions and
personal address belong in the ES memo.

Do not add an in-app UI selector or promise live switching as part of translation.
If a selector is requested, explicitly design startup resolution, persistence,
fallback, and refresh/restart behavior first. Audit cached `ResourceLoader`,
static option arrays, constructed pages, and badge measurements before claiming
that a language change updates everything. Verify system/per-app language
changes after a cold launch on supported platforms.

The technical implementation should start from the existing classes:

- Parameterize `ResourceCompletenessTests` across `en`, `ru`, and `es`; check
  duplicates before converting keys to a set, nonempty values, composite-format
  placeholder identity and valid syntax. Equal text needs a small reviewed
  allowlist for proper names, Pāli, or intentional common text, not automatic
  rejection or approval.
- Keep `AppTextFormatter` as the place for formatting. Verify Russian integer
  counts at 0, 1–5, 11–14, 21–25 and 101–114; Spanish at 0, 1, 2 and larger
  values. Current tests cover only six Russian examples. Scope plural rules to
  the integer counts actually shown.
- Verify UI resource language, plural selection, and formatting culture agree
  under regional and fallback settings. Current formatters use
  `CurrentUICulture`; decide deliberately if device-region date/number formats
  should be independent. Do not let the dictionary-language preference choose
  UI grammar or dates.
- Inspect `Grammar.Table.LikePrefix` / `LikeSuffix` and assembled headers. Use a
  localizable template if word order requires it, while preserving separate
  Pāli font runs. Do not build sentences from translated English fragments.
- Reuse `GrammarText` and `BadgeLabelMaps`; ensure every requested compact/table
  label is available. Preserve the call-site binding rules in `AGENTS.md` and
  the `RegularText` / `PaliText` distinction.
- Keep URL destinations and placeholders intact unless a replacement learning
  resource is explicitly reviewed. Localize link text naturally. Clearly label
  retained English learning resources; do not substitute a weak local resource
  merely to avoid an English link. Preserve quotation attribution and license
  meaning without expanding this into a legal-text project.

## Acceptance checks

Mechanical checks cannot certify terminology. Require both technical evidence
and a locale-specific linguistic review.

1. **Resources:** all three locales have matching intended keys, valid XML,
   placeholders and markup; no unexpected English text or `[resource.key]`
   output. Check assembled labels and non-resource literals too.
2. **Semantics:** every high-risk family has a decision and supporting evidence;
   the reviewer checks case hints, voice, mood, citation form, abbreviations,
   and FAQ claims against the accepted shared meaning.
3. **Behavior:** test `en`, `ru-RU`, another Russian regional tag, `es-ES`,
   `es-MX`, and an unsupported language. Check English fallback and mixed
   UI/dictionary preferences, including English fallback for missing meanings.
   Language changes must leave practice history, filters, and form IDs intact.
4. **Rendering:** inspect all screens at narrow supported widths and enlarged
   text; include four-badge verb cards, tables, dialogs, settings, statistics,
   Help and About. Check Cyrillic, Spanish punctuation, Pāli diacritics,
   wrapping, truncation, tooltip text, links, and screen-reader labels.
5. **Platforms:** record a desktop integration run and native iOS/Android
   resource/font smoke checks; list which Windows/macOS/Linux targets were
   actually exercised. A .NET unit-test pass or one desktop build does not
   establish that every platform loads the same language correctly.
6. **Handoff:** run `python3 quality/gate.py auto --base <source-commit>` as
   required by the repository and retain the reported evidence path. Record
   failures and untested platforms without claiming a complete release pass.

For maintenance, review only changed English keys and affected terminology
families in each isolated locale session. Reuse accepted evidence unless the
meaning changes. A concise source revision and decision note in each locale memo
are sufficient; no additional orchestration machinery is required.

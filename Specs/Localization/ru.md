# Russian UI localization: terminology and review plan

Status: 262-key Russian resource draft aligned to Russian DPD terminology, 2026-09-08. Fresh review of this terminology revision and final technical verification remain with the coordinator.
Scope: UI labels, grammar explanations, and Russian presentation conventions. Dictionary meanings and Pāli forms are separate content.
The terminology pass used English resources and grammar code first; the existing Russian resource file was inspected afterwards as an unverified translation baseline. The owner-authored English is the source of intended meaning, including its explanations of poetic usage, uncommon infinitives, and omitted aorist forms. This memo supports faithful localization, not a source-correction task. UI language follows the system; dictionary translation preference is independent.

## Current terminology: Russian DPD alignment — 2026-09-08

The owner selected [Russian DPD](https://ru.dpdict.net/) as the preferred Russian
terminology reference. This section supersedes the earlier provisional glossary
and the historical implementation decisions below wherever they differ. It does
not replace the English resource meanings or change any grammatical identities.
This pass inspected only the shared brief, RU memo/resources, English resources,
relevant code, and the Russian DPD sources; no sibling-locale material was used.

### Directly inspected sources

- **R6 — A:** [DPD grammatical abbreviations](https://devamitta.github.io/dpd.rus/abbreviations/),
  section «Грамматические сокращения», retrieved 2026-09-08. Establishes the
  compact Russian tokens and their expanded meanings in the table below. In
  particular, `возвр` is explained as «возвратный глагол; средний залог».
- **R7 — A:** [DPD features](https://devamitta.github.io/dpd.rus/features/features/),
  section «Склонение / Спряжение», reached from the owner's
  [features index](https://devamitta.github.io/dpd.rus/features/). Supports
  склонение, спряжение, основа, шаблон, неправильные склонения, and the distinction
  between generated forms and attestation. Its specific corpus coverage is not
  imported into the app's broader authored corpus wording.
- **R8 — A:** [DPD grammar dictionary](https://devamitta.github.io/dpd.rus/features/grammardict/),
  opening explanation, explicitly uses «таблицы спряжения возвратных глаголов».
  This corroborates R6's возвратный terminology within the same DPD project;
  it is not an independent scholarly source.

The requested pages were retrieved directly as HTML after the web reader
returned cache misses for the documentation URLs. R6, R7, and R8 are direct
source inspections, not search-snippet evidence. The features index itself
contains navigation rather than the detailed morphology explanation.

### Supported terms and compact forms

Full labels below are the app's labels; omitting падеж beneath case headings,
capitalizing badge labels, and retaining род/число/время/наклонение in standalone
full labels are UI adaptations. DPD's compact tokens have **no final periods**.
`Short` capitalizes the token; `Table` and `Grammar.Pattern.*` use lowercase.

| Resource family | Full label | DPD token used for Short / Table |
|---|---|---|
| `Grammar.Case.Nominative` | Именительный | Имен / имен |
| `Grammar.Case.Accusative` | Винительный | Вин / вин |
| `Grammar.Case.Instrumental` | Творительный | Твор / твор |
| `Grammar.Case.Dative` | Дательный | Дат / дат |
| `Grammar.Case.Ablative` | Отделительный | Отд / отд |
| `Grammar.Case.Genitive` | Родительный | Род / род |
| `Grammar.Case.Locative` | Местный | Мест / мест |
| `Grammar.Case.Vocative` | Звательный | Зват / зват |
| `Grammar.Gender.Masculine` | Мужской род | Муж / муж |
| `Grammar.Gender.Feminine` | Женский род | Жен / жен |
| `Grammar.Gender.Neuter` | Средний род | Ср / ср |
| `Grammar.Number.Singular` | Единственное число | Ед / ед |
| `Grammar.Number.Plural` | Множественное число | Мн / мн |
| `Grammar.Tense.Present` | Настоящее время | Наст / наст |
| `Grammar.Tense.Future` | Будущее время | Буд / буд |
| `Grammar.Tense.Imperative` | Повелительное наклонение | Повелит / повелит |
| `Grammar.Tense.Optative` | Желательное наклонение | Желат / желат |
| `Grammar.Voice.Reflexive` | Возвратный залог | Возвр / возвр |
| `Grammar.Person.First/Second/Third` | 1-е / 2-е / 3-е лицо | 1 / 2 / 3 (Short only) |

R6 lists both `ср` and `сред` for neuter; use `ср` consistently for the shorter
UI form, including `Grammar.Pattern.Neuter`. It cannot be confused with the
voice token `возвр`. Person digits mean grammatical person, as explicitly defined
by R6. Full labels retain лицо; the settings digits appear under Лицо, and table
rows and compact card badges use the same first/second/third-person values.
`Grammar.Pattern.Masculine/Feminine/Neuter/Plural/Present` reuse their table tokens.

### UI adaptations and retained distinctions

- **Voice:** `Возвратный залог` adapts R6/R8's naming to the app's Залог selector.
  It denotes the existing attanopada category, also called middle voice in the
  reference. It does not claim that every form has reflexive meaning. Use the
  same возвратный stem in the full label, both settings options and the voice
  hint; retain `(attanopada)`, the contrast with `(parassapada)`, and «в основном
  в стихах». No enumeration, form identity, or English text changes.
- **Active:** the inspected DPD abbreviation table has no standalone active
  token. Retain Действительный залог (supported by R1 and consistent with R6's
  действительного залога in its participle entry). `Действ` is an explicitly
  UI-specific compact label, with final punctuation removed to match the other
  grammar badges. Do not claim that this abbreviation comes from DPD.
- **Other full terms:** the existing full case, gender, number and tense/mood
  terms already agree with R6. Do not add its alternative synonyms to concise
  UI labels. Aорист and инфинитив are also directly attested in R6; preserve
  the app's aorist explanation and citation-form rationale without expansion.
- **Inflection:** retain склонение, спряжение, неправильное склонение and
  неправильное спряжение; R6's irregular entry and R7 support their meaning.
  Вариант склонения remains an app-specific rendering of a supported
  non-standard pattern, distinct from irregular morphology. ` (по образцу `
  remains the natural UI construction before a Pāli model word, not a literal
  import of R7's implementation term шаблон. Окончания remains the shared
  filter label; do not rename noun stems or citation-form endings to корни.
- **Prose and other compact labels:** preserve case cues, wishes/possibility/
  obligation, attestation, FAQ claims, and current ordinary Russian wording.
  DPD establishes terminology, not a prose template. `вост.` and `Стат.` are
  unrelated UI-specific abbreviations and retain their existing punctuation.
  The owner's `Start.*`, `Settings.ReviewExplanation`, and `Settings.Range*`
  values remain unchanged.

This revision changes 49 resource values, restricted to terminology/compact
labels and their voice-selector explanations. XML parsing, unique key parity,
placeholder and Markdown-target preservation, and protected-value preservation
were checked. Fresh linguistic review and the coordinator's repository gate
remain separate acceptance steps. Prior screenshots do not verify the new
labels; in particular, `Повелит` is longer than the previous compact label.

## Existing text is fully replaceable

The owner explicitly authorizes scrapping all existing Russian UI wording if
needed. It was written without a localization spec and has no preservation
priority. Translate from the English source, this spec and the approved glossary;
there is no requirement to read, audit, reuse or incrementally repair the old
Russian text. Reuse a phrase only if it independently meets those requirements.
Complete replacement needs no further permission. Preserve resource keys,
placeholders and the authored English meaning; this permission concerns UI text,
not the separately sourced Russian dictionary meanings.

## Evidence and limits

For general Pāli grammar understanding, first use the owner-selected Duroiselle
edition in the [shared grammar reference](README.md#shared-grammar-reference).
The sources below serve Russian terminology and supplementary understanding;
they do not replace that shared reference or authorize changing authored English.

Grades: **A** = directly inspected Russian Pāli teaching/reference text; **B** = directly inspected specialist Pāli evidence in another language; **C** = general locale standard; **P** = proposed product wording requiring review. A grade supports terminology, not every historical claim in its source.

| ID | Inspected source and location | Use and limitation |
|---|---|---|
| R1 — A | Charles Duroiselle, *Практическая грамматика языка пали*, Russian translation by A. Gunsky, 2003–2005; [PDF](https://dhamma.ru/paali/durois/duroiselle.pdf). Title page; §116, PDF/printed p. 27; §§354–359, pp. 85–86. | Russian case, gender, number, voice, tense/mood, and inflection terminology. §357 qualifies the earlier semantic description of middle voice. This is a translated historical reference, not contemporary consensus on every analysis. |
| R2 — A | James W. Gair and W. S. Karunatillake, *Новый курс по чтению пали*, translation/adaptation by D. A. Ivakhnenko; [PDF](https://www.dhamma.ru/paali/new_pali/new_pali.pdf). Title/credits, PDF pp. 1–4; III.4, pp. 67–68; VI.7, p. 134 onward; IX.3, p. 188; XI.4, pp. 230–232; infinitive examples pp. 43–44. | A teaching source for optative, past tense, future, and middle endings. PDF extraction has damaged characters: section identifiers and page numbers are safer than copied Pāli examples. |
| R3 — A | D. A. Ivakhnenko, [*Краткий пали-русский словарь*](https://www.dhamma.ru/paali/slovar.html), opening abbreviation table. | Attested Russian abbreviations, including distinct neuter and middle labels. Same editorial community as R1/R2; these are not three independent Russian schools. |
| R4 — B | Ānandajoti Bhikkhu, [*A Practical Guide to Pāḷi Grammar*](https://ancient-buddhist-texts.net/Textual-Studies/Grammar/Guide-to-Pali-Grammar.htm), “Verbs,” opening classification and “Summary of Verb Meanings.” | Independent Pāli cross-check: middle morphology need not imply reflexive meaning; tense/mood summaries and infinitive recognition. Does not establish Russian wording. |
| R5 — C | Unicode CLDR, [Russian number formats](https://unicode.org/cldr/charts/49/verify/numbers/ru.html), “Plural Rules.” | Integer one/few/many and decimal other categories. Locale mechanics, not Pāli grammar. |

The cited PDF text was inspected through the web reader. Screenshot retrieval failed; no visual verification of these PDFs is claimed. No textbook was considered verified merely because it appeared in another bibliography. In particular, *Язык пали* by T. Ya. Elizarenkova and V. N. Toporov is a candidate for a second scholarly check, not a source inspected here. No Sanskrit textbook is needed to justify the proposed Russian labels; Sanskrit comparisons embedded in R1 must not become statements about Pāli without Pāli evidence.

## Historical provisional glossary mapped to resource keys

Below, `{Full,Short,Table}` means the three existing keys with those suffixes, not a new key. Full labels are standalone; short labels are badges; table labels are lowercase. Badge spellings are product proposals even when the full term is attested.

| Existing key family | Full term | Short / Table proposal | Evidence and decision |
|---|---|---|---|
| `Grammar.Case.Nominative.{Full,Short,Table}` | Именительный | Им. / им. | R1; P abbreviation. |
| `Grammar.Case.Accusative.{Full,Short,Table}` | Винительный | Вин. / вин. | R1/R3. |
| `Grammar.Case.Instrumental.{Full,Short,Table}` | Творительный | Твор. / твор. | R1/R3; prefer familiar Russian term over инструменталис. |
| `Grammar.Case.Dative.{Full,Short,Table}` | Дательный | Дат. / дат. | R1/R3. |
| `Grammar.Case.Ablative.{Full,Short,Table}` | Отделительный | Отд. / отд. | R1/R3; аблатив is a useful help-text synonym, not a replacement forced by English. |
| `Grammar.Case.Genitive.{Full,Short,Table}` | Родительный | Род. / род. | R1/R3. |
| `Grammar.Case.Locative.{Full,Short,Table}` | Местный | Местн. / местн. | R1/R3; do not substitute Russian school-grammar предложный. |
| `Grammar.Case.Vocative.{Full,Short,Table}` | Звательный | Зват. / зват. | R1; expanded badge is P. |
| `Grammar.Gender.Masculine.{Full,Short,Table}` | Мужской род | М. р. / м. р. | R1, P disambiguation. |
| `Grammar.Gender.Feminine.{Full,Short,Table}` | Женский род | Ж. р. / ж. р. | R1, P disambiguation. |
| `Grammar.Gender.Neuter.{Full,Short,Table}` | Средний род | Ср. р. / ср. р. | R1/R3, P disambiguation from voice. |
| `Grammar.Number.Singular.{Full,Short,Table}` | Единственное число | Ед. ч. / ед. ч. | R1, P expanded badges. |
| `Grammar.Number.Plural.{Full,Short,Table}` | Множественное число | Мн. ч. / мн. ч. | R1, P expanded badges. |
| `Grammar.Person.First.{Full,Short}` | 1-е лицо | 1-е | R2 III.4; P compact form. |
| `Grammar.Person.Second.{Full,Short}` | 2-е лицо | 2-е | Same. |
| `Grammar.Person.Third.{Full,Short}` | 3-е лицо | 3-е | Same. |
| `Grammar.Tense.Present.{Full,Short,Table}` | Настоящее время | Наст. / наст. | R1/R3. |
| `Grammar.Tense.Future.{Full,Short,Table}` | Будущее время | Буд. / буд. | R1/R2 IX.3. |
| `Grammar.Tense.Imperative.{Full,Short,Table}` | Повелительное наклонение | Повел. / повел. | R1 §359; P short form. |
| `Grammar.Tense.Optative.{Full,Short,Table}` | Желательное наклонение | Желат. / желат. | R1/R2 III.4. Introduce оптатив as synonym in help, without equating it only with wishes. |
| `Grammar.Voice.Active.{Full,Short}` | Действительный залог | Действ. | R1/R3; active table label currently falls back to Short. |
| `Grammar.Voice.Reflexive.{Full,Short,Table}` | Средний залог | Средн. / средн. | R2 XI.4/R3. Медиальный is an attested R1 synonym. Provisional Russian alternative for the same attanopada referent; Возвратный remains an option. No English change required. |
| `Grammar.Type.Declension` | склонение | — | R1; label remains lowercase when embedded. |
| `Grammar.Type.Conjugation` | спряжение | — | R1. |
| `Grammar.Type.IrregularDeclension`, `Grammar.Type.IrregularConjugation` | неправильное склонение / неправильное спряжение | — | P: means irregular morphology; review whether особое is clearer to learners. |
| `Grammar.Type.NonStandardDeclension` | вариант склонения | — | P translation review: preserve the source distinction from irregular forms. |

Full forms ending in род, число, лицо, время, наклонение, залог improve standalone clarity but need layout review. They can be shortened in a control whose heading already supplies the noun. Do not append the same noun twice. The current badge conventions are not automatically wrong; choose one set after rendering full practice cards and tables. R3 supports distinguishing `ср.` (gender) from `средн.` (voice); avoid two identical “Ср.” badges.

| Concept / actual resource touchpoint | Provisional choice and boundary |
|---|---|
| Case / `Settings.Section.Cases` | Падежи. Keep enum order; do not reorder to match Russian school tables. |
| Number/person/voice / `Settings.Row.Number`, `.Person`, `.Voice` | Число / Лицо / Залог. Preserve the English explanation and the parassapada/attanopada referents. |
| Tense and mood / `Settings.Section.Tenses`, `Start.Verbs.Title`, `Help.HowToPractice.Content` | Времена for the existing heading; Времена и наклонения is a provisional Russian clarification if useful in context. Preserve the same category set and navigation purpose; no English taxonomy change is required. |
| Stem / `Settings.Row.Endings` and Pāli pattern chips | Основа, specifically основа на … where appropriate. No standalone stem key exists in English. Noun chips such as a, ar, ant classify stems; verb chips ati, āti, eti, oti display citation-form endings. Use both callers to assess the shared Окончания translation; do not require a source-key split for this memo. |
| Paradigm / `Grammar.Table.PageTitle.Declension`, `.Conjugation`; `Grammar.Table.LikePrefix`/`.LikeSuffix` | Таблица склонения / Таблица спряжения for the screen; образец склонения/спряжения for a model word. Парадигма is suitable in technical help, not required in beginner navigation. These are P choices. |
| Citation form / `Settings.CitationFormWarning.Content` | Словарная форма is a clear P choice. Explain the actual prompt form explicitly. Do not call it an infinitive, a root, or a verb stem. |
| Aorist / `Settings.Tenses.AoristFooter`, `Help.Faq.MissingForms` | Аорист is attested in R2 VI.7. Translate the authored gloss “past narrative tense” as повествовательное прошедшее время and retain its explanation of current coverage and possible future trainer. Do not replace it with a broader gloss. |

`InflectionTableViewModel` exposes `RawPattern` as `PatternName`; these upstream pattern labels need a separate display audit. Translators must not alter pattern identifiers, enum values, database identities, Pāli endings, or reconstruction rules. “Root” and “stem” must not be merged merely because R1's older introductory wording does so.

## Fidelity requirements for grammar explanations

Research informs Russian terminology and idiom. It does not authorize removing, expanding, or requiring revalidation of owner-authored English claims. Any source change requires an explicit owner request; translation can proceed without one.

1. **Voice terminology.** For `Grammar.Voice.Reflexive.*` and `Settings.VoiceOption.*`, evaluate Возвратный, Средний, and Медиальный as Russian names for the same attanopada category. Apply the chosen term consistently with Действительный or Активный for parassapada. `Settings.Row.VoiceHint` must retain both the relative frequency and the observation that attanopada mostly occurs in poetic verses. Do not change “mostly” to “only,” delete the observation, or add a new grammar lesson. Keep `Voice.Reflexive = 2` and English labels unchanged.
2. **Category labels.** Present/future and imperative/optative share one filter group. Choose readable Russian labels for those existing categories. Повелительное and Желательное are established mood names; a Russian heading clarification is a translation choice, not a required change to the English heading or enum.
3. **Citation form rationale.** `Settings.CitationFormWarning.Content` must preserve the causal explanation: present third-person singular serves as the citation form because Pāli does not commonly use infinitives. Preserve the subsequent explanation that first and second person have been re-enabled. The existence of infinitive examples does not rebut a claim about uncommon use. Match the source's degree; a candidate phrase is «поскольку инфинитив в пали употребляется нечасто».
4. **Aorist and omitted forms.** Preserve “past narrative tense” in `Settings.Tenses.AoristFooter`, the stated variation across verbs and word-by-word learning rationale, and the possible dedicated trainer. Preserve the authored explanation for both aorists and participles in `Help.Faq.MissingForms`. Textbook formation classes do not justify replacing the app's stated rationale. Avoid adding claims about Russian aspect, Sanskrit, or Greek.
5. **Case hints.** `Grammar.Case.*.PracticeHint` and `.SettingsHint` should retain the source's questions and uses in idiomatic Russian. Research helps avoid replacing the Pāli local case with Russian предложный or losing a use during translation. Preserve the distinction between short card cues and fuller settings explanations; do not turn them into exhaustive definitions.
6. **Endings and attestation.** Match the existing referent of `Settings.Row.Endings`. In `Grammar.Table.NonCorpusHint` and `Help.Faq.MultipleForms`, preserve the authored corpus wording and degree: “not found” must not become “does not exist.” No corpus-source revalidation is a prerequisite for translation.

For `Grammar.Tense.*.SettingsHint`, convey every English cue naturally: the optative's wishes, possibility, and obligation; the imperative's commands, requests, and encouragement; and the stated present/future uses. Extra uses encountered in references are background for the translator, not instructions to expand the source explanations.

## Optional observations about the previous Russian text

Read after the independent glossary pass: `PaliPractice/PaliPractice/Strings/ru/Resources.resw`.

These are historical research observations, not a repair checklist or a mandate
to retain any phrase. A translator starting from scratch may skip this section.

| Existing value / keys | Assessment |
|---|---|
| Отделительный, Местный, Желательное | Supported by Russian Pāli teaching usage. Retain the core terms. |
| Возвратный / Возвр.; Активный / Актив. | Provisional alternatives are Действительный and Средний/Медиальный. Select Russian terms for the same intended categories; preserve the English explanation. Avoid unexplained alternation between synonyms. |
| “инфинитив почти не употребляется” in citation warning | Stronger than English “does not commonly use”; use a closer degree such as «употребляется нечасто», while preserving the causal premise. |
| “повествовательное прошедшее время” in aorist footer | Faithfully retains the authored “past narrative tense” gloss. Keep it and the accompanying rationale. |
| Времена in settings; Времена и спряжения in start subtitle | Review as a Russian navigation pair for clarity and fidelity. No English taxonomy decision is needed. |
| Мест./Зв.; Муж./Жен./Ср.; Ед./Мн.; 1-е | Viable compact UI proposals, not evidence of pedagogical approval. Test recognizability against the glossary's expanded alternatives. |
| “настоящие и привычные действия” | “Настоящие” can mean “real.” Prefer a natural description such as actions occurring now or regularly; this is editorial revision. |
| “о, ...! (обращение)” | Preserve the address cue and example, with natural Russian punctuation rather than a literal English comma pattern. |
| Instrumental settings hint omits English “passive” | Restore the source's passive-use cue in natural Russian; retain the other listed uses rather than substitute a different explanation. |
| `{0} день`, `{0} дня`, `{0} дней` | Correct candidates for existing integer day-count resources; runtime selection still needs verification. |

## Russian style and formatting contract

- Use neutral instructional prose and sentence case. Buttons can use concise infinitives, for example Показать ответ; help instructions can use consistent plural imperatives. Avoid alternating ты/вы or gendering the learner.
- Use пали in Russian prose and preserve the product name Pāli Practice. Retain Latin Pāli with its diacritics in forms and examples. Do not transliterate form data into Cyrillic. Keep ё consistently in prose where chosen.
- Prefer «…» for Russian quotations. Keep resource placeholders, Markdown destinations, escape syntax, intentional line breaks, and Pāli spelling intact. Translations may reorder numbered placeholders where grammar requires it.
- `Common.DayCount.{One,Few,Many}` requires 1 день, 2 дня, 5 дней; 11–14 use дней; 21 returns to день. R5 distinguishes fractional `other`, whereas `AppTextFormatter.SelectPluralForm` currently accepts integers. Do not reuse this helper for decimals without a separate design decision.
- Verify 0, 1, 2, 4, 5, 11, 12, 14, 20, 21, 22, 25, 101, 111, 112. Inspect the complete phrase, because grammatical case can change the counted noun. `Common.AvailableCount` can use the invariant construction Доступно: {0}, avoiding a guessed noun or adjective agreement.
- `FormatHistoryHeader` and `FormatCalendarTooltip` use culture-specific dates. Check Russian month forms and weekday abbreviations at runtime; do not replace them with a manually translated English month list.
- Full labels, badges, table headers, and accessibility labels are separate presentation needs. Render the longest gender/number/voice combinations and expanded mood labels at the smallest supported width and enlarged text size. Preserve readable Cyrillic and combining Pāli marks; do not solve overflow by silently dropping grammar information.

## Bounded locale workflow and acceptance

1. Freeze the current owner-authored English as the meaning brief. Record the key, screen context, intended referent, and placeholders. Preserve its claims and explanations; source changes require a separate explicit owner request.
2. Have a Russian-speaking Pāli teacher or philologist review only this memo and the relevant English source meanings. Request translation decisions on Возвратный/Средний/Медиальный, the short labels, and Russian case cues. Additional terminology evidence may help a disputed Russian choice, but revalidating English claims is not a prerequisite.
3. Approve the glossary, then translate Russian UI resources from scratch or revise selected existing wording, whichever produces the better result. Use two bounded batches: grammar/settings/practice first; navigation/help/statistics/about second. Review each batch in screen context rather than translate 234 keys without checkpoints. No reuse quota or old-versus-new justification is required. Dictionary meanings remain outside this pass.
4. A fresh Russian review checks fidelity against the authored English and terminology against the cited sources, without using another locale as authority. Record each contested term, source section, decision, and affected keys; no full translation-management platform is needed.
5. Verify key and placeholder parity, XML/Markdown integrity, plural boundaries, dates, start/settings/practice/history/table screens, and the citation-warning trigger. Inspect voice badges next to gender badges and all three persons in singular and plural. The reviewer must explain what each badge means without consulting English.
6. Release when Russian terminology choices are resolved, every source claim and qualifier is retained, no untranslated raw pattern label is mistaken for UI prose, and rendering remains readable. Verify that system UI language and dictionary-language preference remain independent. Run the repository gate in the implementation task. This research-only memo does not certify current translations or runtime behavior.

Open decisions are deliberately small: final voice term; whether full labels include their category noun in each control; expanded versus existing short badges; the shared Russian wording for `Settings.Row.Endings`; and faithful phrasing for irregular/variant paradigms. These are locale editorial decisions, not requirements to alter or revalidate the English.

## Implementation pass — 2026-09-08

Source frozen at `2120723cd2603d96b4697dacd416d86e6423a4c0` (234 English keys).
A fresh RU-only agent authored batch 1 from English and this memo: all grammar,
settings and practice resources, plus the pilot grammar/missing-forms FAQ,
day-count forms, relative history dates and calendar tooltip (166 keys).
No sibling-locale resources or notes were consulted. This is agent editorial
work, not a claim of review by a human Pāli teacher or philologist. Batch 2 waits
for the independent pilot checkpoint. Native macOS menu keys identified by the
coordinator will be included in batch 2 after English extraction.

### Reconciled terminology for implementation

- **Supported:** `Grammar.Voice.*`, `Settings.VoiceOption.*`, and
  `Settings.Row.VoiceHint` use Действительный залог / Средний залог. The
  owner-selected [English Duroiselle](https://palistudies.blogspot.com/p/a-practical-grammar-of-pali-language.html),
  §§354–357, was consulted first for the referents. [Russian Duroiselle](https://dhamma.ru/paali/durois/duroiselle.pdf),
  §355–357, PDF p. 85, directly gives медиальный, средний, возвратный for
  attanopada; [Russian dictionary abbreviations](https://www.dhamma.ru/paali/slovar.html)
  distinguish средн. (voice) from ср. (gender). Средний is selected consistently;
  the alternative terms are not mixed in the UI. The relative frequency and
  predominantly poetic usage remain explicit. These Russian sources share an
  editorial community; independent human specialist confirmation is not claimed.
- **Supported terms, proposed presentation:** full labels include род, число,
  лицо, время, наклонение and залог; compact labels follow the glossary above.
  Case labels keep their adjective alone under the Cases heading. Средн. and
  Ср. р. are distinct. `Grammar.Tense.Optative.SettingsHint` explicitly retains
  wishes, possibility and obligation; it does not equate the optative with
  Russian conditional morphology. `Settings.Section.Tenses` uses Времена и
  наклонения to cover the existing categories without changing them.
- **Supported case referents, proposed Russian cues:** English Duroiselle §116
  and §599 were directly inspected for categories and instrumental agency.
  Instrumental settings retain страдательный залог and совместность. Accusative
  settings include destination; local case remains Местный, with place,
  relationship and time. The cues are memory aids, not exhaustive definitions.
- **Proposed product wording:** Словарная форма retains the complete causal
  citation-form explanation with употребляется нечасто, followed by re-enabling
  first/second person. Аорист retains повествовательное прошедшее время and
  per-verb learning; future support remains a possibility.
- **Proposed product wording:** неправильное склонение/спряжение labels irregular
  morphology; вариант склонения labels the supported non-standard variant.
  Neither is a change to pattern identities. Окончания remains the shared
  filter label: the inspected conjugation caller displays citation endings;
  noun filters group stem endings. No new claim that those are roots is added.
  The table uses ` (по образцу ` + Pāli example + `)`, retaining font-separated
  assembly and spaces. Raw pattern display still needs coordinator audit.

Validation so far: XML parses after batch 1. No app rendering, screenshots,
platform smoke checks, runtime dates, or repository gate were performed by this
translation agent. Expanded labels require actual layout evidence; character
counts do not establish fit. All prior research evidence limitations remain.

### Pilot checkpoint and completed resource draft

The coordinator reported that fresh RU semantic review found no pilot blockers.
The coordinator also reported readable noun-card and conjugation-settings labels
on iOS at 402 pt width. The table corpus hint clipped and was changed to wrap by
the implementation owner. These are coordinator-reported observations, not
screenshots inspected by this translation agent; enlarged-text and remaining
screen/platform checks are not implied.

After that checkpoint, batch 2 authored the remaining 68 original keys plus
22 extracted native macOS menu entries and 6 extracted pattern qualifiers.
The draft now covers **262 English keys**. The extracted keys do not change
source claims: `Grammar.Pattern.*` uses existing lowercase grammatical
abbreviations; Eastern uses вост. as an invariant compact geographical qualifier (corrected in final review). Native menu
wording uses conventional Russian actions (Правка, Отменить, Вырезать,
Скопировать, Вставить). The rest of the UI retains consistent infinitive buttons,
plural instructions and neutral prose. `Help.Faq.MissingWords` explicitly retains
both beginner and intermediate learners, counts, corpus and exclusions. Help
retains repetition levels/intervals, offline/backup distinctions and all FAQ
claims. About retains all contributors, automated translation disclosure, the
quotation, licenses and link destinations. The quoted passage is translated from
the supplied English text; no published Russian translation is attributed.

Mechanical checks performed on the completed RU draft: 262 matching unique keys,
XML parse, exact numbered-placeholder sets and unchanged Markdown destinations.
Intentional values equal to English: `Common.Ok`, `About.AppNameFormat`, and
`Grammar.Table.LikeSuffix`. All original 234 keys were authored/reconsidered in
the two batches; no unchanged resource was silently treated as verified.
Final independent linguistic review and coordinator platform evidence remain
pending. This memo does not certify release readiness or human qualifications.

### Final linguistic review correction

The coordinator reported that the fresh full Russian review found one linguistic
correction: `Grammar.Pattern.Eastern` now uses **вост.** rather than восточный.
The invariant abbreviation avoids adjective agreement errors when a raw pattern
qualifier precedes склонение. This is the only requested resource correction
from that review; no human philologist qualification is claimed. The translator
applied it and verified XML parsing.

Further coordinator rendering exposed clipping in a four-badge verb card: code
abbreviated number and voice but did not apply available compact tense/person
labels. The implementation owner is correcting consumers of
`UseAbbreviatedLabels`, including case labels, and will recheck rendering. This
is pending technical evidence, not an unresolved Russian term. Final technical
verification and platform acceptance are left to the coordinator.

### Small-screen compact button adjustment

The coordinator reported actual iPhone SE3 rendering where the half-width
statistics button clipped. `Statistics.ButtonTitle` now uses **Стат.**, a compact
label corresponding to English “Stats”; the full screen title remains
**Статистика**. This is a presentation-specific abbreviation, not a terminology
change. The reported start-screen noun title clipping will be addressed by
constrained wrapping in code, retaining both nouns and cases in the Russian text.
The coordinator owns the subsequent rendered recheck.

### Coordinator implementation and rendered verification (2026-09-08)

The completed draft contains 262 keys. Native macOS menu titles and grammar
pattern qualifiers now come from English/Russian resources. The original English
values, Pāli forms, database contents and practice identities remain unchanged.
iOS declares English and Russian bundle localizations. Rendered clipping was
corrected with constrained wrapping on the start/statistics screens, measured
grammar row headings, wrapped table notes and badge rows, and consistent use of
compact case, person and tense labels.

Focused localization tests passed (42 tests), including ru-RU/ru-BY plural
boundaries, UI-culture date formatting, resource parity, composite placeholders,
Markdown structure and link targets. Final repository gate and independent
technical-review results are recorded in the task handoff.

### Owner-directed prose revision — 2026-09-08

The owner rejected literal, bureaucratic Russian in the draft. Longer explanations
now use ordinary verbs, shorter sentences and direct instructions: for example,
«Мы будем рады отзывам и помощи», «Посмотрите руководство», and «Откройте раздел».
Preserve natural existing Russian when it conveys the English meaning; a more
literal replacement is not an improvement. Keep counts, qualifiers, attribution,
technical distinctions and markup intact. The attributed sutta quotation remains
unchanged in this prose pass. This is an editorial revision, not new terminology
research or a claim of human specialist review.

The owner's current `Start.*`, `Settings.ReviewExplanation` and `Settings.Range*`
values are protected in this pass. Short labels and established terms need no
expansion merely to resemble English; in particular, retain «Топ». This supersedes
the earlier recommendation to retain expanded noun/case wording on Start.

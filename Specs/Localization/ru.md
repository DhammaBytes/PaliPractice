# Russian UI localization: terminology and review plan

Status: provisional research memo, 2026-09-08. No production translations changed.
Scope: UI labels, grammar explanations, and Russian presentation conventions. Dictionary meanings and Pāli forms are separate content.
The terminology pass used English resources and grammar code first; the existing Russian resource file was inspected afterwards as an unverified translation baseline. The owner-authored English is the source of intended meaning, including its explanations of poetic usage, uncommon infinitives, and omitted aorist forms. This memo supports faithful localization, not a source-correction task. UI language follows the system; dictionary translation preference is independent.

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

## Provisional glossary mapped to resource keys

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

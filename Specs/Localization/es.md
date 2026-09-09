# Spanish UI localization research memo

Status: all 263 Spanish resources accepted by independent AI review on 8 September 2026. Native pilot and final-screen checks completed within the limits below; final history-title rendering, cleanup and repository gate rerun verified. Original research checked 7 September 2026; follow-up evidence below checked 8 September 2026. Scope is broadly usable Spanish (`es`) for Pāli Practice UI, selected by the system language. Dictionary-meaning language remains an independent preference. The owner-authored English resources are the semantic baseline; this memo researches faithful Spanish terminology, not changes to English explanations.

## Evidence and limits

For general Pāli grammar understanding, first use the owner-selected Duroiselle
edition in the [shared grammar reference](README.md#shared-grammar-reference).
The sources below serve Spanish terminology and supplementary understanding;
they do not replace that shared reference or authorize changing authored English.

Grades describe evidence, not approval: **A** = directly inspected Spanish Pāli teaching/reference text; **B** = directly inspected academic Sanskrit terminology or authoritative Spanish linguistic/orthographic fallback; **P** = editorial choice (review outcome recorded below); **L** = lead, with relevant content not yet inspected. A source can establish usage without proving every grammatical claim it contains.

| ID | Source and inspected location | What it establishes; limits |
|---|---|---|
| A1 | AEBT, *Diccionario Pali-Español*, September 2009, based on A. P. Buddhadatta; revision and edition by Marco Antonio Montava Belda. [PDF](https://budismolibre.org.mx/docs/libros_budistas/Diccionario_Pali_Espanol.pdf): printed pp. 1–4 (credits, preface, abbreviations), p. 121, entries *kātave*, *kātuṃ*. | Attributable Spanish Pāli lexicographic work. Uses **aoristo**, **masculino**, **femenino**, **neutro**, **singular**, **plural**, **infinitivo**. Infinitive entries provide terminology examples; their existence says nothing about frequency and does not contradict the English explanation of uncommon use. Text extraction was inspected; PDF screenshots failed. Its translation quality is uneven; do not copy definitions wholesale. |
| A2 | Alejandro Gutman, [*Indoario Medio: Pali y Prácritos*](https://www.elportaldelaindia.com/El_Portal_de_la_India_Antigua/Pali.html), © 2010–2011, **Morfología**, nominal and verbal subsections. | Authored Spanish instructional overview: eight case names, gender/number, **tema** versus root, **optativo**, and surviving middle endings. This is an explanatory website, not a peer-reviewed grammar. Its bibliography was seen; the books listed there were not checked. |
| A3 | IEBH, [*Diplomado en Pāḷi*](https://iebh.org/diplomado-en-pa%E1%B8%B7i/), **Descripción** and **contenido**, instructor Dr. Aleix Ruiz-Falqués. | Confirms a Spanish-language teaching route with beginner grammar and canonical examples. Public course description only: dossiers, exercises, and classroom terminology were not inspected. Potential reviewer/source lead, not grammatical evidence. |
| B1 | Universidad de Buenos Aires, Rosalía Clara Vofchuk, [*Sánscrito*, 2022 course programme](https://letrasclasicas.filo.uba.ar/sites/letrasclasicas.filo.uba.ar/files/0597%20-%20S%C3%81NSCRITO%20-%20VOFCHUK%202022.pdf), PDF pages 3–4, **Unidades 4–5**. | Academic Spanish fallback for **sustantivo**, **declinación**, **tema**, **raíz**, **desinencia**, **tiempo**, **modo**, **voz**, **optativo**, **imperativo**, **aoristo**. Sanskrit categories do not establish Pāli morphology or usage. Text extraction inspected; no claim to have read the cited textbooks. |
| B2 | RAE/ASALE, [*Ortografía*: Formación, §3.2.2](https://www.rae.es/ortograf%C3%ADa/formaci%C3%B3n). | Abbreviation construction and the required point before ordinal superscripts; supports **1.ª**, **2.ª**, **3.ª** for *persona*. Does not prescribe this app's complete badge set. |
| B3 | RAE/ASALE, DLE: [*lema*, sense 8](https://dle.rae.es/lema), [*paradigma*, sense 4](https://dle.rae.es/paradigma). | General linguistic fallback distinguishing a dictionary headword from a scheme of inflected/derived forms. Does not prescribe Pāli citation conventions. |
| B4 | Universidad Complutense de Madrid, [*Voz o diátesis*](https://www.ucm.es/plataformaele/voz-o-diatesis), **Afirmación de la diátesis media**. | Spanish linguistic terminology distinguishes *media* and *reflexiva* while discussing their relation. Supports avoiding a simple synonym claim; not evidence for Pāli distributions. |

Spanish Pāli sources exist and must be searched first. Additional leads remain deliberately unverified: [Duroiselle, *Una gramática práctica de la lengua pāli*, Spanish translation by Montava, part III](https://www.bosquetheravada.org/wp-content/uploads/Una-Grama%CC%81tica-Pra%CC%81ctica-de-la-Lengua-Pali-Part-3-Charles-Duroiselle.pdf.pdf) appeared in search, but opening returned a challenge page; [Pariyatti, ETP-Spanish lesson](https://learning.pariyatti.org/mod/page/view.php?id=2641) exposed *voz media* in search but direct fetch failed. These are **L**, not verified passages. A1's preface independently attests the Duroiselle translation. Obtain readable lessons before relying on them for usage explanations.

## Repository contract inspected

Baseline: `PaliPractice/PaliPractice/Strings/en/Resources.resw`, `Models/Enums.cs`, `Localization/GrammarText.cs`, `Localization/AppTextFormatter.cs`, `Presentation/Practice/Common/BadgeLabelMaps.cs`, and relevant `Models/Inflection/VerbEndings.cs` branches.

`Case` has eight values; `Number` has singular/plural only. `Tense` mixes present/future with imperative/optative. `Voice.Reflexive` documents *attanopada* and selects endings such as present third-person singular `-ate`, against active `-ati`. Spanish terminology must preserve this intended referent. Stable enum values, keys and English labels remain unchanged; Spanish labels below are now implemented and independently reviewed.

`GrammarText` separates full, short, table and hint values. `BadgeLabelMaps` uses short labels when width is constrained. Active table voice falls back to `.Short`; there is no `Grammar.Voice.Active.Table` key. Person has `.Full` and `.Short`, not `.Table`. No standalone stem or paradigm label exists in the English resource inventory.

## Glossary established for implementation

In the tables, `{X}` enumerates actual existing key segments, not a proposed new key. Full labels below use initial capitalization for standalone UI labels; use lower case in prose. Short/table labels are lowercase abbreviations with points; observed layout checks are recorded below. These glossary choices are implemented; the decision and review records below supersede the original proposals.

| Current key family | Full label | Short / table label | Evidence |
|---|---|---|---|
| `Grammar.Case.Nominative.{Full,Short,Table}` | Nominativo | nom. | A2; abbreviation P |
| `Grammar.Case.Accusative.{Full,Short,Table}` | Acusativo | acus. | A2; abbreviation P |
| `Grammar.Case.Instrumental.{Full,Short,Table}` | Instrumental | instr. | A2; abbreviation P |
| `Grammar.Case.Dative.{Full,Short,Table}` | Dativo | dat. | A2; abbreviation P |
| `Grammar.Case.Ablative.{Full,Short,Table}` | Ablativo | abl. | A1/A2 |
| `Grammar.Case.Genitive.{Full,Short,Table}` | Genitivo | gen. | A2; abbreviation P |
| `Grammar.Case.Locative.{Full,Short,Table}` | Locativo | loc. | A2; abbreviation P |
| `Grammar.Case.Vocative.{Full,Short,Table}` | Vocativo | voc. | A2; abbreviation P |
| `Grammar.Gender.Masculine.{Full,Short,Table}` | Masculino | masc. | A1/A2; expanded badge P |
| `Grammar.Gender.Feminine.{Full,Short,Table}` | Femenino | fem. | A1/A2; expanded badge P |
| `Grammar.Gender.Neuter.{Full,Short,Table}` | Neutro | neut. | A1/A2; expanded badge P |
| `Grammar.Number.Singular.{Full,Short,Table}` | Singular | sing. | A1/A2 |
| `Grammar.Number.Plural.{Full,Short,Table}` | Plural | pl. | A1/A2; shorter badge P |
| `Grammar.Person.{First,Second,Third}.Full` | 1.ª persona; 2.ª persona; 3.ª persona | — | B2/P; rendered pilot checked |
| `Grammar.Person.{First,Second,Third}.Short` | — | 1.ª; 2.ª; 3.ª | B2 |
| `Grammar.Tense.Present.{Full,Short,Table}` | Presente | pres. | A2/B1; badge P |
| `Grammar.Tense.Future.{Full,Short,Table}` | Futuro | fut. | A2/B1; badge P |
| `Grammar.Tense.Imperative.{Full,Short,Table}` | Imperativo | imper. | A2/B1; badge P |
| `Grammar.Tense.Optative.{Full,Short,Table}` | Optativo | opt. | A2/B1; badge P |
| `Grammar.Voice.Active.{Full,Short}` | Activa | act. | A2; badge P |
| `Grammar.Voice.Reflexive.{Full,Short,Table}` | Media | med. | A2/B4 + code; accepted in independent AI fidelity review |
| `Grammar.Type.{Declension,Conjugation}` | declinación; conjugación | — | A2/B1 |
| `Grammar.Type.{IrregularDeclension,IrregularConjugation}` | declinación irregular; conjugación irregular | — | B1/P |
| `Grammar.Type.NonStandardDeclension` | variante de declinación | — | P; accepted in independent AI fidelity review |

Do not present these particular abbreviations as universally established Spanish Pāli conventions. A1 uses shorter gender forms (`m.`, `f.`, `nt.`) and `plu.`; longer forms improve recognizability in isolated badges. Prefer `imper.` over ambiguous `imp.` where space permits. Never truncate Pāli spellings to make a badge fit. Full accessible names must identify the grammatical feature even when visual text is short.

| Concept / current location | Proposed Spanish wording | Distinction |
|---|---|---|
| Noun: `Settings.Navigation.NounDeclensions`, `Statistics.Section.Nouns`, practice/help titles | sustantivo; declinación de sustantivos | B1/P; choose one noun term consistently instead of alternating *nombre* and *sustantivo*. |
| Verb: `Settings.Navigation.VerbConjugations`, `Statistics.Section.Verbs` | verbo; conjugación de verbos | A1/B1 |
| Ending: `Settings.Row.Endings` | Terminaciones | P, beginner label; reserve *desinencia* for a specifically inflectional explanation (B1). |
| Stem: no dedicated resource key | tema; tema nominal/verbal | A2/B1; do not collapse into *raíz*. Internal reconstruction stems require inspection before explaining them as linguistic themes. |
| Root: no dedicated resource key | raíz | B1; a distinct linguistic concept, not a substitute for all stored stems. |
| Paradigm: table/help context, no dedicated key | paradigma; modelo de declinación/conjugación | B3/P; *modelo* is the plain-language explanatory choice, *paradigma* the technical term. |
| Citation form: `Settings.CitationFormWarning.Content` | forma de cita; explain as *forma usada para presentar el verbo* | B3/P; *lema* is established dictionary terminology but less transparent for a beginner. |
| Aorist: `Settings.Tenses.AoristFooter`, `Help.Faq.MissingForms` | aoristo | A1/B1; not *aorist*, and not a replacement with a Spanish past-tense name. |
| Participles: `Help.Faq.MissingForms` | participios | A1/B1; do not imply that every participle is irregular. |

## Translation fidelity decisions

The English text was authored by the owner after studying Pāli grammar. Preserve its claims, explanations and qualifiers. These review questions concern Spanish expression only. No source correction or revalidation is required by this localization plan; changes to English content require an explicit owner request.

1. **Time and mood:** *Tiempos y modos* is the accepted Spanish heading for `Settings.Section.Tenses` that describes the existing grouped options; it does not require changing the English heading or enum. Keep *optativo* as the Pāli category rather than substituting *subjuntivo* or *condicional*. For `Grammar.Tense.Optative.SettingsHint`, render the intended wishes, possibility and obligation/advisability naturally in Spanish; preserve all these meanings instead of mechanically translating individual English modal auxiliaries or narrowing the hint to wishes alone.
2. **Middle/reflexive terminology:** use the accepted *media* wording for `Grammar.Voice.Reflexive.*`, `Settings.VoiceOption.{Both,ReflexiveOnly}` and `Settings.Row.VoiceHint` as one consistent Spanish choice referring to the existing *attanopada* forms. Preserve the authored statement that active forms are common and these forms appear mostly in poetic verse, including its frequency qualifier. Terminology research does not make that statement conditional on new evidence. Avoid adding an assertion that every form translates with Spanish *se* or confusing the category with passive voice.
3. **Citation form:** preserve the complete causal explanation in `Settings.CitationFormWarning.Content`: the third-person singular present is used as the citation form because Pāli does not commonly use infinitives. Retain the qualifier *no suele usar* or an equivalent expression of uncommon use; never strengthen it to an absence of infinitives. A1's infinitive entries do not rebut the frequency statement. Also preserve why first and second person have been re-enabled. *Forma de cita* is the accepted term; the implemented sentence preserves the complete rationale.
4. **Aorist and participles:** use *aoristo* and *participios*, preserving the descriptions and exclusion rationale in `Settings.Tenses.AoristFooter` and `Help.Faq.MissingForms`, including variation across verbs, frequent need for verb-by-verb learning and possible future dedicated practice. Translate “past narrative tense” faithfully. Keep “often” and “may” as qualifications; neither delete the explanation nor make it universal or a product commitment.
5. **Case questions:** preserve the mnemonic questions and parenthetical functions in `Grammar.Case.*.{PracticeHint,SettingsHint}`. Spanish *a quién* can fit both accusative and dative prompts, so retain the source's functional cues rather than forcing an artificial one-to-one mapping from Spanish questions to Pāli cases. Useful terminology includes *sujeto*, *objeto directo*, *destino*, *medio*, *compañía*, *finalidad*, *destinatario*, *causa*, *origen*, *posesión*, *lugar*, *relación*, *tiempo* and *apelación directa*. These guide faithful translation, not replacement of the source inventory with a new explanation.
6. **Pattern variants:** `Grammar.Type.NonStandardDeclension` is selected for `isVariantPattern`. Use the accepted *variante de declinación* for Spanish clarity while preserving the existing classification. This is a locale wording question; do not change the English label or domain classification as part of translation.

## Spanish style, counts and presentation

- **Owner-directed tone (2026-09-08):** write simple, friendly, fluent Spanish. Fidelity means preserving the English meaning, not its syntax, word order or sentence structure. Prefer everyday verbs, direct instructions and shorter sentences over bureaucratic phrasing, unnecessary nominalizations and literal English calques. Apply this to all longer text, especially About, Help, FAQs, review requests and settings explanations.
- Preserve existing idiomatic Spanish when it conveys the intended meaning; replacing it with a more literal translation is not an improvement. Keep explanations, causal relationships, counts, frequency qualifiers, attribution, technical distinctions, placeholders and Markdown intact. A lighter tone must not weaken a claim, turn a possible feature into a promise or casually paraphrase an attributed quotation.
- Keep button and navigation labels concise and natural in context. Do not expand a label merely to mirror every English word; put necessary explanation in the supporting text. Retain familiar, broadly understood usage rather than imposing artificial linguistic purity. Verify actual fit at narrow widths instead of treating a longer label as inherently more accurate.
- **Confirmed editorial variety:** use global, transatlantic Spanish with a Latin American stylistic preference. The UI should read naturally across Spanish-speaking regions without adopting European Spanish usage or the regionalisms of any one Latin American country, including Mexico, Argentina and Colombia. Keep one shared `es` translation rather than separate national versions.
- Prefer broadly understood vocabulary, idioms and instructions. Do not import country-specific wording merely because a terminology source or reviewer comes from that country. Academic sources from Spain remain valid terminology evidence; their regional prose and forms of address are not the UI style model.
- **Plural address:** always use *ustedes* when an explicit second-person plural pronoun is needed, with the corresponding Spanish verb and possessive forms. Do not use *vosotros*, *vosotras*, *os*, *vuestro* or their associated forms of address. Omit the subject pronoun when natural; do not insert *ustedes* into every instruction.
- Use sentence case, normal accents and opening question/exclamation marks. Keep the product name **Pāli Practice** unchanged. Use *pāli* in Spanish prose, as decided below; preserve original Pāli strings and their diacritics exactly in all cases.
- Use neutral action labels such as *Mostrar respuesta* for `Practice.Button.RevealAnswer`, *Continuar* for `Common.Continue`, and *Difícil / Fácil* for `Practice.Button.{Hard,Easy}` (**P**). Prefer short infinitive labels over regional imperative choices. Help text can use impersonal instructions or consistent *tú* for singular address; avoid *vos* and switching between *tú* and *usted* across screens. Plural address follows the *ustedes* rule above.
- Person badges should not depend on regional pronoun inventories. In teaching examples, *ustedes* takes third-person Spanish agreement but represents addressees; do not use this as a direct mapping for Pāli second-person endings. Pāli grammatical gender need not match the gender of its Spanish translation.
- `Common.DayCount.One` → `{0} día`; `.Few` and `.Many` → `{0} días` (**P**). For the app's nonnegative integer counts, the inspected formatter selects singular only for 1. Check 0, 1, 2, 11, 21 and 101; do not apply an “ends in 1” rule. This does not specify decimal or compact-number plural behavior.
- `Common.AvailableCount` cannot literally be `{0} disponibles` for every count: prefer the count-neutral *Disponibles: {0}* (**P**) after checking context. If it must remain a sentence fragment, request proper singular/plural resources instead.
- Preserve placeholder numbers, Markdown links, line breaks and meaningful spaces. Check `Common.PageOfFormat`, `Statistics.CalendarTooltipFormat` and `Grammar.Table.LikePrefix/LikeSuffix` as assembled text, not isolated strings. Regional date/number formatting should come from the selected culture, not from hand-translated separators.

## Bounded Spanish workflow and acceptance

1. Resolve the Spanish wording choices above using the glossary and source intent. For disputed translations, allow a bounded follow-up search: direct Spanish Pāli first, then B1-type academic Sanskrit, then Spanish linguistic references. Record unresolved locale terminology without blocking faithful translation on revalidation of the owner-authored English claims.
2. Freeze this glossary and register approved exceptions with source ID and rationale. The initial translation batch contained grammar labels, their hints and associated settings/help passages together. General UI followed after independent pilot review. Dictionary meanings remain a separate workstream.
3. Review Spanish independently against English source intent, the enum/form contract and the cited Spanish sources. Review the complete grammatical feature bundle on noun and verb cards. A Pāli reviewer checks that Spanish preserves category identity, explanations and qualifications; a native Spanish editor checks the confirmed transatlantic style with its Latin American preference, absence of national regionalisms and European forms of address, consistent *ustedes* for plural address, and abbreviation consistency. Read every longer passage as standalone Spanish to catch English calques, stiff constructions and unnecessarily formal wording, then compare it with English to verify that the simpler prose preserves the complete meaning.
4. Validate exact key coverage and placeholders when resources are eventually implemented. Check desktop and narrow mobile cards, expanded and shortened badges, table headers, long hints, Markdown help, ordinal glyphs and Pāli diacritics. Verify system-selected Spanish UI independently of the dictionary translation-language preference; changing that preference must not switch the UI language.
5. Accept only when Spanish preserves the intended grammatical referents, poetic-usage statement, uncommon-infinitive qualifier and causal explanation, and aorist/participle rationale. Count agreement and badges must be clear. Record remaining Spanish terminology choices; any proposed source-content change belongs outside localization and requires an explicit owner request.

The owner authorized UI translation on 8 September 2026. Dictionary data, grammar identities and persisted enum values remain outside this work.


## Implementation decisions and pilot — 8 September 2026

Source revision: `a0bef2b51d25c335b86fd7fed72dd189c4be65f6`; current English
inventory is **262 keys**, including native menu and display-only pattern labels.
The translator read only the shared brief, English resources/code and this Spanish
memo; no Russian resources, notes or reviews were consulted.

Bounded follow-up research started with Spanish Pāli material. A2's **Morfología,
Nominal / Verbal** sections were read directly again, followed by the shared
[Duroiselle reference](https://palistudies.blogspot.com/p/a-practical-grammar-of-pali-language.html),
§§355–359 and the present-system tables at §§381–383. The Spanish Duroiselle PDF
still returns a challenge page and the Pariyatti lesson still fails to open; neither
is upgraded from a lead on the strength of search excerpts. B3 and B4 were also
opened directly. No open-ended literature search was needed.

| Family | Decision for implementation | Evidence and limitation |
|---|---|---|
| `Grammar.Voice.Reflexive.*`, `Settings.VoiceOption.*`, `Settings.Row.VoiceHint` | **Media / med.**, **voz media (attanopada)** in the explanation. | Supported: A2's verbal morphology calls the surviving endings *medias*; Duroiselle §357 connects middle with attanopada. B4 distinguishes middle from reflexive constructions in Spanish. The app's `Voice.Reflexive` selects present `-ate/-ante/-are`, against active `-ati/-anti`. This is the intended inflectional category, with no assertion that every verb takes Spanish *se*. Preserve “es común” and “sobre todo en versos poéticos”. |
| `Settings.Section.Tenses`, `Grammar.Tense.*` | **Tiempos y modos**; **presente, futuro, imperativo, optativo**. Optative hint: **deseos, posibilidad, deber o conveniencia**. | Terms supported by A2; Duroiselle §§359, 383 clarifies mood and modal scope. The hint retains wishes, possibility and obligation/advisability without equating the Pāli optative with one Spanish mood. Hint wording is an editorial choice accepted in independent AI review. |
| `Grammar.Case.*`, gender and number | Adopt the glossary full names and dotted compact/table forms. | A2 supports all eight case names and the gender/number inventory. App-specific abbreviations remain editorial, particularly **acus., instr., imper.**, chosen for recognition rather than minimum character count. Practice/settings hints retain every question and each extra parenthetical function. |
| `Grammar.Person.*` | **1.ª persona / 1.ª**, **2.ª persona / 2.ª**, **3.ª persona / 3.ª**. | Editorial adaptation of B2's ordinal spelling, shorter than spelling out the ordinal. Full text explicitly identifies person; compact text does not depend on regional pronouns. Pilot rendering was subsequently checked. |
| `Settings.CitationFormWarning.Content` | **forma de cita**, **porque en pāli no es habitual usar infinitivos**. | Editorial beginner-facing term; B3 distinguishes *lema* from paradigm. The resource retains the exact present/third/singular specification, the causal frequency claim and the reason for restoring first/second persons. Does not claim absence of infinitives. |
| Aorist footer and missing-forms FAQ | **aoristo (tiempo de pasado narrativo)**; **participios**. | A1/A2 terminology; retain variation between verbs, **a menudo**, and **es posible** for a future dedicated practice. No guarantee of a feature. |
| `Grammar.Type.NonStandardDeclension` | **variante de declinación**, distinct from **declinación irregular**. | Code-backed editorial decision: `GrammarText.GetPatternTypeName` selects it for `isVariantPattern`, after the irregular branch. `NounPattern` explicitly separates base, variant and irregular ranges. B3 supports *paradigma*, but does not prescribe this app classification. |
| Stem/root and pattern qualifiers | **tema** remains distinct from **raíz**; display qualifiers use **masc., fem., neut., pl., pres., oriental**. | A2 explicitly distinguishes root and stem with bhū/bhava/bhavati. No new stem/root explanations are added. `GrammarText.GetPatternQualifier` localizes only qualifiers; identifiers and Pāli runs remain untouched. |
| Spanish prose spelling | **pāli**, lower case as language name; **Pāli Practice** and **Digital Pāli Dictionary** remain proper names. | Editorial consistency with the app's diacritics and teaching context; not a claim that Spanish orthography requires a macron. |

Historical pilot checkpoint: 200 translated values, with all 262 keys present. Untranslated
values intentionally remained English until pilot approval. Complete contexts cover
all grammar labels/hints/pattern qualifiers; noun and four-badge verb practice;
case/conjugation settings and range pages; citation warning; declension/conjugation
tables; grammar and missing-forms FAQs plus multiple-form explanation and practice
instructions; history labels, streak counts and the complete statistics labels and
tooltip. Other Help entries, About, general settings/support and native menus awaited
the second batch, now completed below. Shared navigation labels needed to reach pilot screens are present.

All longer pilot passages were read as standalone Spanish, then compared against
English for meaning and qualifications. The English learning links are unchanged;
translated link text is followed by an explicit note that both resources are in
English. The table fragments assemble as `… (como <Pāli example>)`; leading/trailing
spaces and Pāli font runs are preserved. Singular day count is only for 1;
**Disponibles: {0}** is count-neutral.

Focused XML checks passed: 262 matching keys, placeholder sequences unchanged,
and Markdown link destinations unchanged. These checks do not prove visual fit or
linguistic acceptance. At this pilot checkpoint, fresh Spanish-only review and real rendering were pending. They subsequently completed as recorded below; no human specialist review is claimed.


## Pilot review and completed translation — 8 September 2026

A fresh Spanish-only AI reviewer passed the pilot's terminology, fidelity and
naturalness review. The screenshot review found one required correction:
**ES-PILOT-HEADING** — the assembled English-order heading (`eti pres. conjugación`)
is unnatural in Spanish. Added `Grammar.Table.PatternHeadingFormat` as **{1}: {0}**,
where `{0}` is the separately styled Pāli pattern and qualifiers, and `{1}` is the
grammatical kind. Expected Spanish: **conjugación: eti pres.** or
**declinación: u neut.**. This preserves both fields and lets the integration code
retain Pāli font runs. The coordinator owns the renderer and other locale templates.
The reviewer reported no clipped text or missing glyphs on the 402-point pilot
screens. This is independent AI review, not human specialist review; narrower and
enlarged-text layouts need their own evidence.

The second batch now completes **263 resource keys**, including the heading template,
all Help and About passages, general settings/support, feedback diagnostics and
native macOS menus. The translator reread every longer Spanish passage independently
before checking source fidelity. Dates remain culture-formatted. Proper names,
Pāli words, links, attribution, license conditions, counts, intervals and backup
qualifications are retained. Spanish's `1500` represents the source's `1,500` without
an English thousands separator; it does not change the quantity.

The About quotation was handled separately. The linked
[SN 47:6 English passage](https://www.dhammatalks.org/suttas/SN/SN47_6.html), final
paragraph beginning with the monks' proper range, was inspected directly. The
Spanish reproduces the quoted meaning and source link, uses the required plural
address (**Recorran**, **les**, **su**), and explicitly says it is a translation of
the English passage for this app. It is not presented as a published Spanish
translation. Original illustration and app-design credit remains intact.

Final translator checks: valid XML; 263 unique nonempty values; source placeholders
preserved (the heading deliberately reorders them); all source Markdown link
URLs unchanged. Intentional equal values are `Settings.Section.General`,
`About.AppNameFormat`, `Statistics.Streak.Total`, `Grammar.Table.LikeSuffix`,
`Grammar.Case.Instrumental.Full`, `Grammar.Number.Singular.Full`,
`Grammar.Number.Plural.Full`, `Feedback.Label.App`, and `MacMenu.Zoom`.
They are proper names, symbols, shared Spanish words or familiar interface terms.
No untranslated passage remains. Final independent review and the coordinator's platform/gate evidence are recorded below.


## Final acceptance and validation record — 8 September 2026

A fresh Spanish-only AI reviewer accepted all **263 keys** for terminology,
fidelity and naturalness. No human specialist review is claimed. The assembled
heading correction is implemented and verified natively. Start-screen column
widths now keep the concise labels on one line; the coordinator verified the fix
at 375 points. The final native history screenshot exposed overlap between the
long title and Back. The reviewer explicitly approved **Historial** for both
`History.Title.Declension` and `History.Title.Conjugation`: the practice context
supplies the category, and the title matches the entry label. Both values are now
updated; the rebuilt native history screen now shows the full title without overlap.

Evidence reported by the coordinator:

- iPhone 17, 402 points, `es-MX`: pilot contexts and final Help, About, settings,
  tables, and a four-badge imperative card rendered. Heading fix verified.
- iPhone SE 3, 375 points, `es-ES`: native startup before/after the start-column
  fix, plus unsupported `fr-FR` startup with English fallback. Maestro calls
  intended for the SE targeted the iPhone 17 instead; SE evidence is limited to
  startup screenshots captured with the Xcode tool.
- `es-ES` and `es-MX` count/history-date unit tests passed; focused localization
  suite: 52 passing checks. First repository gate: **PASS**, 204 seconds; log:
  `/tmp/agentic-quality-loop-501/runner-logs/run-20260908T052538.751082Z-20885-497d2fe9/gate.log`.
  The final rerun after the history-title edit also passed; see the completion record below.
- macOS build passed, but its locked UI prevented runtime inspection. No Android
  device was available. No 320-point or complete 375-point screen sweep, native
  statistics tooltip, or older native history-date check was completed.
- The simulator's XXXL size setting produced no visible font enlargement; this
  does **not** count as an enlarged-text pass.
- No extra `.app` copies were created. The original bundle was temporarily
  installed on the iPhone 17 and SE simulators, where it had been absent;
  both temporary installations were removed after verification. The SE text-size
  setting and shutdown state were restored; pre-existing simulator apps were untouched.

The fresh technical reviewer found no blocking issues in the rendering changes,
resource checks, font/binding preservation, or separation of UI and dictionary
language. Native history rendering is verified in
`/private/tmp/pali-es-evidence/final-history-fixed.jpg`. The same evidence directory
contains pilot and final screen captures, including native `0 días` / `1 día` and
an actual Spanish dictionary meaning with Spanish UI. One practice response was
recorded only in the newly installed temporary simulator app to check history and
statistics, then removed with that test installation; existing practice history
and dictionary data were not edited. The final `.NET iOS` build passed (existing
compiler/linker warnings remain). The macOS launch process exited without runtime
UI evidence; no desktop integration pass is claimed.

One nonblocking cosmetic observation remains: the punctuation after the
DhammaBytes link wraps onto the next line in About. These limits constrain
platform/rendering coverage, not the independent acceptance of the Spanish text.
Final repository gate: **PASS**, `auto`, exit 0, 200.41 seconds, base
`a0bef2b51d25c335b86fd7fed72dd189c4be65f6`. It ran the data, desktop, .NET,
Python, quality and submodule groups, including full tests, coverage collection,
compiler/analyzer checks and the desktop build. Log:
`/tmp/agentic-quality-loop-501/runner-logs/run-20260908T053324.243940Z-92058-75a1621e/gate.log`.
This completion note was added after the gate; no product or test files changed
after that passing run.

During the last simulator run, a rapid scripted start-to-history transition after
cold launch produced a malformed intermediate layout. Relaunching and navigating
one screen at a time produced the normal practice and fixed history layouts.
The cause was not isolated; rapid-navigation stability is not established by this
localization check.

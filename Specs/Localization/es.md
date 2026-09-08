# Spanish UI localization research memo

Status: provisional terminology and review plan; no production translations. Research checked 7 September 2026. Scope is broadly usable Spanish (`es`) for Pāli Practice UI, selected by the system language. Dictionary-meaning language remains an independent preference. The owner-authored English resources are the semantic baseline; this memo researches faithful Spanish terminology, not changes to English explanations.

## Evidence and limits

For general Pāli grammar understanding, first use the owner-selected Duroiselle
edition in the [shared grammar reference](README.md#shared-grammar-reference).
The sources below serve Spanish terminology and supplementary understanding;
they do not replace that shared reference or authorize changing authored English.

Grades describe evidence, not approval: **A** = directly inspected Spanish Pāli teaching/reference text; **B** = directly inspected academic Sanskrit terminology or authoritative Spanish linguistic/orthographic fallback; **P** = editorial proposal requiring review; **L** = lead, with relevant content not yet inspected. A source can establish usage without proving every grammatical claim it contains.

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

`Case` has eight values; `Number` has singular/plural only. `Tense` mixes present/future with imperative/optative. `Voice.Reflexive` documents *attanopada* and selects endings such as present third-person singular `-ate`, against active `-ati`. Spanish terminology must preserve this intended referent. Stable enum values, keys and English labels remain unchanged; alternative Spanish labels below are provisional localization choices.

`GrammarText` separates full, short, table and hint values. `BadgeLabelMaps` uses short labels when width is constrained. Active table voice falls back to `.Short`; there is no `Grammar.Voice.Active.Table` key. Person has `.Full` and `.Short`, not `.Table`. No standalone stem or paradigm label exists in the English resource inventory.

## Provisional glossary tied to current keys

In the tables, `{X}` enumerates actual existing key segments, not a proposed new key. Full labels below use initial capitalization for standalone UI labels; use lower case in prose. Short/table proposals are lowercase abbreviations with points and require layout checks. These are a small terminology seed, not a translated resource file.

| Current key family | Provisional full label | Short / table proposal | Evidence |
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
| `Grammar.Person.{First,Second,Third}.Full` | Primera persona; Segunda persona; Tercera persona | — | B2/P; confirm full-label width |
| `Grammar.Person.{First,Second,Third}.Short` | — | 1.ª; 2.ª; 3.ª | B2 |
| `Grammar.Tense.Present.{Full,Short,Table}` | Presente | pres. | A2/B1; badge P |
| `Grammar.Tense.Future.{Full,Short,Table}` | Futuro | fut. | A2/B1; badge P |
| `Grammar.Tense.Imperative.{Full,Short,Table}` | Imperativo | imper. | A2/B1; badge P |
| `Grammar.Tense.Optative.{Full,Short,Table}` | Optativo | opt. | A2/B1; badge P |
| `Grammar.Voice.Active.{Full,Short}` | Activa | act. | A2; badge P |
| `Grammar.Voice.Reflexive.{Full,Short,Table}` | Media | med. | A2/B4 + code; Spanish term pending fidelity review |
| `Grammar.Type.{Declension,Conjugation}` | declinación; conjugación | — | A2/B1 |
| `Grammar.Type.{IrregularDeclension,IrregularConjugation}` | declinación irregular; conjugación irregular | — | B1/P |
| `Grammar.Type.NonStandardDeclension` | variante de declinación | — | P; Spanish term pending fidelity review |

Do not present these particular abbreviations as universally established Spanish Pāli conventions. A1 uses shorter gender forms (`m.`, `f.`, `nt.`) and `plu.`; longer proposals improve recognizability in isolated badges. Prefer `imper.` over ambiguous `imp.` where space permits. Never truncate Pāli spellings to make a badge fit. Full accessible names must identify the grammatical feature even when visual text is short.

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

1. **Time and mood:** *Tiempos y modos* is a provisional Spanish heading for `Settings.Section.Tenses` that describes the existing grouped options; it does not require changing the English heading or enum. Keep *optativo* as the Pāli category rather than substituting *subjuntivo* or *condicional*. For `Grammar.Tense.Optative.SettingsHint`, render the intended wishes, possibility and obligation/advisability naturally in Spanish; preserve all these meanings instead of mechanically translating individual English modal auxiliaries or narrowing the hint to wishes alone.
2. **Middle/reflexive terminology:** consider *media*, *reflexiva* or an explanatory combination for `Grammar.Voice.Reflexive.*`, `Settings.VoiceOption.{Both,ReflexiveOnly}` and `Settings.Row.VoiceHint` as one consistent Spanish choice referring to the existing *attanopada* forms. Preserve the authored statement that active forms are common and these forms appear mostly in poetic verse, including its frequency qualifier. Terminology research does not make that statement conditional on new evidence. Avoid adding an assertion that every form translates with Spanish *se* or confusing the category with passive voice.
3. **Citation form:** preserve the complete causal explanation in `Settings.CitationFormWarning.Content`: the third-person singular present is used as the citation form because Pāli does not commonly use infinitives. Retain the qualifier *no suele usar* or an equivalent expression of uncommon use; never strengthen it to an absence of infinitives. A1's infinitive entries do not rebut the frequency statement. Also preserve why first and second person have been re-enabled. *Forma de cita* is the provisional term; a brief plain-language equivalent may help readers without changing the rationale.
4. **Aorist and participles:** use *aoristo* and *participios*, preserving the descriptions and exclusion rationale in `Settings.Tenses.AoristFooter` and `Help.Faq.MissingForms`, including variation across verbs, frequent need for verb-by-verb learning and possible future dedicated practice. Translate “past narrative tense” faithfully. Keep “often” and “may” as qualifications; neither delete the explanation nor make it universal or a product commitment.
5. **Case questions:** preserve the mnemonic questions and parenthetical functions in `Grammar.Case.*.{PracticeHint,SettingsHint}`. Spanish *a quién* can fit both accusative and dative prompts, so retain the source's functional cues rather than forcing an artificial one-to-one mapping from Spanish questions to Pāli cases. Useful terminology includes *sujeto*, *objeto directo*, *destino*, *medio*, *compañía*, *finalidad*, *destinatario*, *causa*, *origen*, *posesión*, *lugar*, *relación*, *tiempo* and *apelación directa*. These guide faithful translation, not replacement of the source inventory with a new explanation.
6. **Pattern variants:** `Grammar.Type.NonStandardDeclension` is selected for `isVariantPattern`. Compare *declinación no estándar* with provisional *variante de declinación* for Spanish clarity while preserving the existing classification. This is a locale wording question; do not change the English label or domain classification as part of translation.

## Spanish style, counts and presentation

- **Confirmed editorial variety:** use global, transatlantic Spanish with a Latin American stylistic preference. The UI should read naturally across Spanish-speaking regions without adopting European Spanish usage or the regionalisms of any one Latin American country, including Mexico, Argentina and Colombia. Keep one shared `es` translation rather than separate national versions.
- Prefer broadly understood vocabulary, idioms and instructions. Do not import country-specific wording merely because a terminology source or reviewer comes from that country. Academic sources from Spain remain valid terminology evidence; their regional prose and forms of address are not the UI style model.
- **Plural address:** always use *ustedes* when an explicit second-person plural pronoun is needed, with the corresponding Spanish verb and possessive forms. Do not use *vosotros*, *vosotras*, *os*, *vuestro* or their associated forms of address. Omit the subject pronoun when natural; do not insert *ustedes* into every instruction.
- Use sentence case, normal accents and opening question/exclamation marks. Keep the product name **Pāli Practice** unchanged. A decision on *pali* versus *pāli* in Spanish prose is still needed; preserve original Pāli strings and their diacritics exactly in all cases.
- Use neutral action labels such as *Mostrar respuesta* for `Practice.Button.RevealAnswer`, *Continuar* for `Common.Continue`, and *Difícil / Fácil* for `Practice.Button.{Hard,Easy}` (**P**). Prefer short infinitive labels over regional imperative choices. Help text can use impersonal instructions or consistent *tú* for singular address; avoid *vos* and switching between *tú* and *usted* across screens. Plural address follows the *ustedes* rule above.
- Person badges should not depend on regional pronoun inventories. In teaching examples, *ustedes* takes third-person Spanish agreement but represents addressees; do not use this as a direct mapping for Pāli second-person endings. Pāli grammatical gender need not match the gender of its Spanish translation.
- `Common.DayCount.One` → `{0} día`; `.Few` and `.Many` → `{0} días` (**P**). For the app's nonnegative integer counts, the inspected formatter selects singular only for 1. Check 0, 1, 2, 11, 21 and 101; do not apply an “ends in 1” rule. This does not specify decimal or compact-number plural behavior.
- `Common.AvailableCount` cannot literally be `{0} disponibles` for every count: prefer the count-neutral *Disponibles: {0}* (**P**) after checking context. If it must remain a sentence fragment, request proper singular/plural resources instead.
- Preserve placeholder numbers, Markdown links, line breaks and meaningful spaces. Check `Common.PageOfFormat`, `Statistics.CalendarTooltipFormat` and `Grammar.Table.LikePrefix/LikeSuffix` as assembled text, not isolated strings. Regional date/number formatting should come from the selected culture, not from hand-translated separators.

## Bounded Spanish workflow and acceptance

1. Resolve the Spanish wording choices above using the glossary and source intent. For disputed translations, allow a bounded follow-up search: direct Spanish Pāli first, then B1-type academic Sanskrit, then Spanish linguistic references. Record unresolved locale terminology without blocking faithful translation on revalidation of the owner-authored English claims.
2. Freeze this glossary and register approved exceptions with source ID and rationale. The initial translation batch should contain grammar labels, their hints and the associated settings/help passages together. General UI follows only after those concepts are stable. Dictionary meanings remain a separate workstream.
3. Review Spanish independently against English source intent, the enum/form contract and the cited Spanish sources. Review the complete grammatical feature bundle on noun and verb cards. A Pāli reviewer checks that Spanish preserves category identity, explanations and qualifications; a native Spanish editor checks the confirmed transatlantic style with its Latin American preference, absence of national regionalisms and European forms of address, consistent *ustedes* for plural address, and abbreviation consistency.
4. Validate exact key coverage and placeholders when resources are eventually implemented. Check desktop and narrow mobile cards, expanded and shortened badges, table headers, long hints, Markdown help, ordinal glyphs and Pāli diacritics. Verify system-selected Spanish UI independently of the dictionary translation-language preference; changing that preference must not switch the UI language.
5. Accept only when Spanish preserves the intended grammatical referents, poetic-usage statement, uncommon-infinitive qualifier and causal explanation, and aorist/participle rationale. Count agreement and badges must be clear. Record remaining Spanish terminology choices; any proposed source-content change belongs outside localization and requires an explicit owner request.

This memo supplies research and decisions for a later implementation. It neither creates `Strings/es/Resources.resw` nor authorizes changes to dictionary data, grammar identities or persisted enum values.

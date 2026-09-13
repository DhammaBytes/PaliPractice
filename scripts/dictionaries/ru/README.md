# Russian dictionary review — 13 September 2026

Owner approved implementation on 13 September 2026. The reviewed scope is 81
historically changed translated senses and all 18 Russian gaps in the September
12 bundle. This is not a linguistic review of every inherited translation.

The [review record](review-20260913.json) retains the pinned source hashes,
DPD examples, 68 retain decisions, 13 approved corrections and 18 new translations.
Executable corrections are in [localized translations](../../configs/localized_translations.json).

## Resolved terminology decisions

| Lemma | Approved Russian | Spanish action |
| --- | --- | --- |
| saṅghādisesa 2 | нарушение, влекущее за собой отстранение (Виная) | Retain existing translation. |
| cakka 4 | влияющий фактор; определяющее условие; (комм) достижение; букв.: колесо | Add `lit. rueda`. |
| samudayadhamma 2 | то, что подвержено возникновению; возникающее явление | Prepend `fenómeno sujeto a originación`. |
| parinibbāna 1 | (об омрачениях) полное освобождение от уз; полное успокоение; полное затухание | Prepend `(de las impurezas mentales) desatadura completa`; retain extinguishing/cooling glosses. |

`samudayadhamma 2` also receives English primary `subject to origination`,
retaining `an arising phenomenon`. It remains noun ID 89243; adjective ID 59988
is outside this change. The owner chose the technical terminology in all three
languages. Spanish already covered every selected sense; these three edits add
preferred terminology and a literal gloss.

The original editable questions and owner notes were kept in the local audit
report. This tracked record carries the approved outcomes for future rebuilds.
The raw DPD and source-meaning guards require re-review if either text changes.

## Verification and promotion

Promoted as dictionary data version `2026091301` on 13 September 2026.
Both ES and RU cover all 3,722 selected senses and all 2,250 practice-primary
lemmas. The full quality gate passed with zero findings; independent review
found no blocking issues. The packaged database and manifest match the exact
verified candidate. App version and visual QA are separate release work.

- Gate run: `20260913T143954.001578Z-86046-640ff3`.
- Packaged database SHA-256: `eda119bdfa0a4acdcd9a1b14aa0111e9c1408ba5312ae091b8e723bbd3b4c271`.
- [Approved comparison decisions](../resync/20260913-russian-review.json).

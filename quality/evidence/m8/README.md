# M8 — Selected-language meanings and source credits

The app stores one resolved meaning per displayed sense. Neutral details load
without meaning columns. The repository queries only the requested language,
then batches English for missing/blank senses. Preference changes clear cached
meanings under the repository lock; returning to practice refreshes meanings.
Existing English=0 and Russian=1 preferences remain stable; Spanish=2 is appended.

About now contains separate RU and ES paragraphs in both existing UI languages,
with repository links, Venerable Devamitta's maintenance credit, Paññābhūmi's
coordination credit, and upstream AI/automatic-translation provenance.

Focused tests passed 26/26 against the M7 multilingual candidate. They cover
query projections, missing-only fallback, cache replacement and in-flight
invalidation, legacy Russian bundles, preferences, and every displayed sense
through real noun/verb repositories in all three languages. Eligible practice
IDs remain unchanged across language switches.

## Native verification

An isolated copy of the current app uses the M7 repeatability run-1 candidate,
version 2026090702, and app ID `org.dhammabytes.palipractice.m8smoke`. Original
production data, version and registry remain pending and unpromoted.

iOS 26.5 / iPhone 17: build and launch passed with ad-hoc simulator signing.
Actual Settings interactions selected ES, EN, RU and ES again. Noun and verb
cards rendered Spanish; the same cached noun rendered English and Russian after
switching. No grading action was taken. Restart restored Español and the stored
preference was 2. About displayed both source paragraphs and links. Screenshots
in this directory record those checks. Uno's native accessibility tree exposes
no actionable controls, so simulator taps used screenshot-observed coordinates.

Android API 37 / ephemeral `17_Resizable`: self-contained debug APK build,
installation, launch, English About credits, Spanish noun rendering and restart
persistence passed. The saved preference is 2. The first APK expected IDE fast
deployment and aborted; rebuilding with `EmbedAssembliesIntoApk=true` resolved
that packaging issue. No production source change was needed.

Native limitation: direct taps did not open either the existing Theme or
Translation language ComboBox. Keyboard activation opened the menu, and touching
Spanish selected it correctly. This is not proof of working direct-touch
activation. M9 must resolve it on the supported release runtime/device before
publication. The independent reviewer found no demonstrated M8 regression here.

## Gate and review

Universal runner `auto`, base `201f4cc0b916b0284053ee1ce593547e42c25801`, passed
in 239.98 seconds: 2,933 .NET tests, 54 producer tests, 48 quality tests; zero
failures or skipped .NET tests. Desktop compilation, complexity policy/liveness,
English and multilingual repeatability, source preservation and exact enrichment
verification all passed. The .NET tests consumed the enriched candidate.

Gate log: `/tmp/agentic-quality-loop-501/runner-logs/run-20260907T214656.575246Z-53246-eb26c41c/gate.log`.
External evidence run: `20260907T214656.662386Z-53273-c63968`.
The durable completion record is [gate-completion.json](gate-completion.json).

Fresh read-only reviewer `m8_review` found no blocking findings after examining
the task diff, tests, producer/consumer hops and supplied gate evidence. No
reviewer fixes or additional verification pass were required. Final documentation
records native results and the M9 touch follow-up; it changes no checked source.

Native candidate SHA-256:
`7ce322fe8064fd7ef81ca36277acace90fc219a6de58a1c4f3f5e4b260b03213`.
The original pending production database remains
`e0822e1e7c21a1be5143b952420813eb7014fd2e0f0887efb286bdae246fca42`.

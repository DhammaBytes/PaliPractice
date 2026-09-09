# Deliberate quality-gate gaps

Active gate improvements are listed below. Product and accessibility work lives in
[QUALITY-TODO.md](../QUALITY-TODO.md) and
[ACCESSIBILITY-TODO.md](../ACCESSIBILITY-TODO.md).

The database hardening and package-remediation milestones are complete; their
historical evidence is retained in [M5](evidence/m5/README.md) and
[M9](evidence/m9/README.md). The gate now checks repeatability and exact candidate
consumers when supplied with pinned inputs. Current dependency policy and
verification commands live in [README.md](README.md).

- Add architecture enforcement after the intended layer boundaries are
  documented; do not introduce ArchUnitNET speculatively.
- Add NUnit categories, then split a deterministic fast suite from integration
  and platform suites.
- Keep coverage percentage advisory. Require task-specific behavioral tests in
  the agent/reviewer loop until there is a trustworthy changed-code coverage
  signal; do not add an arbitrary repository-wide percentage floor.

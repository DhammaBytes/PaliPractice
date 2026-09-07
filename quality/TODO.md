# Deliberate quality-gate gaps

Execution order for DPD correctness, candidate generation, and RU/ES translation
checks is tracked in [DATA-REBUILD-ROADMAP.md](../DATA-REBUILD-ROADMAP.md).
Milestones M1–M5 stabilize the English core before translation work begins.

- Complete M5 package remediation using the locked dependency paths:
  - `sqlite-net-pcl` / legacy SQLite bundles resolve vulnerable
    `SQLitePCLRaw.lib.e_sqlite3` 2.1.2 in app targets and 2.1.11 in tests.
    The [SQLite advisory](https://github.com/advisories/GHSA-2m69-gcr7-jv3q)
    lists no patched legacy package; choose supported bundle/provider packaging
    and verify the actual native SQLite runtime is at least 3.50.2 on each target.
    An Android-only bundle reference does not fix desktop or test dependencies.
  - `Uno.WinUI.Runtime.Skia.X11` resolves `Tmds.DBus.Protocol` 0.21.2.
    The [D-Bus advisory](https://github.com/advisories/GHSA-xrw6-gwf8-vvr9)
    identifies 0.21.3 as the backported fix (or 0.92.0 for the newer series).
    Validate the resolved graph and desktop runtime after the upgrade.
  - Keep NU1903 visible. A successful build with warnings is not release
    remediation. Review current advisories again at the M5 checkpoint.
- M2 provides pinned inputs and isolated English candidates. In M5, integrate
  repeatability and candidate-consumer checks after semantic validation is complete.
- Add architecture enforcement after the intended layer boundaries are
  documented; do not introduce ArchUnitNET speculatively.
- Add NUnit categories, then split a deterministic fast suite from integration
  and platform suites.
- Keep coverage percentage advisory. Require task-specific behavioral tests in
  the agent/reviewer loop until there is a trustworthy changed-code coverage
  signal; do not add an arbitrary repository-wide percentage floor.

from __future__ import annotations

import json
import io
import os
import subprocess
import tempfile
import time
import unittest
from contextlib import redirect_stdout
from pathlib import Path
from types import SimpleNamespace
from unittest import mock

from quality import gate


def run_git(root: Path, *arguments: str) -> str:
    completed = subprocess.run(
        ["git", *arguments],
        cwd=root,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        check=False,
    )
    if completed.returncode:
        raise AssertionError(completed.stderr)
    return completed.stdout.strip()


class ChangeDiscoveryTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary = tempfile.TemporaryDirectory()
        self.root = Path(self.temporary.name)
        run_git(self.root, "init", "-q")
        run_git(self.root, "config", "user.name", "Quality Test")
        run_git(self.root, "config", "user.email", "quality@example.invalid")
        for name in ("committed.py", "staged.py", "work.py", "delete.py", "rename.py"):
            (self.root / name).write_text(f"{name}\n", encoding="utf-8")
        run_git(self.root, "add", ".")
        run_git(self.root, "commit", "-qm", "base")
        self.base = run_git(self.root, "rev-parse", "HEAD")

    def tearDown(self) -> None:
        self.temporary.cleanup()

    def test_discovers_every_git_state_and_rename_end(self) -> None:
        (self.root / "committed.py").write_text("committed\n", encoding="utf-8")
        run_git(self.root, "add", "committed.py")
        run_git(self.root, "commit", "-qm", "committed change")
        (self.root / "staged.py").write_text("staged\n", encoding="utf-8")
        run_git(self.root, "add", "staged.py")
        (self.root / "work.py").write_text("unstaged\n", encoding="utf-8")
        run_git(self.root, "rm", "-q", "delete.py")
        run_git(self.root, "mv", "rename.py", "renamed.py")
        (self.root / "unknown.bin").write_bytes(b"untracked")

        with mock.patch.object(gate, "ROOT", self.root):
            changes = gate.discover_changes(self.base)

        observed = {(change.source, change.status[:1], change.path) for change in changes}
        self.assertIn(("committed", "M", "committed.py"), observed)
        self.assertIn(("staged", "M", "staged.py"), observed)
        self.assertIn(("unstaged", "M", "work.py"), observed)
        self.assertIn(("staged", "D", "delete.py"), observed)
        self.assertIn(("staged", "R", "renamed.py"), observed)
        self.assertIn(("untracked", "A", "unknown.bin"), observed)
        rename = next(change for change in changes if change.status.startswith("R"))
        self.assertEqual("rename.py", rename.old_path)

    def test_unknown_or_rename_destination_routes_full(self) -> None:
        changes = [gate.Change("staged", "R100", "README.other", "scripts/old.py")]
        self.assertEqual(gate.ALL_GROUPS, gate.route_changes(changes))

    def test_root_quality_todo_routes_to_quality_self_tests_only(self) -> None:
        changes = [gate.Change("untracked", "A", "QUALITY-TODO.md")]
        self.assertEqual({"quality"}, gate.route_changes(changes))
        disguised = [gate.Change("untracked", "A", ".QUALITY-TODO.md")]
        self.assertEqual(gate.ALL_GROUPS, gate.route_changes(disguised))

    def test_sensitive_families_route_every_affected_consumer(self) -> None:
        expected_data = {"data", "submodule", "dotnet", "desktop"}
        for path in (
            "dpd-db",
            "dpd-db/shared_data/frequency/file.json",
            "PaliPractice/PaliPractice/Data/pali.db",
        ):
            with self.subTest(path=path):
                self.assertTrue(
                    expected_data.issubset(
                        gate.route_changes([gate.Change("unstaged", "M", path)])
                    )
                )
        self.assertEqual(
            gate.ALL_GROUPS,
            gate.route_changes(
                [gate.Change("staged", "R100", "quality/new.py", "quality/old.py")]
            ),
        )


class FingerprintAndLockTests(unittest.TestCase):
    def test_fingerprint_detects_tracked_and_untracked_mutation(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            run_git(root, "init", "-q")
            run_git(root, "config", "user.name", "Quality Test")
            run_git(root, "config", "user.email", "quality@example.invalid")
            tracked = root / "tracked.txt"
            tracked.write_text("one\n", encoding="utf-8")
            run_git(root, "add", ".")
            run_git(root, "commit", "-qm", "base")
            with mock.patch.object(gate, "ROOT", root):
                original = gate.worktree_fingerprint()
                tracked.write_text("two\n", encoding="utf-8")
                changed = gate.worktree_fingerprint()
                self.assertNotEqual(original, changed)
                tracked.write_text("one\n", encoding="utf-8")
                (root / "new.txt").write_text("new\n", encoding="utf-8")
                self.assertNotEqual(original, gate.worktree_fingerprint())

    def test_repository_lock_rejects_second_owner(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "gate.lock"
            with gate.RepositoryLock(path):
                with self.assertRaises(RuntimeError):
                    with gate.RepositoryLock(path):
                        pass

    def test_repository_lock_rejects_symlink_without_touching_target(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            target = root / "target"
            target.write_text("sentinel\n", encoding="utf-8")
            lock = root / "gate.lock"
            lock.symlink_to(target)
            with self.assertRaises(gate.GateError):
                with gate.RepositoryLock(lock):
                    pass
            self.assertEqual("sentinel\n", target.read_text(encoding="utf-8"))

    def test_retention_ignores_incomplete_crash_directory(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / "artifacts"
            with mock.patch.dict(
                os.environ, {"QUALITY_ARTIFACT_ROOT": str(root)}
            ):
                previous = gate.Evidence(1)
                (previous.run / "completion.json").write_text(
                    "{}\n", encoding="utf-8"
                )
                incomplete = gate.Evidence(1)
                current = gate.Evidence(1)
                (current.run / "completion.json").write_text(
                    "{}\n", encoding="utf-8"
                )
                current.prune()
            self.assertTrue(previous.run.exists())
            self.assertTrue(current.run.exists())
            self.assertFalse(incomplete.run.exists())

    def test_evidence_hardens_owned_base_and_rejects_symlinks_before_write(
        self,
    ) -> None:
        with tempfile.TemporaryDirectory() as directory:
            temporary = Path(directory)
            insecure = temporary / "insecure"
            insecure.mkdir(mode=0o755)
            insecure.chmod(0o755)
            with mock.patch.dict(
                os.environ, {"QUALITY_ARTIFACT_ROOT": str(insecure)}
            ):
                gate.Evidence(1)
            self.assertEqual(0o700, insecure.stat().st_mode & 0o777)

            target = temporary / "target"
            target.mkdir()
            base_alias = temporary / "base-alias"
            base_alias.symlink_to(target, target_is_directory=True)
            with mock.patch.dict(
                os.environ, {"QUALITY_ARTIFACT_ROOT": str(base_alias)}
            ):
                with self.assertRaises(gate.GateError):
                    gate.Evidence(1)
            self.assertEqual([], list(target.iterdir()))

            broken_alias = temporary / "broken-alias"
            broken_alias.symlink_to(temporary / "missing", target_is_directory=True)
            with mock.patch.dict(
                os.environ, {"QUALITY_ARTIFACT_ROOT": str(broken_alias)}
            ):
                with self.assertRaises(gate.GateError):
                    gate.Evidence(1)
            self.assertTrue(broken_alias.is_symlink())

            base = temporary / "artifacts"
            base.mkdir(mode=0o700)
            tool_target = temporary / "tool-target"
            tool_target.mkdir()
            (base / "tool-cache").symlink_to(
                tool_target, target_is_directory=True
            )
            with mock.patch.dict(
                os.environ, {"QUALITY_ARTIFACT_ROOT": str(base)}
            ):
                with self.assertRaises(gate.GateError):
                    gate.Evidence(1)
            self.assertFalse((base / "runs").exists())
            self.assertEqual([], list(tool_target.iterdir()))

            unowned = temporary / "unowned"
            unowned.mkdir(mode=0o700)
            with (
                mock.patch.dict(
                    os.environ, {"QUALITY_ARTIFACT_ROOT": str(unowned)}
                ),
                mock.patch.object(
                    gate.os, "geteuid", return_value=os.geteuid() + 1
                ),
            ):
                with self.assertRaises(gate.GateError):
                    gate.Evidence(1)
            self.assertFalse((unowned / "runs").exists())

    def test_evidence_rejects_candidate_and_run_symlinks_before_pruning(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / "artifacts"
            with mock.patch.dict(
                os.environ, {"QUALITY_ARTIFACT_ROOT": str(root)}
            ):
                evidence = gate.Evidence(1)
                target = Path(directory) / "target"
                target.mkdir()
                sentinel = target / "keep.txt"
                sentinel.write_text("keep\n", encoding="utf-8")
                (evidence.run / "dotnet-candidate").symlink_to(
                    target, target_is_directory=True
                )
                with self.assertRaises(gate.GateError):
                    evidence.create_private_directory("dotnet-candidate")
                self.assertTrue(sentinel.exists())
                (evidence.run / "dotnet-candidate").unlink()

                prior_run = (
                    evidence.runs / "20000101T000000.000000Z-2-fedcba"
                )
                prior_run.mkdir(mode=0o700)
                (prior_run / "dotnet-candidate").symlink_to(
                    target, target_is_directory=True
                )
                with self.assertRaises(gate.GateError):
                    evidence.prune()
                self.assertTrue(sentinel.exists())
                (prior_run / "dotnet-candidate").unlink()
                prior_run.rmdir()

                hostile_run = (
                    evidence.runs / "20000101T000000.000000Z-1-abcdef"
                )
                hostile_run.symlink_to(target, target_is_directory=True)
                with self.assertRaises(gate.GateError):
                    evidence.prune()
                self.assertTrue(sentinel.exists())

    def test_started_child_retains_repository_lock_after_gate_is_killed(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            lock_path = root / "gate.lock"
            evidence = root / "evidence"
            evidence.mkdir()
            ready = root / "ready"
            code = (
                "from pathlib import Path\n"
                "from types import SimpleNamespace\n"
                "from quality import gate\n"
                "import sys\n"
                "with gate.RepositoryLock(Path(sys.argv[1])) as lock:\n"
                "  runner = gate.Gate(SimpleNamespace("
                "run=Path(sys.argv[2]), "
                "new_file=lambda name: Path(sys.argv[2]) / name), "
                "lock_fd=lock.fileno())\n"
                "  runner.command('long', [sys.executable, '-c', "
                "\"from pathlib import Path; import sys,time; "
                "Path(sys.argv[1]).write_text('ready'); time.sleep(0.6)\", "
                "sys.argv[3]])\n"
            )
            helper = subprocess.Popen(
                [
                    gate.sys.executable,
                    "-B",
                    "-c",
                    code,
                    str(lock_path),
                    str(evidence),
                    str(ready),
                ],
                cwd=gate.ROOT,
            )
            deadline = time.time() + 3
            while not ready.exists() and time.time() < deadline:
                time.sleep(0.01)
            self.assertTrue(ready.exists())
            helper.kill()
            helper.wait()
            with self.assertRaises(RuntimeError):
                with gate.RepositoryLock(lock_path):
                    pass
            deadline = time.time() + 3
            while True:
                try:
                    with gate.RepositoryLock(lock_path):
                        break
                except RuntimeError:
                    if time.time() >= deadline:
                        self.fail("child did not release inherited repository lock")
                    time.sleep(0.02)


class ResultParserTests(unittest.TestCase):
    def _trx(self, attributes: str) -> Path:
        directory = tempfile.mkdtemp()
        path = Path(directory) / "test.trx"
        path.write_text(
            f'<TestRun xmlns="urn:test"><ResultSummary><Counters {attributes}/>'
            "</ResultSummary></TestRun>",
            encoding="utf-8",
        )
        self.addCleanup(lambda: __import__("shutil").rmtree(directory))
        return path

    def test_trx_accepts_only_nonzero_all_passed(self) -> None:
        passing = self._trx('total="2" executed="2" passed="2"')
        self.assertEqual([], gate.trx_errors(passing))
        for attributes in (
            'total="0" executed="0" passed="0"',
            'total="2" executed="2" passed="1" failed="1"',
            'total="2" executed="1" passed="1" notExecuted="1"',
            'total="1" executed="1" passed="0" inconclusive="1"',
        ):
            with self.subTest(attributes=attributes):
                self.assertTrue(gate.trx_errors(self._trx(attributes)))

    def _coverage(
        self, filename: str, lines: str, package: str = "PaliPractice"
    ) -> Path:
        directory = tempfile.mkdtemp()
        path = Path(directory) / "coverage.cobertura.xml"
        path.write_text(
            f'<coverage><packages><package name="{package}"><classes>'
            f'<class filename="{filename}">'
            f"<lines>{lines}</lines></class></classes></package></packages></coverage>",
            encoding="utf-8",
        )
        self.addCleanup(lambda: __import__("shutil").rmtree(directory))
        return path

    def test_coverage_requires_fresh_first_party_nonempty_lines(self) -> None:
        aliases = {
            "Services/Example.cs": "PaliPractice/PaliPractice/Services/Example.cs"
        }
        valid = self._coverage(
            "Services/Example.cs",
            '<line number="1" hits="1"/><line number="2" hits="0"/>',
        )
        errors, counts = gate.coverage_errors(valid, time.time() - 1, aliases)
        self.assertEqual([], errors)
        self.assertEqual((1, 2), counts)

        foreign = self._coverage("/dependency/Foreign.cs", '<line number="1" hits="1"/>')
        self.assertTrue(gate.coverage_errors(foreign, time.time() - 1, aliases)[0])
        nonexistent = self._coverage("Services/Missing.cs", '<line number="1" hits="1"/>')
        self.assertTrue(
            gate.coverage_errors(nonexistent, time.time() - 1, aliases)[0]
        )
        empty = self._coverage("Services/Example.cs", "")
        self.assertTrue(gate.coverage_errors(empty, time.time() - 1, aliases)[0])
        generated = self._coverage(
            "obj/Generated.g.cs",
            '<line number="1" hits="1"/>',
        )
        self.assertTrue(gate.coverage_errors(generated, time.time() - 1, aliases)[0])
        wrong_assembly = self._coverage(
            "Services/Example.cs", '<line number="1" hits="1"/>', "Other"
        )
        self.assertTrue(
            gate.coverage_errors(wrong_assembly, time.time() - 1, aliases)[0]
        )
        os.utime(valid, (1, 1))
        self.assertIn(
            "stale", gate.coverage_errors(valid, time.time(), aliases)[0][0]
        )

    def test_duplicate_coverage_must_be_byte_identical(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            first = Path(directory) / "first.xml"
            second = Path(directory) / "second.xml"
            first.write_text("<coverage/>", encoding="utf-8")
            second.write_bytes(first.read_bytes())
            selected, errors = gate.select_coverage_report([first, second])
            self.assertEqual([], errors)
            self.assertIsNotNone(selected)
            second.write_text("<coverage changed='yes'/>", encoding="utf-8")
            self.assertTrue(gate.select_coverage_report([first, second])[1])

    def test_candidate_coverage_accepts_observed_run_relative_source_path(
        self,
    ) -> None:
        canonical = "PaliPractice/PaliPractice/Services/Example.cs"
        with tempfile.TemporaryDirectory() as directory:
            candidate_root = Path(directory) / "run" / "dotnet-candidate"
            candidate = gate.DotnetCandidate(
                root=candidate_root,
                dotnet_root=candidate_root / "PaliPractice",
                solution=candidate_root / "PaliPractice/PaliPractice.sln",
                app_project=(
                    candidate_root
                    / "PaliPractice/PaliPractice/PaliPractice.csproj"
                ),
                test_project=(
                    candidate_root
                    / "PaliPractice/PaliPractice.Tests/PaliPractice.Tests.csproj"
                ),
                probes={},
            )
            with mock.patch.object(
                gate,
                "production_source_aliases",
                return_value={canonical: canonical},
            ):
                aliases = gate.candidate_source_aliases(candidate)
        observed = (
            "dotnet-candidate/PaliPractice/PaliPractice/Services/Example.cs"
        )
        self.assertEqual(canonical, aliases[observed])
        report = self._coverage(
            observed,
            '<line number="1" hits="1"/><line number="2" hits="0"/>',
        )
        errors, counts = gate.coverage_errors(
            report,
            time.time() - 1,
            aliases,
        )
        self.assertEqual([], errors)
        self.assertEqual((1, 2), counts)

    def test_complexity_ratchet_allows_old_debt_but_rejects_regression(self) -> None:
        baseline = {("scripts/a.py", "old"): 20, ("scripts/a.py", "worse"): 16}
        current = {
            ("scripts/a.py", "old"): 20,
            ("scripts/a.py", "worse"): 17,
            ("scripts/new.py", "new"): 16,
        }
        errors = gate.complexity_regressions(current, baseline)
        self.assertEqual(2, len(errors))
        self.assertFalse(any(" old " in error for error in errors))

    def test_ca1502_baseline_is_a_maximum(self) -> None:
        baseline = {
            "schema": 1,
            "threshold": 15,
            "anchor": "base",
            "allowed": [
                {"path": "a.cs", "symbol": "Old", "max_complexity": 17}
            ],
        }
        findings = {("a.cs", "Old"): 17, ("b.cs", "New"): 16}
        errors = gate.ca1502_errors(findings, baseline)
        self.assertEqual(1, len(errors))
        self.assertIn("b.cs", errors[0])

    def test_ca1502_policy_pins_metrics_and_liveness_consumes_exact_probes(
        self,
    ) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            config = root / "config"
            config.mkdir()
            metrics = config / "CodeMetricsConfig.txt"
            metrics.write_text("CA1502: 15\n", encoding="utf-8")
            with mock.patch.object(gate, "CONFIG", config):
                self.assertEqual([], gate.ca1502_policy_errors())
                metrics.write_text("CA1502: 99\n", encoding="utf-8")
                self.assertTrue(gate.ca1502_policy_errors())

            probes = {
                ("PaliPractice/App/probe.cs", "Exercise"): 16,
                ("PaliPractice/Tests/probe.cs", "Exercise"): 16,
            }
            findings = {
                *probes.keys(),
            }
            observed = {key: 16 for key in findings}
            observed[("PaliPractice/App/Product.cs", "Complex")] = 18
            remaining, errors = gate.consume_ca1502_probes(observed, probes)
            self.assertEqual([], errors)
            self.assertEqual(
                {("PaliPractice/App/Product.cs", "Complex"): 18},
                remaining,
            )
            del observed[next(iter(probes))]
            self.assertTrue(gate.consume_ca1502_probes(observed, probes)[1])

    def test_ca1502_policy_rejects_local_and_generated_escape_hatches(
        self,
    ) -> None:
        variants = (
            (
                "split suppression",
                '[assembly: SuppressMessage("Maintainability", "CA15" + "02")]\n',
                None,
            ),
            (
                "pragma",
                "#  pragma  warning  disable CA1502\nclass Hidden {}\n",
                None,
            ),
            (
                "generated attribute",
                "[GeneratedCode(\"tool\", \"1\")]\nclass Hidden {}\n",
                None,
            ),
            (
                "auto-generated marker",
                "// <auto-generated/>\nclass Hidden {}\n",
                None,
            ),
            ("generated filename", "class Hidden {}\n", "Hidden.g.cs"),
            (
                "targeted category config",
                "class Hidden {}\n",
                ".editorconfig",
            ),
            (
                "repository-root category config",
                "class Hidden {}\n",
                ".globalconfig",
            ),
        )
        for label, source_text, special_name in variants:
            with self.subTest(label=label), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                dotnet = root / "PaliPractice"
                models = dotnet / "PaliPractice" / "Models"
                models.mkdir(parents=True)
                (dotnet / ".editorconfig").write_text(
                    "dotnet_diagnostic.CA1502.severity = warning\n",
                    encoding="utf-8",
                )
                (models / "Enums.cs").write_text(
                    '[SuppressMessage("ReSharper", "UnusedMember.Global")]\n',
                    encoding="utf-8",
                )
                config = root / "quality" / "config"
                config.mkdir(parents=True)
                (config / "CodeMetricsConfig.txt").write_text(
                    "CA1502: 15\n",
                    encoding="utf-8",
                )
                if special_name == ".editorconfig":
                    nested = dotnet / "PaliPractice" / ".editorconfig"
                    nested.write_text(
                        "[Existing.cs]\n"
                        "dotnet_analyzer_diagnostic."
                        "category-Maintainability.severity = none\n",
                        encoding="utf-8",
                    )
                    (dotnet / "PaliPractice" / "Existing.cs").write_text(
                        source_text,
                        encoding="utf-8",
                    )
                elif special_name == ".globalconfig":
                    (root / ".globalconfig").write_text(
                        "is_global = true\n"
                        "dotnet_analyzer_diagnostic."
                        "category-Maintainability.severity = none\n",
                        encoding="utf-8",
                    )
                    (dotnet / "PaliPractice" / "Existing.cs").write_text(
                        source_text,
                        encoding="utf-8",
                    )
                else:
                    name = special_name or "Hidden.cs"
                    (dotnet / "PaliPractice" / name).write_text(
                        source_text,
                        encoding="utf-8",
                    )
                with mock.patch.object(gate, "CONFIG", config):
                    self.assertTrue(gate.ca1502_policy_errors(root, dotnet))

    def test_dotnet_candidate_is_private_and_does_not_touch_source(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root / "source" / "PaliPractice"
            app = source / "PaliPractice"
            tests = source / "PaliPractice.Tests"
            app.mkdir(parents=True)
            tests.mkdir()
            root_editor = source.parent / ".editorconfig"
            root_editor.write_text("root = true\n", encoding="utf-8")
            root_targets = source.parent / "Directory.Build.targets"
            root_targets.write_text("<Project />\n", encoding="utf-8")
            root_ruleset = source.parent / "quality.ruleset"
            root_ruleset.write_text("<RuleSet />\n", encoding="utf-8")
            original = app / "Existing.cs"
            original.write_text("class Existing {}\n", encoding="utf-8")
            config = root / "config"
            config.mkdir()
            (config / "CodeMetricsConfig.txt").write_text(
                "CA1502: 15\n",
                encoding="utf-8",
            )
            artifacts = root / "artifacts"
            with (
                mock.patch.dict(
                    os.environ, {"QUALITY_ARTIFACT_ROOT": str(artifacts)}
                ),
                mock.patch.object(gate, "ROOT", source.parent),
                mock.patch.object(gate, "DOTNET_ROOT", source),
                mock.patch.object(gate, "CONFIG", config),
            ):
                evidence = gate.Evidence(1)
                candidate = gate.create_dotnet_candidate(evidence)
            self.assertEqual("class Existing {}\n", original.read_text())
            self.assertEqual(2, len(candidate.probes))
            for path, _ in candidate.probes:
                self.assertTrue((candidate.root / path).is_file())
            for path in (root_editor, root_targets, root_ruleset):
                self.assertEqual(
                    path.read_bytes(),
                    (candidate.root / path.name).read_bytes(),
                )
            self.assertEqual(0, candidate.root.stat().st_mode & 0o077)

    def test_invalid_analyzer_configuration_does_not_block_independent_lanes(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            runner = mock.Mock()
            runner.evidence.run = root
            runner.evidence.new_file.side_effect = lambda name: root / name
            runner.ca_logs = []
            runner.check.side_effect = lambda name, errors: not list(errors)
            runner.command.side_effect = [
                SimpleNamespace(returncode=0),
                SimpleNamespace(returncode=1, stdout=root / "test.log", stderr=root / "test.err"),
                SimpleNamespace(returncode=0, stdout=root / "build.log", stderr=root / "build.err"),
            ]
            candidate = SimpleNamespace(
                root=root, dotnet_root=root, solution=root / "app.sln",
                test_project=root / "test.csproj", app_project=root / "app.csproj", probes=[],
            )
            with (
                mock.patch.object(gate, "ca1502_policy_errors", return_value=["disabled analyzer"]),
                mock.patch.object(gate, "trusted_ca1502_baseline", return_value=(None, ["bad anchor"])),
                mock.patch.object(gate, "create_dotnet_candidate", return_value=candidate),
                mock.patch.object(gate, "_dotnet_environment", return_value={}),
                mock.patch.object(gate, "parse_ca1502", return_value={}),
                mock.patch.object(gate, "consume_ca1502_probes", return_value=({}, [])),
            ):
                gate.run_dotnet_checks(runner, True, "base")
            self.assertEqual(
                ["dotnet-restore", "dotnet-test", "dotnet-desktop"],
                [call.args[0] for call in runner.command.call_args_list],
            )
            # Restore and --no-restore consumers must select the same dependency graph.
            for call in runner.command.call_args_list:
                self.assertIn("-p:Configuration=Release", call.args[1])
            runner.check.assert_any_call("ca1502-policy", ["disabled analyzer"])
            runner.check.assert_any_call("ca1502-baseline", ["bad anchor"])
            runner.check.assert_any_call("ca1502", ["Cannot evaluate allowances: analyzer baseline is invalid"])
            self.assertFalse((root / "ca1502-baseline-used.json").exists())

    def test_every_dotnet_command_contract_forces_analyzers(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            runner = SimpleNamespace(
                evidence=SimpleNamespace(run=Path(directory))
            )
            arguments = gate._dotnet_contract_arguments(runner)
        for required in (
            "-p:EnableNETAnalyzers=true",
            "-p:RunAnalyzersDuringBuild=true",
            "-p:AnalysisLevel=latest-recommended",
            "-p:NoWarn=NU1507%3BNETSDK1201%3BPRI257",
        ):
            self.assertIn(required, arguments)

    def test_lizard_csv_without_header_is_parsed(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root / "scripts" / "sample.py"
            source.parent.mkdir()
            source.write_text("def sample(): pass\n", encoding="utf-8")
            report = root / "lizard.csv"
            report.write_text(
                f'1,16,5,0,1,"sample@1-1@{source}","{source}",'
                '"sample","sample( )",1,1\n',
                encoding="utf-8",
            )
            findings = gate.parse_lizard(report, root)
            self.assertEqual(16, findings[("scripts/sample.py", "sample( )")])
            self.assertEqual(
                [],
                gate.lizard_inventory_errors(report, root, [source]),
            )

    def test_lizard_rejects_empty_malformed_and_incomplete_evidence(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            scripts = root / "scripts"
            scripts.mkdir()
            first = scripts / "first.py"
            second = scripts / "second.py"
            first.write_text("def first(): pass\n", encoding="utf-8")
            second.write_text("def second(): pass\n", encoding="utf-8")
            report = root / "lizard.csv"
            report.write_text("", encoding="utf-8")
            with self.assertRaises(gate.GateError):
                gate.parse_lizard(report, root)
            report.write_text("malformed,row\n", encoding="utf-8")
            with self.assertRaises(gate.GateError):
                gate.parse_lizard(report, root)
            report.write_text(
                f'1,1,5,0,1,"first@1-1@{first}","{first}",'
                '"first","first( )",1,1\n',
                encoding="utf-8",
            )
            self.assertTrue(
                gate.lizard_inventory_errors(report, root, [first, second])
            )


class RunnerContractTests(unittest.TestCase):
    def test_positional_profile_is_supported(self) -> None:
        with mock.patch.object(gate.sys, "argv", ["gate.py", "auto"]):
            self.assertEqual("auto", gate._arguments().profile)
        with mock.patch.object(gate.sys, "argv", ["gate.py", "--profile", "fast"]):
            self.assertEqual("fast", gate._arguments().profile)

    def test_external_step_timeout_is_enforced(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            evidence = SimpleNamespace(
                run=Path(directory),
                new_file=lambda name: Path(directory) / name,
            )
            quality_gate = gate.Gate(evidence, step_timeout=0.05)
            with redirect_stdout(io.StringIO()):
                result = quality_gate.command(
                    "timeout",
                    [gate.sys.executable, "-c", "import time; time.sleep(2)"],
                )
            self.assertEqual(124, result.returncode)
            self.assertIn("timed out", result.stderr.read_text(encoding="utf-8"))

    def test_command_failure_surfaces_stdout_error_excerpt(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            evidence = SimpleNamespace(
                run=Path(directory),
                new_file=lambda name: Path(directory) / name,
            )
            quality_gate = gate.Gate(evidence)
            with redirect_stdout(io.StringIO()):
                result = quality_gate.command(
                    "failure",
                    [
                        gate.sys.executable,
                        "-c",
                        "print('sample.cs(1,1): error CS0001: useful detail'); "
                        "raise SystemExit(2)",
                    ],
                )
            self.assertEqual(2, result.returncode)
            self.assertTrue(
                any("useful detail" in failure for failure in quality_gate.failures)
            )

    def test_initial_ca1502_allowance_needs_matching_base_evidence(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            run_git(root, "init", "-q")
            run_git(root, "config", "user.name", "Quality Test")
            run_git(root, "config", "user.email", "quality@example.invalid")
            (root / "a.cs").write_text("base\n", encoding="utf-8")
            run_git(root, "add", ".")
            run_git(root, "commit", "-qm", "base")
            base = run_git(root, "rev-parse", "HEAD")
            current = root / "baseline.json"
            payload = {
                "schema": 1,
                "threshold": 15,
                "anchor": base,
                "allowed": [
                    {"path": "a.cs", "symbol": "Old", "max_complexity": 16}
                ],
            }
            current.write_text(json.dumps(payload), encoding="utf-8")
            with mock.patch.object(gate, "ROOT", root):
                baseline, errors = gate.trusted_ca1502_baseline(base, current)
                self.assertEqual([], errors)
                self.assertIsNotNone(baseline)
                assert baseline is not None
                findings = {("a.cs", "Old"): 16}
                self.assertEqual(
                    [],
                    gate.initial_ca1502_evidence_errors(base, baseline, findings),
                )
                self.assertTrue(
                    gate.initial_ca1502_evidence_errors(base, baseline, {})
                )
                (root / "a.cs").write_text("changed\n", encoding="utf-8")
                self.assertTrue(
                    gate.initial_ca1502_evidence_errors(base, baseline, findings)
                )
                payload["anchor"] = "wrong"
                current.write_text(json.dumps(payload), encoding="utf-8")
                self.assertTrue(gate.trusted_ca1502_baseline(base, current)[1])

    def test_ca1502_current_baseline_must_byte_match_base_blob(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            config = root / "quality" / "config" / "ca1502-baseline.json"
            config.parent.mkdir(parents=True)
            run_git(root, "init", "-q")
            run_git(root, "config", "user.name", "Quality Test")
            run_git(root, "config", "user.email", "quality@example.invalid")
            committed = {
                "schema": 1,
                "threshold": 15,
                "anchor": "trusted-anchor",
                "allowed": [
                    {"path": "a.cs", "symbol": "Old", "max_complexity": 16}
                ],
            }
            config.write_text(json.dumps(committed), encoding="utf-8")
            run_git(root, "add", ".")
            run_git(root, "commit", "-qm", "baseline")
            base = run_git(root, "rev-parse", "HEAD")
            with mock.patch.object(gate, "ROOT", root):
                baseline, errors = gate.trusted_ca1502_baseline(base, config)
            self.assertEqual([], errors)
            self.assertEqual(committed["allowed"], baseline["allowed"])

            widened = dict(committed)
            widened["allowed"] = [
                *committed["allowed"],
                {"path": "b.cs", "symbol": "New", "max_complexity": 99},
            ]
            config.write_text(json.dumps(widened), encoding="utf-8")
            with mock.patch.object(gate, "ROOT", root):
                baseline, errors = gate.trusted_ca1502_baseline(base, config)
            self.assertIsNone(baseline)
            self.assertTrue(errors)

            tampered_anchor = dict(committed)
            tampered_anchor["anchor"] = "replacement"
            config.write_text(json.dumps(tampered_anchor), encoding="utf-8")
            with mock.patch.object(gate, "ROOT", root):
                baseline, errors = gate.trusted_ca1502_baseline(base, config)
            self.assertIsNone(baseline)
            self.assertTrue(errors)

            config.unlink()
            with mock.patch.object(gate, "ROOT", root):
                baseline, errors = gate.trusted_ca1502_baseline(base, config)
            self.assertIsNone(baseline)
            self.assertTrue(errors)


if __name__ == "__main__":
    unittest.main()

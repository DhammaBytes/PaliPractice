#!/usr/bin/env python3
"""Deterministic, repository-owned quality gate for PaliPractice."""

from __future__ import annotations

import argparse
import ast
from collections import Counter
import csv
import fcntl
import hashlib
import json
import os
import re
import secrets
import shutil
import stat
import subprocess
import sys
import tempfile
import time
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from datetime import UTC, datetime
from pathlib import Path
from typing import Iterable, Sequence

sys.dont_write_bytecode = True

ROOT = Path(__file__).resolve().parents[1]
DOTNET_ROOT = ROOT / "PaliPractice"
SOLUTION = DOTNET_ROOT / "PaliPractice.sln"
APP_PROJECT = DOTNET_ROOT / "PaliPractice" / "PaliPractice.csproj"
TEST_PROJECT = DOTNET_ROOT / "PaliPractice.Tests" / "PaliPractice.Tests.csproj"
APP_SOURCE = APP_PROJECT.parent
CONFIG = ROOT / "quality" / "config"
ALL_GROUPS = {"quality", "python", "data", "submodule", "dotnet", "desktop"}
GIT_TIMEOUT_SECONDS = 60
REPOSITORY_DIGEST = hashlib.sha256(str(ROOT).encode()).hexdigest()[:12]
RUN_NAME = re.compile(r"^\d{8}T\d{6}\.\d{6}Z-\d+-[0-9a-f]{6}$")
LOCK_ROOT = Path(tempfile.gettempdir()) / "agentic-quality-repository-locks"
CACHE_NAMES = ("nuget", "uv", "uv-python", "uv-tools")


class GateError(RuntimeError):
    pass


def _require_owned_directory(path: Path, label: str) -> os.stat_result:
    try:
        metadata = path.lstat()
    except OSError as error:
        raise GateError(f"{label} is unavailable: {path}: {error}") from error
    if (
        not stat.S_ISDIR(metadata.st_mode)
        or stat.S_ISLNK(metadata.st_mode)
        or metadata.st_uid != os.geteuid()
    ):
        raise GateError(
            f"{label} must be a current-user directory "
            f"(owner={metadata.st_uid}): {path}"
        )
    return metadata


def _require_private_directory(path: Path, label: str) -> None:
    metadata = _require_owned_directory(path, label)
    mode = stat.S_IMODE(metadata.st_mode)
    if mode & 0o077:
        raise GateError(f"{label} is not private (mode={mode:#o}): {path}")


def _harden_private_directory(path: Path, label: str) -> None:
    metadata = _require_owned_directory(path, label)
    if stat.S_IMODE(metadata.st_mode) != 0o700:
        os.chmod(path, 0o700)
    _require_private_directory(path, label)


def _require_direct_child(path: Path, parent: Path, label: str) -> None:
    resolved_parent = parent.resolve()
    resolved = path.resolve(strict=False)
    if path.parent != parent or resolved.parent != resolved_parent:
        raise GateError(f"{label} escaped {parent}: {path} -> {resolved}")


def _create_private_directory(
    path: Path, label: str, parent: Path | None = None
) -> None:
    if parent is not None:
        _require_direct_child(path, parent, label)
    try:
        path.mkdir(mode=0o700)
    except FileExistsError as error:
        raise GateError(f"{label} already exists: {path}") from error
    except OSError as error:
        raise GateError(f"cannot create {label}: {path}: {error}") from error
    _require_private_directory(path, label)
    if parent is not None:
        _require_direct_child(path, parent, label)


def _is_regular_file(path: Path) -> bool:
    try:
        return stat.S_ISREG(path.lstat().st_mode)
    except OSError:
        return False


if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from quality.checks.data_contract import validate_data  # noqa: E402


@dataclass(frozen=True)
class Change:
    source: str
    status: str
    path: str
    old_path: str | None = None

    @property
    def paths(self) -> tuple[str, ...]:
        return (self.old_path, self.path) if self.old_path else (self.path,)


@dataclass
class CommandResult:
    returncode: int
    stdout: Path
    stderr: Path


@dataclass(frozen=True)
class DotnetCandidate:
    root: Path
    dotnet_root: Path
    solution: Path
    app_project: Path
    test_project: Path
    probes: dict[tuple[str, str], int]


class Evidence:
    def __init__(self, keep_previous: int) -> None:
        default = (
            Path(tempfile.gettempdir())
            / f"pali-practice-quality-{REPOSITORY_DIGEST}"
        )
        requested = Path(os.environ.get("QUALITY_ARTIFACT_ROOT", default)).expanduser()
        if requested.is_symlink():
            raise GateError(f"evidence base must not be a symlink: {requested}")
        self.base = requested.resolve()
        repository = ROOT.resolve()
        if self.base == repository or self.base.is_relative_to(repository):
            raise GateError("QUALITY_ARTIFACT_ROOT must be outside the repository")
        self.runs = self.base / "runs"
        self.tool_cache = self.base / "tool-cache"

        # Inspect every pre-existing gate-owned directory before creating any
        # missing child. This prevents a hostile runs/tool-cache symlink from
        # receiving writes during initialization.
        existing: list[tuple[Path, str]] = []
        if self.base.exists():
            _require_owned_directory(self.base, "evidence base")
            existing.append((self.base, "evidence base"))
            for path, label in (
                (self.runs, "evidence runs"),
                (self.tool_cache, "evidence tool cache"),
            ):
                if path.exists() or path.is_symlink():
                    _require_direct_child(path, self.base, label)
                    _require_owned_directory(path, label)
                    existing.append((path, label))
            if self.runs.exists():
                existing.extend(self._owned_run_directories())
            if self.tool_cache.exists():
                existing.extend(self._owned_cache_directories())
            for path, label in existing:
                _harden_private_directory(path, label)
        else:
            self.base.mkdir(mode=0o700, parents=True)
            _require_private_directory(self.base, "evidence base")

        for path, label in (
            (self.runs, "evidence runs"),
            (self.tool_cache, "evidence tool cache"),
        ):
            if not path.exists():
                _create_private_directory(path, label, self.base)
        for name in CACHE_NAMES:
            path = self.tool_cache / name
            if not path.exists():
                _create_private_directory(
                    path,
                    f"evidence tool cache {name}",
                    self.tool_cache,
                )

        stamp = datetime.now(UTC).strftime("%Y%m%dT%H%M%S.%fZ")
        self.run = self.runs / f"{stamp}-{os.getpid()}-{secrets.token_hex(3)}"
        _create_private_directory(self.run, "current evidence run", self.runs)
        self.keep_previous = keep_previous

    def _validate_run_directories(self) -> None:
        _require_private_directory(self.runs, "evidence runs")
        for path in self.runs.iterdir():
            if not RUN_NAME.fullmatch(path.name):
                continue
            _require_direct_child(path, self.runs, f"evidence run {path.name}")
            _require_private_directory(path, f"evidence run {path.name}")
            candidate = path / "dotnet-candidate"
            if candidate.exists() or candidate.is_symlink():
                _require_direct_child(candidate, path, "dotnet candidate")
                _require_private_directory(candidate, "dotnet candidate")

    def _owned_run_directories(self) -> list[tuple[Path, str]]:
        owned: list[tuple[Path, str]] = []
        for path in self.runs.iterdir():
            if not RUN_NAME.fullmatch(path.name):
                continue
            label = f"evidence run {path.name}"
            _require_direct_child(path, self.runs, label)
            _require_owned_directory(path, label)
            owned.append((path, label))
            candidate = path / "dotnet-candidate"
            if candidate.exists() or candidate.is_symlink():
                candidate_label = f"dotnet candidate in {path.name}"
                _require_direct_child(candidate, path, candidate_label)
                _require_owned_directory(candidate, candidate_label)
                owned.append((candidate, candidate_label))
        return owned

    def _validate_cache_directories(self) -> None:
        _require_private_directory(self.tool_cache, "evidence tool cache")
        for name in CACHE_NAMES:
            path = self.tool_cache / name
            if path.exists() or path.is_symlink():
                _require_direct_child(
                    path, self.tool_cache, f"evidence tool cache {name}"
                )
                _require_private_directory(path, f"evidence tool cache {name}")

    def _owned_cache_directories(self) -> list[tuple[Path, str]]:
        owned: list[tuple[Path, str]] = []
        for name in CACHE_NAMES:
            path = self.tool_cache / name
            if path.exists() or path.is_symlink():
                label = f"evidence tool cache {name}"
                _require_direct_child(path, self.tool_cache, label)
                _require_owned_directory(path, label)
                owned.append((path, label))
        return owned

    def create_private_directory(self, name: str) -> Path:
        if not name or Path(name).name != name:
            raise GateError(f"invalid private evidence directory name: {name!r}")
        self.validate()
        path = self.run / name
        _create_private_directory(
            path,
            f"evidence directory {name}",
            self.run,
        )
        return path

    def new_file(self, name: str) -> Path:
        if not name or Path(name).name != name:
            raise GateError(f"invalid evidence file name: {name!r}")
        self.validate()
        path = self.run / name
        _require_direct_child(path, self.run, f"evidence file {name}")
        if path.exists() or path.is_symlink():
            raise GateError(f"evidence file already exists: {path}")
        return path

    def validate(self) -> None:
        for path, label in (
            (self.base, "evidence base"),
            (self.runs, "evidence runs"),
            (self.tool_cache, "evidence tool cache"),
            (self.run, "current evidence run"),
        ):
            _require_private_directory(path, label)
        _require_direct_child(self.runs, self.base, "evidence runs")
        _require_direct_child(self.tool_cache, self.base, "evidence tool cache")
        _require_direct_child(self.run, self.runs, "current evidence run")
        if self.run.parent != self.runs or not RUN_NAME.fullmatch(self.run.name):
            raise GateError("current evidence run escaped its private runs directory")
        self._validate_run_directories()
        self._validate_cache_directories()
        candidate = self.run / "dotnet-candidate"
        if candidate.exists() or candidate.is_symlink():
            _require_direct_child(candidate, self.run, "dotnet candidate")
            _require_private_directory(candidate, "dotnet candidate")

    def prune(self) -> None:
        self.validate()
        candidates = sorted(
            (
                path
                for path in self.runs.iterdir()
                if RUN_NAME.fullmatch(path.name)
            ),
            key=lambda path: path.name,
            reverse=True,
        )
        allowed = {self.run}
        allowed.update(
            path
            for path in candidates
            if path != self.run
            and _is_regular_file(path / "completion.json")
            and len(allowed) < self.keep_previous + 1
        )
        for stale in candidates:
            if stale in allowed:
                continue
            _require_private_directory(stale, f"stale evidence run {stale.name}")
            if stale.parent != self.runs or not RUN_NAME.fullmatch(stale.name):
                raise GateError(f"refusing to prune escaped evidence path: {stale}")
            shutil.rmtree(stale)


class Gate:
    def __init__(
        self,
        evidence: Evidence,
        step_timeout: float = 1_200,
        lock_fd: int | None = None,
    ) -> None:
        self.evidence = evidence
        self.step_timeout = step_timeout
        self.lock_fd = lock_fd
        self.failures: list[str] = []
        self.ca_logs: list[Path] = []

    def check(self, name: str, errors: Iterable[str]) -> bool:
        messages = list(errors)
        if messages:
            self.failures.extend(f"{name}: {message}" for message in messages)
            print(f"FAIL {name}")
            for message in messages[:8]:
                print(f"  {message}")
            if len(messages) > 8:
                print(f"  ... {len(messages) - 8} more (see evidence)")
            return False
        print(f"PASS {name}")
        return True

    def command(
        self,
        name: str,
        arguments: Sequence[str],
        *,
        cwd: Path = ROOT,
        environment: dict[str, str] | None = None,
        ok_codes: set[int] | None = None,
        timeout: float | None = None,
    ) -> CommandResult:
        stdout_path = self.evidence.new_file(f"{name}.stdout.log")
        stderr_path = self.evidence.new_file(f"{name}.stderr.log")
        command_path = self.evidence.new_file(f"{name}.command.json")
        env = os.environ.copy()
        env.update(environment or {})
        command_path.write_text(
            json.dumps({"cwd": str(cwd), "argv": list(arguments)}, indent=2),
            encoding="utf-8",
        )
        try:
            with stdout_path.open("wb") as stdout, stderr_path.open("wb") as stderr:
                completed = subprocess.run(
                    arguments,
                    cwd=cwd,
                    env=env,
                    stdout=stdout,
                    stderr=stderr,
                    check=False,
                    pass_fds=(self.lock_fd,) if self.lock_fd is not None else (),
                    timeout=timeout or self.step_timeout,
                )
            code = completed.returncode
        except subprocess.TimeoutExpired:
            with stderr_path.open("ab") as stderr:
                stderr.write(
                    f"\ncommand timed out after {timeout or self.step_timeout:g}s\n".encode()
                )
            code = 124
        except FileNotFoundError as error:
            stderr_path.write_text(str(error), encoding="utf-8")
            code = 127
        allowed = ok_codes if ok_codes is not None else {0}
        if code not in allowed:
            details = [
                f"command exited {code}; logs: {stdout_path}, {stderr_path}",
                *self._failure_excerpt(stdout_path, stderr_path),
            ]
            self.check(name, details)
        return CommandResult(code, stdout_path, stderr_path)

    @staticmethod
    def _failure_excerpt(stdout_path: Path, stderr_path: Path) -> list[str]:
        """Return a few useful failure lines without dumping a build log."""
        fallback: list[str] = []
        for path in (stderr_path, stdout_path):
            try:
                lines = [
                    line.strip()
                    for line in path.read_text(
                        encoding="utf-8", errors="replace"
                    ).splitlines()
                    if line.strip()
                ]
            except OSError:
                continue
            if not fallback and lines:
                fallback = lines[-3:]
            matches = [
                line
                for line in lines
                if re.search(
                    r"(?i)(?::\s|^)(?:error|fatal)\b|"
                    r"\b(?:exception|failed|timed out)\b",
                    line,
                )
            ]
            if matches:
                return matches[:3]
        return fallback


class RepositoryLock:
    def __init__(self, path: Path) -> None:
        self.path = path
        self.file = None

    def __enter__(self) -> "RepositoryLock":
        self.path.parent.mkdir(mode=0o700, parents=True, exist_ok=True)
        _require_owned_directory(
            self.path.parent,
            "repository lock directory",
        )
        _require_direct_child(
            self.path,
            self.path.parent,
            "repository lock file",
        )
        if self.path.exists() or self.path.is_symlink():
            existing = self.path.lstat()
            if (
                not stat.S_ISREG(existing.st_mode)
                or existing.st_uid != os.geteuid()
                or existing.st_nlink != 1
            ):
                raise GateError(
                    f"repository lock is not a private regular file: {self.path}"
                )
        _harden_private_directory(
            self.path.parent,
            "repository lock directory",
        )
        if not hasattr(os, "O_NOFOLLOW"):
            raise GateError("repository locks require O_NOFOLLOW support")
        flags = (
            os.O_RDWR
            | os.O_CREAT
            | getattr(os, "O_CLOEXEC", 0)
            | os.O_NOFOLLOW
        )
        try:
            descriptor = os.open(self.path, flags, 0o600)
        except OSError as error:
            raise GateError(f"cannot securely open repository lock: {error}") from error
        descriptor_open = True
        try:
            opened = os.fstat(descriptor)
            linked = self.path.lstat()
            if (
                not stat.S_ISREG(opened.st_mode)
                or opened.st_uid != os.geteuid()
                or opened.st_nlink != 1
                or (opened.st_dev, opened.st_ino)
                != (linked.st_dev, linked.st_ino)
            ):
                raise GateError(
                    f"repository lock identity is not trustworthy: {self.path}"
                )
            os.fchmod(descriptor, 0o600)
            self.file = os.fdopen(descriptor, "r+", encoding="utf-8")
            descriptor_open = False
        except BaseException:
            if descriptor_open:
                os.close(descriptor)
            raise
        try:
            fcntl.flock(self.file.fileno(), fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError as error:
            self.file.seek(0)
            owner = self.file.read().strip() or "unknown owner"
            self.file.close()
            self.file = None
            raise RuntimeError(f"quality gate is already running ({owner})") from error
        self.file.seek(0)
        self.file.truncate()
        json.dump(
            {
                "pid": os.getpid(),
                "repository": str(ROOT),
                "started_utc": datetime.now(UTC).isoformat(),
            },
            self.file,
        )
        self.file.flush()
        return self

    def __exit__(self, *_: object) -> None:
        if self.file is not None:
            fcntl.flock(self.file.fileno(), fcntl.LOCK_UN)
            self.file.close()

    def fileno(self) -> int:
        if self.file is None:
            raise RuntimeError("repository lock is not held")
        return self.file.fileno()


def git_bytes(arguments: Sequence[str], cwd: Path | None = None) -> bytes:
    cwd = cwd or ROOT
    try:
        completed = subprocess.run(
            ["git", *arguments],
            cwd=cwd,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            check=False,
            timeout=GIT_TIMEOUT_SECONDS,
        )
    except subprocess.TimeoutExpired as error:
        raise RuntimeError(
            f"git {' '.join(arguments)} timed out after {GIT_TIMEOUT_SECONDS}s"
        ) from error
    if completed.returncode:
        detail = completed.stderr.decode(errors="replace").strip()
        raise RuntimeError(f"git {' '.join(arguments)} failed: {detail}")
    return completed.stdout


def resolve_base(requested: str | None) -> str:
    candidate = requested or os.environ.get("QUALITY_BASE")
    if candidate is None:
        try:
            upstream = subprocess.run(
                [
                    "git",
                    "rev-parse",
                    "--abbrev-ref",
                    "--symbolic-full-name",
                    "@{upstream}",
                ],
                cwd=ROOT,
                stdout=subprocess.PIPE,
                stderr=subprocess.DEVNULL,
                text=True,
                check=False,
                timeout=GIT_TIMEOUT_SECONDS,
            )
        except subprocess.TimeoutExpired as error:
            raise RuntimeError("upstream discovery timed out") from error
        if upstream.returncode == 0:
            candidate = git_bytes(
                ["merge-base", "HEAD", upstream.stdout.strip()]
            ).decode().strip()
        else:
            candidate = "HEAD^"
    return git_bytes(["rev-parse", "--verify", f"{candidate}^{{commit}}"]).decode().strip()


def _parse_name_status(payload: bytes, source: str) -> list[Change]:
    fields = payload.decode("utf-8", errors="surrogateescape").split("\0")
    fields = fields[:-1] if fields and fields[-1] == "" else fields
    changes: list[Change] = []
    index = 0
    while index < len(fields):
        status = fields[index]
        index += 1
        if status[:1] in {"R", "C"}:
            old_path, path = fields[index : index + 2]
            index += 2
            changes.append(Change(source, status, path, old_path))
        else:
            changes.append(Change(source, status, fields[index]))
            index += 1
    return changes


def discover_changes(base: str) -> list[Change]:
    common = ["--name-status", "-z", "--find-renames"]
    changes = _parse_name_status(
        git_bytes(["diff", *common, base, "HEAD"]), "committed"
    )
    changes += _parse_name_status(git_bytes(["diff", "--cached", *common]), "staged")
    changes += _parse_name_status(git_bytes(["diff", *common]), "unstaged")
    untracked = git_bytes(["ls-files", "--others", "--exclude-standard", "-z"])
    for path in untracked.decode("utf-8", errors="surrogateescape").split("\0"):
        if path:
            changes.append(Change("untracked", "A", path))
    return changes


def route_path(path: str) -> set[str]:
    if path == "QUALITY-TODO.md":
        return {"quality"}
    normalized = path.replace("\\", "/").lstrip("./")
    if normalized == "AGENTS.md" or normalized.startswith("quality/"):
        return set(ALL_GROUPS)
    if normalized in {
        "PaliPractice/global.json",
        "PaliPractice/Directory.Build.props",
        "PaliPractice/.editorconfig",
        "PaliPractice/Directory.Packages.props",
    }:
        return {"quality", "dotnet", "desktop"}
    if normalized == ".gitmodules" or normalized == "dpd-db":
        return {"data", "submodule", "dotnet", "desktop"}
    if normalized.startswith("dpd-db/"):
        return {"data", "submodule", "dotnet", "desktop"}
    if normalized.startswith("scripts/"):
        return {"python", "data", "submodule", "dotnet"}
    if normalized.startswith("PaliPractice/PaliPractice/Data/"):
        return {"data", "submodule", "dotnet", "desktop"}
    if normalized.startswith("PaliPractice/PaliPractice.Tests/"):
        return {"dotnet"}
    if normalized.startswith("PaliPractice/PaliPractice/"):
        return {"dotnet", "desktop"}
    return set(ALL_GROUPS)


def route_changes(changes: Iterable[Change]) -> set[str]:
    groups: set[str] = {"quality"}
    for change in changes:
        for path in change.paths:
            groups.update(route_path(path))
    return groups


def _submodule_rows() -> list[tuple[str, str]]:
    output = git_bytes(["submodule", "status", "--recursive"]).decode(
        errors="surrogateescape"
    )
    rows: list[tuple[str, str]] = []
    pattern = re.compile(r"^(.)([0-9a-f]+) (.*?)(?: \(.+\))?$")
    for line in output.splitlines():
        match = pattern.match(line)
        if match:
            rows.append((match.group(1), match.group(3)))
    return rows


def submodule_errors() -> list[str]:
    errors: list[str] = []
    for marker, path in _submodule_rows():
        if marker in {"-", "+", "U"}:
            errors.append(f"{path}: gitlink state marker {marker!r}")
        location = ROOT / path
        if not location.is_dir():
            errors.append(f"{path}: working tree is missing")
            continue
        status = git_bytes(
            ["status", "--porcelain=v1", "-z", "--untracked-files=all"], location
        )
        if status:
            entries = status.decode(errors="replace").split("\0")
            summary = ", ".join(entry for entry in entries if entry)[:500]
            errors.append(f"{path}: dirty working tree ({summary})")
    return errors


def _hash_file(hasher: "hashlib._Hash", path: Path) -> None:
    hasher.update(str(path).encode("utf-8", errors="surrogateescape"))
    if path.is_symlink():
        hasher.update(b"link\0" + os.readlink(path).encode(errors="surrogateescape"))
        return
    with path.open("rb") as stream:
        while chunk := stream.read(1024 * 1024):
            hasher.update(chunk)


def worktree_fingerprint() -> str:
    hasher = hashlib.sha256()
    repositories = [ROOT]
    repositories.extend(ROOT / path for _, path in _submodule_rows())
    for repository in repositories:
        hasher.update(str(repository.relative_to(ROOT)).encode())
        for arguments in (
            ["rev-parse", "HEAD"],
            ["ls-files", "-s", "-z"],
            ["diff", "--binary", "--no-ext-diff"],
            ["diff", "--cached", "--binary", "--no-ext-diff"],
            ["status", "--porcelain=v1", "-z", "--untracked-files=all"],
        ):
            hasher.update(git_bytes(arguments, repository))
        untracked = git_bytes(
            ["ls-files", "--others", "--exclude-standard", "-z"], repository
        )
        for raw in untracked.split(b"\0"):
            if raw:
                _hash_file(
                    hasher,
                    repository / raw.decode("utf-8", errors="surrogateescape"),
                )
    return hasher.hexdigest()


def _materialize_base(base: str, destination: Path) -> list[Path]:
    payload = git_bytes(["ls-tree", "-r", "-z", "--name-only", base, "--", "scripts"])
    files: list[Path] = []
    for raw in payload.split(b"\0"):
        if not raw:
            continue
        logical = raw.decode("utf-8", errors="surrogateescape")
        if not logical.endswith(".py"):
            continue
        target = destination / logical
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(git_bytes(["show", f"{base}:{logical}"]))
        files.append(target)
    return files


def parse_lizard(path: Path, source_root: Path) -> dict[tuple[str, str], int]:
    findings: dict[tuple[str, str], int] = {}
    fields = (
        "NLOC",
        "CCN",
        "token",
        "PARAM",
        "length",
        "location",
        "file",
        "function",
        "long_name",
        "start",
        "end",
    )
    rows = 0
    with path.open(encoding="utf-8", errors="replace", newline="") as stream:
        for values in csv.reader(stream):
            if not values:
                continue
            if values[0] == "NLOC":
                continue
            if len(values) < len(fields):
                raise GateError(
                    f"Lizard CSV row has {len(values)} fields, expected {len(fields)}"
                )
            row = dict(zip(fields, values, strict=False))
            filename = row.get("file", "")
            if not filename.endswith(".py"):
                raise GateError(f"Lizard reported a non-Python source: {filename!r}")
            candidate = Path(filename)
            try:
                logical = candidate.resolve().relative_to(source_root.resolve()).as_posix()
            except (OSError, ValueError):
                raise GateError(
                    f"Lizard reported source outside analysis root: {filename!r}"
                )
            symbol = row.get("long_name") or row.get("function") or "<module>"
            try:
                complexity = int(float(row.get("CCN", "0")))
            except ValueError:
                raise GateError(f"Lizard reported invalid CCN for {logical}: {row!r}")
            key = (logical, symbol)
            findings[key] = max(findings.get(key, 0), complexity)
            rows += 1
    if rows == 0:
        raise GateError("Lizard produced no function evidence")
    return findings


def lizard_inventory_errors(
    report: Path, source_root: Path, source_files: Iterable[Path]
) -> list[str]:
    expected: Counter[str] = Counter()
    errors: list[str] = []
    for path in source_files:
        try:
            tree = ast.parse(path.read_text(encoding="utf-8"), filename=str(path))
        except (OSError, UnicodeError, SyntaxError) as error:
            errors.append(f"cannot inventory Python functions in {path}: {error}")
            continue
        count = sum(
            isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef))
            for node in ast.walk(tree)
        )
        if count:
            expected[path.resolve().relative_to(source_root.resolve()).as_posix()] = count

    observed: Counter[str] = Counter()
    with report.open(encoding="utf-8", errors="replace", newline="") as stream:
        for values in csv.reader(stream):
            if not values or values[0] == "NLOC":
                continue
            if len(values) < 11:
                continue
            candidate = Path(values[6]).resolve()
            try:
                logical = candidate.relative_to(source_root.resolve()).as_posix()
            except ValueError:
                continue
            observed[logical] += 1
    for logical, count in sorted(expected.items()):
        if observed[logical] < count:
            errors.append(
                f"Lizard inventory for {logical} has {observed[logical]}/{count} functions"
            )
    return errors


def complexity_regressions(
    current: dict[tuple[str, str], int],
    baseline: dict[tuple[str, str], int],
    threshold: int = 15,
) -> list[str]:
    return [
        f"{path}: {symbol} CCN {complexity} "
        f"(base {baseline.get((path, symbol), 0)}, limit {threshold})"
        for (path, symbol), complexity in sorted(current.items())
        if complexity > threshold and complexity > baseline.get((path, symbol), 0)
    ]


def run_python_checks(gate: Gate, base: str) -> None:
    allow_network = os.environ.get("QUALITY_ALLOW_NETWORK") == "1"
    environment = {
        "UV_CACHE_DIR": str(gate.evidence.tool_cache / "uv"),
        "UV_TOOL_DIR": str(gate.evidence.tool_cache / "uv-tools"),
        "UV_PYTHON_INSTALL_DIR": str(gate.evidence.tool_cache / "uv-python"),
        "UV_NO_PROGRESS": "1",
        "UV_LINK_MODE": "copy",
    }
    if not allow_network:
        environment["UV_OFFLINE"] = "1"
    uv_mode = [] if allow_network else ["--offline"]
    ruff_files = sorted(
        path
        for directory in (ROOT / "scripts", ROOT / "quality")
        for path in directory.rglob("*.py")
    )
    lizard_files = sorted((ROOT / "scripts").rglob("*.py"))
    ruff = gate.command(
        "ruff",
        [
            "uvx",
            *uv_mode,
            "--from",
            "ruff==0.15.22",
            "ruff",
            "check",
            "--no-cache",
            "--select",
            "E9,F63,F7,F82",
            *map(str, ruff_files),
        ],
        environment=environment,
    )
    if ruff.returncode == 0:
        gate.check("ruff", [])

    current = gate.command(
        "lizard-current",
        [
            "uvx",
            *uv_mode,
            "--from",
            "lizard==1.23.0",
            "lizard",
            "--csv",
            "-l",
            "python",
            "-C",
            "15",
            *map(str, lizard_files),
        ],
        environment=environment,
        ok_codes={0, 1},
    )
    base_root = gate.evidence.create_private_directory("base-source")
    base_files = _materialize_base(base, base_root)
    baseline: dict[tuple[str, str], int] = {}
    base_result = None
    if base_files:
        base_result = gate.command(
            "lizard-base",
            [
                "uvx",
                *uv_mode,
                "--from",
                "lizard==1.23.0",
                "lizard",
                "--csv",
                "-l",
                "python",
                "-C",
                "15",
                *map(str, base_files),
            ],
            environment=environment,
            ok_codes={0, 1},
        )
    tool_errors = []
    if current.returncode not in {0, 1}:
        tool_errors.append(f"current analysis exited {current.returncode}")
    if base_result is not None and base_result.returncode not in {0, 1}:
        tool_errors.append(f"base analysis exited {base_result.returncode}")
    if tool_errors:
        gate.check("lizard", tool_errors)
        return
    try:
        current_findings = parse_lizard(current.stdout, ROOT)
        tool_errors.extend(
            lizard_inventory_errors(current.stdout, ROOT, lizard_files)
        )
        if base_result is not None:
            baseline = parse_lizard(base_result.stdout, base_root)
    except (OSError, GateError) as error:
        tool_errors.append(str(error))
    if tool_errors:
        gate.check("lizard", tool_errors)
        return
    gate.check("lizard", complexity_regressions(current_findings, baseline))


def trx_errors(path: Path) -> list[str]:
    try:
        root = ET.parse(path).getroot()
    except (OSError, ET.ParseError) as error:
        return [f"invalid TRX: {error}"]
    counters = root.find(".//{*}Counters")
    if counters is None:
        return ["TRX has no result counters"]

    def number(name: str) -> int:
        try:
            return int(counters.attrib.get(name, "0"))
        except ValueError:
            return -1

    total, executed, passed = number("total"), number("executed"), number("passed")
    errors = []
    if total <= 0 or executed <= 0:
        errors.append(f"test execution must be nonzero (total={total}, executed={executed})")
    rejected = {
        name: number(name)
        for name in (
            "failed",
            "error",
            "timeout",
            "aborted",
            "inconclusive",
            "notExecuted",
            "disconnected",
            "warning",
        )
        if number(name) != 0
    }
    if rejected:
        errors.append(f"non-passing test outcomes: {rejected}")
    if passed != executed or executed != total:
        errors.append(f"expected passed=executed=total, got {passed}/{executed}/{total}")
    return errors


def production_source_aliases() -> dict[str, str]:
    relative_root = APP_SOURCE.relative_to(ROOT).as_posix()
    payloads = (
        git_bytes(["ls-files", "-z", "--", relative_root]),
        git_bytes(
            [
                "ls-files",
                "--others",
                "--exclude-standard",
                "-z",
                "--",
                relative_root,
            ]
        ),
    )
    aliases: dict[str, str] = {}
    for payload in payloads:
        for raw in payload.split(b"\0"):
            if not raw:
                continue
            relative = raw.decode("utf-8", errors="surrogateescape")
            path = ROOT / relative
            lowered = relative.lower()
            if (
                path.suffix.lower() != ".cs"
                or "/obj/" in f"/{lowered}/"
                or "/generated/" in f"/{lowered}/"
                or lowered.endswith((".g.cs", ".g.i.cs", ".generated.cs"))
            ):
                continue
            canonical = Path(relative).as_posix()
            for alias in (
                canonical,
                path.relative_to(DOTNET_ROOT).as_posix(),
                path.relative_to(APP_SOURCE).as_posix(),
                str(path.resolve()).replace("\\", "/").lstrip("/"),
            ):
                aliases[alias.lstrip("/")] = canonical
    return aliases


def select_coverage_report(paths: Iterable[Path]) -> tuple[Path | None, list[str]]:
    reports = sorted(path for path in paths if path.is_file())
    if not reports:
        return None, ["fresh coverage report is missing"]
    digests: dict[str, list[Path]] = {}
    for report in reports:
        digest = hashlib.sha256(report.read_bytes()).hexdigest()
        digests.setdefault(digest, []).append(report)
    if len(digests) != 1:
        return None, [
            f"coverage produced {len(reports)} non-identical reports; "
            "assembly evidence is ambiguous"
        ]
    return reports[0], []


def coverage_errors(
    path: Path,
    started: float,
    source_aliases: dict[str, str] | None = None,
) -> tuple[list[str], tuple[int, int]]:
    if not path.is_file():
        return [f"coverage report is missing: {path}"], (0, 0)
    if path.stat().st_mtime + 1 < started:
        return ["coverage report is stale"], (0, 0)
    try:
        root = ET.parse(path).getroot()
    except ET.ParseError as error:
        return [f"invalid coverage XML: {error}"], (0, 0)
    packages = root.findall(".//package")
    production_packages = [
        package for package in packages if package.attrib.get("name") == "PaliPractice"
    ]
    errors = []
    if len(production_packages) != 1 or len(packages) != 1:
        errors.append(
            "coverage must contain exactly one package named PaliPractice "
            f"(packages={[package.attrib.get('name') for package in packages]!r})"
        )
        return errors, (0, 0)
    aliases = source_aliases if source_aliases is not None else production_source_aliases()
    line_hits: dict[tuple[str, str], int] = {}
    first_party_classes = 0
    for element in production_packages[0].findall(".//class"):
        filename = element.attrib.get("filename", "").replace("\\", "/").lstrip("/")
        canonical = aliases.get(filename)
        if canonical is None:
            continue
        first_party_classes += 1
        for line in element.findall("./lines/line"):
            try:
                hits = int(line.attrib.get("hits", "0"))
            except ValueError:
                return ["coverage contains a non-integer hit count"], (0, 0)
            key = (canonical, line.attrib.get("number", ""))
            line_hits[key] = max(line_hits.get(key, 0), hits)
    total = len(line_hits)
    covered = sum(hits > 0 for hits in line_hits.values())
    if first_party_classes == 0:
        errors.append("coverage contains no PaliPractice first-party classes")
    if total == 0:
        errors.append("coverage contains no first-party executable lines")
    return errors, (covered, total)


def _copy_candidate_tree(source: Path, destination: Path) -> None:
    skipped_directories = {
        ".git",
        ".idea",
        ".run",
        ".vs",
        "TestResults",
        "artifacts",
        "bin",
        "obj",
    }
    _create_private_directory(
        destination,
        f"candidate source {source.name}",
        destination.parent,
    )
    for current, directory_names, file_names in os.walk(source, followlinks=False):
        current_path = Path(current)
        relative = current_path.relative_to(source)
        target = destination / relative
        retained: list[str] = []
        for name in directory_names:
            child = current_path / name
            if name in skipped_directories:
                continue
            if child.is_symlink():
                raise GateError(f"candidate source directory is a symlink: {child}")
            _create_private_directory(
                target / name,
                "candidate source directory",
                target,
            )
            retained.append(name)
        directory_names[:] = retained
        for name in file_names:
            if name == ".DS_Store":
                continue
            child = current_path / name
            metadata = child.lstat()
            if not stat.S_ISREG(metadata.st_mode):
                raise GateError(f"candidate source input is not a regular file: {child}")
            shutil.copy2(child, target / name)


def _is_root_dotnet_configuration(path: Path) -> bool:
    exact_names = {
        ".editorconfig",
        ".globalconfig",
        "global.json",
    }
    lowered = path.name.lower()
    return (
        path.name in exact_names
        or lowered == "nuget.config"
        or path.suffix.lower()
        in {".globalconfig", ".props", ".ruleset", ".targets"}
    )


def _copy_root_dotnet_configuration(source_root: Path, candidate_root: Path) -> None:
    for source in source_root.iterdir():
        if not _is_root_dotnet_configuration(source):
            continue
        metadata = source.lstat()
        if not stat.S_ISREG(metadata.st_mode):
            raise GateError(
                f"repository-root .NET configuration is not a regular file: {source}"
            )
        destination = candidate_root / source.name
        _require_direct_child(
            destination,
            candidate_root,
            "candidate repository-root configuration",
        )
        shutil.copy2(source, destination)


def create_dotnet_candidate(evidence: Evidence) -> DotnetCandidate:
    candidate_root = evidence.create_private_directory("dotnet-candidate")
    _copy_root_dotnet_configuration(ROOT, candidate_root)
    candidate_dotnet = candidate_root / "PaliPractice"
    _copy_candidate_tree(DOTNET_ROOT, candidate_dotnet)
    candidate_config = candidate_root / "quality"
    _create_private_directory(
        candidate_config,
        "candidate quality directory",
        candidate_root,
    )
    candidate_config = candidate_config / "config"
    _create_private_directory(
        candidate_config,
        "candidate quality config",
        candidate_config.parent,
    )
    candidate_metrics = candidate_config / "CodeMetricsConfig.txt"
    _require_direct_child(
        candidate_metrics,
        candidate_config,
        "candidate CodeMetricsConfig.txt",
    )
    shutil.copy2(CONFIG / "CodeMetricsConfig.txt", candidate_metrics)

    token = f"Q{secrets.token_hex(8)}"
    method = f"Exercise{token}"
    filename = f"{secrets.token_hex(16)}.cs"
    body = "\n".join(
        f"        if (value == {number}) score++;" for number in range(15)
    )
    source = (
        "namespace QualityGateAnalyzerLiveness;\n\n"
        f"public static class {token}\n"
        "{\n"
        f"    public static int {method}(int value)\n"
        "    {\n"
        "        var score = 0;\n"
        f"{body}\n"
        "        return score;\n"
        "    }\n"
        "}\n"
    )
    probes: dict[tuple[str, str], int] = {}
    for project in ("PaliPractice", "PaliPractice.Tests"):
        relative = Path("PaliPractice") / project / filename
        probe = candidate_root / relative
        _require_private_directory(probe.parent, "candidate probe parent")
        _require_direct_child(probe, probe.parent, "candidate analyzer probe")
        if probe.exists() or probe.is_symlink():
            raise GateError(f"candidate analyzer probe already exists: {probe}")
        probe.write_text(source, encoding="utf-8")
        probes[(relative.as_posix(), method)] = 16

    return DotnetCandidate(
        root=candidate_root,
        dotnet_root=candidate_dotnet,
        solution=candidate_dotnet / "PaliPractice.sln",
        app_project=candidate_dotnet / "PaliPractice" / "PaliPractice.csproj",
        test_project=(
            candidate_dotnet / "PaliPractice.Tests" / "PaliPractice.Tests.csproj"
        ),
        probes=probes,
    )


def candidate_source_aliases(candidate: DotnetCandidate) -> dict[str, str]:
    aliases = production_source_aliases()
    for canonical in set(aliases.values()):
        candidate_path = candidate.root / canonical
        aliases[str(candidate_path.resolve()).replace("\\", "/").lstrip("/")] = (
            canonical
        )
        aliases[(Path(candidate.root.name) / canonical).as_posix()] = canonical
    return aliases


def parse_ca1502(
    paths: Iterable[Path], source_root: Path = ROOT
) -> dict[tuple[str, str], int]:
    pattern = re.compile(
        r"^(.*?\.cs)\(\d+,\d+\): (?:warning|error) CA1502: "
        r"'([^']+)' has a cyclomatic complexity of '(\d+)'",
        re.MULTILINE,
    )
    findings: dict[tuple[str, str], int] = {}
    for path in paths:
        text = path.read_text(encoding="utf-8", errors="replace")
        for filename, symbol, raw_complexity in pattern.findall(text):
            candidate = Path(filename)
            try:
                logical = candidate.resolve().relative_to(source_root.resolve()).as_posix()
            except (OSError, ValueError):
                logical = filename.replace("\\", "/")
            key = (logical, symbol)
            findings[key] = max(findings.get(key, 0), int(raw_complexity))
    return findings


def consume_ca1502_probes(
    findings: dict[tuple[str, str], int],
    probes: dict[tuple[str, str], int],
) -> tuple[dict[tuple[str, str], int], list[str]]:
    remaining = dict(findings)
    errors: list[str] = []
    for key, expected in probes.items():
        observed = remaining.pop(key, None)
        if observed != expected:
            errors.append(
                f"Roslyn did not report analyzer liveness probe {key!r} "
                f"at complexity {expected} (observed={observed!r})"
            )
    return remaining, errors


def ca1502_policy_errors(
    root: Path = ROOT, dotnet_root: Path = DOTNET_ROOT
) -> list[str]:
    """Pin metrics and reserve file-local analyzer escape-hatch mechanisms."""
    errors: list[str] = []
    try:
        metrics = (CONFIG / "CodeMetricsConfig.txt").read_text(encoding="utf-8")
    except OSError as error:
        return [f"cannot read CodeMetricsConfig.txt: {error}"]
    if metrics != "CA1502: 15\n":
        errors.append("CodeMetricsConfig.txt must contain exactly 'CA1502: 15'")

    expected_config = (
        dotnet_root / ".editorconfig",
        "dotnet_diagnostic.CA1502.severity = warning",
    )
    config_occurrences: list[tuple[Path, str]] = []
    config_names = {".editorconfig", ".globalconfig"}
    config_paths = {
        path
        for path in dotnet_root.rglob("*")
        if (
            path.name in config_names
            or path.suffix.lower()
            in {".csproj", ".globalconfig", ".props", ".ruleset", ".targets"}
        )
    }
    config_paths.update(
        path for path in root.iterdir() if _is_root_dotnet_configuration(path)
    )
    for path in sorted(config_paths):
        if (
            "bin" in path.parts
            or "obj" in path.parts
        ):
            continue
        try:
            metadata = path.lstat()
        except OSError as error:
            errors.append(f"cannot inspect analyzer configuration {path}: {error}")
            continue
        if not stat.S_ISREG(metadata.st_mode):
            errors.append(f"analyzer configuration is not a regular file: {path}")
            continue
        try:
            text = path.read_text(encoding="utf-8")
        except (OSError, UnicodeError) as error:
            errors.append(f"cannot read analyzer configuration {path}: {error}")
            continue
        lowered = text.casefold()
        for line in text.splitlines():
            if "dotnet_diagnostic.ca1502.severity" in line.casefold():
                config_occurrences.append((path, line.strip()))
        for reserved in (
            "dotnet_analyzer_diagnostic.category-maintainability.severity",
            "dotnet_analyzer_diagnostic.severity",
            "suppressmessage",
            "generated_code",
            "generatedcode",
            "compilergenerated",
            "<auto-generated",
            "<autogenerated",
        ):
            if reserved in lowered:
                errors.append(
                    f"{path.relative_to(root)} contains reserved analyzer "
                    f"escape hatch {reserved!r}"
                )
    if config_occurrences != [expected_config]:
        rendered = [
            f"{path.relative_to(root)}: {line}"
            for path, line in config_occurrences
        ]
        errors.append(
            "CA1502 editorconfig inventory differs from the one required "
            f"warning line: {rendered!r}"
        )

    expected_suppression = (
        dotnet_root / "PaliPractice" / "Models" / "Enums.cs",
        '[SuppressMessage("ReSharper", "UnusedMember.Global")]',
    )
    suppression_occurrences: list[tuple[Path, str]] = []
    generated_suffixes = (
        ".designer.cs",
        ".g.cs",
        ".g.i.cs",
        ".generated.cs",
    )
    for path in sorted(dotnet_root.rglob("*.cs")):
        if "bin" in path.parts or "obj" in path.parts:
            continue
        lowered_name = path.name.casefold()
        if lowered_name.endswith(generated_suffixes) or lowered_name.startswith(
            "temporarygeneratedfile_"
        ):
            errors.append(
                f"{path.relative_to(root)} uses a generated-source filename "
                "reserved by the CA1502 gate"
            )
        try:
            text = path.read_text(encoding="utf-8")
        except (OSError, UnicodeError) as error:
            errors.append(f"cannot read C# analyzer policy source {path}: {error}")
            continue
        lowered = text.casefold()
        for line in text.splitlines():
            if "suppressmessage" in line.casefold():
                suppression_occurrences.append((path, line.strip()))
        reserved_markers = {
            "#pragma warning disable": re.search(
                r"#\s*pragma\s+warning\s+disable", lowered
            ),
            "GeneratedCode": "generatedcode" in lowered,
            "CompilerGenerated": "compilergenerated" in lowered,
            "auto-generated marker": any(
                marker in lowered
                for marker in ("<auto-generated", "<autogenerated")
            ),
        }
        for marker, present in reserved_markers.items():
            if present:
                errors.append(
                    f"{path.relative_to(root)} contains reserved analyzer "
                    f"escape hatch {marker!r}; these spellings are reserved "
                    "even in comments and strings"
                )
    if suppression_occurrences != [expected_suppression]:
        rendered = [
            f"{path.relative_to(root)}: {line}"
            for path, line in suppression_occurrences
        ]
        errors.append(
            "SuppressMessage inventory differs from the one existing "
            f"ReSharper-only allowance: {rendered!r}"
        )
    return errors


def _parse_ca1502_baseline(
    text: str, source: str
) -> tuple[dict[str, object] | None, list[str]]:
    try:
        baseline = json.loads(text)
    except json.JSONDecodeError as error:
        return None, [f"invalid CA1502 baseline from {source}: {error}"]
    if not isinstance(baseline, dict):
        return None, [f"CA1502 baseline from {source} must be a JSON object"]
    errors = []
    if baseline.get("schema") != 1:
        errors.append(f"CA1502 baseline from {source} must use schema 1")
    if baseline.get("threshold") != 15:
        errors.append(f"CA1502 baseline from {source} must retain threshold 15")
    if not isinstance(baseline.get("anchor"), str) or not baseline["anchor"]:
        errors.append(f"CA1502 baseline from {source} needs a commit anchor")
    if not isinstance(baseline.get("allowed"), list):
        errors.append(f"CA1502 baseline from {source} needs an allowed list")
    else:
        seen: set[tuple[str, str]] = set()
        for index, entry in enumerate(baseline["allowed"]):
            valid = (
                isinstance(entry, dict)
                and isinstance(entry.get("path"), str)
                and bool(entry["path"])
                and "\\" not in entry["path"]
                and not Path(entry["path"]).is_absolute()
                and ".." not in Path(entry["path"]).parts
                and entry["path"].endswith(".cs")
                and isinstance(entry.get("symbol"), str)
                and bool(entry["symbol"])
                and isinstance(entry.get("max_complexity"), int)
                and not isinstance(entry["max_complexity"], bool)
                and entry["max_complexity"] > 15
            )
            if not valid:
                errors.append(
                    f"CA1502 baseline entry {index} from {source} is malformed"
                )
                continue
            key = (entry["path"], entry["symbol"])
            if key in seen:
                errors.append(f"CA1502 baseline from {source} duplicates {key}")
            seen.add(key)
    return (baseline if not errors else None), errors


def _git_file_bytes_at_commit(base: str, relative_path: str) -> bytes | None:
    try:
        completed = subprocess.run(
            ["git", "show", f"{base}:{relative_path}"],
            cwd=ROOT,
            stdout=subprocess.PIPE,
            stderr=subprocess.DEVNULL,
            check=False,
            timeout=GIT_TIMEOUT_SECONDS,
        )
    except subprocess.TimeoutExpired as error:
        raise RuntimeError(f"reading {relative_path} from --base timed out") from error
    return completed.stdout if completed.returncode == 0 else None


def _git_file_at_commit(base: str, relative_path: str) -> str | None:
    payload = _git_file_bytes_at_commit(base, relative_path)
    if payload is None:
        return None
    try:
        return payload.decode("utf-8")
    except UnicodeDecodeError as error:
        raise GateError(f"{relative_path} at --base is not UTF-8") from error


def trusted_ca1502_baseline(
    base: str, current_path: Path | None = None
) -> tuple[dict[str, object] | None, list[str]]:
    relative = "quality/config/ca1502-baseline.json"
    committed = _git_file_bytes_at_commit(base, relative)
    path = current_path or CONFIG / "ca1502-baseline.json"
    if committed is not None:
        try:
            metadata = path.lstat()
        except OSError as error:
            return None, [f"cannot read current CA1502 baseline: {error}"]
        if not stat.S_ISREG(metadata.st_mode):
            return None, [f"current CA1502 baseline is not a regular file: {path}"]
        try:
            current = path.read_bytes()
        except OSError as error:
            return None, [f"cannot read current CA1502 baseline: {error}"]
        if current != committed:
            return None, [
                "current CA1502 baseline must byte-match the trusted --base blob"
            ]
        try:
            text = committed.decode("utf-8")
        except UnicodeDecodeError as error:
            return None, [f"CA1502 baseline at --base is not UTF-8: {error}"]
        return _parse_ca1502_baseline(text, f"{base}:{relative}")
    try:
        metadata = path.lstat()
    except OSError as error:
        return None, [f"cannot read initial CA1502 baseline: {error}"]
    if not stat.S_ISREG(metadata.st_mode):
        return None, [f"initial CA1502 baseline is not a regular file: {path}"]
    try:
        current = path.read_text(encoding="utf-8")
    except (OSError, UnicodeError) as error:
        return None, [f"cannot read initial CA1502 baseline: {error}"]
    baseline, errors = _parse_ca1502_baseline(current, str(path))
    if baseline is None:
        return None, errors
    if baseline["anchor"] != base:
        errors.append("initial CA1502 baseline must be anchored exactly to --base")
    return (baseline if not errors else None), errors


def initial_ca1502_evidence_errors(
    base: str,
    baseline: dict[str, object],
    findings: dict[tuple[str, str], int],
) -> list[str]:
    relative = "quality/config/ca1502-baseline.json"
    if _git_file_at_commit(base, relative) is not None:
        return []
    errors: list[str] = []
    for entry in baseline["allowed"]:
        path = str(entry["path"])
        symbol = str(entry["symbol"])
        cap = int(entry["max_complexity"])
        observed = findings.get((path, symbol))
        if observed != cap:
            errors.append(
                f"initial allowance {path}: {symbol} is {cap}, "
                f"but fresh Roslyn evidence is {observed!r}"
            )
            continue
        exists = subprocess.run(
            ["git", "cat-file", "-e", f"{base}:{path}"],
            cwd=ROOT,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
            check=False,
            timeout=GIT_TIMEOUT_SECONDS,
        )
        unchanged = subprocess.run(
            ["git", "diff", "--quiet", base, "--", path],
            cwd=ROOT,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
            check=False,
            timeout=GIT_TIMEOUT_SECONDS,
        )
        if exists.returncode != 0 or unchanged.returncode != 0:
            errors.append(
                f"initial allowance is not provably unchanged from {base}: {path}"
            )
    return errors


def ca1502_errors(
    findings: dict[tuple[str, str], int], baseline: dict[str, object]
) -> list[str]:
    allowed = {
        (entry["path"], entry["symbol"]): int(entry["max_complexity"])
        for entry in baseline["allowed"]
    }
    return [
        f"{path}: {symbol} complexity {complexity} "
        f"(baseline {allowed.get((path, symbol), 0)}, limit 15)"
        for (path, symbol), complexity in sorted(findings.items())
        if complexity > allowed.get((path, symbol), 0)
    ]


def candidate_spec() -> tuple[Path, Path] | None:
    candidate = os.environ.get("PALIPRACTICE_CANDIDATE_DIRECTORY")
    inputs = os.environ.get("PALIPRACTICE_INPUT_MANIFEST")
    if not candidate and not inputs:
        return None
    if not candidate or not inputs:
        raise GateError("Candidate verification requires both PALIPRACTICE_CANDIDATE_DIRECTORY and PALIPRACTICE_INPUT_MANIFEST")
    return Path(candidate).resolve(), Path(inputs).resolve()


def regenerate_candidate(gate: Gate, groups: set[str]) -> None:
    inputs = os.environ.get("PALIPRACTICE_INPUT_MANIFEST")
    if not inputs or not groups.intersection({"data", "dotnet", "desktop"}):
        return
    output = gate.evidence.run / "english-repeatability"
    command = [str(ROOT / ".venv/bin/python"), "-B", "scripts/check_repeatability.py",
               "--inputs", str(Path(inputs).resolve()), "--output", str(output)]
    supplied = os.environ.get("PALIPRACTICE_CANDIDATE_DIRECTORY")
    if supplied:
        command.extend(["--compare", str(Path(supplied).resolve())])
    result = gate.command("english-repeatability", command)
    if result.returncode != 0:
        raise GateError("English regeneration failed; inspect the isolated build logs")
    gate.check("english-repeatability", [])
    os.environ["PALIPRACTICE_CANDIDATE_DIRECTORY"] = str(output / "run-1")


def verify_supplied_candidate(gate: Gate, name: str) -> None:
    specification = candidate_spec()
    if specification:
        candidate, inputs = specification
        result = gate.command(name, [str(ROOT / ".venv/bin/python"), "-B", "scripts/verify_candidate.py",
                                     "--candidate", str(candidate), "--inputs", str(inputs)])
        if result.returncode == 0:
            gate.check(name, [])


def _dotnet_environment(gate: Gate) -> dict[str, str]:
    environment = {
        "DOTNET_CLI_TELEMETRY_OPTOUT": "1",
        "DOTNET_CLI_UI_LANGUAGE": "en",
        "DOTNET_NOLOGO": "1",
        "NUGET_PACKAGES": str(gate.evidence.tool_cache / "nuget"),
        "PALIPRACTICE_REPO_ROOT": str(ROOT),
        "VSLANG": "1033",
    }

    specification = candidate_spec()
    if specification:
        candidate, inputs = specification
        environment["PALIPRACTICE_CANDIDATE_DB"] = str(candidate / "pali.db")
        environment["PALIPRACTICE_INPUT_MANIFEST"] = str(inputs)
    return environment


def _dotnet_output_arguments(gate: Gate) -> list[str]:
    return [
        "-p:UseArtifactsOutput=true",
        f"-p:ArtifactsPath={gate.evidence.run / 'dotnet-artifacts'}",
    ]


def _dotnet_analysis_arguments() -> list[str]:
    return [
        "-p:EnableNETAnalyzers=true",
        "-p:RunAnalyzersDuringBuild=true",
        "-p:AnalysisLevel=latest-recommended",
        "-p:NoWarn=NU1507%3BNETSDK1201%3BPRI257",
    ]


def _dotnet_contract_arguments(gate: Gate) -> list[str]:
    return [
        "--disable-build-servers",
        "-m:1",
        "/nodeReuse:false",
        *_dotnet_analysis_arguments(),
        *_dotnet_output_arguments(gate),
    ]


def run_dotnet_checks(gate: Gate, include_desktop: bool, base: str) -> None:
    gate.check("ca1502-policy", ca1502_policy_errors())
    baseline, baseline_errors = trusted_ca1502_baseline(base)
    baseline_valid = gate.check("ca1502-baseline", baseline_errors)
    if baseline_valid:
        assert baseline is not None
        gate.evidence.new_file("ca1502-baseline-used.json").write_text(
            json.dumps(baseline, indent=2), encoding="utf-8"
        )
    try:
        candidate = create_dotnet_candidate(gate.evidence)
    except (GateError, OSError) as error:
        gate.check("dotnet-candidate", [str(error)])
        return
    environment = _dotnet_environment(gate)
    contract = _dotnet_contract_arguments(gate)
    restore = gate.command(
        "dotnet-restore",
        [
            "dotnet",
            "restore",
            str(candidate.solution),
            "--locked-mode",
            *contract,
        ],
        cwd=candidate.dotnet_root,
        environment=environment,
    )
    if restore.returncode:
        return
    started = time.time()
    results = gate.evidence.run / "test-results"
    test = gate.command(
        "dotnet-test",
        [
            "dotnet",
            "test",
            str(candidate.test_project),
            "--no-restore",
            "--logger",
            "trx;LogFileName=tests.trx",
            "--results-directory",
            str(results),
            "--collect",
            "XPlat Code Coverage",
            "--settings",
            str(CONFIG / "coverage.runsettings"),
            "-c",
            "Release",
            *contract,
        ],
        cwd=candidate.dotnet_root,
        environment=environment,
    )
    gate.ca_logs.extend([test.stdout, test.stderr])
    if test.returncode == 0:
        trx = next(iter(results.rglob("tests.trx")), results / "tests.trx")
        gate.check("test-results", trx_errors(trx))
        coverage, selection_errors = select_coverage_report(
            results.rglob("coverage.cobertura.xml")
        )
        if gate.check("coverage-report", selection_errors) and coverage is not None:
            errors, (covered, total) = coverage_errors(
                coverage,
                started,
                candidate_source_aliases(candidate),
            )
            if gate.check("coverage", errors) and total:
                print(f"INFO coverage {covered}/{total} lines ({covered / total:.1%}); advisory")
    if include_desktop:
        desktop = gate.command(
            "dotnet-desktop",
            [
                "dotnet",
                "build",
                str(candidate.app_project),
                "-f",
                "net10.0-desktop",
                "--no-restore",
                "-c",
                "Release",
                *contract,
            ],
            cwd=candidate.dotnet_root,
            environment=environment,
        )
        gate.ca_logs.extend([desktop.stdout, desktop.stderr])
        if desktop.returncode == 0:
            gate.check("desktop-build", [])

    findings = parse_ca1502(gate.ca_logs, candidate.root)
    findings, liveness_errors = consume_ca1502_probes(findings, candidate.probes)
    gate.check("ca1502-liveness", liveness_errors)
    report = gate.evidence.new_file("ca1502-current.json")
    report.write_text(
        json.dumps(
            [
                {"path": path, "symbol": symbol, "complexity": complexity}
                for (path, symbol), complexity in sorted(findings.items())
            ],
            indent=2,
        ),
        encoding="utf-8",
    )
    if baseline_valid:
        gate.check(
            "ca1502-initial-evidence",
            initial_ca1502_evidence_errors(base, baseline, findings),
        )
        gate.check("ca1502", ca1502_errors(findings, baseline))
    else:
        gate.check("ca1502", ["Cannot evaluate allowances: analyzer baseline is invalid"])


def run_selected(gate: Gate, groups: set[str], base: str) -> None:
    if os.environ.get("PALIPRACTICE_INPUT_MANIFEST"):
        gate.command("semantic-sources", [str(ROOT / ".venv/bin/python"), "-B", "scripts/semantic_evidence.py",
                     "--capture-sources", str(gate.evidence.run / "semantic-sources.json")])
    regenerate_candidate(gate, groups)
    verify_supplied_candidate(gate, "candidate-inputs-before")
    self_test = gate.command(
        "quality-tests",
        [
            sys.executable,
            "-B",
            "-m",
            "unittest",
            "discover",
            "-s",
            "quality/tests",
            "-t",
            ".",
            "-v",
        ],
    )
    if self_test.returncode == 0:
        gate.check("quality-tests", [])
    if "python" in groups:
        producers = gate.command("producer-tests", [str(ROOT / ".venv/bin/python"), "-B",
                                  "-m", "unittest", "discover", "-s", "scripts/tests", "-v"])
        if producers.returncode == 0:
            gate.check("producer-tests", [])
        run_python_checks(gate, base)
    if "data" in groups:
        specification = candidate_spec()
        directory = specification[0] if specification else APP_SOURCE / "Data"
        registry = directory / "lemma_registry.json" if specification else ROOT / "scripts/configs/lemma_registry.json"
        errors, metrics = validate_data(directory / "pali.db", directory / "pali.version.txt", registry)
        if gate.check("data-contract", errors):
            print(
                "INFO data "
                f"{metrics.get('nouns_rows', 0)} noun rows, "
                f"{metrics.get('verbs_rows', 0)} verb rows"
            )
    if "submodule" in groups:
        gate.check("submodules", submodule_errors())
    if "dotnet" in groups or "desktop" in groups:
        run_dotnet_checks(gate, "desktop" in groups, base)
    verify_supplied_candidate(gate, "candidate-inputs-after")


def _arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "profile_positional",
        nargs="?",
        choices=("auto", "fast", "full"),
        help="profile (positional form retained for the universal runner)",
    )
    parser.add_argument(
        "--profile",
        dest="profile_option",
        choices=("auto", "fast", "full"),
        help="profile (default: auto)",
    )
    parser.add_argument("--base", help="commit used for changed-code ratchets")
    parser.add_argument(
        "--keep-previous-runs",
        type=int,
        default=1,
        metavar="N",
        help="retain the current evidence directory plus N prior runs (default: 1)",
    )
    parser.add_argument(
        "--step-timeout-seconds",
        type=float,
        default=1_200,
        metavar="SECONDS",
        help="maximum time for each external check (default: 1200)",
    )
    arguments = parser.parse_args()
    if arguments.keep_previous_runs < 0:
        parser.error("--keep-previous-runs must be nonnegative")
    if arguments.step_timeout_seconds <= 0:
        parser.error("--step-timeout-seconds must be positive")
    if (
        arguments.profile_positional
        and arguments.profile_option
        and arguments.profile_positional != arguments.profile_option
    ):
        parser.error("positional profile and --profile disagree")
    arguments.profile = (
        arguments.profile_option or arguments.profile_positional or "auto"
    )
    return arguments


def main() -> int:
    arguments = _arguments()
    try:
        lock_path = LOCK_ROOT / f"pali-practice-{REPOSITORY_DIGEST}.lock"
        with RepositoryLock(lock_path) as lock:
            evidence = Evidence(arguments.keep_previous_runs)
            base = resolve_base(arguments.base)
            changes = discover_changes(base)
            groups = {
                "quality",
                "python",
                "data",
                "submodule",
            } if arguments.profile == "fast" else set(ALL_GROUPS)
            if arguments.profile == "auto":
                groups = route_changes(changes)
            manifest = {
                "profile": arguments.profile,
                "base": base,
                "head": git_bytes(["rev-parse", "HEAD"]).decode().strip(),
                "groups": sorted(groups),
                "changes": [change.__dict__ for change in changes],
            }
            evidence.new_file("manifest.json").write_text(
                json.dumps(manifest, indent=2), encoding="utf-8"
            )
            print(f"INFO evidence {evidence.run}")
            print(f"INFO base {base}; groups {', '.join(sorted(groups))}")
            before = worktree_fingerprint()
            gate = Gate(
                evidence,
                arguments.step_timeout_seconds,
                lock_fd=lock.fileno(),
            )
            run_selected(gate, groups, base)
            after = worktree_fingerprint()
            gate.check(
                "worktree-unchanged",
                [] if before == after else ["tracked or untracked repository state changed"],
            )
            evidence.new_file("completion.json").write_text(
                json.dumps(
                    {
                        "status": "fail" if gate.failures else "pass",
                        "findings": len(gate.failures),
                    },
                    indent=2,
                ),
                encoding="utf-8",
            )
            evidence.prune()
            if not gate.failures and {"data", "dotnet", "desktop"} <= groups and candidate_spec():
                candidate, inputs = candidate_spec()
                gate.command("semantic-verification", [str(ROOT / ".venv/bin/python"), "-B",
                             "scripts/semantic_evidence.py", "--candidate", str(candidate),
                             "--inputs", str(inputs), "--gate-run", str(evidence.run)])
                if gate.failures:
                    (evidence.run / "completion.json").write_text(json.dumps(
                        {"status": "fail", "findings": len(gate.failures)}, indent=2))
            if gate.failures:
                print(f"FAIL gate ({len(gate.failures)} finding(s)); evidence {evidence.run}")
                return 1
            print(f"PASS gate; evidence {evidence.run}")
            return 0
    except RuntimeError as error:
        print(f"UNAVAILABLE {error}", file=sys.stderr)
        return 75


if __name__ == "__main__":
    raise SystemExit(main())

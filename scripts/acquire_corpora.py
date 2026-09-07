"""Reproduce canonical corpus wordlists from explicit local Git revisions.

This acquisition command is separate from English extraction and the gate.
It runs upstream conversion tools only inside a newly created workspace.
"""

import argparse
import io
import importlib.metadata
import json
import os
import re
import subprocess
import sys
import tarfile
from pathlib import Path

from extraction.inputs import CORPORA, InputError, load_corpus_words, sha256


def archive(repository: Path, revision: str, paths: list[str], destination: Path):
    if not re.fullmatch(r"[0-9a-f]{40}", revision):
        raise InputError("Source revisions must be full Git commit IDs")
    data = subprocess.check_output(["git", "archive", revision, "--", *paths], cwd=repository)
    destination.mkdir(parents=True, exist_ok=True)
    with tarfile.open(fileobj=io.BytesIO(data)) as source:
        source.extractall(destination, filter="data")


def run(command: list[str], workspace: Path, log: Path):
    environment = os.environ.copy()
    environment["PYTHONPATH"] = str(workspace)
    environment["GOPROXY"] = "off"
    environment["GOSUMDB"] = "off"
    environment["GOCACHE"] = str(workspace.parent / ".go-cache")
    environment["GOMODCACHE"] = os.environ.get(
        "PALIPRACTICE_GO_MODULE_CACHE", str(workspace.parent / ".go-mod-cache")
    )
    environment["GOTELEMETRY"] = "off"
    with log.open("w") as stream:
        subprocess.run(command, cwd=workspace, env=environment, stdout=stream,
                       stderr=subprocess.STDOUT, check=True)


def check_conversion(source: Path, target: Path, suffix: str, excluded: str = "",
                     keep_suffix: bool = False):
    expected = {path.name + ".txt" if keep_suffix else path.stem + ".txt"
                for path in source.iterdir()
                if path.suffix == suffix and (not excluded or excluded not in path.name)}
    produced = {path.name: path for path in target.glob("*.txt")}
    if not expected or set(produced) != expected or any(path.stat().st_size == 0 for path in produced.values()):
        raise InputError(f"Incomplete upstream corpus conversion: {source}")


def acquire(repository: Path, output: Path, revisions: dict[str, str]) -> Path:
    output.mkdir(parents=True, exist_ok=False)
    workspace = output / "workspace"
    archive(repository, revisions["dpd"], [
        "go.mod", "go.sum", "go_modules/tools", "go_modules/frequency",
        "scripts/build/cst4_xml_to_txt.py", "scripts/build/transliterate_bjt.py",
        "tools", "resources/syāmaraṭṭha_1927", "pyproject.toml", "uv.lock",
    ], workspace)
    archive(repository / "resources/dpd_submodules", revisions["texts"],
            ["cst", "bjt"], workspace / "resources/dpd_submodules")
    archive(repository / "resources/sc-data", revisions["sc"],
            ["sc_bilara_data/root/pli/ms"], workspace / "resources/sc-data")
    run(["go", "mod", "download"], workspace, output / "go-dependencies.log")
    for script in ("cst4_xml_to_txt", "transliterate_bjt"):
        run([sys.executable, f"scripts/build/{script}.py"], workspace, output / f"{script}.log")
    return finish(workspace, output, revisions)


def finish(workspace: Path, output: Path, revisions: dict[str, str]) -> Path:
    """Run frequency analysis after successful isolated corpus conversion."""
    texts = workspace / "resources/dpd_submodules"
    check_conversion(texts / "cst/romn", texts / "cst/romn_txt", ".xml", "toc")
    check_conversion(texts / "bjt/public/static/text", texts / "bjt/public/static/roman_txt",
                     ".json", keep_suffix=True)
    (workspace / "shared_data/frequency").mkdir(parents=True, exist_ok=True)
    run(["go", "run", "./go_modules/frequency/setup"], workspace, output / "frequency.log")
    paths = [workspace / "shared_data/frequency" / f"{name}_wordlist.json" for name in CORPORA]
    load_corpus_words(paths)
    outputs = {}
    for name, path in zip(CORPORA, paths):
        canonical = output / f"{name}_wordlist.json"
        canonical.write_text(json.dumps(sorted(set(json.loads(path.read_text()))),
                                       ensure_ascii=False, separators=(",", ":")) + "\n")
        outputs[name] = {"file": canonical.name, "sha256": sha256(canonical)}
    provenance = {
        "recipe": "scripts/acquire_corpora.py: upstream CST/BJT conversion, Go frequency/setup, sorted unique UTF-8 JSON",
        "recipe_sha256": sha256(Path(__file__)), "revisions": revisions,
        "outputs": outputs,
        "python_packages": {name: importlib.metadata.version(name) for name in
                            ("beautifulsoup4", "lxml", "aksharamukha", "rich")},
        "python": subprocess.check_output([sys.executable, "--version"], text=True).strip(),
        "go": subprocess.check_output(["go", "version"], text=True).strip(),
    }
    (output / "provenance.json").write_text(json.dumps(provenance, indent=2) + "\n")
    return output


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    for name in ("dpd", "texts", "sc"):
        parser.add_argument(f"--{name}-revision", required=True)
    args = parser.parse_args()
    revisions = {name: getattr(args, name + "_revision") for name in ("dpd", "texts", "sc")}
    print(acquire(args.repository.resolve(), args.output.resolve(), revisions))


if __name__ == "__main__":
    main()

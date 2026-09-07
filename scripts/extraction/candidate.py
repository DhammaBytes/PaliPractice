"""Build and validate English candidates without a production write path."""

import argparse
import json
import importlib.metadata
import platform
import sqlite3
import subprocess
import sys
from pathlib import Path

from .compatibility import validate_identities
from .validate_forms import validate_forms
from .inputs import CORPORA, InputError, load_manifest, read_json, sha256

ROOT = Path(__file__).resolve().parents[2]
OUTPUTS = ("pali.db", "pali.version.txt", "lemma_registry.json", "inflection_validation.log", "practice_registry.json", "paradigm_corrections.json", "compatibility.json", "primary_forms.json")


def code_identity() -> dict:
    subprocess.run(["git", "diff", "--quiet", "HEAD", "--", "db", "tools"],
                   cwd=ROOT / "dpd-db", check=True)
    sources = [ROOT / "scripts/extract_nouns_and_verbs.py"]
    for directory in ("extraction", "configs"):
        sources.extend((ROOT / "scripts" / directory).rglob("*.py"))
    return {
        "revision": subprocess.check_output(
            ["git", "rev-parse", "HEAD"], cwd=ROOT, text=True
        ).strip(),
        "dpd_revision": subprocess.check_output(
            ["git", "rev-parse", "HEAD"], cwd=ROOT / "dpd-db", text=True
        ).strip(),
        "files": {str(path.relative_to(ROOT)): sha256(path)
                  for path in sorted(sources)},
        "python": platform.python_version(),
        "packages": {name: importlib.metadata.version(name) for name in
                     ("SQLAlchemy", "aksharamukha")},
        "sqlite": sqlite3.sqlite_version,
    }


def structural_errors(directory: Path) -> list[str]:
    # Use the same structural contract as the gate. M4 expands this contract;
    # a structural pass is deliberately not a release/promotion approval.
    sys.path.insert(0, str(ROOT))
    from quality.checks.data_contract import validate_data
    errors, _ = validate_data(
        directory / "pali.db", directory / "pali.version.txt",
        directory / "lemma_registry.json",
    )
    return errors


def build_candidate(manifest_path: Path, output: Path) -> Path:
    manifest, paths = load_manifest(manifest_path.resolve())
    output = output.resolve()
    if any(path == output or output in path.parents for path in paths.values()):
        raise InputError("Candidate directory must not contain any input")
    initial_code = code_identity()
    # The unique leaf is never reused, even after failure or interruption.
    output.mkdir(parents=True, exist_ok=False)
    (output / "BUILDING").write_text("Incomplete candidate; do not promote.\n")
    from extract_nouns_and_verbs import NounVerbExtractor
    extractor = NounVerbExtractor(
        dpd_path=paths["dpd"], corpus_paths=[paths[name] for name in CORPORA],
        registry_path=paths["registry"], adjustments_path=paths["adjustments"],
        practice_registry_path=paths["practice_registry"], corrections_path=paths["corrections"],
        output_db_path=output / "pali.db",
        noun_limit=manifest["configuration"]["noun_limit"],
        verb_limit=manifest["configuration"]["verb_limit"],
        database_version=manifest["database_version"],
    )
    try:
        extractor.extract_and_save()
    finally:
        extractor.close()
    (output / "pali.version.txt").write_text(str(manifest["database_version"]) + "\n")
    (output / "paradigm_corrections.json").write_bytes(paths["corrections"].read_bytes())
    validate_identities(output)
    validate_forms(output)
    errors = structural_errors(output)
    if errors:
        raise InputError("Candidate structural validation failed: " + "; ".join(errors))
    # Detect changes during extraction before issuing a completed manifest.
    if load_manifest(manifest_path.resolve()) != (manifest, paths) or code_identity() != initial_code:
        raise InputError("Inputs or extraction code changed during generation")
    completed = {
        "schema": 3, "language_layer": "en", "validation_level": "structural",
        "database_version": manifest["database_version"],
        "configuration": manifest["configuration"],
        "inputs": {name: {key: value for key, value in entry.items() if key != "path"}
                   for name, entry in manifest["inputs"].items()},
        "corpus_generation": manifest["corpus_generation"],
        "code": initial_code,
        "outputs": {name: sha256(output / name) for name in OUTPUTS},
    }
    (output / "candidate.json").write_text(
        json.dumps(completed, indent=2, ensure_ascii=False, sort_keys=True) + "\n"
    )
    (output / "BUILDING").unlink()
    return output


def validate_candidate(directory: Path) -> dict:
    if (directory / "BUILDING").exists():
        raise InputError("Candidate build was interrupted or failed")
    manifest = read_json(directory / "candidate.json")
    if not isinstance(manifest, dict) or type(manifest.get("schema")) is not int or manifest["schema"] != 3:
        raise InputError("Invalid candidate manifest")
    if manifest.get("language_layer") != "en" or manifest.get("validation_level") != "structural":
        raise InputError("Unsupported candidate contract")
    outputs = manifest.get("outputs")
    if not isinstance(outputs, dict) or set(outputs) != set(OUTPUTS):
        raise InputError("Incomplete candidate output manifest")
    for name, expected in outputs.items():
        if not (directory / name).is_file() or sha256(directory / name) != expected:
            raise InputError(f"Candidate checksum mismatch: {name}")
    if (directory / "pali.version.txt").read_text().strip() != str(manifest.get("database_version")):
        raise InputError("Candidate manifest/version mismatch")
    validate_identities(directory)
    validate_forms(directory)
    errors = structural_errors(directory)
    if errors:
        raise InputError("Candidate structural validation failed: " + "; ".join(errors))
    return manifest


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    build = commands.add_parser("build")
    build.add_argument("--manifest", type=Path, required=True)
    build.add_argument("--output", type=Path, required=True)
    validate = commands.add_parser("validate")
    validate.add_argument("candidate", type=Path)
    args = parser.parse_args()
    if args.command == "build":
        print(build_candidate(args.manifest, args.output))
    else:
        validate_candidate(args.candidate)
        print("PASS structural candidate checks; semantic release checks remain required")

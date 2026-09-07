"""Record existing local inputs; never download data or generate an app database."""

import argparse
import json
from pathlib import Path

from extraction.inputs import CORPORA, InputError, configuration, load_manifest, read_json, sha256


def pin(dpd: Path, registry: Path, adjustments: Path, corpora: Path,
        output: Path, version: int, noun_limit: int, verb_limit: int,
        practice_registry: Path, corrections: Path):
    provenance = read_json(corpora / "provenance.json")
    inputs = {}
    for name, path in {"dpd": dpd, "registry": registry, "adjustments": adjustments,
                       "practice_registry": practice_registry, "corrections": corrections}.items():
        digest = sha256(path)
        inputs[name] = {"path": str(path.resolve()), "sha256": digest,
                        "source": {"origin": "explicit local " + name,
                                   "revision": "sha256:" + digest}}
    for name in CORPORA:
        path = corpora / f"{name}_wordlist.json"
        digest = sha256(path)
        if digest != provenance["outputs"][name]["sha256"]:
            raise InputError(f"Corpus differs from acquisition provenance: {name}")
        inputs[name] = {"path": str(path.resolve()), "sha256": digest,
                        "source": {"origin": provenance["recipe"], "revision": "sha256:" + digest}}
    manifest = {"schema": 2, "database_version": version,
                "configuration": configuration(noun_limit, verb_limit),
                "inputs": inputs, "corpus_generation": provenance}
    with output.open("x") as stream:
        json.dump(manifest, stream, indent=2)
        stream.write("\n")
    load_manifest(output)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("dpd", "registry", "adjustments", "corpora", "output", "practice-registry", "corrections"):
        parser.add_argument("--" + name, type=Path, required=True)
    parser.add_argument("--version", type=int, required=True)
    parser.add_argument("--nouns", type=int, default=1500)
    parser.add_argument("--verbs", type=int, default=750)
    args = parser.parse_args()
    pin(args.dpd, args.registry, args.adjustments, args.corpora,
        args.output, args.version, args.nouns, args.verbs, args.practice_registry, args.corrections)


if __name__ == "__main__":
    main()

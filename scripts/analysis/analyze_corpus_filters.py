#!/usr/bin/env python3
"""
Analyze corpus form coverage for various filter combinations.
Identifies settings combinations that yield low or zero corpus forms.
"""

import sqlite3
from collections import defaultdict
from dataclasses import dataclass
from typing import NamedTuple
from pathlib import Path
from itertools import combinations, product

# Database paths (script is in scripts/analysis/)
PALI_DB = Path(__file__).parent.parent.parent / "PaliPractice/PaliPractice/Data/pali.db"

# Enum mappings (matching C# Enums.cs)
CASES = {1: "Nominative", 2: "Accusative", 3: "Instrumental", 4: "Dative",
         5: "Ablative", 6: "Genitive", 7: "Locative", 8: "Vocative"}
GENDERS = {1: "Masculine", 2: "Neuter", 3: "Feminine"}
NUMBERS = {1: "Singular", 2: "Plural"}
TENSES = {1: "Present", 2: "Imperative", 3: "Optative", 4: "Future"}
PERSONS = {1: "First", 2: "Second", 3: "Third"}
VOICES = {1: "Active", 2: "Reflexive"}

# Pattern mappings to settings categories
NOUN_PATTERN_TO_SETTINGS = {
    # Masculine
    "a masc": ("Masculine", "a"), "a2 masc": ("Masculine", "a"), "a masc east": ("Masculine", "a"),
    "a masc pl": ("Masculine", "a"), "addha masc": ("Masculine", "a"),
    "rāja masc": ("Masculine", "a"), "brahma masc": ("Masculine", "a"),
    "yuva masc": ("Masculine", "a"), "go masc": ("Masculine", "a"),
    "i masc": ("Masculine", "i"),
    "ī masc": ("Masculine", "ī"), "ī masc pl": ("Masculine", "ī"),
    "u masc": ("Masculine", "u"), "u masc pl": ("Masculine", "u"), "jantu masc": ("Masculine", "u"),
    "ū masc": ("Masculine", "ū"),
    "as masc": ("Masculine", "as"),
    "ar masc": ("Masculine", "ar"), "ar2 masc": ("Masculine", "ar"),
    "ant masc": ("Masculine", "ant"), "anta masc": ("Masculine", "ant"),
    "arahant masc": ("Masculine", "ant"), "bhavant masc": ("Masculine", "ant"), "santa masc": ("Masculine", "ant"),
    # Feminine
    "ā fem": ("Feminine", "ā"), "parisā fem": ("Feminine", "ā"),
    "i fem": ("Feminine", "i"), "jāti fem": ("Feminine", "i"), "ratti fem": ("Feminine", "i"),
    "ī fem": ("Feminine", "ī"), "nadī fem": ("Feminine", "ī"), "pokkharaṇī fem": ("Feminine", "ī"),
    "u fem": ("Feminine", "u"),
    "ar fem": ("Feminine", "ar"), "mātar fem": ("Feminine", "ar"),
    # Neuter
    "a nt": ("Neuter", "a"), "a nt east": ("Neuter", "a"), "a nt irreg": ("Neuter", "a"),
    "a nt pl": ("Neuter", "a"), "kamma nt": ("Neuter", "a"),
    "i nt": ("Neuter", "i"),
    "u nt": ("Neuter", "u"),
}

VERB_PATTERN_TO_ENDING = {
    "ati pr": "ati", "atthi pr": "ati", "dakkhati pr": "ati", "dammi pr": "ati",
    "hanati pr": "ati", "hoti pr": "ati", "kubbati pr": "ati", "natthi pr": "ati",
    "āti pr": "āti",
    "eti pr": "eti", "eti pr 2": "eti",
    "oti pr": "oti", "brūti pr": "oti", "karoti pr": "oti",
}


class DeclensionFormInfo(NamedTuple):
    lemma_id: int
    case: int
    gender: int
    number: int
    ending_id: int


class ConjugationFormInfo(NamedTuple):
    lemma_id: int
    tense: int
    person: int
    number: int
    voice: int
    ending_id: int


def parse_declension_form_id(form_id: int) -> DeclensionFormInfo:
    """Parse declension form_id: LLLLLCGNE"""
    ending_id = form_id % 10
    form_id //= 10
    number = form_id % 10
    form_id //= 10
    gender = form_id % 10
    form_id //= 10
    case = form_id % 10
    lemma_id = form_id // 10
    return DeclensionFormInfo(lemma_id, case, gender, number, ending_id)


def parse_conjugation_form_id(form_id: int) -> ConjugationFormInfo:
    """Parse conjugation form_id: LLLLLTNVPE → LLLLLTP NVE"""
    ending_id = form_id % 10
    form_id //= 10
    voice = form_id % 10
    form_id //= 10
    number = form_id % 10
    form_id //= 10
    person = form_id % 10
    form_id //= 10
    tense = form_id % 10
    lemma_id = form_id // 10
    return ConjugationFormInfo(lemma_id, tense, person, number, voice, ending_id)


def analyze_noun_corpus():
    """Analyze noun corpus forms by filter combinations."""
    conn = sqlite3.connect(PALI_DB)

    # Get all nouns with their patterns (use lemma_id, not id!)
    nouns = {}
    for row in conn.execute("SELECT lemma_id, pattern FROM nouns"):
        nouns[row[0]] = row[1]

    # Get corpus form_ids
    corpus_forms = set()
    for row in conn.execute("SELECT form_id FROM nouns_corpus_forms"):
        corpus_forms.add(row[0])

    # Parse corpus forms and aggregate by filter dimensions
    stats = defaultdict(lambda: {"total": 0, "corpus": 0, "lemmas": set()})

    # We need to iterate through all possible forms to understand coverage
    # For this, let's get the form_ids directly and parse them

    # Build a comprehensive stats structure
    by_case = defaultdict(lambda: {"corpus": 0, "lemmas": set()})
    by_gender_pattern = defaultdict(lambda: {"corpus": 0, "lemmas": set()})
    by_number = defaultdict(lambda: {"corpus": 0, "lemmas": set()})
    by_case_number = defaultdict(lambda: {"corpus": 0, "lemmas": set()})
    by_case_gender = defaultdict(lambda: {"corpus": 0, "lemmas": set()})
    by_gender_pattern_case = defaultdict(lambda: {"corpus": 0, "lemmas": set()})
    by_gender_pattern_case_number = defaultdict(lambda: {"corpus": 0, "lemmas": set()})

    for form_id in corpus_forms:
        info = parse_declension_form_id(form_id)
        pattern = nouns.get(info.lemma_id)
        if not pattern:
            continue

        gender_pattern = NOUN_PATTERN_TO_SETTINGS.get(pattern)
        if not gender_pattern:
            continue

        gender_name, pattern_suffix = gender_pattern
        case_name = CASES.get(info.case, f"?{info.case}")
        number_name = NUMBERS.get(info.number, f"?{info.number}")

        # Aggregate stats
        by_case[case_name]["corpus"] += 1
        by_case[case_name]["lemmas"].add(info.lemma_id)

        by_gender_pattern[(gender_name, pattern_suffix)]["corpus"] += 1
        by_gender_pattern[(gender_name, pattern_suffix)]["lemmas"].add(info.lemma_id)

        by_number[number_name]["corpus"] += 1
        by_number[number_name]["lemmas"].add(info.lemma_id)

        by_case_number[(case_name, number_name)]["corpus"] += 1
        by_case_number[(case_name, number_name)]["lemmas"].add(info.lemma_id)

        by_case_gender[(case_name, gender_name)]["corpus"] += 1
        by_case_gender[(case_name, gender_name)]["lemmas"].add(info.lemma_id)

        by_gender_pattern_case[(gender_name, pattern_suffix, case_name)]["corpus"] += 1
        by_gender_pattern_case[(gender_name, pattern_suffix, case_name)]["lemmas"].add(info.lemma_id)

        by_gender_pattern_case_number[(gender_name, pattern_suffix, case_name, number_name)]["corpus"] += 1
        by_gender_pattern_case_number[(gender_name, pattern_suffix, case_name, number_name)]["lemmas"].add(info.lemma_id)

    conn.close()

    return {
        "by_case": dict(by_case),
        "by_gender_pattern": dict(by_gender_pattern),
        "by_number": dict(by_number),
        "by_case_number": dict(by_case_number),
        "by_case_gender": dict(by_case_gender),
        "by_gender_pattern_case": dict(by_gender_pattern_case),
        "by_gender_pattern_case_number": dict(by_gender_pattern_case_number),
        "total_corpus": len(corpus_forms),
    }


def analyze_verb_corpus():
    """Analyze verb corpus forms by filter combinations."""
    conn = sqlite3.connect(PALI_DB)

    # Get all verbs with their patterns (use lemma_id, not id!)
    verbs = {}
    for row in conn.execute("SELECT lemma_id, pattern FROM verbs"):
        verbs[row[0]] = row[1]

    # Get non-reflexive verbs
    nonreflexive = set()
    for row in conn.execute("SELECT lemma_id FROM verbs_nonreflexive"):
        nonreflexive.add(row[0])

    # Get corpus form_ids
    corpus_forms = set()
    for row in conn.execute("SELECT form_id FROM verbs_corpus_forms"):
        corpus_forms.add(row[0])

    # Aggregate by filter dimensions
    by_tense = defaultdict(lambda: {"corpus": 0, "lemmas": set()})
    by_person = defaultdict(lambda: {"corpus": 0, "lemmas": set()})
    by_number = defaultdict(lambda: {"corpus": 0, "lemmas": set()})
    by_voice = defaultdict(lambda: {"corpus": 0, "lemmas": set()})
    by_ending = defaultdict(lambda: {"corpus": 0, "lemmas": set()})
    by_tense_person = defaultdict(lambda: {"corpus": 0, "lemmas": set()})
    by_tense_number = defaultdict(lambda: {"corpus": 0, "lemmas": set()})
    by_tense_voice = defaultdict(lambda: {"corpus": 0, "lemmas": set()})
    by_person_number = defaultdict(lambda: {"corpus": 0, "lemmas": set()})
    by_ending_tense = defaultdict(lambda: {"corpus": 0, "lemmas": set()})
    by_ending_person = defaultdict(lambda: {"corpus": 0, "lemmas": set()})
    by_ending_tense_person = defaultdict(lambda: {"corpus": 0, "lemmas": set()})
    by_ending_tense_person_number = defaultdict(lambda: {"corpus": 0, "lemmas": set()})
    by_tense_person_number_voice = defaultdict(lambda: {"corpus": 0, "lemmas": set()})

    for form_id in corpus_forms:
        info = parse_conjugation_form_id(form_id)
        pattern = verbs.get(info.lemma_id)
        if not pattern:
            continue

        ending = VERB_PATTERN_TO_ENDING.get(pattern)
        if not ending:
            continue

        tense_name = TENSES.get(info.tense, f"?{info.tense}")
        person_name = PERSONS.get(info.person, f"?{info.person}")
        number_name = NUMBERS.get(info.number, f"?{info.number}")
        voice_name = VOICES.get(info.voice, f"?{info.voice}")

        # Aggregate stats
        by_tense[tense_name]["corpus"] += 1
        by_tense[tense_name]["lemmas"].add(info.lemma_id)

        by_person[person_name]["corpus"] += 1
        by_person[person_name]["lemmas"].add(info.lemma_id)

        by_number[number_name]["corpus"] += 1
        by_number[number_name]["lemmas"].add(info.lemma_id)

        by_voice[voice_name]["corpus"] += 1
        by_voice[voice_name]["lemmas"].add(info.lemma_id)

        by_ending[ending]["corpus"] += 1
        by_ending[ending]["lemmas"].add(info.lemma_id)

        by_tense_person[(tense_name, person_name)]["corpus"] += 1
        by_tense_person[(tense_name, person_name)]["lemmas"].add(info.lemma_id)

        by_tense_number[(tense_name, number_name)]["corpus"] += 1
        by_tense_number[(tense_name, number_name)]["lemmas"].add(info.lemma_id)

        by_tense_voice[(tense_name, voice_name)]["corpus"] += 1
        by_tense_voice[(tense_name, voice_name)]["lemmas"].add(info.lemma_id)

        by_person_number[(person_name, number_name)]["corpus"] += 1
        by_person_number[(person_name, number_name)]["lemmas"].add(info.lemma_id)

        by_ending_tense[(ending, tense_name)]["corpus"] += 1
        by_ending_tense[(ending, tense_name)]["lemmas"].add(info.lemma_id)

        by_ending_person[(ending, person_name)]["corpus"] += 1
        by_ending_person[(ending, person_name)]["lemmas"].add(info.lemma_id)

        by_ending_tense_person[(ending, tense_name, person_name)]["corpus"] += 1
        by_ending_tense_person[(ending, tense_name, person_name)]["lemmas"].add(info.lemma_id)

        by_ending_tense_person_number[(ending, tense_name, person_name, number_name)]["corpus"] += 1
        by_ending_tense_person_number[(ending, tense_name, person_name, number_name)]["lemmas"].add(info.lemma_id)

        by_tense_person_number_voice[(tense_name, person_name, number_name, voice_name)]["corpus"] += 1
        by_tense_person_number_voice[(tense_name, person_name, number_name, voice_name)]["lemmas"].add(info.lemma_id)

    conn.close()

    return {
        "by_tense": dict(by_tense),
        "by_person": dict(by_person),
        "by_number": dict(by_number),
        "by_voice": dict(by_voice),
        "by_ending": dict(by_ending),
        "by_tense_person": dict(by_tense_person),
        "by_tense_number": dict(by_tense_number),
        "by_tense_voice": dict(by_tense_voice),
        "by_person_number": dict(by_person_number),
        "by_ending_tense": dict(by_ending_tense),
        "by_ending_person": dict(by_ending_person),
        "by_ending_tense_person": dict(by_ending_tense_person),
        "by_ending_tense_person_number": dict(by_ending_tense_person_number),
        "by_tense_person_number_voice": dict(by_tense_person_number_voice),
        "total_corpus": len(corpus_forms),
        "nonreflexive_count": len(nonreflexive),
    }


def find_zero_combinations_nouns(noun_stats):
    """Find filter combinations that would yield zero corpus forms for nouns."""
    # Define all possible values for each filter
    all_cases = list(CASES.values())
    all_genders = list(GENDERS.values())
    all_numbers = list(NUMBERS.values())

    # Get all patterns by gender from actual data
    patterns_by_gender = defaultdict(set)
    for (gender, pattern), _ in noun_stats["by_gender_pattern"].items():
        patterns_by_gender[gender].add(pattern)

    zero_combos = []
    low_combos = []  # < 10 corpus forms

    # Check gender+pattern combinations that are missing
    for gender in all_genders:
        expected_patterns = {
            "Masculine": {"a", "i", "ī", "u", "ū", "as", "ar", "ant"},
            "Feminine": {"ā", "i", "ī", "u", "ar"},
            "Neuter": {"a", "i", "u"},
        }
        for pattern in expected_patterns.get(gender, []):
            key = (gender, pattern)
            if key not in noun_stats["by_gender_pattern"]:
                zero_combos.append(f"Gender={gender}, Pattern={pattern}: 0 corpus forms")
            else:
                count = noun_stats["by_gender_pattern"][key]["corpus"]
                lemmas = len(noun_stats["by_gender_pattern"][key]["lemmas"])
                if count < 10:
                    low_combos.append(f"Gender={gender}, Pattern={pattern}: {count} corpus forms ({lemmas} lemmas)")

    # Check gender+pattern+case combinations
    for (gender, pattern), data in noun_stats["by_gender_pattern"].items():
        for case in all_cases:
            key = (gender, pattern, case)
            if key not in noun_stats["by_gender_pattern_case"]:
                zero_combos.append(f"Gender={gender}, Pattern={pattern}, Case={case}: 0 corpus forms")
            else:
                count = noun_stats["by_gender_pattern_case"][key]["corpus"]
                lemmas = len(noun_stats["by_gender_pattern_case"][key]["lemmas"])
                if count < 5:
                    low_combos.append(f"Gender={gender}, Pattern={pattern}, Case={case}: {count} corpus forms ({lemmas} lemmas)")

    # Check gender+pattern+case+number (most specific)
    for (gender, pattern, case), _ in noun_stats["by_gender_pattern_case"].items():
        for number in all_numbers:
            key = (gender, pattern, case, number)
            if key not in noun_stats["by_gender_pattern_case_number"]:
                zero_combos.append(f"Gender={gender}, Pattern={pattern}, Case={case}, Number={number}: 0 corpus forms")
            else:
                count = noun_stats["by_gender_pattern_case_number"][key]["corpus"]
                lemmas = len(noun_stats["by_gender_pattern_case_number"][key]["lemmas"])
                if count < 3:
                    low_combos.append(f"Gender={gender}, Pattern={pattern}, Case={case}, Number={number}: {count} corpus forms ({lemmas} lemmas)")

    return zero_combos, low_combos


def find_zero_combinations_verbs(verb_stats):
    """Find filter combinations that would yield zero corpus forms for verbs."""
    all_tenses = list(TENSES.values())
    all_persons = list(PERSONS.values())
    all_numbers = list(NUMBERS.values())
    all_voices = list(VOICES.values())
    all_endings = ["ati", "āti", "eti", "oti"]

    zero_combos = []
    low_combos = []  # < 10 corpus forms

    # Check ending+tense combinations
    for ending in all_endings:
        for tense in all_tenses:
            key = (ending, tense)
            if key not in verb_stats["by_ending_tense"]:
                zero_combos.append(f"Ending={ending}, Tense={tense}: 0 corpus forms")
            else:
                count = verb_stats["by_ending_tense"][key]["corpus"]
                lemmas = len(verb_stats["by_ending_tense"][key]["lemmas"])
                if count < 10:
                    low_combos.append(f"Ending={ending}, Tense={tense}: {count} corpus forms ({lemmas} lemmas)")

    # Check tense+voice combinations
    for tense in all_tenses:
        for voice in all_voices:
            key = (tense, voice)
            if key not in verb_stats["by_tense_voice"]:
                zero_combos.append(f"Tense={tense}, Voice={voice}: 0 corpus forms")
            else:
                count = verb_stats["by_tense_voice"][key]["corpus"]
                lemmas = len(verb_stats["by_tense_voice"][key]["lemmas"])
                if count < 10:
                    low_combos.append(f"Tense={tense}, Voice={voice}: {count} corpus forms ({lemmas} lemmas)")

    # Check ending+tense+person combinations
    for ending in all_endings:
        for tense in all_tenses:
            for person in all_persons:
                key = (ending, tense, person)
                if key not in verb_stats["by_ending_tense_person"]:
                    zero_combos.append(f"Ending={ending}, Tense={tense}, Person={person}: 0 corpus forms")
                else:
                    count = verb_stats["by_ending_tense_person"][key]["corpus"]
                    lemmas = len(verb_stats["by_ending_tense_person"][key]["lemmas"])
                    if count < 5:
                        low_combos.append(f"Ending={ending}, Tense={tense}, Person={person}: {count} corpus forms ({lemmas} lemmas)")

    # Check tense+person+number+voice (most specific excluding ending)
    for tense in all_tenses:
        for person in all_persons:
            for number in all_numbers:
                for voice in all_voices:
                    key = (tense, person, number, voice)
                    if key not in verb_stats["by_tense_person_number_voice"]:
                        zero_combos.append(f"Tense={tense}, Person={person}, Number={number}, Voice={voice}: 0 corpus forms")
                    else:
                        count = verb_stats["by_tense_person_number_voice"][key]["corpus"]
                        lemmas = len(verb_stats["by_tense_person_number_voice"][key]["lemmas"])
                        if count < 5:
                            low_combos.append(f"Tense={tense}, Person={person}, Number={number}, Voice={voice}: {count} corpus forms ({lemmas} lemmas)")

    return zero_combos, low_combos


def print_section(title, items, max_items=50):
    """Print a section with items."""
    print(f"\n{'='*60}")
    print(f" {title}")
    print('='*60)
    if not items:
        print("  (none)")
    else:
        for item in items[:max_items]:
            print(f"  • {item}")
        if len(items) > max_items:
            print(f"  ... and {len(items) - max_items} more")


def print_stats_table(title, data, key_formatter=str):
    """Print statistics in a table format."""
    print(f"\n{title}")
    print("-" * 60)

    # Sort by corpus count ascending to show lowest first
    sorted_items = sorted(data.items(), key=lambda x: x[1]["corpus"])

    for key, stats in sorted_items:
        key_str = key_formatter(key) if callable(key_formatter) else str(key)
        corpus = stats["corpus"]
        lemmas = len(stats["lemmas"])
        print(f"  {key_str:40} {corpus:6} forms ({lemmas:4} lemmas)")


def main():
    print("="*70)
    print(" PALI PRACTICE - CORPUS FILTER COVERAGE ANALYSIS")
    print("="*70)

    # Analyze nouns
    print("\n\n" + "="*70)
    print(" NOUN DECLENSION ANALYSIS")
    print("="*70)

    noun_stats = analyze_noun_corpus()
    print(f"\nTotal noun corpus forms: {noun_stats['total_corpus']}")

    # Show basic distributions
    print_stats_table("By Case:", noun_stats["by_case"])
    print_stats_table("By Gender + Pattern:", noun_stats["by_gender_pattern"],
                     lambda x: f"{x[0]:12} {x[1]}")
    print_stats_table("By Number:", noun_stats["by_number"])

    # Find problematic combinations
    zero_nouns, low_nouns = find_zero_combinations_nouns(noun_stats)
    print_section("ZERO corpus forms (problematic!)", zero_nouns)
    print_section("LOW corpus forms (<5 or <10)", low_nouns)

    # Analyze verbs
    print("\n\n" + "="*70)
    print(" VERB CONJUGATION ANALYSIS")
    print("="*70)

    verb_stats = analyze_verb_corpus()
    print(f"\nTotal verb corpus forms: {verb_stats['total_corpus']}")
    print(f"Verbs without reflexive forms: {verb_stats['nonreflexive_count']}")

    # Show basic distributions
    print_stats_table("By Tense:", verb_stats["by_tense"])
    print_stats_table("By Person:", verb_stats["by_person"])
    print_stats_table("By Number:", verb_stats["by_number"])
    print_stats_table("By Voice:", verb_stats["by_voice"])
    print_stats_table("By Ending:", verb_stats["by_ending"])
    print_stats_table("By Tense + Voice:", verb_stats["by_tense_voice"],
                     lambda x: f"{x[0]:12} {x[1]}")

    # Find problematic combinations
    zero_verbs, low_verbs = find_zero_combinations_verbs(verb_stats)
    print_section("ZERO corpus forms (problematic!)", zero_verbs)
    print_section("LOW corpus forms (<5 or <10)", low_verbs)

    # Summary tables
    print("\n\n" + "="*70)
    print(" SUMMARY TABLES")
    print("="*70)

    # Noun patterns summary table
    print("\n## Noun Declension Coverage by Pattern\n")
    print("| Gender | Pattern | Corpus Forms | Lemmas |")
    print("|--------|---------|--------------|--------|")

    # Sort by gender order, then by corpus count descending
    gender_order = {"Masculine": 1, "Feminine": 2, "Neuter": 3}
    sorted_noun_patterns = sorted(
        noun_stats["by_gender_pattern"].items(),
        key=lambda x: (gender_order.get(x[0][0], 99), -x[1]["corpus"])
    )
    for (gender, pattern), stats in sorted_noun_patterns:
        print(f"| {gender} | {pattern} | {stats['corpus']:,} | {len(stats['lemmas'])} |")

    # Verb patterns summary table
    print("\n## Verb Conjugation Coverage by Ending\n")
    print("| Ending | Corpus Forms | Lemmas |")
    print("|--------|--------------|--------|")

    sorted_verb_endings = sorted(
        verb_stats["by_ending"].items(),
        key=lambda x: -x[1]["corpus"]
    )
    for ending, stats in sorted_verb_endings:
        print(f"| {ending} | {stats['corpus']:,} | {len(stats['lemmas'])} |")

    # Verb by tense
    print("\n## Verb Conjugation Coverage by Tense\n")
    print("| Tense | Corpus Forms | Lemmas |")
    print("|-------|--------------|--------|")

    sorted_verb_tenses = sorted(
        verb_stats["by_tense"].items(),
        key=lambda x: -x[1]["corpus"]
    )
    for tense, stats in sorted_verb_tenses:
        print(f"| {tense} | {stats['corpus']:,} | {len(stats['lemmas'])} |")

    # Verb by voice
    print("\n## Verb Conjugation Coverage by Voice\n")
    print("| Voice | Corpus Forms | Lemmas |")
    print("|-------|--------------|--------|")

    sorted_verb_voices = sorted(
        verb_stats["by_voice"].items(),
        key=lambda x: -x[1]["corpus"]
    )
    for voice, stats in sorted_verb_voices:
        print(f"| {voice} | {stats['corpus']:,} | {len(stats['lemmas'])} |")

    # Verb by tense + voice
    print("\n## Verb Conjugation Coverage by Tense + Voice\n")
    print("| Tense | Voice | Corpus Forms | Lemmas |")
    print("|-------|-------|--------------|--------|")

    sorted_tense_voice = sorted(
        verb_stats["by_tense_voice"].items(),
        key=lambda x: -x[1]["corpus"]
    )
    for (tense, voice), stats in sorted_tense_voice:
        print(f"| {tense} | {voice} | {stats['corpus']:,} | {len(stats['lemmas'])} |")

    # Noun by case
    print("\n## Noun Declension Coverage by Case\n")
    print("| Case | Corpus Forms | Lemmas |")
    print("|------|--------------|--------|")

    # Filter out invalid cases (like ?0)
    valid_cases = {k: v for k, v in noun_stats["by_case"].items() if not k.startswith("?")}
    sorted_cases = sorted(valid_cases.items(), key=lambda x: -x[1]["corpus"])
    for case, stats in sorted_cases:
        print(f"| {case} | {stats['corpus']:,} | {len(stats['lemmas'])} |")

    # Final summary
    print("\n\n" + "="*70)
    print(" PROBLEM SUMMARY")
    print("="*70)
    print(f"\nNoun combinations with ZERO corpus forms: {len(zero_nouns)}")
    print(f"Noun combinations with LOW corpus forms: {len(low_nouns)}")
    print(f"Verb combinations with ZERO corpus forms: {len(zero_verbs)}")
    print(f"Verb combinations with LOW corpus forms: {len(low_verbs)}")

    return noun_stats, verb_stats


if __name__ == "__main__":
    main()

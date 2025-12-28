#!/usr/bin/env python3
"""
Inflection validation module for PaliPractice extraction.

Validates completeness of noun declensions and verb conjugations during import,
outputting irregularity reports to a timestamped log file.
"""

from datetime import datetime
from pathlib import Path
from typing import Dict, List, Set, Tuple, Any, Optional
from dataclasses import dataclass, field


@dataclass
class PluralOnlyMatch:
    """Match info for a plural-only noun against singular lemmas."""
    lemma: str
    pattern: str
    match_ratio: float  # 0.0-1.0


@dataclass
class NounIrregularity:
    """Represents a noun with incomplete or unusual declensions."""
    lemma: str
    pattern: str
    is_plural_only: bool = False  # Pattern ends in 'pl'
    is_singular_only: bool = False  # No plural forms in template
    missing_cases: List[str] = field(default_factory=list)  # e.g., ["acc_sg", "dat_pl"]
    missing_numbers: List[str] = field(default_factory=list)  # "sg" or "pl"
    plural_only_matches: List[PluralOnlyMatch] = field(default_factory=list)  # Candidates for plural-only


@dataclass
class VerbIrregularity:
    """Represents a verb with incomplete or unusual conjugations."""
    lemma: str
    pattern: str
    unusual_tenses: List[str] = field(default_factory=list)  # e.g., ["aor"]
    missing_tenses: List[str] = field(default_factory=list)  # e.g., ["fut"]
    incomplete_conjugations: List[str] = field(default_factory=list)  # e.g., ["imp_2nd_sg"]
    is_impersonal: bool = False  # Only 3rd person forms
    defective_persons: Dict[str, List[str]] = field(default_factory=dict)  # tense -> missing persons
    has_reflexive: bool = False


class InflectionValidator:
    """
    Validates noun declensions and verb conjugations for completeness.

    Usage:
        validator = InflectionValidator()

        # During extraction loop:
        validator.validate_noun(lemma, pattern, forms)
        validator.validate_verb(lemma, pattern, forms)

        # At end of extraction:
        validator.write_report()
        validator.print_summary()
    """

    # Expected noun case/number combinations (8 cases x 2 numbers = 16)
    EXPECTED_CASES = {1, 2, 3, 4, 5, 6, 7, 8}  # Nominative through Vocative
    EXPECTED_NUMBERS = {1, 2}  # Singular, Plural

    # Expected verb tense/person/number combinations (4 tenses x 3 persons x 2 numbers = 24 per voice)
    EXPECTED_TENSES = {1, 2, 3, 4}  # Present, Imperative, Optative, Future (NO Aorist=5)
    EXPECTED_PERSONS = {1, 2, 3}  # First, Second, Third

    # Case ID to name mapping
    CASE_NAMES = {
        1: 'nom', 2: 'acc', 3: 'instr', 4: 'dat',
        5: 'abl', 6: 'gen', 7: 'loc', 8: 'voc'
    }

    # Tense ID to name mapping
    TENSE_NAMES = {
        1: 'pr', 2: 'imp', 3: 'opt', 4: 'fut', 5: 'aor'
    }

    # Person ID to name mapping
    PERSON_NAMES = {1: '1st', 2: '2nd', 3: '3rd'}

    # Number ID to name mapping
    NUMBER_NAMES = {1: 'sg', 2: 'pl'}

    def __init__(self, log_dir: Optional[Path] = None):
        """Initialize validator with optional log directory."""
        self.log_dir = log_dir or Path(__file__).parent
        self.noun_irregularities: List[NounIrregularity] = []
        self.verb_irregularities: List[VerbIrregularity] = []
        self.noun_count = 0
        self.verb_count = 0

    def is_plural_only_pattern(self, pattern: str) -> bool:
        """Check if pattern is a known plural-only pattern (ends in 'pl')."""
        return pattern.strip().endswith(' pl')

    def validate_noun(
        self,
        lemma: str,
        pattern: str,
        forms: List[Dict[str, Any]],
        plural_only_matches: Optional[List[PluralOnlyMatch]] = None
    ) -> None:
        """
        Validate noun declension completeness.

        Args:
            lemma: The noun lemma (e.g., "dhamma")
            pattern: The inflection pattern (e.g., "a masc", "a masc pl")
            forms: List of form dicts from parse_inflection_template()
            plural_only_matches: For plural-only patterns, list of stem matches with ratios
        """
        self.noun_count += 1

        # Track which case/number combinations we have
        found_cases: Set[int] = set()
        found_numbers: Set[int] = set()
        found_combos: Set[Tuple[int, int]] = set()  # (case, number)

        for form in forms:
            case_val = form.get('case_name', 0)
            number_val = form.get('number', 0)

            if case_val > 0:
                found_cases.add(case_val)
            if number_val > 0:
                found_numbers.add(number_val)
            if case_val > 0 and number_val > 0:
                found_combos.add((case_val, number_val))

        # Check for irregularities
        is_plural_only = self.is_plural_only_pattern(pattern)

        # Check for singular-only (no plural forms in template)
        is_singular_only = 2 not in found_numbers and 1 in found_numbers and not is_plural_only

        # For plural-only patterns, only expect plural number
        # For singular-only nouns, only expect singular number
        if is_plural_only:
            expected_numbers = {2}
        elif is_singular_only:
            expected_numbers = {1}
        else:
            expected_numbers = self.EXPECTED_NUMBERS

        missing_numbers = expected_numbers - found_numbers

        # Build missing combo list
        missing_combos = []
        for case_val in self.EXPECTED_CASES:
            for number_val in expected_numbers:
                if (case_val, number_val) not in found_combos:
                    case_name = self.CASE_NAMES.get(case_val, f'case{case_val}')
                    number_name = self.NUMBER_NAMES.get(number_val, f'num{number_val}')
                    missing_combos.append(f"{case_name}_{number_name}")

        # Only record if there are irregularities
        if missing_combos or is_plural_only or is_singular_only:
            irregularity = NounIrregularity(
                lemma=lemma,
                pattern=pattern,
                is_plural_only=is_plural_only,
                is_singular_only=is_singular_only,
                missing_cases=missing_combos,
                missing_numbers=[self.NUMBER_NAMES.get(n, f'num{n}') for n in missing_numbers],
                plural_only_matches=plural_only_matches or []
            )
            self.noun_irregularities.append(irregularity)

    def validate_verb(self, lemma: str, pattern: str, forms: List[Dict[str, Any]]) -> None:
        """
        Validate verb conjugation completeness.

        Args:
            lemma: The verb lemma (e.g., "karoti")
            pattern: The inflection pattern (e.g., "ati pr")
            forms: List of form dicts from parse_inflection_template()
        """
        self.verb_count += 1

        # Track which tense/person/number combinations we have (separate for active/reflexive)
        found_tenses: Set[int] = set()
        found_persons: Set[int] = set()  # Track all persons found across all tenses
        found_combos_active: Set[Tuple[int, int, int]] = set()  # (tense, person, number)
        found_combos_reflexive: Set[Tuple[int, int, int]] = set()
        persons_by_tense: Dict[int, Set[int]] = {}  # tense -> set of persons found
        has_reflexive = False
        unusual_tenses: Set[int] = set()

        for form in forms:
            tense_val = form.get('tense', 0)
            person_val = form.get('person', 0)
            number_val = form.get('number', 0)
            reflexive_val = form.get('reflexive', 0)

            if tense_val > 0:
                found_tenses.add(tense_val)

                # Check for unusual tenses (Aorist for pr verbs)
                if tense_val == 5:  # Aorist
                    unusual_tenses.add(tense_val)

            if person_val > 0:
                found_persons.add(person_val)
                # Track persons by tense (for defective person detection)
                if tense_val > 0:
                    if tense_val not in persons_by_tense:
                        persons_by_tense[tense_val] = set()
                    persons_by_tense[tense_val].add(person_val)

            if tense_val > 0 and person_val > 0 and number_val > 0:
                combo = (tense_val, person_val, number_val)
                if reflexive_val == 1:
                    found_combos_reflexive.add(combo)
                    has_reflexive = True
                else:
                    found_combos_active.add(combo)

        # Check for missing expected tenses
        missing_tenses = self.EXPECTED_TENSES - found_tenses

        # Check for impersonal verb (only 3rd person forms)
        is_impersonal = found_persons == {3} and len(found_persons) == 1

        # Check for defective persons in each tense (missing some but not all persons)
        defective_persons: Dict[str, List[str]] = {}
        for tense_val in self.EXPECTED_TENSES:
            if tense_val in persons_by_tense:
                missing_persons_in_tense = self.EXPECTED_PERSONS - persons_by_tense[tense_val]
                # Only report if some persons are missing but not all (that would be missing tense)
                if missing_persons_in_tense and persons_by_tense[tense_val]:
                    # Don't report as defective if it's an impersonal verb
                    if not is_impersonal:
                        tense_name = self.TENSE_NAMES.get(tense_val, f'tense{tense_val}')
                        defective_persons[tense_name] = [
                            self.PERSON_NAMES.get(p, f'per{p}') for p in sorted(missing_persons_in_tense)
                        ]

        # Build missing combo list for active voice
        missing_combos = []
        for tense_val in self.EXPECTED_TENSES:
            for person_val in self.EXPECTED_PERSONS:
                for number_val in self.EXPECTED_NUMBERS:
                    if (tense_val, person_val, number_val) not in found_combos_active:
                        tense_name = self.TENSE_NAMES.get(tense_val, f'tense{tense_val}')
                        person_name = self.PERSON_NAMES.get(person_val, f'per{person_val}')
                        number_name = self.NUMBER_NAMES.get(number_val, f'num{number_val}')
                        missing_combos.append(f"{tense_name}_{person_name}_{number_name}")

        # Only record if there are irregularities
        if missing_tenses or unusual_tenses or missing_combos or is_impersonal or defective_persons:
            irregularity = VerbIrregularity(
                lemma=lemma,
                pattern=pattern,
                unusual_tenses=[self.TENSE_NAMES.get(t, f'tense{t}') for t in unusual_tenses],
                missing_tenses=[self.TENSE_NAMES.get(t, f'tense{t}') for t in missing_tenses],
                incomplete_conjugations=missing_combos,
                is_impersonal=is_impersonal,
                defective_persons=defective_persons,
                has_reflexive=has_reflexive
            )
            self.verb_irregularities.append(irregularity)

    def write_report(self) -> Path:
        """
        Write validation report to a timestamped log file.

        Returns:
            Path to the generated log file.
        """
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        log_path = self.log_dir / f"inflection_validation_{timestamp}.log"

        with open(log_path, 'w', encoding='utf-8') as f:
            f.write("=" * 60 + "\n")
            f.write("INFLECTION VALIDATION REPORT\n")
            f.write(f"Generated: {datetime.now().isoformat()}\n")
            f.write("=" * 60 + "\n\n")

            # Summary
            f.write("=== SUMMARY ===\n")
            f.write(f"Nouns validated: {self.noun_count}\n")
            f.write(f"Nouns with irregularities: {len(self.noun_irregularities)}\n")
            f.write(f"Verbs validated: {self.verb_count}\n")
            f.write(f"Verbs with irregularities: {len(self.verb_irregularities)}\n\n")

            # Noun irregularities
            self._write_noun_section(f)

            # Verb irregularities
            self._write_verb_section(f)

        return log_path

    def _write_noun_section(self, f) -> None:
        """Write noun irregularities section to file."""
        f.write("=" * 60 + "\n")
        f.write("=== NOUN IRREGULARITIES ===\n")
        f.write("=" * 60 + "\n\n")

        # Separate by type
        plural_only = [i for i in self.noun_irregularities if i.is_plural_only]
        singular_only = [i for i in self.noun_irregularities if i.is_singular_only]
        missing_forms = [i for i in self.noun_irregularities
                         if not i.is_plural_only and not i.is_singular_only and i.missing_cases]

        # Known plural-only patterns
        if plural_only:
            f.write("PLURAL-ONLY (pattern ends in 'pl'):\n")
            f.write("-" * 40 + "\n")
            for irr in plural_only:
                if not irr.plural_only_matches:
                    f.write(f'  {irr.lemma} - "{irr.pattern}" - no other forms with same stem\n')
                else:
                    matches_str = ", ".join(
                        f'{m.lemma} ({m.match_ratio:.0%})' for m in irr.plural_only_matches
                    )
                    f.write(f'  {irr.lemma} - "{irr.pattern}" - {matches_str}\n')
            f.write(f"\nTotal: {len(plural_only)} nouns\n\n")

        # Singular-only nouns
        if singular_only:
            f.write("SINGULAR-ONLY (no plural forms in template):\n")
            f.write("-" * 40 + "\n")
            for irr in singular_only:
                f.write(f'  {irr.lemma} - "{irr.pattern}" - lacks plural declensions\n')
            f.write(f"\nTotal: {len(singular_only)} nouns\n\n")

        # Missing forms
        if missing_forms:
            f.write("MISSING FORMS:\n")
            f.write("-" * 40 + "\n")
            for irr in missing_forms:
                missing_str = ", ".join(irr.missing_cases[:10])
                if len(irr.missing_cases) > 10:
                    missing_str += f" ... (+{len(irr.missing_cases) - 10} more)"
                f.write(f'  {irr.lemma} - "{irr.pattern}" - missing: [{missing_str}]\n')
            f.write(f"\nTotal: {len(missing_forms)} nouns with missing forms\n\n")

        if not plural_only and not singular_only and not missing_forms:
            f.write("No noun irregularities found.\n\n")

    def _write_verb_section(self, f) -> None:
        """Write verb irregularities section to file."""
        f.write("=" * 60 + "\n")
        f.write("=== VERB IRREGULARITIES ===\n")
        f.write("=" * 60 + "\n\n")

        # Separate by type
        unusual = [i for i in self.verb_irregularities if i.unusual_tenses]
        missing_tenses = [i for i in self.verb_irregularities if i.missing_tenses]
        impersonal = [i for i in self.verb_irregularities if i.is_impersonal]
        defective = [i for i in self.verb_irregularities if i.defective_persons]
        incomplete = [i for i in self.verb_irregularities
                      if i.incomplete_conjugations and not i.missing_tenses and not i.is_impersonal]

        # Unusual tenses
        if unusual:
            f.write("UNUSUAL TENSES:\n")
            f.write("-" * 40 + "\n")
            for irr in unusual:
                tenses_str = ", ".join(irr.unusual_tenses)
                f.write(f'  {irr.lemma} - "{irr.pattern}" - has {tenses_str} tense (unexpected for pr pattern)\n')
            f.write(f"\nTotal: {len(unusual)} verbs with unusual tenses\n\n")

        # Missing tenses
        if missing_tenses:
            f.write("MISSING TENSES:\n")
            f.write("-" * 40 + "\n")
            for irr in missing_tenses:
                tenses_str = ", ".join(irr.missing_tenses)
                f.write(f'  {irr.lemma} - "{irr.pattern}" - missing tenses: [{tenses_str}]\n')
            f.write(f"\nTotal: {len(missing_tenses)} verbs with missing tenses\n\n")

        # Impersonal verbs (3rd person only)
        if impersonal:
            f.write("IMPERSONAL (3rd person only):\n")
            f.write("-" * 40 + "\n")
            for irr in impersonal:
                f.write(f'  {irr.lemma} - "{irr.pattern}" - only has 3rd person forms\n')
            f.write(f"\nTotal: {len(impersonal)} impersonal verbs\n\n")

        # Defective persons (missing some persons in certain tenses)
        if defective:
            f.write("DEFECTIVE PERSONS (missing some persons in certain tenses):\n")
            f.write("-" * 40 + "\n")
            for irr in defective:
                for tense, persons in irr.defective_persons.items():
                    persons_str = ", ".join(persons)
                    f.write(f'  {irr.lemma} - "{irr.pattern}" - {tense}: missing [{persons_str}]\n')
            f.write(f"\nTotal: {len(defective)} verbs with defective persons\n\n")

        # Incomplete conjugations (has all tenses but missing person/number combos)
        if incomplete:
            f.write("INCOMPLETE CONJUGATIONS:\n")
            f.write("-" * 40 + "\n")
            for irr in incomplete:
                missing_str = ", ".join(irr.incomplete_conjugations[:10])
                if len(irr.incomplete_conjugations) > 10:
                    missing_str += f" ... (+{len(irr.incomplete_conjugations) - 10} more)"
                f.write(f'  {irr.lemma} - "{irr.pattern}" - missing: [{missing_str}]\n')
            f.write(f"\nTotal: {len(incomplete)} verbs with incomplete conjugations\n\n")

        if not unusual and not missing_tenses and not impersonal and not defective and not incomplete:
            f.write("No verb irregularities found.\n\n")

    def print_summary(self) -> None:
        """Print a brief summary to stdout (not to log file)."""
        noun_plural_only = sum(1 for i in self.noun_irregularities if i.is_plural_only)
        noun_singular_only = sum(1 for i in self.noun_irregularities if i.is_singular_only)
        noun_missing = sum(1 for i in self.noun_irregularities
                           if not i.is_plural_only and not i.is_singular_only and i.missing_cases)
        verb_unusual = sum(1 for i in self.verb_irregularities if i.unusual_tenses)
        verb_missing = sum(1 for i in self.verb_irregularities if i.missing_tenses)
        verb_impersonal = sum(1 for i in self.verb_irregularities if i.is_impersonal)
        verb_defective = sum(1 for i in self.verb_irregularities if i.defective_persons)
        verb_incomplete = sum(1 for i in self.verb_irregularities
                              if i.incomplete_conjugations and not i.missing_tenses and not i.is_impersonal)

        print(f"\n=== INFLECTION VALIDATION SUMMARY ===")
        print(f"Nouns: {self.noun_count} validated, {len(self.noun_irregularities)} with irregularities")
        print(f"  - Plural-only patterns: {noun_plural_only}")
        print(f"  - Singular-only: {noun_singular_only}")
        print(f"  - Missing forms: {noun_missing}")
        print(f"Verbs: {self.verb_count} validated, {len(self.verb_irregularities)} with irregularities")
        print(f"  - Unusual tenses: {verb_unusual}")
        print(f"  - Missing tenses: {verb_missing}")
        print(f"  - Impersonal (3rd only): {verb_impersonal}")
        print(f"  - Defective persons: {verb_defective}")
        print(f"  - Incomplete conjugations: {verb_incomplete}")

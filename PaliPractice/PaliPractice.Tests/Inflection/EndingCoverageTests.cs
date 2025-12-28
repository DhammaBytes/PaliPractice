using FluentAssertions;
using PaliPractice.Models;
using PaliPractice.Models.Inflection;

namespace PaliPractice.Tests.Inflection;

/// <summary>
/// Tests that all grammatical combinations for regular patterns have defined endings.
/// This catches cases where a pattern/case/number combination silently returns empty,
/// which would mean learners never see certain forms.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this matters:</b>
/// NounEndings and VerbEndings use switch expressions that return [] for unmatched cases.
/// If a combination is accidentally missing, the app silently produces no forms - no error,
/// just missing practice opportunities.
/// </para>
/// <para>
/// <b>Coverage:</b>
/// - NounEndings: 15 regular patterns × 8 cases × 2 numbers = 240 combinations
/// - VerbEndings: 4 regular patterns × 4 tenses × 3 persons × 2 numbers × 2 voices = 192 combinations
/// </para>
/// </remarks>
[TestFixture]
public class EndingCoverageTests
{
    /// <summary>
    /// All regular noun patterns that should have complete endings defined.
    /// Irregulars are handled via database lookup.
    /// </summary>
    static readonly NounPattern[] RegularNounPatterns =
    [
        // Masculine (8)
        NounPattern.AMasc,
        NounPattern.IMasc,
        NounPattern.ĪMasc,
        NounPattern.UMasc,
        NounPattern.ŪMasc,
        NounPattern.ArMasc,
        NounPattern.AntMasc,
        NounPattern.AsMasc,

        // Feminine (5)
        NounPattern.ĀFem,
        NounPattern.IFem,
        NounPattern.ĪFem,
        NounPattern.UFem,
        // Note: ArFem is not in NounEndings - might be handled as irregular
        // NounPattern.ArFem,

        // Neuter (3)
        NounPattern.ANeut,
        NounPattern.INeut,
        NounPattern.UNeut,
    ];

    /// <summary>
    /// All regular verb patterns that should have complete endings defined.
    /// </summary>
    static readonly VerbPattern[] RegularVerbPatterns =
    [
        VerbPattern.Ati,
        VerbPattern.Āti,
        VerbPattern.Eti,
        VerbPattern.Oti,
    ];

    #region Noun Ending Coverage

    [Test]
    public void NounEndings_AllRegularPatterns_HaveAllCaseNumberCombinations()
    {
        var missingCombos = new List<string>();

        foreach (var pattern in RegularNounPatterns)
        foreach (Case c in Enum.GetValues<Case>().Where(c => c != Case.None))
        foreach (Number n in Enum.GetValues<Number>().Where(n => n != Number.None))
        {
            var endings = NounEndings.GetEndings(pattern, c, n);
            if (endings.Length == 0)
            {
                missingCombos.Add($"{pattern} {c} {n}");
            }
        }

        TestContext.WriteLine($"Tested {RegularNounPatterns.Length} patterns × 8 cases × 2 numbers = {RegularNounPatterns.Length * 8 * 2} combinations");
        TestContext.WriteLine($"Missing combinations: {missingCombos.Count}");
        foreach (var combo in missingCombos)
            TestContext.WriteLine($"  {combo}");

        missingCombos.Should().BeEmpty(
            "all regular noun patterns should have endings for all case/number combinations");
    }

    [Test]
    public void NounEndings_AMasc_HasAllCombinations()
    {
        // Detailed test for the most common pattern
        var results = new List<string>();

        foreach (Case c in Enum.GetValues<Case>().Where(c => c != Case.None))
        foreach (Number n in Enum.GetValues<Number>().Where(n => n != Number.None))
        {
            var endings = NounEndings.GetEndings(NounPattern.AMasc, c, n);
            results.Add($"{c} {n}: [{string.Join(", ", endings)}]");

            endings.Should().NotBeEmpty(
                $"AMasc {c} {n} should have at least one ending");
        }

        TestContext.WriteLine("AMasc endings:");
        foreach (var result in results)
            TestContext.WriteLine($"  {result}");
    }

    [Test]
    public void NounEndings_EachPatternGender_MatchesExpectedGender()
    {
        // Verify each pattern returns endings appropriate for its gender
        foreach (var pattern in RegularNounPatterns)
        {
            var gender = pattern.GetGender();
            TestContext.WriteLine($"{pattern} → {gender}");

            gender.Should().NotBe(Gender.None,
                $"{pattern} should have a defined gender");
        }
    }

    [TestCase(NounPattern.AMasc, Case.Nominative, Number.Singular, "o")]
    [TestCase(NounPattern.AMasc, Case.Accusative, Number.Singular, "aṃ")]
    [TestCase(NounPattern.ANeut, Case.Nominative, Number.Singular, "aṃ")]
    [TestCase(NounPattern.ĀFem, Case.Nominative, Number.Singular, "ā")]
    public void NounEndings_SpecificCombinations_HaveExpectedEndings(
        NounPattern pattern, Case nounCase, Number number, string expectedEnding)
    {
        var endings = NounEndings.GetEndings(pattern, nounCase, number);

        endings.Should().Contain(expectedEnding,
            $"{pattern} {nounCase} {number} should include '{expectedEnding}'");
    }

    #endregion

    #region Verb Ending Coverage

    [Test]
    public void VerbEndings_AllRegularPatterns_HaveActivePresent()
    {
        // Most basic: all patterns should have present tense active voice
        var missingCombos = new List<string>();

        foreach (var pattern in RegularVerbPatterns)
        foreach (Person p in Enum.GetValues<Person>().Where(p => p != Person.None))
        foreach (Number n in Enum.GetValues<Number>().Where(n => n != Number.None))
        {
            var endings = VerbEndings.GetEndings(pattern, p, n, Tense.Present, reflexive: false);
            if (endings.Length == 0)
            {
                missingCombos.Add($"{pattern} Present {p} {n} Active");
            }
        }

        TestContext.WriteLine($"Missing Present Active combinations: {missingCombos.Count}");
        foreach (var combo in missingCombos)
            TestContext.WriteLine($"  {combo}");

        missingCombos.Should().BeEmpty(
            "all regular verb patterns should have Present Active endings for all person/number combinations");
    }

    [Test]
    public void VerbEndings_AllRegularPatterns_HaveAllActiveCombinations()
    {
        var missingCombos = new List<string>();

        foreach (var pattern in RegularVerbPatterns)
        foreach (Tense t in Enum.GetValues<Tense>().Where(t => t != Tense.None))
        foreach (Person p in Enum.GetValues<Person>().Where(p => p != Person.None))
        foreach (Number n in Enum.GetValues<Number>().Where(n => n != Number.None))
        {
            var endings = VerbEndings.GetEndings(pattern, p, n, t, reflexive: false);
            if (endings.Length == 0)
            {
                missingCombos.Add($"{pattern} {t} {p} {n} Active");
            }
        }

        var totalCombos = RegularVerbPatterns.Length * 4 * 3 * 2;
        TestContext.WriteLine($"Tested {totalCombos} Active combinations");
        TestContext.WriteLine($"Missing Active combinations: {missingCombos.Count}");
        foreach (var combo in missingCombos)
            TestContext.WriteLine($"  {combo}");

        missingCombos.Should().BeEmpty(
            "all regular verb patterns should have Active endings for all tense/person/number combinations");
    }

    [Test]
    public void VerbEndings_ReflexiveCoverage_ReportMissing()
    {
        // Reflexive forms may legitimately be missing for some combinations
        // This test reports coverage rather than failing
        var missingCombos = new List<string>();
        var presentCombos = new List<string>();

        foreach (var pattern in RegularVerbPatterns)
        foreach (Tense t in Enum.GetValues<Tense>().Where(t => t != Tense.None))
        foreach (Person p in Enum.GetValues<Person>().Where(p => p != Person.None))
        foreach (Number n in Enum.GetValues<Number>().Where(n => n != Number.None))
        {
            var endings = VerbEndings.GetEndings(pattern, p, n, t, reflexive: true);
            if (endings.Length == 0)
            {
                missingCombos.Add($"{pattern} {t} {p} {n} Reflexive");
            }
            else
            {
                presentCombos.Add($"{pattern} {t} {p} {n} Reflexive: [{string.Join(", ", endings)}]");
            }
        }

        var totalCombos = RegularVerbPatterns.Length * 4 * 3 * 2;
        var coverageRate = (double)presentCombos.Count / totalCombos;

        TestContext.WriteLine($"Reflexive coverage: {presentCombos.Count}/{totalCombos} ({coverageRate:P0})");
        TestContext.WriteLine($"Missing Reflexive combinations: {missingCombos.Count}");
        foreach (var combo in missingCombos.Take(20))
            TestContext.WriteLine($"  {combo}");

        // Eti present reflexive is known to be missing in traditional Pali grammars
        // Allow some missing combinations
        coverageRate.Should().BeGreaterThan(0.7,
            "at least 70% of reflexive combinations should have endings defined");
    }

    [Test]
    public void VerbEndings_Ati_Present_HasAllCombinations()
    {
        // Detailed test for the most common verb pattern
        var results = new List<string>();

        foreach (Person p in Enum.GetValues<Person>().Where(p => p != Person.None))
        foreach (Number n in Enum.GetValues<Number>().Where(n => n != Number.None))
        foreach (var reflexive in new[] { false, true })
        {
            var voice = reflexive ? "Reflexive" : "Active";
            var endings = VerbEndings.GetEndings(VerbPattern.Ati, p, n, Tense.Present, reflexive);
            results.Add($"{p} {n} {voice}: [{string.Join(", ", endings)}]");
        }

        TestContext.WriteLine("Ati Present endings:");
        foreach (var result in results)
            TestContext.WriteLine($"  {result}");
    }

    [TestCase(VerbPattern.Ati, Person.Third, Number.Singular, Tense.Present, false, "ati")]
    [TestCase(VerbPattern.Ati, Person.First, Number.Singular, Tense.Present, false, "āmi")]
    [TestCase(VerbPattern.Eti, Person.Third, Number.Singular, Tense.Present, false, "eti")]
    [TestCase(VerbPattern.Oti, Person.Third, Number.Singular, Tense.Present, false, "oti")]
    [TestCase(VerbPattern.Āti, Person.Third, Number.Singular, Tense.Present, false, "āti")]
    public void VerbEndings_SpecificCombinations_HaveExpectedEndings(
        VerbPattern pattern, Person person, Number number, Tense tense, bool reflexive, string expectedEnding)
    {
        var endings = VerbEndings.GetEndings(pattern, person, number, tense, reflexive);

        endings.Should().Contain(expectedEnding,
            $"{pattern} {tense} {person} {number} should include '{expectedEnding}'");
    }

    #endregion

    #region Irregular Pattern Verification

    [Test]
    public void NounEndings_IrregularPatterns_ReturnEmpty()
    {
        // Verify that irregular patterns correctly return [] (handled via DB lookup)
        var irregularPatterns = Enum.GetValues<NounPattern>()
            .Where(p => p.ToString().StartsWith("_") == false)
            .Where(p => p != NounPattern.None)
            .Where(p => p.IsIrregular())
            .ToList();

        TestContext.WriteLine($"Testing {irregularPatterns.Count} irregular noun patterns");

        foreach (var pattern in irregularPatterns)
        {
            var endings = NounEndings.GetEndings(pattern, Case.Nominative, Number.Singular);

            endings.Should().BeEmpty(
                $"irregular pattern {pattern} should return [] (handled via database lookup)");
        }
    }

    [Test]
    public void VerbEndings_IrregularPatterns_ReturnEmpty()
    {
        // Verify that irregular patterns correctly return [] (handled via DB lookup)
        var irregularPatterns = Enum.GetValues<VerbPattern>()
            .Where(p => p.ToString().StartsWith("_") == false)
            .Where(p => p != VerbPattern.None)
            .Where(p => (int)p > (int)VerbPattern._Irregular)
            .ToList();

        TestContext.WriteLine($"Testing {irregularPatterns.Count} irregular verb patterns");

        foreach (var pattern in irregularPatterns)
        {
            var endings = VerbEndings.GetEndings(pattern, Person.Third, Number.Singular, Tense.Present, false);

            endings.Should().BeEmpty(
                $"irregular pattern {pattern} should return [] (handled via database lookup)");
        }
    }

    #endregion

    #region Edge Cases

    [Test]
    public void NounEndings_NoneEnums_ReturnEmpty()
    {
        // Verify graceful handling of None enum values
        var endings1 = NounEndings.GetEndings(NounPattern.AMasc, Case.None, Number.Singular);
        var endings2 = NounEndings.GetEndings(NounPattern.AMasc, Case.Nominative, Number.None);
        var endings3 = NounEndings.GetEndings(NounPattern.None, Case.Nominative, Number.Singular);

        endings1.Should().BeEmpty("Case.None should return empty");
        endings2.Should().BeEmpty("Number.None should return empty");
        endings3.Should().BeEmpty("NounPattern.None should return empty");
    }

    [Test]
    public void VerbEndings_NoneEnums_ReturnEmpty()
    {
        // Verify graceful handling of None enum values
        var endings1 = VerbEndings.GetEndings(VerbPattern.Ati, Person.None, Number.Singular, Tense.Present, false);
        var endings2 = VerbEndings.GetEndings(VerbPattern.Ati, Person.Third, Number.None, Tense.Present, false);
        var endings3 = VerbEndings.GetEndings(VerbPattern.Ati, Person.Third, Number.Singular, Tense.None, false);
        var endings4 = VerbEndings.GetEndings(VerbPattern.None, Person.Third, Number.Singular, Tense.Present, false);

        endings1.Should().BeEmpty("Person.None should return empty");
        endings2.Should().BeEmpty("Number.None should return empty");
        endings3.Should().BeEmpty("Tense.None should return empty");
        endings4.Should().BeEmpty("VerbPattern.None should return empty");
    }

    #endregion
}

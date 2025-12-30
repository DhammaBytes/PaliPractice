using FluentAssertions;
using PaliPractice.Models;
using PaliPractice.Models.Inflection;

namespace PaliPractice.Tests.Inflection;

/// <summary>
/// Tests for enum ↔ database string mapping stability.
/// These tests ensure that:
/// 1. All enum values can roundtrip through ToDbString() → Parse()
/// 2. Irregular patterns have valid parent patterns
/// 3. Gender detection via breakpoints works correctly
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this matters:</b>
/// Pattern strings in the database must match exactly what Parse() expects.
/// If ToDbString() produces a different string than Parse() expects, or if
/// enum values are added/removed without updating both methods, the app
/// will throw exceptions when loading words.
/// </para>
/// </remarks>
[TestFixture]
public class PatternEnumStabilityTests
{
    #region NounPattern Tests

    [Test]
    public void NounPattern_AllValues_RoundtripThroughDbString()
    {
        var failures = new List<string>();
        var tested = new List<string>();

        foreach (NounPattern pattern in Enum.GetValues<NounPattern>())
        {
            // Skip None and breakpoint markers
            if (pattern == NounPattern.None) continue;
            if (pattern.ToString().StartsWith("_")) continue;

            try
            {
                var dbString = pattern.ToDbString();
                var parsed = NounPatternHelper.Parse(dbString);

                if (parsed != pattern)
                {
                    failures.Add($"{pattern}: ToDbString='{dbString}' parsed back as {parsed}");
                }
                else
                {
                    tested.Add($"{pattern} ↔ '{dbString}'");
                }
            }
            catch (Exception ex)
            {
                failures.Add($"{pattern}: {ex.GetType().Name} - {ex.Message}");
            }
        }

        TestContext.WriteLine($"Successfully tested {tested.Count} patterns:");
        foreach (var msg in tested)
            TestContext.WriteLine($"  {msg}");

        if (failures.Count > 0)
        {
            TestContext.WriteLine($"\nFailures ({failures.Count}):");
            foreach (var msg in failures)
                TestContext.WriteLine($"  {msg}");
        }

        failures.Should().BeEmpty(
            "all NounPattern values should roundtrip through ToDbString/Parse");
    }

    [Test]
    public void NounPattern_ParseTryParse_AreConsistent()
    {
        foreach (NounPattern pattern in Enum.GetValues<NounPattern>())
        {
            if (pattern == NounPattern.None) continue;
            if (pattern.ToString().StartsWith("_")) continue;

            var dbString = pattern.ToDbString();

            // Both should succeed for valid patterns
            var parseResult = NounPatternHelper.Parse(dbString);
            var tryParseSuccess = NounPatternHelper.TryParse(dbString, out var tryParseResult);

            tryParseSuccess.Should().BeTrue($"TryParse should succeed for valid pattern '{dbString}'");
            tryParseResult.Should().Be(parseResult, $"TryParse and Parse should return same result for '{dbString}'");
        }
    }

    [Test]
    public void NounPattern_TryParse_ReturnsFalseForInvalid()
    {
        var invalidStrings = new[]
        {
            "",
            "invalid",
            "a masc invalid",
            "x masc",
            "a",
            "masc",
            null!
        };

        foreach (var invalid in invalidStrings.Where(s => s != null))
        {
            var success = NounPatternHelper.TryParse(invalid, out _);
            success.Should().BeFalse($"TryParse should return false for '{invalid}'");
        }
    }

    [Test]
    public void NounPattern_NonBasePatterns_AllHaveValidParent()
    {
        // Non-base patterns include both variants and irregulars
        var nonBasePatterns = Enum.GetValues<NounPattern>()
            .Where(p => p != NounPattern.None)
            .Where(p => !p.ToString().StartsWith("_"))
            .Where(p => !p.IsBase())
            .ToList();

        TestContext.WriteLine($"Testing {nonBasePatterns.Count} non-base noun patterns (variants + irregulars)");

        var failures = new List<string>();

        foreach (var pattern in nonBasePatterns)
        {
            try
            {
                var parent = pattern.ParentBase();

                // Parent should be a base pattern
                if (!parent.IsBase())
                {
                    failures.Add($"{pattern} → {parent} (parent is not a base pattern!)");
                }
                else
                {
                    var patternType = pattern.IsIrregular() ? "irregular" : "variant";
                    TestContext.WriteLine($"  {pattern} ({patternType}) → {parent}");
                }
            }
            catch (Exception ex)
            {
                failures.Add($"{pattern}: {ex.Message}");
            }
        }

        failures.Should().BeEmpty(
            "all non-base patterns should have a valid base parent");
    }

    [Test]
    public void NounPattern_BasePatterns_ThrowOnParentBase()
    {
        var basePatterns = Enum.GetValues<NounPattern>()
            .Where(p => p != NounPattern.None)
            .Where(p => !p.ToString().StartsWith("_"))
            .Where(p => p.IsBase())
            .ToList();

        TestContext.WriteLine($"Testing {basePatterns.Count} base noun patterns");

        foreach (var pattern in basePatterns)
        {
            var act = () => pattern.ParentBase();
            act.Should().Throw<InvalidOperationException>(
                $"base pattern {pattern} should throw when ParentBase() is called");
        }
    }

    [Test]
    public void NounPattern_GenderBreakpoints_WorkCorrectly()
    {
        // Test that gender detection via breakpoints is correct
        var genderGroups = new Dictionary<Gender, List<NounPattern>>
        {
            [Gender.Masculine] = [],
            [Gender.Feminine] = [],
            [Gender.Neuter] = []
        };

        foreach (NounPattern pattern in Enum.GetValues<NounPattern>())
        {
            if (pattern == NounPattern.None) continue;
            if (pattern.ToString().StartsWith("_")) continue;

            var gender = pattern.GetGender();
            if (gender != Gender.None)
            {
                genderGroups[gender].Add(pattern);
            }
        }

        TestContext.WriteLine("Gender groupings:");
        foreach (var kvp in genderGroups)
        {
            TestContext.WriteLine($"  {kvp.Key}: {kvp.Value.Count} patterns");
            foreach (var p in kvp.Value.Take(5))
                TestContext.WriteLine($"    {p}");
            if (kvp.Value.Count > 5)
                TestContext.WriteLine($"    ... and {kvp.Value.Count - 5} more");
        }

        // Verify expected counts (approximately)
        genderGroups[Gender.Masculine].Count.Should().BeGreaterThan(10,
            "should have multiple masculine patterns (regular + irregulars)");
        genderGroups[Gender.Feminine].Count.Should().BeGreaterThan(5,
            "should have multiple feminine patterns");
        genderGroups[Gender.Neuter].Count.Should().BeGreaterThan(3,
            "should have multiple neuter patterns");
    }

    [Test]
    public void NounPattern_PluralOnlyPatterns_AreMarkedCorrectly()
    {
        var expectedPluralOnly = new[]
        {
            NounPattern.AMascPl,
            NounPattern.ĪMascPl,
            NounPattern.UMascPl,
            NounPattern.ANeutPl
        };

        foreach (var pattern in expectedPluralOnly)
        {
            pattern.IsPluralOnly().Should().BeTrue(
                $"{pattern} should be marked as plural-only");
        }

        // Verify regular patterns are NOT plural-only
        var regularPatterns = new[]
        {
            NounPattern.AMasc, NounPattern.ANeut, NounPattern.ĀFem
        };

        foreach (var pattern in regularPatterns)
        {
            pattern.IsPluralOnly().Should().BeFalse(
                $"{pattern} should NOT be marked as plural-only");
        }
    }

    #endregion

    #region VerbPattern Tests

    [Test]
    public void VerbPattern_AllValues_RoundtripThroughDbString()
    {
        var failures = new List<string>();
        var tested = new List<string>();

        foreach (VerbPattern pattern in Enum.GetValues<VerbPattern>())
        {
            // Skip None and breakpoint markers
            if (pattern == VerbPattern.None) continue;
            if (pattern.ToString().StartsWith("_")) continue;

            try
            {
                var dbString = pattern.ToDbString();
                var parsed = VerbPatternHelper.Parse(dbString);

                if (parsed != pattern)
                {
                    failures.Add($"{pattern}: ToDbString='{dbString}' parsed back as {parsed}");
                }
                else
                {
                    tested.Add($"{pattern} ↔ '{dbString}'");
                }
            }
            catch (Exception ex)
            {
                failures.Add($"{pattern}: {ex.GetType().Name} - {ex.Message}");
            }
        }

        TestContext.WriteLine($"Successfully tested {tested.Count} patterns:");
        foreach (var msg in tested)
            TestContext.WriteLine($"  {msg}");

        if (failures.Count > 0)
        {
            TestContext.WriteLine($"\nFailures ({failures.Count}):");
            foreach (var msg in failures)
                TestContext.WriteLine($"  {msg}");
        }

        failures.Should().BeEmpty(
            "all VerbPattern values should roundtrip through ToDbString/Parse");
    }

    [Test]
    public void VerbPattern_ParseTryParse_AreConsistent()
    {
        foreach (VerbPattern pattern in Enum.GetValues<VerbPattern>())
        {
            if (pattern == VerbPattern.None) continue;
            if (pattern.ToString().StartsWith("_")) continue;

            var dbString = pattern.ToDbString();

            var parseResult = VerbPatternHelper.Parse(dbString);
            var tryParseSuccess = VerbPatternHelper.TryParse(dbString, out var tryParseResult);

            tryParseSuccess.Should().BeTrue($"TryParse should succeed for valid pattern '{dbString}'");
            tryParseResult.Should().Be(parseResult, $"TryParse and Parse should return same result for '{dbString}'");
        }
    }

    [Test]
    public void VerbPattern_TryParse_ReturnsFalseForInvalid()
    {
        var invalidStrings = new[]
        {
            "",
            "invalid",
            "ati",
            "pr",
            "ati future", // Wrong tense marker
        };

        foreach (var invalid in invalidStrings)
        {
            var success = VerbPatternHelper.TryParse(invalid, out _);
            success.Should().BeFalse($"TryParse should return false for '{invalid}'");
        }
    }

    [Test]
    public void VerbPattern_IrregularPatterns_AllHaveValidParent()
    {
        var irregulars = Enum.GetValues<VerbPattern>()
            .Where(p => p != VerbPattern.None)
            .Where(p => !p.ToString().StartsWith("_"))
            .Where(p => p.IsIrregular())
            .ToList();

        TestContext.WriteLine($"Testing {irregulars.Count} irregular verb patterns");

        var failures = new List<string>();

        foreach (var pattern in irregulars)
        {
            try
            {
                var parent = pattern.ParentRegular();

                // Parent should be a regular pattern
                if (parent.IsIrregular())
                {
                    failures.Add($"{pattern} → {parent} (parent is also irregular!)");
                }
                else
                {
                    TestContext.WriteLine($"  {pattern} → {parent}");
                }
            }
            catch (Exception ex)
            {
                failures.Add($"{pattern}: {ex.Message}");
            }
        }

        failures.Should().BeEmpty(
            "all irregular verb patterns should have a valid regular parent");
    }

    [Test]
    public void VerbPattern_RegularPatterns_ThrowOnParentRegular()
    {
        var regulars = new[]
        {
            VerbPattern.Ati,
            VerbPattern.Āti,
            VerbPattern.Eti,
            VerbPattern.Oti
        };

        foreach (var pattern in regulars)
        {
            var act = () => pattern.ParentRegular();
            act.Should().Throw<InvalidOperationException>(
                $"regular pattern {pattern} should throw when ParentRegular() is called");
        }
    }

    [Test]
    public void VerbPattern_IsIrregular_CorrectlyIdentifiesPatterns()
    {
        // Regular patterns
        VerbPattern.Ati.IsIrregular().Should().BeFalse();
        VerbPattern.Āti.IsIrregular().Should().BeFalse();
        VerbPattern.Eti.IsIrregular().Should().BeFalse();
        VerbPattern.Oti.IsIrregular().Should().BeFalse();

        // Irregular patterns
        VerbPattern.Atthi.IsIrregular().Should().BeTrue();
        VerbPattern.Hoti.IsIrregular().Should().BeTrue();
        VerbPattern.Karoti.IsIrregular().Should().BeTrue();
        VerbPattern.Brūti.IsIrregular().Should().BeTrue();
    }

    #endregion

    #region Cross-Pattern Consistency

    [Test]
    public void AllPatterns_DbStringsAreUnique()
    {
        var nounDbStrings = Enum.GetValues<NounPattern>()
            .Where(p => p != NounPattern.None && !p.ToString().StartsWith("_"))
            .Select(p => (Pattern: p.ToString(), DbString: p.ToDbString()))
            .ToList();

        var verbDbStrings = Enum.GetValues<VerbPattern>()
            .Where(p => p != VerbPattern.None && !p.ToString().StartsWith("_"))
            .Select(p => (Pattern: p.ToString(), DbString: p.ToDbString()))
            .ToList();

        // Check noun uniqueness
        var nounDuplicates = nounDbStrings
            .GroupBy(x => x.DbString)
            .Where(g => g.Count() > 1)
            .ToList();

        // Check verb uniqueness
        var verbDuplicates = verbDbStrings
            .GroupBy(x => x.DbString)
            .Where(g => g.Count() > 1)
            .ToList();

        nounDuplicates.Should().BeEmpty("each NounPattern should have a unique database string");
        verbDuplicates.Should().BeEmpty("each VerbPattern should have a unique database string");
    }

    [Test]
    public void AllPatterns_DisplayLabelsAreReasonable()
    {
        // Verify display labels don't have unexpected content
        foreach (NounPattern pattern in Enum.GetValues<NounPattern>())
        {
            if (pattern == NounPattern.None || pattern.ToString().StartsWith("_")) continue;

            var label = pattern.ToDisplayLabel();
            label.Should().NotBeNullOrEmpty($"{pattern} should have a display label");
            label.Should().NotContain(" ", $"{pattern}'s display label should not contain spaces");
        }

        foreach (VerbPattern pattern in Enum.GetValues<VerbPattern>())
        {
            if (pattern == VerbPattern.None || pattern.ToString().StartsWith("_")) continue;

            var label = pattern.ToDisplayLabel();
            label.Should().NotBeNullOrEmpty($"{pattern} should have a display label");
            label.Should().NotContain(" ", $"{pattern}'s display label should not contain spaces");
        }
    }

    #endregion
}

using PaliPractice.Models.Inflection;
using PaliPractice.Services.UserData;

namespace PaliPractice.Tests.UserData;

/// <summary>
/// Sanity checks for SettingsKeys defaults.
/// Catches accidental changes that could break initialization.
/// </summary>
[TestFixture]
public class SettingsKeysTests
{
    #region No None Values in Default Arrays

    [Test]
    public void NounsDefaultCases_ShouldNotContainNone()
    {
        SettingsKeys.NounsDefaultCases.Should().NotContain(Case.None);
    }

    [Test]
    public void DefaultNumbers_ShouldNotContainNone()
    {
        SettingsKeys.DefaultNumbers.Should().NotContain(Number.None);
    }

    [Test]
    public void VerbsDefaultTenses_ShouldNotContainNone()
    {
        SettingsKeys.VerbsDefaultTenses.Should().NotContain(Tense.None);
    }

    [Test]
    public void VerbsDefaultPersons_ShouldNotContainNone()
    {
        SettingsKeys.VerbsDefaultPersons.Should().NotContain(Person.None);
    }

    [Test]
    public void VerbsDefaultVoices_ShouldNotContainNone()
    {
        SettingsKeys.VerbsDefaultVoices.Should().NotContain(Voice.None);
    }

    [Test]
    public void NounsDefaultMascPatterns_ShouldOnlyContainBasePatterns()
    {
        SettingsKeys.NounsDefaultMascPatterns.Should().NotContain(NounPattern.None);

        // All default patterns must be base patterns (not variants or irregulars)
        foreach (var pattern in SettingsKeys.NounsDefaultMascPatterns)
        {
            pattern.IsBase().Should().BeTrue(
                $"{pattern} in NounsDefaultMascPatterns should be a base pattern");
            pattern.IsVariant().Should().BeFalse(
                $"{pattern} should not be a variant pattern");
            pattern.IsIrregular().Should().BeFalse(
                $"{pattern} should not be an irregular pattern");
        }
    }

    [Test]
    public void NounsDefaultFemPatterns_ShouldOnlyContainBasePatterns()
    {
        SettingsKeys.NounsDefaultFemPatterns.Should().NotContain(NounPattern.None);

        foreach (var pattern in SettingsKeys.NounsDefaultFemPatterns)
        {
            pattern.IsBase().Should().BeTrue(
                $"{pattern} in NounsDefaultFemPatterns should be a base pattern");
            pattern.IsVariant().Should().BeFalse(
                $"{pattern} should not be a variant pattern");
            pattern.IsIrregular().Should().BeFalse(
                $"{pattern} should not be an irregular pattern");
        }
    }

    [Test]
    public void NounsDefaultNeutPatterns_ShouldOnlyContainBasePatterns()
    {
        SettingsKeys.NounsDefaultNeutPatterns.Should().NotContain(NounPattern.None);

        foreach (var pattern in SettingsKeys.NounsDefaultNeutPatterns)
        {
            pattern.IsBase().Should().BeTrue(
                $"{pattern} in NounsDefaultNeutPatterns should be a base pattern");
            pattern.IsVariant().Should().BeFalse(
                $"{pattern} should not be a variant pattern");
            pattern.IsIrregular().Should().BeFalse(
                $"{pattern} should not be an irregular pattern");
        }
    }

    [Test]
    public void VerbsDefaultPatterns_ShouldOnlyContainRegularPatterns()
    {
        SettingsKeys.VerbsDefaultPatterns.Should().NotContain(VerbPattern.None);
        SettingsKeys.VerbsDefaultPatterns.Should().NotContain(VerbPattern._Irregular);

        // All default patterns must be regular (not irregular)
        foreach (var pattern in SettingsKeys.VerbsDefaultPatterns)
        {
            pattern.IsIrregular().Should().BeFalse(
                $"{pattern} in VerbsDefaultPatterns should not be an irregular pattern");
        }
    }

    #endregion

    #region No Duplicates in Default Arrays

    [Test]
    public void NounsDefaultCases_ShouldHaveUniqueItems()
    {
        SettingsKeys.NounsDefaultCases.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void DefaultNumbers_ShouldHaveUniqueItems()
    {
        SettingsKeys.DefaultNumbers.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void VerbsDefaultTenses_ShouldHaveUniqueItems()
    {
        SettingsKeys.VerbsDefaultTenses.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void VerbsDefaultVoices_ShouldHaveUniqueItems()
    {
        SettingsKeys.VerbsDefaultVoices.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void NounsDefaultMascPatterns_ShouldHaveUniqueItems()
    {
        SettingsKeys.NounsDefaultMascPatterns.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void VerbsDefaultPatterns_ShouldHaveUniqueItems()
    {
        SettingsKeys.VerbsDefaultPatterns.Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region Numeric Defaults Validity

    [Test]
    public void DefaultDailyGoal_ShouldBePositive()
    {
        SettingsKeys.DefaultDailyGoal.Should().BePositive();
    }

    [Test]
    public void DefaultLemmaRange_ShouldBeValid()
    {
        SettingsKeys.DefaultLemmaMin.Should().BePositive();
        SettingsKeys.DefaultLemmaMax.Should().BeGreaterThan(SettingsKeys.DefaultLemmaMin);
    }

    #endregion
}

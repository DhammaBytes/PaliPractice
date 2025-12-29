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
    public void NounsDefaultMascPatterns_ShouldNotContainNoneOrMarkers()
    {
        SettingsKeys.NounsDefaultMascPatterns.Should().NotContain(NounPattern.None);
        SettingsKeys.NounsDefaultMascPatterns.Should().NotContain(NounPattern._RegularFem);
        SettingsKeys.NounsDefaultMascPatterns.Should().NotContain(NounPattern._RegularNeut);
        SettingsKeys.NounsDefaultMascPatterns.Should().NotContain(NounPattern._Irregular);
    }

    [Test]
    public void VerbsDefaultPatterns_ShouldNotContainNoneOrMarkers()
    {
        SettingsKeys.VerbsDefaultPatterns.Should().NotContain(VerbPattern.None);
        SettingsKeys.VerbsDefaultPatterns.Should().NotContain(VerbPattern._Irregular);
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

    #region Expected Array Sizes

    [Test]
    public void NounsDefaultCases_ShouldHaveSevenCases()
    {
        // All 8 cases minus Vocative
        SettingsKeys.NounsDefaultCases.Should().HaveCount(7);
    }

    [Test]
    public void DefaultNumbers_ShouldHaveBothSingularAndPlural()
    {
        SettingsKeys.DefaultNumbers.Should().HaveCount(2);
        SettingsKeys.DefaultNumbers.Should().Contain(Number.Singular);
        SettingsKeys.DefaultNumbers.Should().Contain(Number.Plural);
    }

    [Test]
    public void VerbsDefaultVoices_ShouldHaveBothNormalAndReflexive()
    {
        SettingsKeys.VerbsDefaultVoices.Should().HaveCount(2);
        SettingsKeys.VerbsDefaultVoices.Should().Contain(Voice.Active);
        SettingsKeys.VerbsDefaultVoices.Should().Contain(Voice.Reflexive);
    }

    #endregion
}

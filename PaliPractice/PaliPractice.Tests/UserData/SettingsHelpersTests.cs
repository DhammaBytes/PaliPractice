using PaliPractice.Services.UserData;

namespace PaliPractice.Tests.UserData;

[TestFixture]
public class SettingsHelpersTests
{
    #region ToCsv Tests

    [Test]
    public void ToCsv_SingleValue_ReturnsIntegerString()
    {
        SettingsHelpers.ToCsv([Number.Singular]).Should().Be("1");
    }

    [Test]
    public void ToCsv_MultipleValues_ReturnsCommaSeparated()
    {
        SettingsHelpers.ToCsv([Number.Singular, Number.Plural]).Should().Be("1,2");
    }

    [Test]
    public void ToCsv_EmptyCollection_ReturnsEmptyString()
    {
        SettingsHelpers.ToCsv(Array.Empty<Number>()).Should().BeEmpty();
    }

    [Test]
    public void ToCsv_AllCases_ReturnsCorrectValues()
    {
        var allCases = new[] { Case.Nominative, Case.Accusative, Case.Instrumental,
                               Case.Dative, Case.Ablative, Case.Genitive,
                               Case.Locative, Case.Vocative };
        SettingsHelpers.ToCsv(allCases).Should().Be("1,2,3,4,5,6,7,8");
    }

    #endregion

    #region FromCsv Edge Cases

    [Test]
    public void FromCsv_EmptyString_ReturnsEmptyList()
    {
        SettingsHelpers.FromCsv<Number>("").Should().BeEmpty();
    }

    [Test]
    public void FromCsv_WhitespaceOnly_ReturnsEmptyList()
    {
        SettingsHelpers.FromCsv<Number>("   ").Should().BeEmpty();
    }

    [Test]
    public void FromCsv_SpacesAroundValues_ParsesCorrectly()
    {
        SettingsHelpers.FromCsv<Number>(" 1 , 2 ").Should().Equal(Number.Singular, Number.Plural);
    }

    [Test]
    public void FromCsv_InvalidMixedWithValid_FiltersInvalid()
    {
        // "invalid" should be filtered out by TryParse
        SettingsHelpers.FromCsv<Number>("1,invalid,2").Should().Equal(Number.Singular, Number.Plural);
    }

    [Test]
    public void FromCsv_EmptySegments_IgnoresEmpty()
    {
        SettingsHelpers.FromCsv<Number>("1,,2").Should().Equal(Number.Singular, Number.Plural);
    }

    [Test]
    public void FromCsv_LeadingTrailingCommas_IgnoresEmpty()
    {
        SettingsHelpers.FromCsv<Number>(",1,2,").Should().Equal(Number.Singular, Number.Plural);
    }

    [Test]
    public void FromCsv_OutOfRangeEnumValue_StillParses()
    {
        // Value 99 will parse to an invalid enum value but won't throw
        var result = SettingsHelpers.FromCsv<Number>("99");
        result.Should().HaveCount(1);
        // The enum value is technically (Number)99, which is not None/Singular/Plural
    }

    #endregion

    #region Roundtrip Tests

    [Test]
    public void Roundtrip_Number_PreservesValues()
    {
        var original = new[] { Number.Singular, Number.Plural };
        var csv = SettingsHelpers.ToCsv(original);
        var restored = SettingsHelpers.FromCsv<Number>(csv);
        restored.Should().Equal(original);
    }

    [Test]
    public void Roundtrip_Case_PreservesValues()
    {
        var original = new[] { Case.Nominative, Case.Instrumental, Case.Locative };
        var csv = SettingsHelpers.ToCsv(original);
        var restored = SettingsHelpers.FromCsv<Case>(csv);
        restored.Should().Equal(original);
    }

    [Test]
    public void Roundtrip_Voice_PreservesValues()
    {
        var original = new[] { Voice.Normal, Voice.Reflexive };
        var csv = SettingsHelpers.ToCsv(original);
        var restored = SettingsHelpers.FromCsv<Voice>(csv);
        restored.Should().Equal(original);
    }

    [Test]
    public void Roundtrip_Tense_PreservesValues()
    {
        var original = new[] { Tense.Present, Tense.Imperative, Tense.Future };
        var csv = SettingsHelpers.ToCsv(original);
        var restored = SettingsHelpers.FromCsv<Tense>(csv);
        restored.Should().Equal(original);
    }

    #endregion

    #region Predicate Methods

    [Test]
    public void IncludesSingular_WithSingular_ReturnsTrue()
    {
        SettingsHelpers.IncludesSingular("1").Should().BeTrue();
        SettingsHelpers.IncludesSingular("1,2").Should().BeTrue();
    }

    [Test]
    public void IncludesSingular_WithoutSingular_ReturnsFalse()
    {
        SettingsHelpers.IncludesSingular("2").Should().BeFalse();
        SettingsHelpers.IncludesSingular("").Should().BeFalse();
    }

    [Test]
    public void IncludesPlural_WithPlural_ReturnsTrue()
    {
        SettingsHelpers.IncludesPlural("2").Should().BeTrue();
        SettingsHelpers.IncludesPlural("1,2").Should().BeTrue();
    }

    [Test]
    public void IncludesPlural_WithoutPlural_ReturnsFalse()
    {
        SettingsHelpers.IncludesPlural("1").Should().BeFalse();
        SettingsHelpers.IncludesPlural("").Should().BeFalse();
    }

    [Test]
    public void IncludesNormal_WithNormal_ReturnsTrue()
    {
        // Voice.Normal = 1
        SettingsHelpers.IncludesNormal("1").Should().BeTrue();
        SettingsHelpers.IncludesNormal("1,2").Should().BeTrue();
    }

    [Test]
    public void IncludesNormal_WithoutNormal_ReturnsFalse()
    {
        // Voice.Reflexive = 2
        SettingsHelpers.IncludesNormal("2").Should().BeFalse();
        SettingsHelpers.IncludesNormal("").Should().BeFalse();
    }

    [Test]
    public void IncludesReflexive_WithReflexive_ReturnsTrue()
    {
        SettingsHelpers.IncludesReflexive("2").Should().BeTrue();
        SettingsHelpers.IncludesReflexive("1,2").Should().BeTrue();
    }

    [Test]
    public void IncludesReflexive_WithoutReflexive_ReturnsFalse()
    {
        SettingsHelpers.IncludesReflexive("1").Should().BeFalse();
        SettingsHelpers.IncludesReflexive("").Should().BeFalse();
    }

    #endregion
}

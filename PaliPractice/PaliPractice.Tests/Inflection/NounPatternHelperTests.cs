using PaliPractice.Models.Inflection;

namespace PaliPractice.Tests.Inflection;

/// <summary>
/// Tests for NounPatternHelper methods using breakpoint-based pattern hierarchy.
///
/// Three-tier structure per gender: Base → Variant → (next gender or Irregular)
///
/// Breakpoints:
/// - pattern &lt; _VariantMasc → Base Masculine
/// - _VariantMasc &lt; pattern &lt; _BaseFem → Variant Masculine
/// - _BaseFem &lt; pattern &lt; _BaseNeut → Base Feminine
/// - _BaseNeut &lt; pattern &lt; _VariantNeut → Base Neuter
/// - _VariantNeut &lt; pattern &lt; _Irregular → Variant Neuter
/// - pattern &gt; _Irregular → Irregular
/// </summary>
[TestFixture]
public class NounPatternHelperTests
{
    #region ToDbString / Parse roundtrip tests

    [Test]
    public void ToDbString_BasePatterns_ReturnsCorrectFormat()
    {
        NounPattern.AMasc.ToDbString().Should().Be("a masc");
        NounPattern.ANeut.ToDbString().Should().Be("a nt");
        NounPattern.ĀFem.ToDbString().Should().Be("ā fem");
        NounPattern.ĪMasc.ToDbString().Should().Be("ī masc");
        NounPattern.ArMasc.ToDbString().Should().Be("ar masc");
        NounPattern.AntMasc.ToDbString().Should().Be("ant masc");
    }

    [Test]
    public void ToDbString_VariantPatterns_ReturnsCorrectFormat()
    {
        NounPattern.A2Masc.ToDbString().Should().Be("a2 masc");
        NounPattern.AMascEast.ToDbString().Should().Be("a masc east");
        NounPattern.ANeutIrreg.ToDbString().Should().Be("a nt irreg");
    }

    [Test]
    public void ToDbString_IrregularPatterns_ReturnsCorrectFormat()
    {
        NounPattern.RājaMasc.ToDbString().Should().Be("rāja masc");
        NounPattern.BrahmaMasc.ToDbString().Should().Be("brahma masc");
        NounPattern.KammaNeut.ToDbString().Should().Be("kamma nt");
        NounPattern.NadīFem.ToDbString().Should().Be("nadī fem");
    }

    [Test]
    public void Parse_RoundTripsAllUsablePatterns()
    {
        // Test all patterns except None and breakpoint markers
        var usablePatterns = Enum.GetValues<NounPattern>()
            .Where(p => p != NounPattern.None &&
                        p != NounPattern._VariantMasc &&
                        p != NounPattern._BaseFem &&
                        p != NounPattern._BaseNeut &&
                        p != NounPattern._VariantNeut &&
                        p != NounPattern._Irregular);

        foreach (var pattern in usablePatterns)
        {
            var dbString = pattern.ToDbString();
            var parsed = NounPatternHelper.Parse(dbString);
            parsed.Should().Be(pattern, $"roundtrip failed for {pattern}");
        }
    }

    [Test]
    public void TryParse_ValidPattern_ReturnsTrue()
    {
        var success = NounPatternHelper.TryParse("a masc", out var pattern);
        success.Should().BeTrue();
        pattern.Should().Be(NounPattern.AMasc);
    }

    [Test]
    public void TryParse_InvalidPattern_ReturnsFalse()
    {
        var success = NounPatternHelper.TryParse("invalid pattern", out var pattern);
        success.Should().BeFalse();
        pattern.Should().Be(default(NounPattern));
    }

    #endregion

    #region IsIrregular tests

    [Test]
    public void IsIrregular_BaseMasculine_ReturnsFalse()
    {
        NounPattern.AMasc.IsIrregular().Should().BeFalse();
        NounPattern.IMasc.IsIrregular().Should().BeFalse();
        NounPattern.ĪMasc.IsIrregular().Should().BeFalse();
        NounPattern.UMasc.IsIrregular().Should().BeFalse();
        NounPattern.ŪMasc.IsIrregular().Should().BeFalse();
        NounPattern.AsMasc.IsIrregular().Should().BeFalse();
        NounPattern.ArMasc.IsIrregular().Should().BeFalse();
        NounPattern.AntMasc.IsIrregular().Should().BeFalse();
    }

    [Test]
    public void IsIrregular_BaseFeminine_ReturnsFalse()
    {
        NounPattern.ĀFem.IsIrregular().Should().BeFalse();
        NounPattern.IFem.IsIrregular().Should().BeFalse();
        NounPattern.ĪFem.IsIrregular().Should().BeFalse();
        NounPattern.UFem.IsIrregular().Should().BeFalse();
        NounPattern.ArFem.IsIrregular().Should().BeFalse();
    }

    [Test]
    public void IsIrregular_BaseNeuter_ReturnsFalse()
    {
        NounPattern.ANeut.IsIrregular().Should().BeFalse();
        NounPattern.INeut.IsIrregular().Should().BeFalse();
        NounPattern.UNeut.IsIrregular().Should().BeFalse();
    }

    [Test]
    public void IsIrregular_VariantPatterns_ReturnsFalse()
    {
        // Variant patterns use stem+ending (alternate tables), not irregular
        NounPattern.A2Masc.IsIrregular().Should().BeFalse();
        NounPattern.AMascEast.IsIrregular().Should().BeFalse();
        NounPattern.AMascPl.IsIrregular().Should().BeFalse();
        NounPattern.AntaMasc.IsIrregular().Should().BeFalse();
        NounPattern.ANeutEast.IsIrregular().Should().BeFalse();
        NounPattern.ANeutPl.IsIrregular().Should().BeFalse();
    }

    [Test]
    public void IsIrregular_TrulyIrregularPatterns_ReturnsTrue()
    {
        // Only patterns with DPD like='irreg' are truly irregular
        NounPattern.RājaMasc.IsIrregular().Should().BeTrue();
        NounPattern.BrahmaMasc.IsIrregular().Should().BeTrue();
        NounPattern.ArahantMasc.IsIrregular().Should().BeTrue();
        NounPattern.ParisāFem.IsIrregular().Should().BeTrue();
        NounPattern.NadīFem.IsIrregular().Should().BeTrue();
        NounPattern.KammaNeut.IsIrregular().Should().BeTrue();
    }

    [Test]
    public void IsIrregular_BreakpointMarkers_ReturnsFalse()
    {
        NounPattern._VariantMasc.IsIrregular().Should().BeFalse();
        NounPattern._BaseFem.IsIrregular().Should().BeFalse();
        NounPattern._BaseNeut.IsIrregular().Should().BeFalse();
        NounPattern._VariantNeut.IsIrregular().Should().BeFalse();
        NounPattern._Irregular.IsIrregular().Should().BeFalse();
    }

    #endregion

    #region IsVariant tests

    [Test]
    public void IsVariant_VariantMasculine_ReturnsTrue()
    {
        NounPattern.A2Masc.IsVariant().Should().BeTrue();
        NounPattern.AMascEast.IsVariant().Should().BeTrue();
        NounPattern.AMascPl.IsVariant().Should().BeTrue();
        NounPattern.AntaMasc.IsVariant().Should().BeTrue();
        NounPattern.Ar2Masc.IsVariant().Should().BeTrue();
        NounPattern.ĪMascPl.IsVariant().Should().BeTrue();
        NounPattern.UMascPl.IsVariant().Should().BeTrue();
    }

    [Test]
    public void IsVariant_VariantNeuter_ReturnsTrue()
    {
        NounPattern.ANeutEast.IsVariant().Should().BeTrue();
        NounPattern.ANeutIrreg.IsVariant().Should().BeTrue();
        NounPattern.ANeutPl.IsVariant().Should().BeTrue();
    }

    [Test]
    public void IsVariant_BasePatterns_ReturnsFalse()
    {
        NounPattern.AMasc.IsVariant().Should().BeFalse();
        NounPattern.ĀFem.IsVariant().Should().BeFalse();
        NounPattern.ANeut.IsVariant().Should().BeFalse();
    }

    [Test]
    public void IsVariant_IrregularPatterns_ReturnsFalse()
    {
        NounPattern.RājaMasc.IsVariant().Should().BeFalse();
        NounPattern.KammaNeut.IsVariant().Should().BeFalse();
    }

    #endregion

    #region IsBase tests

    [Test]
    public void IsBase_BasePatterns_ReturnsTrue()
    {
        NounPattern.AMasc.IsBase().Should().BeTrue();
        NounPattern.ĀFem.IsBase().Should().BeTrue();
        NounPattern.ANeut.IsBase().Should().BeTrue();
    }

    [Test]
    public void IsBase_VariantPatterns_ReturnsFalse()
    {
        NounPattern.A2Masc.IsBase().Should().BeFalse();
        NounPattern.ANeutPl.IsBase().Should().BeFalse();
    }

    [Test]
    public void IsBase_IrregularPatterns_ReturnsFalse()
    {
        NounPattern.RājaMasc.IsBase().Should().BeFalse();
        NounPattern.KammaNeut.IsBase().Should().BeFalse();
    }

    #endregion

    #region GetPatternType tests

    [Test]
    public void GetPatternType_ReturnsCorrectType()
    {
        NounPattern.AMasc.GetPatternType().Should().Be(PatternType.Base);
        NounPattern.A2Masc.GetPatternType().Should().Be(PatternType.Variant);
        NounPattern.RājaMasc.GetPatternType().Should().Be(PatternType.Irregular);
    }

    #endregion

    #region ParentBase tests

    [Test]
    public void ParentBase_VariantMasculine_ReturnsCorrectParent()
    {
        NounPattern.A2Masc.ParentBase().Should().Be(NounPattern.AMasc);
        NounPattern.AMascEast.ParentBase().Should().Be(NounPattern.AMasc);
        NounPattern.AMascPl.ParentBase().Should().Be(NounPattern.AMasc);
        NounPattern.AntaMasc.ParentBase().Should().Be(NounPattern.AntMasc);
        NounPattern.Ar2Masc.ParentBase().Should().Be(NounPattern.ArMasc);
        NounPattern.ĪMascPl.ParentBase().Should().Be(NounPattern.ĪMasc);
        NounPattern.UMascPl.ParentBase().Should().Be(NounPattern.UMasc);
    }

    [Test]
    public void ParentBase_VariantNeuter_ReturnsANeut()
    {
        NounPattern.ANeutEast.ParentBase().Should().Be(NounPattern.ANeut);
        NounPattern.ANeutIrreg.ParentBase().Should().Be(NounPattern.ANeut);
        NounPattern.ANeutPl.ParentBase().Should().Be(NounPattern.ANeut);
    }

    [Test]
    public void ParentBase_IrregularMasculineA_ReturnsAMasc()
    {
        NounPattern.RājaMasc.ParentBase().Should().Be(NounPattern.AMasc);
        NounPattern.BrahmaMasc.ParentBase().Should().Be(NounPattern.AMasc);
        NounPattern.GoMasc.ParentBase().Should().Be(NounPattern.AMasc);
        NounPattern.YuvaMasc.ParentBase().Should().Be(NounPattern.AMasc);
        NounPattern.AddhaMasc.ParentBase().Should().Be(NounPattern.AMasc);
    }

    [Test]
    public void ParentBase_IrregularMasculineOther_ReturnsCorrectParent()
    {
        NounPattern.JantuMasc.ParentBase().Should().Be(NounPattern.UMasc);
    }

    [Test]
    public void ParentBase_IrregularAntMasc_ReturnsAntMasc()
    {
        NounPattern.ArahantMasc.ParentBase().Should().Be(NounPattern.AntMasc);
        NounPattern.BhavantMasc.ParentBase().Should().Be(NounPattern.AntMasc);
        NounPattern.SantaMasc.ParentBase().Should().Be(NounPattern.AntMasc);
    }

    [Test]
    public void ParentBase_IrregularNeuter_ReturnsANeut()
    {
        NounPattern.KammaNeut.ParentBase().Should().Be(NounPattern.ANeut);
    }

    [Test]
    public void ParentBase_IrregularFeminine_ReturnsCorrectParent()
    {
        NounPattern.ParisāFem.ParentBase().Should().Be(NounPattern.ĀFem);
        NounPattern.JātiFem.ParentBase().Should().Be(NounPattern.IFem);
        NounPattern.RattiFem.ParentBase().Should().Be(NounPattern.IFem);
        NounPattern.NadīFem.ParentBase().Should().Be(NounPattern.ĪFem);
        NounPattern.PokkharaṇīFem.ParentBase().Should().Be(NounPattern.ĪFem);
        NounPattern.MātarFem.ParentBase().Should().Be(NounPattern.ArFem);
    }

    [Test]
    public void ParentBase_BasePattern_ThrowsInvalidOperation()
    {
        var act = () => NounPattern.AMasc.ParentBase();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not a variant or irregular*");
    }

    #endregion

    #region GetGender tests

    [Test]
    public void GetGender_BaseMasculine_ReturnsMasculine()
    {
        NounPattern.AMasc.GetGender().Should().Be(Gender.Masculine);
        NounPattern.IMasc.GetGender().Should().Be(Gender.Masculine);
        NounPattern.ĪMasc.GetGender().Should().Be(Gender.Masculine);
        NounPattern.UMasc.GetGender().Should().Be(Gender.Masculine);
        NounPattern.ŪMasc.GetGender().Should().Be(Gender.Masculine);
        NounPattern.AsMasc.GetGender().Should().Be(Gender.Masculine);
        NounPattern.ArMasc.GetGender().Should().Be(Gender.Masculine);
        NounPattern.AntMasc.GetGender().Should().Be(Gender.Masculine);
    }

    [Test]
    public void GetGender_BaseFeminine_ReturnsFeminine()
    {
        NounPattern.ĀFem.GetGender().Should().Be(Gender.Feminine);
        NounPattern.IFem.GetGender().Should().Be(Gender.Feminine);
        NounPattern.ĪFem.GetGender().Should().Be(Gender.Feminine);
        NounPattern.UFem.GetGender().Should().Be(Gender.Feminine);
        NounPattern.ArFem.GetGender().Should().Be(Gender.Feminine);
    }

    [Test]
    public void GetGender_BaseNeuter_ReturnsNeuter()
    {
        NounPattern.ANeut.GetGender().Should().Be(Gender.Neuter);
        NounPattern.INeut.GetGender().Should().Be(Gender.Neuter);
        NounPattern.UNeut.GetGender().Should().Be(Gender.Neuter);
    }

    [Test]
    public void GetGender_VariantMasculine_ReturnsMasculine()
    {
        NounPattern.A2Masc.GetGender().Should().Be(Gender.Masculine);
        NounPattern.AMascPl.GetGender().Should().Be(Gender.Masculine);
        NounPattern.ĪMascPl.GetGender().Should().Be(Gender.Masculine);
    }

    [Test]
    public void GetGender_VariantNeuter_ReturnsNeuter()
    {
        NounPattern.ANeutEast.GetGender().Should().Be(Gender.Neuter);
        NounPattern.ANeutPl.GetGender().Should().Be(Gender.Neuter);
    }

    [Test]
    public void GetGender_IrregularMasculine_ReturnsMasculine()
    {
        NounPattern.RājaMasc.GetGender().Should().Be(Gender.Masculine);
        NounPattern.BrahmaMasc.GetGender().Should().Be(Gender.Masculine);
        NounPattern.ArahantMasc.GetGender().Should().Be(Gender.Masculine);
    }

    [Test]
    public void GetGender_IrregularFeminine_ReturnsFeminine()
    {
        NounPattern.ParisāFem.GetGender().Should().Be(Gender.Feminine);
        NounPattern.RattiFem.GetGender().Should().Be(Gender.Feminine);
        NounPattern.NadīFem.GetGender().Should().Be(Gender.Feminine);
        NounPattern.MātarFem.GetGender().Should().Be(Gender.Feminine);
    }

    [Test]
    public void GetGender_IrregularNeuter_ReturnsNeuter()
    {
        NounPattern.KammaNeut.GetGender().Should().Be(Gender.Neuter);
    }

    #endregion

    #region ToDisplayLabel tests

    [Test]
    public void ToDisplayLabel_ReturnsFirstPartOfDbString()
    {
        NounPattern.AMasc.ToDisplayLabel().Should().Be("a");
        NounPattern.ĀFem.ToDisplayLabel().Should().Be("ā");
        NounPattern.ArMasc.ToDisplayLabel().Should().Be("ar");
        NounPattern.AntMasc.ToDisplayLabel().Should().Be("ant");
        NounPattern.RājaMasc.ToDisplayLabel().Should().Be("rāja");
    }

    #endregion

    #region Breakpoint boundary tests

    [Test]
    public void Breakpoints_OrderIsCorrect()
    {
        // Base masculine → _VariantMasc
        (NounPattern.AsMasc < NounPattern._VariantMasc).Should().BeTrue(
            "Last base masculine (as) should be before _VariantMasc");

        // _VariantMasc → Variant masculine → _BaseFem
        (NounPattern._VariantMasc < NounPattern.A2Masc).Should().BeTrue(
            "_VariantMasc should be before first variant masculine");
        (NounPattern.UMascPl < NounPattern._BaseFem).Should().BeTrue(
            "Last variant masculine should be before _BaseFem");

        // _BaseFem → Base feminine → _BaseNeut
        (NounPattern._BaseFem < NounPattern.ĀFem).Should().BeTrue(
            "_BaseFem should be before first base feminine");
        (NounPattern.ArFem < NounPattern._BaseNeut).Should().BeTrue(
            "Last base feminine should be before _BaseNeut");

        // _BaseNeut → Base neuter → _VariantNeut
        (NounPattern._BaseNeut < NounPattern.ANeut).Should().BeTrue(
            "_BaseNeut should be before first base neuter");
        (NounPattern.UNeut < NounPattern._VariantNeut).Should().BeTrue(
            "Last base neuter should be before _VariantNeut");

        // _VariantNeut → Variant neuter → _Irregular
        (NounPattern._VariantNeut < NounPattern.ANeutEast).Should().BeTrue(
            "_VariantNeut should be before first variant neuter");
        (NounPattern.ANeutPl < NounPattern._Irregular).Should().BeTrue(
            "Last variant neuter should be before _Irregular");

        // _Irregular → Irregular patterns
        (NounPattern._Irregular < NounPattern.AddhaMasc).Should().BeTrue(
            "_Irregular should be before first irregular pattern");
    }

    [Test]
    public void AllVariantsAndIrregulars_HaveValidParent()
    {
        var nonBasePatterns = Enum.GetValues<NounPattern>()
            .Where(p => p != NounPattern.None &&
                        p != NounPattern._VariantMasc &&
                        p != NounPattern._BaseFem &&
                        p != NounPattern._BaseNeut &&
                        p != NounPattern._VariantNeut &&
                        p != NounPattern._Irregular)
            .Where(p => !p.IsBase());

        foreach (var pattern in nonBasePatterns)
        {
            var parent = pattern.ParentBase();
            parent.IsBase().Should().BeTrue(
                $"{pattern}'s parent {parent} should be a base pattern");
            pattern.GetGender().Should().Be(parent.GetGender(),
                $"{pattern} should have same gender as parent {parent}");
        }
    }

    #endregion
}

using PaliPractice.Models.Inflection;

namespace PaliPractice.Tests.Inflection;

/// <summary>
/// Tests for NounPatternHelper methods using breakpoint-based pattern hierarchy.
///
/// Enum structure:
/// - Regular Masculine (pattern &lt; _RegularFem)
/// - Regular Feminine (_RegularFem &lt; pattern &lt; _RegularNeut)
/// - Regular Neuter (_RegularNeut &lt; pattern &lt; _Irregular)
/// - Irregular (pattern &gt;= _Irregular)
/// </summary>
[TestFixture]
public class NounPatternHelperTests
{
    #region ToDbString / Parse roundtrip tests

    [Test]
    public void ToDbString_RegularPatterns_ReturnsCorrectFormat()
    {
        NounPattern.AMasc.ToDbString().Should().Be("a masc");
        NounPattern.ANeut.ToDbString().Should().Be("a nt");
        NounPattern.ĀFem.ToDbString().Should().Be("ā fem");
        NounPattern.ĪMasc.ToDbString().Should().Be("ī masc");
        NounPattern.ArMasc.ToDbString().Should().Be("ar masc");
        NounPattern.AntMasc.ToDbString().Should().Be("ant masc");
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
        // Test all patterns except breakpoint markers
        var usablePatterns = Enum.GetValues<NounPattern>()
            .Where(p => p != NounPattern._RegularFem &&
                        p != NounPattern._RegularNeut &&
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

    #region IsIrregular tests (breakpoint-based)

    [Test]
    public void IsIrregular_RegularMasculine_ReturnsFalse()
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
    public void IsIrregular_RegularFeminine_ReturnsFalse()
    {
        NounPattern.ĀFem.IsIrregular().Should().BeFalse();
        NounPattern.IFem.IsIrregular().Should().BeFalse();
        NounPattern.ĪFem.IsIrregular().Should().BeFalse();
        NounPattern.UFem.IsIrregular().Should().BeFalse();
        NounPattern.ArFem.IsIrregular().Should().BeFalse();
    }

    [Test]
    public void IsIrregular_RegularNeuter_ReturnsFalse()
    {
        NounPattern.ANeut.IsIrregular().Should().BeFalse();
        NounPattern.INeut.IsIrregular().Should().BeFalse();
        NounPattern.UNeut.IsIrregular().Should().BeFalse();
    }

    [Test]
    public void IsIrregular_IrregularPatterns_ReturnsTrue()
    {
        // Sample from each irregular group
        NounPattern.RājaMasc.IsIrregular().Should().BeTrue();
        NounPattern.BrahmaMasc.IsIrregular().Should().BeTrue();
        NounPattern.ĪMascPl.IsIrregular().Should().BeTrue();
        NounPattern.ArahantMasc.IsIrregular().Should().BeTrue();
        NounPattern.ParisāFem.IsIrregular().Should().BeTrue();
        NounPattern.NadīFem.IsIrregular().Should().BeTrue();
        NounPattern.KammaNeut.IsIrregular().Should().BeTrue();
        NounPattern.ANeutPl.IsIrregular().Should().BeTrue();
    }

    [Test]
    public void IsIrregular_BreakpointMarkers_ReturnsTrue()
    {
        // Breakpoint markers are >= _Irregular or are breakpoints themselves
        // _RegularFem and _RegularNeut are < _Irregular
        NounPattern._RegularFem.IsIrregular().Should().BeFalse();
        NounPattern._RegularNeut.IsIrregular().Should().BeFalse();
        NounPattern._Irregular.IsIrregular().Should().BeTrue();
    }

    #endregion

    #region ParentRegular tests

    [Test]
    public void ParentRegular_IrregularMasculineA_ReturnsAMasc()
    {
        NounPattern.RājaMasc.ParentRegular().Should().Be(NounPattern.AMasc);
        NounPattern.BrahmaMasc.ParentRegular().Should().Be(NounPattern.AMasc);
        NounPattern.GoMasc.ParentRegular().Should().Be(NounPattern.AMasc);
        NounPattern.YuvaMasc.ParentRegular().Should().Be(NounPattern.AMasc);
        NounPattern.A2Masc.ParentRegular().Should().Be(NounPattern.AMasc);
        NounPattern.AMascEast.ParentRegular().Should().Be(NounPattern.AMasc);
        NounPattern.AMascPl.ParentRegular().Should().Be(NounPattern.AMasc);
        NounPattern.AddhaMasc.ParentRegular().Should().Be(NounPattern.AMasc);
    }

    [Test]
    public void ParentRegular_IrregularMasculineOther_ReturnsCorrectParent()
    {
        NounPattern.ĪMascPl.ParentRegular().Should().Be(NounPattern.ĪMasc);
        NounPattern.JantuMasc.ParentRegular().Should().Be(NounPattern.UMasc);
        NounPattern.UMascPl.ParentRegular().Should().Be(NounPattern.UMasc);
        NounPattern.Ar2Masc.ParentRegular().Should().Be(NounPattern.ArMasc);
    }

    [Test]
    public void ParentRegular_IrregularAntMasc_ReturnsAntMasc()
    {
        NounPattern.AntaMasc.ParentRegular().Should().Be(NounPattern.AntMasc);
        NounPattern.ArahantMasc.ParentRegular().Should().Be(NounPattern.AntMasc);
        NounPattern.BhavantMasc.ParentRegular().Should().Be(NounPattern.AntMasc);
        NounPattern.SantaMasc.ParentRegular().Should().Be(NounPattern.AntMasc);
    }

    [Test]
    public void ParentRegular_IrregularNeuter_ReturnsANeut()
    {
        NounPattern.KammaNeut.ParentRegular().Should().Be(NounPattern.ANeut);
        NounPattern.ANeutEast.ParentRegular().Should().Be(NounPattern.ANeut);
        NounPattern.ANeutIrreg.ParentRegular().Should().Be(NounPattern.ANeut);
        NounPattern.ANeutPl.ParentRegular().Should().Be(NounPattern.ANeut);
    }

    [Test]
    public void ParentRegular_IrregularFeminine_ReturnsCorrectParent()
    {
        NounPattern.ParisāFem.ParentRegular().Should().Be(NounPattern.ĀFem);
        // jāti ends in short i, ratti ends in short i
        NounPattern.JātiFem.ParentRegular().Should().Be(NounPattern.IFem);
        NounPattern.RattiFem.ParentRegular().Should().Be(NounPattern.IFem);
        // nadī ends in long ī, pokkharaṇī ends in long ī
        NounPattern.NadīFem.ParentRegular().Should().Be(NounPattern.ĪFem);
        NounPattern.PokkharaṇīFem.ParentRegular().Should().Be(NounPattern.ĪFem);
        NounPattern.MātarFem.ParentRegular().Should().Be(NounPattern.ArFem);
    }

    [Test]
    public void ParentRegular_RegularPattern_ThrowsInvalidOperation()
    {
        var act = () => NounPattern.AMasc.ParentRegular();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not an irregular*");
    }

    #endregion

    #region GetGender tests (breakpoint-based)

    [Test]
    public void GetGender_RegularMasculine_ReturnsMasculine()
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
    public void GetGender_RegularFeminine_ReturnsFeminine()
    {
        NounPattern.ĀFem.GetGender().Should().Be(Gender.Feminine);
        NounPattern.IFem.GetGender().Should().Be(Gender.Feminine);
        NounPattern.ĪFem.GetGender().Should().Be(Gender.Feminine);
        NounPattern.UFem.GetGender().Should().Be(Gender.Feminine);
        NounPattern.ArFem.GetGender().Should().Be(Gender.Feminine);
    }

    [Test]
    public void GetGender_RegularNeuter_ReturnsNeuter()
    {
        NounPattern.ANeut.GetGender().Should().Be(Gender.Neuter);
        NounPattern.INeut.GetGender().Should().Be(Gender.Neuter);
        NounPattern.UNeut.GetGender().Should().Be(Gender.Neuter);
    }

    [Test]
    public void GetGender_IrregularMasculine_ReturnsMasculine()
    {
        // Irregulars get gender via ParentRegular recursion
        NounPattern.RājaMasc.GetGender().Should().Be(Gender.Masculine);
        NounPattern.BrahmaMasc.GetGender().Should().Be(Gender.Masculine);
        NounPattern.ArahantMasc.GetGender().Should().Be(Gender.Masculine);
        NounPattern.ĪMascPl.GetGender().Should().Be(Gender.Masculine);
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
        NounPattern.ANeutEast.GetGender().Should().Be(Gender.Neuter);
        NounPattern.ANeutPl.GetGender().Should().Be(Gender.Neuter);
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
        // Verify the breakpoint ordering (traditional: a, i, ī, u, ū, ar, ant, as)
        (NounPattern.AsMasc < NounPattern._RegularFem).Should().BeTrue(
            "Last regular masculine (as) should be before _RegularFem");

        (NounPattern._RegularFem < NounPattern.ĀFem).Should().BeTrue(
            "_RegularFem should be before first regular feminine");

        (NounPattern.ArFem < NounPattern._RegularNeut).Should().BeTrue(
            "Last regular feminine should be before _RegularNeut");

        (NounPattern._RegularNeut < NounPattern.ANeut).Should().BeTrue(
            "_RegularNeut should be before first regular neuter");

        (NounPattern.UNeut < NounPattern._Irregular).Should().BeTrue(
            "Last regular neuter should be before _Irregular");

        (NounPattern._Irregular < NounPattern.AddhaMasc).Should().BeTrue(
            "_Irregular should be before first irregular pattern (alphabetical)");
    }

    [Test]
    public void AllIrregulars_HaveValidParent()
    {
        var irregulars = Enum.GetValues<NounPattern>()
            .Where(p => p >= NounPattern._Irregular &&
                        p != NounPattern._Irregular);

        foreach (var pattern in irregulars)
        {
            var parent = pattern.ParentRegular();
            parent.IsIrregular().Should().BeFalse(
                $"{pattern}'s parent {parent} should be regular");
            pattern.GetGender().Should().Be(parent.GetGender(),
                $"{pattern} should have same gender as parent {parent}");
        }
    }

    #endregion
}

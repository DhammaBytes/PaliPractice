using PaliPractice.Models.Inflection;

namespace PaliPractice.Tests.Inflection;

/// <summary>
/// Tests for VerbPatternHelper methods using breakpoint-based pattern hierarchy.
///
/// Enum structure:
/// - Regular patterns (pattern &lt; _Irregular): Ati, Āti, Eti, Oti
/// - Irregular patterns (pattern &gt; _Irregular): grouped by parent
/// </summary>
[TestFixture]
public class VerbPatternHelperTests
{
    #region ToDbString / Parse roundtrip tests

    [Test]
    public void ToDbString_RegularPatterns_ReturnsCorrectFormat()
    {
        VerbPattern.Ati.ToDbString().Should().Be("ati pr");
        VerbPattern.Āti.ToDbString().Should().Be("āti pr");
        VerbPattern.Eti.ToDbString().Should().Be("eti pr");
        VerbPattern.Oti.ToDbString().Should().Be("oti pr");
    }

    [Test]
    public void ToDbString_IrregularPatterns_ReturnsCorrectFormat()
    {
        VerbPattern.Hoti.ToDbString().Should().Be("hoti pr");
        VerbPattern.Atthi.ToDbString().Should().Be("atthi pr");
        VerbPattern.Karoti.ToDbString().Should().Be("karoti pr");
        VerbPattern.Brūti.ToDbString().Should().Be("brūti pr");
        VerbPattern.Kubbati.ToDbString().Should().Be("kubbati pr");
        VerbPattern.Eti2.ToDbString().Should().Be("eti pr 2");
    }

    [Test]
    public void Parse_RoundTripsAllUsablePatterns()
    {
        // Test all patterns except None and breakpoint marker
        var usablePatterns = Enum.GetValues<VerbPattern>()
            .Where(p => p != VerbPattern.None && p != VerbPattern._Irregular);

        foreach (var pattern in usablePatterns)
        {
            var dbString = pattern.ToDbString();
            var parsed = VerbPatternHelper.Parse(dbString);
            parsed.Should().Be(pattern, $"roundtrip failed for {pattern}");
        }
    }

    [Test]
    public void TryParse_ValidPattern_ReturnsTrue()
    {
        var success = VerbPatternHelper.TryParse("ati pr", out var pattern);
        success.Should().BeTrue();
        pattern.Should().Be(VerbPattern.Ati);
    }

    [Test]
    public void TryParse_InvalidPattern_ReturnsFalse()
    {
        var success = VerbPatternHelper.TryParse("invalid pattern", out var pattern);
        success.Should().BeFalse();
        pattern.Should().Be(default(VerbPattern));
    }

    #endregion

    #region IsIrregular tests (breakpoint-based)

    [Test]
    public void IsIrregular_RegularPatterns_ReturnsFalse()
    {
        VerbPattern.Ati.IsIrregular().Should().BeFalse();
        VerbPattern.Āti.IsIrregular().Should().BeFalse();
        VerbPattern.Eti.IsIrregular().Should().BeFalse();
        VerbPattern.Oti.IsIrregular().Should().BeFalse();
    }

    [Test]
    public void IsIrregular_IrregularPatterns_ReturnsTrue()
    {
        VerbPattern.Atthi.IsIrregular().Should().BeTrue();
        VerbPattern.Dakkhati.IsIrregular().Should().BeTrue();
        VerbPattern.Dammi.IsIrregular().Should().BeTrue();
        VerbPattern.Hanati.IsIrregular().Should().BeTrue();
        VerbPattern.Hoti.IsIrregular().Should().BeTrue();
        VerbPattern.Kubbati.IsIrregular().Should().BeTrue();
        VerbPattern.Natthi.IsIrregular().Should().BeTrue();
        VerbPattern.Eti2.IsIrregular().Should().BeTrue();
        VerbPattern.Brūti.IsIrregular().Should().BeTrue();
        VerbPattern.Karoti.IsIrregular().Should().BeTrue();
    }

    [Test]
    public void IsIrregular_BreakpointMarker_ReturnsFalse()
    {
        // _Irregular itself is NOT > _Irregular, so returns false
        VerbPattern._Irregular.IsIrregular().Should().BeFalse();
    }

    #endregion

    #region ParentRegular tests

    [Test]
    public void ParentRegular_AtiIrregulars_ReturnsAti()
    {
        VerbPattern.Atthi.ParentRegular().Should().Be(VerbPattern.Ati);
        VerbPattern.Dakkhati.ParentRegular().Should().Be(VerbPattern.Ati);
        VerbPattern.Dammi.ParentRegular().Should().Be(VerbPattern.Ati);
        VerbPattern.Hanati.ParentRegular().Should().Be(VerbPattern.Ati);
        VerbPattern.Hoti.ParentRegular().Should().Be(VerbPattern.Ati);
        VerbPattern.Kubbati.ParentRegular().Should().Be(VerbPattern.Ati);
        VerbPattern.Natthi.ParentRegular().Should().Be(VerbPattern.Ati);
    }

    [Test]
    public void ParentRegular_EtiIrregulars_ReturnsEti()
    {
        VerbPattern.Eti2.ParentRegular().Should().Be(VerbPattern.Eti);
    }

    [Test]
    public void ParentRegular_OtiIrregulars_ReturnsOti()
    {
        VerbPattern.Brūti.ParentRegular().Should().Be(VerbPattern.Oti);
        VerbPattern.Karoti.ParentRegular().Should().Be(VerbPattern.Oti);
    }

    [Test]
    public void ParentRegular_RegularPattern_ThrowsInvalidOperation()
    {
        var act = () => VerbPattern.Ati.ParentRegular();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not an irregular*");
    }

    #endregion

    #region ToDisplayLabel tests

    [Test]
    public void ToDisplayLabel_ReturnsFirstPartOfDbString()
    {
        VerbPattern.Ati.ToDisplayLabel().Should().Be("ati");
        VerbPattern.Āti.ToDisplayLabel().Should().Be("āti");
        VerbPattern.Eti.ToDisplayLabel().Should().Be("eti");
        VerbPattern.Oti.ToDisplayLabel().Should().Be("oti");
        VerbPattern.Hoti.ToDisplayLabel().Should().Be("hoti");
        VerbPattern.Karoti.ToDisplayLabel().Should().Be("karoti");
    }

    #endregion

    #region Breakpoint boundary tests

    [Test]
    public void Breakpoints_OrderIsCorrect()
    {
        // Verify the breakpoint ordering (traditional: a, ā, e, o)
        (VerbPattern.Ati < VerbPattern.Āti).Should().BeTrue(
            "Ati should be before Āti");
        (VerbPattern.Āti < VerbPattern.Eti).Should().BeTrue(
            "Āti should be before Eti");
        (VerbPattern.Eti < VerbPattern.Oti).Should().BeTrue(
            "Eti should be before Oti");
        (VerbPattern.Oti < VerbPattern._Irregular).Should().BeTrue(
            "Last regular (Oti) should be before _Irregular");
        (VerbPattern._Irregular < VerbPattern.Atthi).Should().BeTrue(
            "_Irregular should be before first irregular (alphabetical)");
    }

    [Test]
    public void AllIrregulars_HaveValidParent()
    {
        var irregulars = Enum.GetValues<VerbPattern>()
            .Where(p => p > VerbPattern._Irregular);

        foreach (var pattern in irregulars)
        {
            var parent = pattern.ParentRegular();
            parent.IsIrregular().Should().BeFalse(
                $"{pattern}'s parent {parent} should be regular");
        }
    }

    #endregion
}

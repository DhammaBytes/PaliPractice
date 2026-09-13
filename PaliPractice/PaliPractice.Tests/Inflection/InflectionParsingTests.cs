using PaliPractice.Tests.Inflection.Helpers;

namespace PaliPractice.Tests.Inflection;

[TestFixture]
public class InflectionParsingTests
{
    /// <summary>
    /// Sanity test: Verify HTML parsing works correctly.
    /// </summary>
    [Test]
    public void HtmlParser_ShouldExtractEndings()
    {
        var html = "<td title='masc nom sg'>dhamm<b>o</b></td><td title='masc nom pl'>dhamm<b>ā</b><br><span class='gray'>dhamm<b>āse</b></span></td>";

        var singularEndings = HtmlParser.ParseNounEndings(html, "masc nom sg");
        singularEndings.Should().Equal("o");

        var pluralEndings = HtmlParser.ParseNounEndings(html, "masc nom pl");
        pluralEndings.Should().Equal("ā", "āse");
    }

    /// <summary>
    /// Sanity test: Verify enum mapping works correctly.
    /// </summary>
    [Test]
    public void EnumMapper_ShouldParseNounTitle()
    {
        var (gender, nounCase, number) = EnumMapper.ParseNounTitle("masc nom sg");

        gender.Should().Be(Gender.Masculine);
        nounCase.Should().Be(Case.Nominative);
        number.Should().Be(Number.Singular);
    }

    /// <summary>
    /// Sanity test: Verify we can parse verb titles correctly.
    /// </summary>
    [Test]
    public void EnumMapper_ShouldParseVerbTitle_Active()
    {
        var (tense, person, number, reflexive) = EnumMapper.ParseVerbTitle("pr 3rd sg");

        tense.Should().Be(Tense.Present);
        person.Should().Be(Person.Third);
        number.Should().Be(Number.Singular);
        reflexive.Should().BeFalse();
    }

    /// <summary>
    /// Sanity test: Verify we can parse reflexive verb titles correctly (optative tense).
    /// </summary>
    [Test]
    public void EnumMapper_ShouldParseVerbTitle_Reflexive()
    {
        var (tense, person, number, reflexive) = EnumMapper.ParseVerbTitle("reflx opt 1st pl");

        tense.Should().Be(Tense.Optative);
        person.Should().Be(Person.First);
        number.Should().Be(Number.Plural);
        reflexive.Should().BeTrue();
    }

    /// <summary>
    /// Sanity test: Verify HTML parsing works for verbs.
    /// </summary>
    [Test]
    public void HtmlParser_ShouldExtractVerbEndings()
    {
        var html = "<td title='pr 3rd sg'>bhav<b>ati</b></td><td title='opt 1st pl'><span class='gray'>bhav<b>ema</b></span><br><span class='gray'>bhav<b>emu</b></span><br><span class='gray'>bhav<b>eyyāma</b></span></td>";

        var prEndings = HtmlParser.ParseVerbEndings(html, "pr 3rd sg");
        prEndings.Should().Equal("ati");

        var optEndings = HtmlParser.ParseVerbEndings(html, "opt 1st pl");
        optEndings.Should().Equal("ema", "emu", "eyyāma");
    }
}

using PaliPractice.Models.Inflection;
using PaliPractice.Tests.Inflection.Helpers;

namespace PaliPractice.Tests.Inflection;

/// <summary>
/// Data-driven tests for NounEndings.cs, validating all patterns against DPD database.
/// These tests ensure our hardcoded patterns match the DPD source of truth exactly.
/// </summary>
[TestFixture]
public class NounPatternsTests
{
    static DpdTestHelper? _dpdHelper;
    const string DpdDbPath = "/Users/ivm/Sources/PaliPractice/dpd-db/dpd.db";

    /// <summary>
    /// Test case data: pattern, word, case, number, expected endings.
    /// </summary>
    public record NounTestCase(
        NounPattern Pattern,
        string Word,
        Case Case,
        Number Number,
        string[] ExpectedEndings)
    {
        public override string ToString() =>
            $"{Pattern.ToDbString()} | {Word} | {Case} {Number} | [{string.Join(", ", ExpectedEndings)}]";
    }

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _dpdHelper = new DpdTestHelper(DpdDbPath);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _dpdHelper?.Dispose();
    }

    /// <summary>
    /// Generate test cases for all regular noun patterns.
    /// Irregular patterns are handled via database lookup, not hardcoded endings.
    /// For each pattern, get top 3 words and test all 16 grammatical combinations.
    /// </summary>
    public static IEnumerable<NounTestCase> GetNounTestCases()
    {
        // Get only BASE patterns (not variants or irregulars)
        // Only base patterns have ending tables defined in NounEndings.cs
        var patterns = Enum.GetValues<NounPattern>()
            .Where(p => p.IsBase())
            .ToList();

        using var helper = new DpdTestHelper(DpdDbPath);

        foreach (var pattern in patterns)
        {
            var dbString = pattern.ToDbString();

            // Get top 3 words for this pattern
            var words = helper.GetTopWordsByPattern(dbString, limit: 3);

            foreach (var word in words)
            {
                // Test all 16 combinations (8 cases × 2 numbers)
                foreach (Case nounCase in Enum.GetValues<Case>())
                {
                    if (nounCase == Case.None) continue;

                    foreach (Number number in Enum.GetValues<Number>())
                    {
                        if (number == Number.None) continue;

                        // Build the DPD title format
                        var gender = dbString.Contains(NounEndings.MascAbbrev) ? NounEndings.MascAbbrev :
                                   dbString.Contains(NounEndings.NeutAbbrev) ? NounEndings.NeutAbbrev : NounEndings.FemAbbrev;

                        var caseStr = nounCase switch
                        {
                            Case.Nominative => "nom",
                            Case.Accusative => "acc",
                            Case.Instrumental => "instr",
                            Case.Dative => "dat",
                            Case.Ablative => "abl",
                            Case.Genitive => "gen",
                            Case.Locative => "loc",
                            Case.Vocative => "voc",
                            _ => throw new ArgumentException($"Unknown case: {nounCase}")
                        };

                        var numberStr = number == Number.Singular ? "sg" : "pl";
                        var title = $"{gender} {caseStr} {numberStr}";

                        // Parse expected endings from DPD HTML
                        var expectedEndings = HtmlParser.ParseNounEndings(word.InflectionsHtml, title);

                        // Skip if no endings found (might be a gap in the pattern)
                        if (expectedEndings.Length == 0)
                        {
                            continue;
                        }

                        yield return new NounTestCase(
                            pattern,
                            word.Lemma1,
                            nounCase,
                            number,
                            expectedEndings
                        );
                    }
                }
            }
        }
    }

    [Test]
    [TestCaseSource(nameof(GetNounTestCases))]
    public void NounPattern_ShouldMatchDpdEndings(NounTestCase testCase)
    {
        // Act: Generate endings using our pattern class
        var actualEndings = NounEndings.GetEndings(testCase.Pattern, testCase.Case, testCase.Number);

        // Assert: Should match DPD exactly (including order)
        actualEndings.Should().Equal(testCase.ExpectedEndings,
            because: $"pattern '{testCase.Pattern.ToDbString()}' for {testCase.Case} {testCase.Number} should match DPD");
    }

    /// <summary>
    /// Sanity test: Verify we can read from DPD database.
    /// </summary>
    [Test]
    public void DpdDatabase_ShouldBeAccessible()
    {
        _dpdHelper.Should().NotBeNull();

        var words = _dpdHelper!.GetTopWordsByPattern("a masc", limit: 1);
        words.Should().NotBeEmpty();
        words[0].InflectionsHtml.Should().NotBeNullOrEmpty();
    }

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
}

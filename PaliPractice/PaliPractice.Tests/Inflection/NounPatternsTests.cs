using PaliPractice.Models.Inflection;
using PaliPractice.Tests.Inflection.Helpers;

namespace PaliPractice.Tests.Inflection;

/// <summary>
/// Data-driven tests for NounPatterns.cs, validating all patterns against DPD database.
/// These tests ensure our hardcoded patterns match the DPD source of truth exactly.
/// </summary>
[TestFixture]
public class NounPatternsTests
{
    private static DpdTestHelper? _dpdHelper;
    private const string DpdDbPath = "/Users/ivm/Sources/PaliPractice/dpd-db/dpd.db";

    /// <summary>
    /// Test case data: pattern, word, case, number, expected endings.
    /// </summary>
    public record NounTestCase(
        string Pattern,
        string Word,
        NounCase Case,
        Number Number,
        string[] ExpectedEndings)
    {
        public override string ToString() =>
            $"{Pattern} | {Word} | {Case} {Number} | [{string.Join(", ", ExpectedEndings)}]";
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
    /// Generate test cases for all noun patterns.
    /// For each of the 12 patterns, get top 3 words and test all 16 grammatical combinations.
    /// </summary>
    public static IEnumerable<NounTestCase> GetNounTestCases()
    {
        // These are the 12 patterns we support (94% coverage)
        var patterns = new[]
        {
            "a masc", "a nt", "ā fem",
            "i masc", "i fem", "ī masc", "ī fem",
            "u masc", "u nt", "u fem",
            "as masc", "ar masc"
        };

        using var helper = new DpdTestHelper(DpdDbPath);

        foreach (var pattern in patterns)
        {
            // Get top 3 words for this pattern
            var words = helper.GetTopWordsByPattern(pattern, limit: 3);

            foreach (var word in words)
            {
                // Test all 16 combinations (8 cases × 2 numbers)
                foreach (NounCase nounCase in Enum.GetValues<NounCase>())
                {
                    if (nounCase == NounCase.None) continue;

                    foreach (Number number in Enum.GetValues<Number>())
                    {
                        if (number == Number.None) continue;

                        // Build the DPD title format
                        var gender = pattern.Contains("masc") ? "masc" :
                                   pattern.Contains("nt") ? "nt" : "fem";

                        var caseStr = nounCase switch
                        {
                            NounCase.Nominative => "nom",
                            NounCase.Accusative => "acc",
                            NounCase.Instrumental => "instr",
                            NounCase.Dative => "dat",
                            NounCase.Ablative => "abl",
                            NounCase.Genitive => "gen",
                            NounCase.Locative => "loc",
                            NounCase.Vocative => "voc",
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
        var actualEndings = NounPatterns.GetEndings(testCase.Pattern, testCase.Case, testCase.Number);

        // Assert: Should match DPD exactly (including order)
        actualEndings.Should().Equal(testCase.ExpectedEndings,
            because: $"pattern '{testCase.Pattern}' for {testCase.Case} {testCase.Number} should match DPD");
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
        singularEndings.Should().Equal(new[] { "o" });

        var pluralEndings = HtmlParser.ParseNounEndings(html, "masc nom pl");
        pluralEndings.Should().Equal(new[] { "ā", "āse" });
    }

    /// <summary>
    /// Sanity test: Verify enum mapping works correctly.
    /// </summary>
    [Test]
    public void EnumMapper_ShouldParseNounTitle()
    {
        var (gender, nounCase, number) = EnumMapper.ParseNounTitle("masc nom sg");

        gender.Should().Be(Gender.Masculine);
        nounCase.Should().Be(NounCase.Nominative);
        number.Should().Be(Number.Singular);
    }
}

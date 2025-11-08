using PaliPractice.Models.Inflection;
using PaliPractice.Tests.Inflection.Helpers;

namespace PaliPractice.Tests.Inflection;

/// <summary>
/// Data-driven tests for VerbPatterns.cs, validating all patterns against DPD database.
/// These tests ensure our hardcoded patterns match the DPD source of truth exactly.
/// </summary>
[TestFixture]
public class VerbPatternsTests
{
    private static DpdTestHelper? _dpdHelper;
    private const string DpdDbPath = "/Users/ivm/Sources/PaliPractice/dpd-db/dpd.db";

    /// <summary>
    /// Test case data: pattern, word, grammatical parameters, expected endings.
    /// </summary>
    public record VerbTestCase(
        string Pattern,
        string Word,
        Tense Tense,
        Person Person,
        Number Number,
        Voice Voice,
        string[] ExpectedEndings)
    {
        public override string ToString() =>
            $"{Pattern} | {Word} | {Tense} {Voice} {Person} {Number} | [{string.Join(", ", ExpectedEndings)}]";
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
    /// Generate test cases for all verb patterns.
    /// For each of the 3 regular patterns, get top 3 words and test all relevant grammatical combinations.
    /// </summary>
    public static IEnumerable<VerbTestCase> GetVerbTestCases()
    {
        // These are the 3 regular present tense patterns we support
        // Aorist and irregular patterns are excluded due to high variability
        var patterns = new[]
        {
            "ati pr", "eti pr", "oti pr"
        };

        using var helper = new DpdTestHelper(DpdDbPath);

        foreach (var pattern in patterns)
        {
            // Get top 3 words for this pattern
            var words = helper.GetTopWordsByPattern(pattern, limit: 3);

            foreach (var word in words)
            {
                // Parse all verb forms from HTML to determine which combinations are valid
                var allForms = HtmlParser.ParseAllVerbEndings(word.InflectionsHtml);

                foreach (var (title, endings) in allForms)
                {
                    // Skip empty forms
                    if (endings.Length == 0)
                    {
                        continue;
                    }

                    // Parse the DPD title to our enum values
                    // Skip forms we don't support yet (e.g., passive, causative)
                    (Tense tense, Person person, Number number, Voice voice)? parsed = null;

                    try
                    {
                        parsed = EnumMapper.ParseVerbTitle(title);
                    }
                    catch (ArgumentException)
                    {
                        // Skip unsupported forms - this is expected since we only implement the most common forms
                        continue;
                    }

                    if (parsed.HasValue)
                    {
                        var (tense, person, number, voice) = parsed.Value;

                        yield return new VerbTestCase(
                            pattern,
                            word.Lemma1,
                            tense,
                            person,
                            number,
                            voice,
                            endings
                        );
                    }
                }
            }
        }
    }

    [Test]
    [TestCaseSource(nameof(GetVerbTestCases))]
    public void VerbPattern_ShouldMatchDpdEndings(VerbTestCase testCase)
    {
        // Act: Generate endings using our pattern class
        var actualEndings = VerbPatterns.GetEndings(
            testCase.Pattern,
            testCase.Person,
            testCase.Number,
            testCase.Tense,
            testCase.Voice
        );

        // Assert: Should match DPD exactly (including order)
        actualEndings.Should().Equal(testCase.ExpectedEndings,
            because: $"pattern '{testCase.Pattern}' for {testCase.Tense} {testCase.Voice} {testCase.Person} {testCase.Number} should match DPD");
    }

    /// <summary>
    /// Sanity test: Verify we can parse verb titles correctly.
    /// </summary>
    [Test]
    public void EnumMapper_ShouldParseVerbTitle_Active()
    {
        var (tense, person, number, voice) = EnumMapper.ParseVerbTitle("pr 3rd sg");

        tense.Should().Be(Tense.Present);
        person.Should().Be(Person.Third);
        number.Should().Be(Number.Singular);
        voice.Should().Be(Voice.Active);
    }

    /// <summary>
    /// Sanity test: Verify we can parse reflexive verb titles correctly (optative tense).
    /// </summary>
    [Test]
    public void EnumMapper_ShouldParseVerbTitle_Reflexive()
    {
        var (tense, person, number, voice) = EnumMapper.ParseVerbTitle("reflx opt 1st pl");

        tense.Should().Be(Tense.Optative);
        person.Should().Be(Person.First);
        number.Should().Be(Number.Plural);
        voice.Should().Be(Voice.Reflexive);
    }

    /// <summary>
    /// Sanity test: Verify HTML parsing works for verbs.
    /// </summary>
    [Test]
    public void HtmlParser_ShouldExtractVerbEndings()
    {
        var html = "<td title='pr 3rd sg'>bhav<b>ati</b></td><td title='opt 1st pl'><span class='gray'>bhav<b>ema</b></span><br><span class='gray'>bhav<b>emu</b></span><br><span class='gray'>bhav<b>eyyāma</b></span></td>";

        var prEndings = HtmlParser.ParseVerbEndings(html, "pr 3rd sg");
        prEndings.Should().Equal(new[] { "ati" });

        var optEndings = HtmlParser.ParseVerbEndings(html, "opt 1st pl");
        optEndings.Should().Equal(new[] { "ema", "emu", "eyyāma" });
    }
}

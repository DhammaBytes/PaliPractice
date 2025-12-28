using PaliPractice.Models.Inflection;
using PaliPractice.Tests.Inflection.Helpers;

namespace PaliPractice.Tests.Inflection;

/// <summary>
/// Data-driven tests for VerbEndings.cs, validating all patterns against DPD database.
/// These tests ensure our hardcoded patterns match the DPD source of truth exactly.
/// </summary>
[TestFixture]
public class VerbPatternsTests
{
    static DpdTestHelper? _dpdHelper;
    const string DpdDbPath = "/Users/ivm/Sources/PaliPractice/dpd-db/dpd.db";

    /// <summary>
    /// Test case data: pattern, word, grammatical parameters, expected endings.
    /// </summary>
    public record VerbTestCase(
        VerbPattern Pattern,
        string Word,
        Tense Tense,
        Person Person,
        Number Number,
        bool Reflexive,
        string[] ExpectedEndings)
    {
        public override string ToString() =>
            $"{Pattern.ToDbString()} | {Word} | {Tense} {(Reflexive ? "Reflexive" : "Active")} {Person} {Number} | [{string.Join(", ", ExpectedEndings)}]";
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
    /// For each pattern, get top 3 words and test all relevant grammatical combinations.
    /// </summary>
    public static IEnumerable<VerbTestCase> GetVerbTestCases()
    {
        // Regular patterns + key irregular patterns (using enum values)
        var patterns = new[]
        {
            // Regular
            VerbPattern.Ati, VerbPattern.Eti, VerbPattern.Oti, VerbPattern.Āti,
            // Irregular
            VerbPattern.Hoti, VerbPattern.Atthi, VerbPattern.Karoti, VerbPattern.Brūti
        };

        using var helper = new DpdTestHelper(DpdDbPath);

        foreach (var pattern in patterns)
        {
            var dbString = pattern.ToDbString();

            // Get top 3 words for this pattern
            var words = helper.GetTopWordsByPattern(dbString, limit: 3);

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
                    (Tense tense, Person person, Number number, bool reflexive)? parsed = null;

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
                        var (tense, person, number, reflexive) = parsed.Value;

                        yield return new VerbTestCase(
                            pattern,
                            word.Lemma1,
                            tense,
                            person,
                            number,
                            reflexive,
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
        var actualEndings = VerbEndings.GetEndings(
            testCase.Pattern,
            testCase.Person,
            testCase.Number,
            testCase.Tense,
            testCase.Reflexive
        );

        // Assert: Should match DPD exactly (including order)
        actualEndings.Should().Equal(testCase.ExpectedEndings,
            because: $"pattern '{testCase.Pattern.ToDbString()}' for {testCase.Tense} {(testCase.Reflexive ? "Reflexive" : "Active")} {testCase.Person} {testCase.Number} should match DPD");
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

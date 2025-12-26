using PaliPractice.Models.Inflection;

namespace PaliPractice.Tests.Inflection;

/// <summary>
/// Tests for FormId encoding/decoding in Declension and Conjugation classes.
/// FormIds provide stable, unique identifiers for grammatical forms.
/// </summary>
[TestFixture]
public class FormIdTests
{
    #region Declension FormId Tests

    [Test]
    public void Declension_ResolveId_ProducesExpectedFormat()
    {
        // lemmaId=10789, case=3(Instrumental), gender=1(Masc), number=2(Plural), endingId=2
        // Expected: 10789_3_1_2_2 → 107893122
        var formId = Declension.ResolveId(10789, Case.Instrumental, Gender.Masculine, Number.Plural, 2);

        formId.Should().Be(107893122);
    }

    [Test]
    public void Declension_ParseId_ReconstructsComponents()
    {
        var formId = 107893122;

        var (lemmaId, @case, gender, number, endingId) = Declension.ParseId(formId);

        lemmaId.Should().Be(10789);
        @case.Should().Be(Case.Instrumental);
        gender.Should().Be(Gender.Masculine);
        number.Should().Be(Number.Plural);
        endingId.Should().Be(2);
    }

    [Test]
    public void Declension_RoundTrip_AllCases()
    {
        foreach (Case c in Enum.GetValues<Case>().Where(c => c != Case.None))
        foreach (Gender g in Enum.GetValues<Gender>().Where(g => g != Gender.None))
        foreach (Number n in Enum.GetValues<Number>().Where(n => n != Number.None))
        for (int e = 0; e <= 4; e++)
        {
            var lemmaId = 12345;
            var formId = Declension.ResolveId(lemmaId, c, g, n, e);
            var parsed = Declension.ParseId(formId);

            parsed.LemmaId.Should().Be(lemmaId);
            parsed.Case.Should().Be(c);
            parsed.Gender.Should().Be(g);
            parsed.Number.Should().Be(n);
            parsed.EndingId.Should().Be(e);
        }
    }

    [Test]
    public void Declension_FormId_ZeroEndingId_IndicatesCombination()
    {
        // EndingId=0 means "combination" (the Declension itself, not a specific form)
        var formId = Declension.ResolveId(10001, Case.Nominative, Gender.Masculine, Number.Singular, 0);

        var parsed = Declension.ParseId(formId);
        parsed.EndingId.Should().Be(0, "EndingId=0 indicates a combination, not a specific ending");
    }

    [Test]
    public void Declension_FormId_Property_UsesZeroEndingId()
    {
        var declension = new Declension
        {
            LemmaId = 10001,
            Case = Case.Nominative,
            Gender = Gender.Masculine,
            Number = Number.Singular,
            Forms = []
        };

        var expected = Declension.ResolveId(10001, Case.Nominative, Gender.Masculine, Number.Singular, 0);
        declension.FormId.Should().Be(expected);
    }

    [TestCase(10001, 100011110, Description = "First noun, Nom Masc Sg, no ending")]
    [TestCase(69999, 699991110, Description = "Last noun ID boundary")]
    public void Declension_BoundaryLemmaIds(int lemmaId, int expectedFormId)
    {
        var formId = Declension.ResolveId(lemmaId, Case.Nominative, Gender.Masculine, Number.Singular, 0);
        formId.Should().Be(expectedFormId);
    }

    #endregion

    #region Conjugation FormId Tests

    [Test]
    public void Conjugation_ResolveId_ProducesExpectedFormat()
    {
        // lemmaId=70683, tense=2(Imperative), person=3(Third), number=1(Sg), voice=2(Reflexive), endingId=3
        // Expected: 70683_2_3_1_2_3 → 7068323123
        var formId = Conjugation.ResolveId(70683, Tense.Imperative, Person.Third, Number.Singular, Voice.Reflexive, 3);

        formId.Should().Be(7068323123L);
    }

    [Test]
    public void Conjugation_ParseId_ReconstructsComponents()
    {
        var formId = 7068323123L;

        var (lemmaId, tense, person, number, voice, endingId) = Conjugation.ParseId(formId);

        lemmaId.Should().Be(70683);
        tense.Should().Be(Tense.Imperative);
        person.Should().Be(Person.Third);
        number.Should().Be(Number.Singular);
        voice.Should().Be(Voice.Reflexive);
        endingId.Should().Be(3);
    }

    [Test]
    public void Conjugation_RoundTrip_AllCombinations()
    {
        foreach (Tense t in Enum.GetValues<Tense>().Where(t => t != Tense.None))
        foreach (Person p in Enum.GetValues<Person>().Where(p => p != Person.None))
        foreach (Number n in Enum.GetValues<Number>().Where(n => n != Number.None))
        foreach (Voice v in Enum.GetValues<Voice>().Where(v => v != Voice.None))
        for (int e = 0; e <= 4; e++)
        {
            var lemmaId = 75000;
            var formId = Conjugation.ResolveId(lemmaId, t, p, n, v, e);
            var parsed = Conjugation.ParseId(formId);

            parsed.LemmaId.Should().Be(lemmaId);
            parsed.Tense.Should().Be(t);
            parsed.Person.Should().Be(p);
            parsed.Number.Should().Be(n);
            parsed.Voice.Should().Be(v);
            parsed.EndingId.Should().Be(e);
        }
    }

    [Test]
    public void Conjugation_FormId_ZeroEndingId_IndicatesCombination()
    {
        var formId = Conjugation.ResolveId(70001, Tense.Present, Person.First, Number.Singular, Voice.Active, 0);

        var parsed = Conjugation.ParseId(formId);
        parsed.EndingId.Should().Be(0, "EndingId=0 indicates a combination, not a specific ending");
    }

    [Test]
    public void Conjugation_FormId_Property_UsesZeroEndingId()
    {
        var conjugation = new Conjugation
        {
            LemmaId = 70001,
            Tense = Tense.Present,
            Person = Person.First,
            Number = Number.Singular,
            Voice = Voice.Active,
            Forms = []
        };

        var expected = Conjugation.ResolveId(70001, Tense.Present, Person.First, Number.Singular, Voice.Active, 0);
        conjugation.FormId.Should().Be(expected);
    }

    [TestCase(70001, 7000111110L, Description = "First verb, Present 1st Sg Active, no ending")]
    [TestCase(99999, 9999911110L, Description = "Last verb ID boundary")]
    public void Conjugation_BoundaryLemmaIds(int lemmaId, long expectedFormId)
    {
        var formId = Conjugation.ResolveId(lemmaId, Tense.Present, Person.First, Number.Singular, Voice.Active, 0);
        formId.Should().Be(expectedFormId);
    }

    [Test]
    public void Conjugation_FormId_IsLong_DueToTenDigits()
    {
        // Max possible: 99999_5_3_2_4_4 = 9999953244
        var maxFormId = Conjugation.ResolveId(99999, Tense.Aorist, Person.Third, Number.Plural, Voice.Causative, 4);

        maxFormId.Should().BeGreaterThan(int.MaxValue, "10-digit verb FormIds exceed int.MaxValue");
    }

    #endregion

    #region Cross-Type Tests

    [Test]
    public void FormIds_NounsAndVerbs_DoNotOverlap()
    {
        // Noun range: 10001-69999 → FormIds start at 100010000
        // Verb range: 70001-99999 → FormIds start at 7000100000

        var maxNounFormId = Declension.ResolveId(69999, Case.Vocative, Gender.Feminine, Number.Plural, 4);
        var minVerbFormId = Conjugation.ResolveId(70001, Tense.Present, Person.First, Number.Singular, Voice.Active, 0);

        ((long)maxNounFormId).Should().BeLessThan(minVerbFormId,
            "Noun FormIds should never overlap with Verb FormIds");
    }

    #endregion
}

using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;
using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.Grammar;
using PaliPractice.Tests.Practice.Fakes;
using PaliPractice.Tests.DataIntegrity.Helpers;
using SQLite;
using System.Text.Json;
using Path = System.IO.Path;

namespace PaliPractice.Tests.DataIntegrity;

[TestFixture]
public class PrimaryAttestationTests
{
    [TestCase(10335, Case.Locative, "addhanesu")]
    [TestCase(11007, Case.Instrumental, "sīhanādebhi")]
    [TestCase(11007, Case.Ablative, "sīhanādebhi")]
    [TestCase(11007, Case.Genitive, "sīhanādāna")]
    public void AlternateParadigmCannotAttestPrimaryForm(int lemmaId, Case nounCase, string text)
    {
        using var connection = new SQLiteConnection(TestPaths.PaliDbPath, SQLiteOpenFlags.ReadOnly);
        var nouns = new NounRepository(connection);
        var service = new InflectionService(new RepositoryBackedTestDatabaseService(
            nouns, new VerbRepository(connection), new FakeUserDataRepository()));
        var noun = (Noun)nouns.GetLemma(lemmaId)!.Primary;
        var forms = service.GenerateNounForms(noun, nounCase, Number.Plural);
        forms.Forms.Single(form => form.Form == text).InCorpus.Should().BeFalse(
            "this rendered form is absent from the pinned corpus despite a colliding alternate-paradigm ID");
    }
    [Test]
    public void EveryExtractedRawPatternIsSupported()
    {
        using var connection = new SQLiteConnection(TestPaths.PaliDbPath, SQLiteOpenFlags.ReadOnly);
        foreach (var pattern in connection.Table<Noun>().Select(word => word.RawPattern).Distinct())
            NounPatternHelper.Parse(pattern).IsMarkerOrNone().Should().BeFalse();
        foreach (var pattern in connection.Table<Verb>().Select(word => word.RawPattern).Distinct())
            VerbPatternHelper.Parse(pattern).IsMarkerOrNone().Should().BeFalse();
    }

    [Test]
    public void EveryPrimaryRenderedFormHasExactCorpusAttestationAndEligibility()
    {
        using var connection = new SQLiteConnection(TestPaths.PaliDbPath, SQLiteOpenFlags.ReadOnly);
        var nouns = new NounRepository(connection);
        var verbs = new VerbRepository(connection);
        var service = new InflectionService(new RepositoryBackedTestDatabaseService(nouns, verbs, new FakeUserDataRepository()));
        var corpus = TipitakaWordlistLoader.GetAllWords();
        var errors = new List<string>();
        using var expectedJson = JsonDocument.Parse(File.ReadAllText(
            TestPaths.PrimaryFormsPath));
        var expected = expectedJson.RootElement.EnumerateArray().ToDictionary(
            row => (row[0].GetInt32(), row[1].GetInt64()), row => row[2].GetString()!);
        int checkedForms = CheckNouns(nouns, service, corpus, errors, expected) + CheckVerbs(verbs, service, corpus, errors, expected);
        expected.Should().BeEmpty("all primary forms from the DPD templates must be rendered");
        checkedForms.Should().BeGreaterThan(0);
        errors.Should().BeEmpty("every actual primary form must agree with corpus membership and repository eligibility");
        TestContext.WriteLine($"Exact primary form checks: {checkedForms}");
    }

    static int CheckNouns(NounRepository nouns, InflectionService service, HashSet<string> corpus, List<string> errors, Dictionary<(int, long), string> expected)
    {
        int checkedForms = 0;
        foreach (var lemma in nouns.GetLemmasByRank(1, nouns.GetCount()))
        {
            var noun = (Noun)lemma.Primary;
            int usable = 0;
            foreach (var nounCase in Enum.GetValues<Case>().Where(value => value != Case.None))
            foreach (var number in new[] { Number.Singular, Number.Plural })
            {
                var forms = service.GenerateNounForms(noun, nounCase, number).Forms;
                usable += forms.Count;
                foreach (var form in forms)
                {
                    checkedForms++;
                    if (!expected.Remove((noun.Id, form.FormId), out var sourceForm) || sourceForm != form.Form)
                        errors.Add($"Noun {noun.Id}/{form.FormId}: rendered form differs from DPD template: {form.Form}");
                    if (form.InCorpus != corpus.Contains(form.Form))
                        errors.Add($"Noun {noun.Id}/{form.FormId}: {form.Form} has wrong attestation");
                }
                if (nouns.HasAttestedForm(noun.LemmaId, nounCase, noun.Gender, number) != forms.Any(f => f.InCorpus))
                    errors.Add($"Noun {noun.Id} {nounCase}/{number}: queue/render disagreement");
            }
            if (usable == 0) errors.Add($"Noun {noun.Id}: no usable forms");
        }
        return checkedForms;
    }

    static int CheckVerbs(VerbRepository verbs, InflectionService service, HashSet<string> corpus, List<string> errors, Dictionary<(int, long), string> expected)
    {
        int checkedForms = 0;
        foreach (var lemma in verbs.GetLemmasByRank(1, verbs.GetCount()))
        {
            var verb = (Verb)lemma.Primary;
            int usable = 0;
            foreach (var tense in Enum.GetValues<Tense>().Where(value => value != Tense.None))
            foreach (var person in new[] { Person.First, Person.Second, Person.Third })
            foreach (var number in new[] { Number.Singular, Number.Plural })
            foreach (var reflexive in new[] { false, true })
            {
                var forms = service.GenerateVerbForms(verb, person, number, tense, reflexive).Forms;
                usable += forms.Count;
                foreach (var form in forms)
                {
                    checkedForms++;
                    if (!expected.Remove((verb.Id, form.FormId), out var sourceForm) || sourceForm != form.Form)
                        errors.Add($"Verb {verb.Id}/{form.FormId}: rendered form differs from DPD template: {form.Form}");
                    if (form.InCorpus != corpus.Contains(form.Form))
                        errors.Add($"Verb {verb.Id}/{form.FormId}: {form.Form} has wrong attestation");
                }
                if (verbs.HasAttestedForm(verb.LemmaId, tense, person, number, reflexive) != forms.Any(f => f.InCorpus))
                    errors.Add($"Verb {verb.Id} {tense}/{person}/{number}/{reflexive}: queue/render disagreement");
            }
            if (usable == 0) errors.Add($"Verb {verb.Id}: no usable forms");
        }
        return checkedForms;
    }

}

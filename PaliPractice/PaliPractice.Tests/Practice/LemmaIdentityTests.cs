using PaliPractice.Models.Words;
using PaliPractice.Services.Database.Entities;
using PaliPractice.Services.Database.Repositories;
using SQLite;

namespace PaliPractice.Tests.Practice;

[TestFixture]
public class LemmaIdentityTests
{
    static Noun Sense(int id, bool primary = false, string pattern = "a masc", string stem = "vass",
        Gender gender = Gender.Masculine, int frequency = 10) => new()
    {
        Id = id, LemmaId = 10001, Lemma = "vassa", RawPattern = pattern,
        Stem = stem, Gender = gender, EbtCount = frequency, PracticePrimary = primary
    };

    [Test]
    public void ExplicitSenseControlsPrimaryAndRankDespiteMajorityOrFrequency()
    {
        IWord[] words = [Sense(3, pattern: "a nt", frequency: 500),
            Sense(2, pattern: "a nt", frequency: 300), Sense(1, primary: true)];
        foreach (var order in new[] { words, words.Reverse().ToArray() })
        {
            var lemma = new Lemma("vassa", order);
            lemma.Primary.Id.Should().Be(1);
            lemma.EbtCount.Should().Be(10);
            lemma.ExcludedWords.Select(w => w.Id).Should().Equal(2, 3);
        }
    }

    [Test]
    public void SamePatternWithDifferentStemOrGenderDoesNotSharePracticeParadigm()
    {
        var lemma = new Lemma("vassa", [Sense(1, primary: true), Sense(2, frequency: 900),
            Sense(3, stem: "different"), Sense(4, gender: Gender.Neuter)]);
        lemma.Words.Select(w => w.Id).Should().Equal(1, 2);
        lemma.ExcludedWords.Select(w => w.Id).Should().Equal(3, 4);
    }

    [Test]
    public void MultipleExplicitChoicesFail()
    {
        Action construct = () => _ = new Lemma("vassa", [Sense(1, true), Sense(2, true)]);
        construct.Should().Throw<InvalidDataException>();
    }

    [Test]
    public void MissingExplicitChoiceFails()
    {
        Action construct = () => _ = new Lemma("vassa", [Sense(3, pattern: "a nt", frequency: 500),
            Sense(2), Sense(1)]);
        construct.Should().Throw<InvalidDataException>();
    }

    [Test]
    public void ExplicitVerbSelectionAlsoControlsPrimary()
    {
        var lemma = new Lemma("musati", [
            new Verb { Id = 2, LemmaId = 70001, RawPattern = "ati pr", Stem = "mus", EbtCount = 900 },
            new Verb { Id = 1, LemmaId = 70001, RawPattern = "ati pr", Stem = "mus", PracticePrimary = true }
        ]);
        lemma.Primary.Id.Should().Be(1);
        lemma.Words.Should().HaveCount(2);
    }
    [Test]
    public void RepositoriesReadExplicitSelectionFromStoredRows()
    {
        using var connection = new SQLiteConnection(":memory:");
        connection.CreateTable<Noun>();
        connection.CreateTable<Verb>();
        connection.CreateTable<NounCorpusForm>();
        connection.CreateTable<VerbCorpusForm>();
        connection.CreateTable<NounIrregularForm>();
        connection.CreateTable<VerbIrregularForm>();
        connection.Execute("CREATE TABLE verbs_nonreflexive (lemma_id INTEGER)");
        connection.InsertAll(new[] { Sense(1, true), Sense(2, frequency: 900) });
        connection.InsertAll(new[] {
            new Verb { Id = 1, LemmaId = 70001, Lemma = "musati", RawPattern = "ati pr",
                Stem = "mus", PracticePrimary = true },
            new Verb { Id = 2, LemmaId = 70001, Lemma = "musati", RawPattern = "ati pr",
                Stem = "mus", EbtCount = 900 }
        });
        var nouns = new NounRepository(connection);
        var verbs = new VerbRepository(connection);
        nouns.GetLemma(10001)!.Primary.Id.Should().Be(1);
        nouns.GetLemma(10001)!.EbtCount.Should().Be(10);
        verbs.GetLemma(70001)!.Primary.Id.Should().Be(1);
        verbs.GetLemma(70001)!.EbtCount.Should().Be(0);
    }

}

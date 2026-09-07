using PaliPractice.Models.Words;
using PaliPractice.Presentation.Practice.ViewModels.Common.Entries;

namespace PaliPractice.Tests.Practice;

[TestFixture]
public class TranslationEntryTests
{
    [Test]
    public void BuildFromAllDetails_GroupsBySelectedLanguageMeaning()
    {
        var details = new IWordDetails[]
        {
            new TestWordDetails
            {
                Id = 1,
                LemmaId = 10001,
                Meaning = "мудрость",
                MeaningLanguage = TranslationLanguagePreference.Russian,
                Example1 = "example one",
                Source1 = "MN",
                Sutta1 = "1"
            },
            new TestWordDetails
            {
                Id = 2,
                LemmaId = 10001,
                Meaning = "мудрость",
                MeaningLanguage = TranslationLanguagePreference.Russian,
                Example1 = "example two",
                Source1 = "SN",
                Sutta1 = "2"
            }
        };

        var entries = TranslationEntry.BuildFromAllDetails(details);

        entries.Should().HaveCount(1);
        entries[0].Meaning.Should().Be("мудрость");
        entries[0].Examples.Should().HaveCount(2);
    }

    [Test]
    public void BuildFromAllDetails_PreservesRepositoryResolvedFallback()
    {
        var details = new IWordDetails[]
        {
            new TestWordDetails
            {
                Id = 1,
                LemmaId = 10001,
                Meaning = "truth",
                MeaningLanguage = TranslationLanguagePreference.English,
                Example1 = "example one",
                Source1 = "MN",
                Sutta1 = "1"
            }
        };

        var entries = TranslationEntry.BuildFromAllDetails(details);

        entries.Should().HaveCount(1);
        entries[0].Meaning.Should().Be("truth");
    }

    sealed class TestWordDetails : IWordDetails
    {
        public int Id { get; init; }
        public int LemmaId { get; init; }
        public string Variant { get; init; } = string.Empty;
        public string Root { get; init; } = string.Empty;
        public string Meaning { get; set; } = string.Empty;
        public TranslationLanguagePreference MeaningLanguage { get; set; } = TranslationLanguagePreference.English;
        public string Source1 { get; init; } = string.Empty;
        public string Sutta1 { get; init; } = string.Empty;
        public string Example1 { get; init; } = string.Empty;
        public string Source2 { get; init; } = string.Empty;
        public string Sutta2 { get; init; } = string.Empty;
        public string Example2 { get; init; } = string.Empty;
    }
}

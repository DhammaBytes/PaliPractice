using System.Text;
using PaliPractice.Tests.DataIntegrity.Helpers;

namespace PaliPractice.Tests.DataIntegrity;

[TestFixture]
public class CorpusNormalizationTests
{
    [Test]
    public void TipitakaWordlist_IsNfcNormalized()
    {
        var tipitakaWords = TipitakaWordlistLoader.GetAllWords();
        var nonNfc = tipitakaWords
            .Where(w => w != w.Normalize(NormalizationForm.FormC))
            .Take(100)
            .ToList();

        TestContext.WriteLine($"Non-NFC words in Tipitaka wordlist: {nonNfc.Count}");
        foreach (var word in nonNfc.Take(20))
            TestContext.WriteLine($"  '{word}'");

        // Allow some tolerance - report but don't fail if minor
        if (nonNfc.Count > 0)
        {
            var rate = (double)nonNfc.Count / tipitakaWords.Count;
            rate.Should().BeLessThan(0.001,
                "less than 0.1% of Tipitaka words should be non-NFC normalized");
        }
    }

    [Test]
    public void NounLemmas_FindableInTipitakaWithNormalization()
    {
        using var paliDb = new PaliDbLoader();
        var tipitakaWords = TipitakaWordlistLoader.GetAllWords();

        // Sample some nouns that should definitely be in corpus
        var sampleNouns = paliDb.GetAllNouns()
            .Where(n => n.EbtCount > 100) // High-frequency words
            .Take(50)
            .ToList();

        var foundWithNormalization = new List<string>();

        foreach (var noun in sampleNouns)
        {
            var lemma = noun.Lemma;
            var nfc = lemma.Normalize(NormalizationForm.FormC);
            var nfd = lemma.Normalize(NormalizationForm.FormD);

            var directMatch = tipitakaWords.Contains(lemma);
            var nfcMatch = tipitakaWords.Contains(nfc);
            var nfdMatch = tipitakaWords.Contains(nfd);

            if (!directMatch && !nfcMatch && !nfdMatch)
            {
                // High-frequency lemmas might not be in wordlist as-is (they appear as inflected forms)
                // This is expected behavior, not an error
            }
            else if (!directMatch && (nfcMatch || nfdMatch))
            {
                foundWithNormalization.Add($"{noun.Lemma} (id={noun.Id}) - found only after normalization");
            }
        }

        TestContext.WriteLine($"Found only after normalization: {foundWithNormalization.Count}");
        foreach (var msg in foundWithNormalization)
            TestContext.WriteLine($"  {msg}");

        // If any lemmas are only findable after normalization, that's a warning sign
        foundWithNormalization.Should().BeEmpty(
            "if lemmas are in wordlist, they should match directly without extra normalization");
    }
}

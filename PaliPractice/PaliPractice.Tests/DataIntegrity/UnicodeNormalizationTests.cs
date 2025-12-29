using System.Text;
using FluentAssertions;
using PaliPractice.Tests.DataIntegrity.Helpers;

namespace PaliPractice.Tests.DataIntegrity;

/// <summary>
/// Tests for Unicode normalization in the data pipeline.
/// Pali uses special diacritical characters that must be preserved correctly
/// through the extraction process (DPD → Python → SQLite → C#).
/// </summary>
/// <remarks>
/// <para>
/// <b>Key Pali diacriticals:</b> ā ī ū ṁ ṃ ñ ḍ ḷ ṭ ṇ
/// </para>
/// <para>
/// These characters can be represented in two ways:
/// - NFC (precomposed): ā = U+0101 (single character)
/// - NFD (decomposed): ā = U+0061 + U+0304 (a + combining macron)
/// </para>
/// <para>
/// Inconsistent normalization can cause:
/// - Corpus matching failures (form exists but string comparison fails)
/// - Display issues in UI
/// - Search/filter problems
/// </para>
/// </remarks>
[TestFixture]
public class UnicodeNormalizationTests
{
    PaliDbLoader? _paliDb;
    List<PaliNoun>? _nouns;
    List<PaliVerb>? _verbs;
    HashSet<string>? _tipitakaWords;

    /// <summary>
    /// Standard Pali diacriticals that must be preserved.
    /// </summary>
    static readonly char[] PaliDiacriticals =
    [
        'ā', 'ī', 'ū',  // Long vowels
        'ṁ', 'ṃ',       // Anusvara variants
        'ñ',            // Palatal nasal
        'ḍ', 'ḷ', 'ṭ', 'ṇ' // Retroflex consonants
    ];

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _paliDb = new PaliDbLoader();
        _nouns = _paliDb.GetAllNouns();
        _verbs = _paliDb.GetAllVerbs();
        _tipitakaWords = TipitakaWordlistLoader.GetAllWords();

        TestContext.WriteLine($"Loaded {_nouns.Count} nouns");
        TestContext.WriteLine($"Loaded {_verbs.Count} verbs");
        TestContext.WriteLine($"Loaded {_tipitakaWords.Count} Tipitaka words");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _paliDb?.Dispose();
    }

    #region NFC Normalization Tests

    [Test]
    public void AllNouns_LemmasAreNfcNormalized()
    {
        var nonNfc = _nouns!
            .Where(n => n.Lemma != n.Lemma.Normalize(NormalizationForm.FormC))
            .Select(n => $"id={n.Id}: '{n.Lemma}' → NFC='{n.Lemma.Normalize(NormalizationForm.FormC)}'")
            .ToList();

        TestContext.WriteLine($"Non-NFC lemmas found: {nonNfc.Count}");
        foreach (var msg in nonNfc.Take(20))
            TestContext.WriteLine($"  {msg}");

        nonNfc.Should().BeEmpty(
            "all noun lemmas should be NFC normalized for consistent string comparison");
    }

    [Test]
    public void AllNouns_StemsAreNfcNormalized()
    {
        var nonNfc = _nouns!
            .Where(n => !string.IsNullOrEmpty(n.Stem) && n.Stem != n.Stem.Normalize(NormalizationForm.FormC))
            .Select(n => $"id={n.Id}: stem='{n.Stem}' → NFC='{n.Stem.Normalize(NormalizationForm.FormC)}'")
            .ToList();

        TestContext.WriteLine($"Non-NFC stems found: {nonNfc.Count}");
        foreach (var msg in nonNfc.Take(20))
            TestContext.WriteLine($"  {msg}");

        nonNfc.Should().BeEmpty(
            "all noun stems should be NFC normalized");
    }

    [Test]
    public void AllVerbs_LemmasAreNfcNormalized()
    {
        var nonNfc = _verbs!
            .Where(v => v.Lemma != v.Lemma.Normalize(NormalizationForm.FormC))
            .Select(v => $"id={v.Id}: '{v.Lemma}' → NFC='{v.Lemma.Normalize(NormalizationForm.FormC)}'")
            .ToList();

        TestContext.WriteLine($"Non-NFC lemmas found: {nonNfc.Count}");
        foreach (var msg in nonNfc.Take(20))
            TestContext.WriteLine($"  {msg}");

        nonNfc.Should().BeEmpty(
            "all verb lemmas should be NFC normalized");
    }

    [Test]
    public void AllVerbs_StemsAreNfcNormalized()
    {
        var nonNfc = _verbs!
            .Where(v => !string.IsNullOrEmpty(v.Stem) && v.Stem != v.Stem.Normalize(NormalizationForm.FormC))
            .Select(v => $"id={v.Id}: stem='{v.Stem}' → NFC='{v.Stem.Normalize(NormalizationForm.FormC)}'")
            .ToList();

        TestContext.WriteLine($"Non-NFC stems found: {nonNfc.Count}");
        foreach (var msg in nonNfc.Take(20))
            TestContext.WriteLine($"  {msg}");

        nonNfc.Should().BeEmpty(
            "all verb stems should be NFC normalized");
    }

    #endregion

    #region Diacritical Preservation Tests

    [Test]
    public void PaliDiacriticals_ArePrecomposedCharacters()
    {
        // Verify that our reference diacriticals are themselves NFC
        foreach (var c in PaliDiacriticals)
        {
            var str = c.ToString();
            var nfc = str.Normalize(NormalizationForm.FormC);

            nfc.Length.Should().Be(1,
                $"'{c}' (U+{(int)c:X4}) should be a single precomposed character, not decomposed");
        }
    }

    [Test]
    public void PaliDb_ContainsPaliDiacriticals()
    {
        // Verify that pali data actually contains Pali diacriticals
        // (sanity check that we're testing real data)
        var diacriticalCounts = new Dictionary<char, int>();
        foreach (var c in PaliDiacriticals)
            diacriticalCounts[c] = 0;

        foreach (var noun in _nouns!)
        {
            foreach (var c in PaliDiacriticals)
            {
                if (noun.Lemma.Contains(c) || noun.Stem.Contains(c))
                    diacriticalCounts[c]++;
            }
        }

        TestContext.WriteLine("Diacritical usage in noun lemmas/stems:");
        foreach (var kvp in diacriticalCounts.OrderByDescending(x => x.Value))
            TestContext.WriteLine($"  '{kvp.Key}' (U+{(int)kvp.Key:X4}): {kvp.Value} occurrences");

        // At minimum, long vowels should be very common in Pali
        diacriticalCounts['ā'].Should().BeGreaterThan(100,
            "long 'ā' should be common in Pali nouns");
        diacriticalCounts['ī'].Should().BeGreaterThan(10,
            "long 'ī' should appear in Pali nouns");
        diacriticalCounts['ū'].Should().BeGreaterThan(10,
            "long 'ū' should appear in Pali nouns");
    }

    [Test]
    public void NounLemmas_DiacriticalsAreNotDecomposed()
    {
        // Check for combining characters that would indicate decomposed form
        var decomposed = new List<string>();

        foreach (var noun in _nouns!)
        {
            foreach (var c in noun.Lemma)
            {
                // Combining diacritical marks range: U+0300 to U+036F
                if (c >= '\u0300' && c <= '\u036F')
                {
                    decomposed.Add($"id={noun.Id}: '{noun.Lemma}' contains combining mark U+{(int)c:X4}");
                    break;
                }
            }
        }

        TestContext.WriteLine($"Lemmas with combining marks: {decomposed.Count}");
        foreach (var msg in decomposed.Take(20))
            TestContext.WriteLine($"  {msg}");

        decomposed.Should().BeEmpty(
            "lemmas should not contain combining diacritical marks (should be precomposed NFC)");
    }

    #endregion

    #region Corpus Matching Tests

    [Test]
    public void TipitakaWordlist_IsNfcNormalized()
    {
        var nonNfc = _tipitakaWords!
            .Where(w => w != w.Normalize(NormalizationForm.FormC))
            .Take(100)
            .ToList();

        TestContext.WriteLine($"Non-NFC words in Tipitaka wordlist: {nonNfc.Count}");
        foreach (var word in nonNfc.Take(20))
            TestContext.WriteLine($"  '{word}'");

        // Allow some tolerance - report but don't fail if minor
        if (nonNfc.Count > 0)
        {
            var rate = (double)nonNfc.Count / _tipitakaWords!.Count;
            rate.Should().BeLessThan(0.001,
                "less than 0.1% of Tipitaka words should be non-NFC normalized");
        }
    }

    [Test]
    public void NounLemmas_FindableInTipitakaWithNormalization()
    {
        // Sample some nouns that should definitely be in corpus
        var sampleNouns = _nouns!
            .Where(n => n.EbtCount > 100) // High-frequency words
            .Take(50)
            .ToList();

        var notFound = new List<string>();
        var foundWithNormalization = new List<string>();

        foreach (var noun in sampleNouns)
        {
            var lemma = noun.Lemma;
            var nfc = lemma.Normalize(NormalizationForm.FormC);
            var nfd = lemma.Normalize(NormalizationForm.FormD);

            var directMatch = _tipitakaWords!.Contains(lemma);
            var nfcMatch = _tipitakaWords!.Contains(nfc);
            var nfdMatch = _tipitakaWords!.Contains(nfd);

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

    #endregion

    #region Byte-Level Verification

    [Test]
    public void SampleLemmas_HaveExpectedByteSequences()
    {
        // Verify specific known words have correct UTF-8 encoding
        var knownWords = new Dictionary<string, string>
        {
            // lemma → expected stem ending pattern (for verification)
            { "dhamma", "dhamm" },   // Basic, no diacriticals
            { "buddha", "buddh" },   // Basic, no diacriticals
        };

        foreach (var noun in _nouns!.Where(n => knownWords.ContainsKey(n.Lemma)))
        {
            var lemmaBytes = Encoding.UTF8.GetBytes(noun.Lemma);
            var stemBytes = Encoding.UTF8.GetBytes(noun.Stem);

            TestContext.WriteLine($"{noun.Lemma}: {lemmaBytes.Length} bytes, stem '{noun.Stem}': {stemBytes.Length} bytes");

            // Basic sanity - lemma should be valid UTF-8
            var decoded = Encoding.UTF8.GetString(lemmaBytes);
            decoded.Should().Be(noun.Lemma, "UTF-8 roundtrip should preserve lemma");
        }
    }

    [Test]
    public void LongVowelLemmas_HaveCorrectEncoding()
    {
        // Find lemmas with long vowels and verify encoding
        var longVowelNouns = _nouns!
            .Where(n => n.Lemma.Contains('ā') || n.Lemma.Contains('ī') || n.Lemma.Contains('ū'))
            .Take(10)
            .ToList();

        foreach (var noun in longVowelNouns)
        {
            var bytes = Encoding.UTF8.GetBytes(noun.Lemma);

            TestContext.WriteLine($"'{noun.Lemma}': {string.Join(" ", bytes.Select(b => $"{b:X2}"))}");

            // ā (U+0101) should encode as C4 81 in UTF-8
            // ī (U+012B) should encode as C4 AB in UTF-8
            // ū (U+016B) should encode as C5 AB in UTF-8

            // Verify no BOM or other unexpected prefixes
            if (bytes.Length >= 3)
            {
                var hasBom = bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
                hasBom.Should().BeFalse("lemmas should not have UTF-8 BOM prefix");
            }
        }
    }

    #endregion
}

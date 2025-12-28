using System.IO;
using System.Text.Json;

namespace PaliPractice.Tests.DataIntegrity.Helpers;

/// <summary>
/// Loads Tipitaka wordlists from dpd-db for corpus attestation validation.
/// These are the same JSON files used by the Python extraction script.
/// </summary>
public static class TipitakaWordlistLoader
{
    static readonly object Lock = new();
    static HashSet<string>? _allWords;

    /// <summary>
    /// Base path to the dpd-db frequency data directory.
    /// </summary>
    const string FrequencyPath = "/Users/ivm/Sources/PaliPractice/dpd-db/shared_data/frequency";

    /// <summary>
    /// The 4 Tipitaka wordlist files used for corpus attestation.
    /// </summary>
    static readonly string[] WordlistFiles =
    [
        "cst_wordlist.json",  // CST edition
        "bjt_wordlist.json",  // Burmese edition
        "sya_wordlist.json",  // Sinhalese edition
        "sc_wordlist.json"    // Sutta Central
    ];

    /// <summary>
    /// Get all Tipitaka words as a HashSet for O(1) lookup.
    /// Loads lazily on first access and caches for subsequent calls.
    /// </summary>
    public static HashSet<string> GetAllWords()
    {
        if (_allWords != null)
            return _allWords;

        lock (Lock)
        {
            if (_allWords != null)
                return _allWords;

            var words = new HashSet<string>();

            foreach (var file in WordlistFiles)
            {
                var path = System.IO.Path.Combine(FrequencyPath, file);
                if (!System.IO.File.Exists(path))
                {
                    throw new FileNotFoundException($"Tipitaka wordlist not found: {path}");
                }

                var json = System.IO.File.ReadAllText(path);
                var wordlist = JsonSerializer.Deserialize<string[]>(json);
                if (wordlist != null)
                {
                    foreach (var word in wordlist)
                    {
                        words.Add(word);
                    }
                }
            }

            _allWords = words;
            return _allWords;
        }
    }

    /// <summary>
    /// Check if an inflected form exists in the Tipitaka corpus.
    /// </summary>
    public static bool IsWordInCorpus(string inflectedForm)
    {
        return GetAllWords().Contains(inflectedForm);
    }

    /// <summary>
    /// Get the count of unique words across all Tipitaka editions.
    /// </summary>
    public static int WordCount => GetAllWords().Count;

    /// <summary>
    /// Clear the cached wordlist (useful for testing).
    /// </summary>
    public static void ClearCache()
    {
        lock (Lock)
        {
            _allWords = null;
        }
    }
}

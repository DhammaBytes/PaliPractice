using System.Text.Json;

namespace PaliPractice.Tests.DataIntegrity.Helpers;

/// <summary>
/// Loads Tipitaka wordlists from dpd-db for corpus attestation validation.
/// These are the same JSON files used by the Python extraction script.
/// </summary>
/// <remarks>
/// If DPD adds new wordlist editions or renames files, this loader will detect
/// the change and throw an exception to alert developers.
/// </remarks>
public static class TipitakaWordlistLoader
{
    static readonly object Lock = new();
    static HashSet<string>? _allWords;

    /// <summary>
    /// Base path to the dpd-db frequency data directory.
    /// </summary>
    const string FrequencyPath = "/Users/ivm/Sources/PaliPractice/dpd-db/shared_data/frequency";

    /// <summary>
    /// The expected Tipitaka wordlist files for corpus attestation.
    /// If DPD adds new editions, update this list and the extraction script.
    /// </summary>
    static readonly string[] ExpectedWordlistFiles =
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
    /// <exception cref="FileNotFoundException">If expected wordlist files are missing</exception>
    /// <exception cref="InvalidOperationException">If new wordlist files are detected</exception>
    public static HashSet<string> GetAllWords()
    {
        if (_allWords != null)
            return _allWords;

        lock (Lock)
        {
            if (_allWords != null)
                return _allWords;

            // Check for unexpected new wordlist files (DPD may add new editions)
            ValidateWordlistFiles();

            var words = new HashSet<string>();

            foreach (var file in ExpectedWordlistFiles)
            {
                var path = System.IO.Path.Combine(FrequencyPath, file);
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException(
                        $"Tipitaka wordlist not found: {path}. " +
                        "If DPD renamed this file, update ExpectedWordlistFiles.");
                }

                var json = File.ReadAllText(path);
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
    /// Validates that no new wordlist files have been added to the frequency directory.
    /// If DPD adds new editions, we need to update both this list and the extraction script.
    /// </summary>
    static void ValidateWordlistFiles()
    {
        if (!Directory.Exists(FrequencyPath))
            return; // Directory check will fail later with better error

        var actualWordlists = Directory.GetFiles(FrequencyPath, "*_wordlist.json")
            .Select(System.IO.Path.GetFileName)
            .ToHashSet();

        var expectedSet = ExpectedWordlistFiles.ToHashSet();

        // Check for new files we're not loading
        var newFiles = actualWordlists.Except(expectedSet).ToList();
        if (newFiles.Count > 0)
        {
            throw new InvalidOperationException(
                $"New Tipitaka wordlist files detected: {string.Join(", ", newFiles)}. " +
                "Update ExpectedWordlistFiles and the extraction script to include these editions.");
        }

        // Note: Missing files will be caught when we try to load them
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

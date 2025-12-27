namespace PaliPractice.Models.Words;

/// <summary>
/// Implementation of ILemma that groups words by lemma.
/// Filters out words with minority inflection patterns into ExcludedWords.
/// Manages lazy-loading of details.
/// </summary>
public class Lemma : ILemma
{
    readonly List<IWord> _words;
    readonly List<IWord> _excluded;

    public int LemmaId { get; }
    public string BaseForm { get; }
    public IReadOnlyList<IWord> Words => _words;
    public IReadOnlyList<IWord> ExcludedWords => _excluded;
    public IWord Primary => _words[0];
    public int EbtCount => Primary.EbtCount;
    public bool HasDetails { get; private set; }

    public Lemma(string baseForm, IEnumerable<IWord> words)
    {
        BaseForm = baseForm;

        // All words with the same lemma share the same LemmaId
        LemmaId = words.First().LemmaId;

        var allWords = words.ToList();

        // Group by pattern and find the dominant one
        var byPattern = allWords
            .GroupBy(w => w.Pattern ?? "")
            .Select(g => new
            {
                Pattern = g.Key,
                Words = g.ToList(),
                Count = g.Count(),
                MinId = g.Min(w => w.Id)
            })
            .OrderByDescending(g => g.Count)
            .ThenBy(g => g.MinId)  // Tie-breaker: lowest ID wins
            .ToList();

        var dominantPattern = byPattern.First().Pattern;

        // Separate words into the main list and excluded
        // Order by EbtCount descending so Primary gets highest frequency variant
        _words = allWords
            .Where(w => (w.Pattern ?? "") == dominantPattern)
            .OrderByDescending(w => w.EbtCount)
            .ThenBy(w => w.Id)
            .ToList();

        _excluded = allWords
            .Where(w => (w.Pattern ?? "") != dominantPattern)
            .OrderBy(w => w.Id)
            .ToList();
    }

    public void LoadDetails(IReadOnlyList<IWordDetails> details)
    {
        if (HasDetails) return;

        // Match details to words by DPD id (1:1 relationship)
        var detailsById = details.ToDictionary(d => d.Id);

        foreach (var word in _words)
        {
            if (detailsById.TryGetValue(word.Id, out var wordDetails))
                word.Details = wordDetails;
        }

        // Also load details for excluded words (for completeness)
        foreach (var word in _excluded)
        {
            if (detailsById.TryGetValue(word.Id, out var wordDetails))
                word.Details = wordDetails;
        }

        HasDetails = true;
    }
}

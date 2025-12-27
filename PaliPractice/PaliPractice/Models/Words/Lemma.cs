namespace PaliPractice.Models.Words;

/// <summary>
/// Implementation of ILemma that groups words by lemma.
/// Filters out words with minority inflection patterns into Variants.
/// </summary>
public class Lemma : ILemma
{
    readonly List<IWord> _words;
    readonly List<IWord> _variants;

    public int LemmaId { get; }
    public string BaseForm { get; }
    public IReadOnlyList<IWord> Words => _words;
    public IReadOnlyList<IWord> ExcludedWords => _variants;
    public int EbtCount => _words.First().EbtCount;

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

        // Separate words into the main list and variants
        // Order by EbtCount descending so .First() gets highest frequency variant
        _words = allWords
            .Where(w => (w.Pattern ?? "") == dominantPattern)
            .OrderByDescending(w => w.EbtCount)
            .ThenBy(w => w.Id)
            .ToList();

        _variants = allWords
            .Where(w => (w.Pattern ?? "") != dominantPattern)
            .OrderBy(w => w.Id)
            .ToList();
    }
}

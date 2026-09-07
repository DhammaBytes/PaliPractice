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
    public TranslationLanguagePreference? MeaningsLanguage { get; set; }

    public Lemma(string baseForm, IEnumerable<IWord> words)
    {
        BaseForm = baseForm;

        var allWords = words.ToList();
        LemmaId = allWords.First().LemmaId;
        var selected = allWords.Where(w => w.PracticePrimary).ToList();
        if (selected.Count > 1)
            throw new InvalidDataException($"Multiple practice senses for lemma {LemmaId}");

        // Existing shipped bundles do not contain practice_primary. Preserve
        // their released selection behavior until a validated bundle replaces them.
        var primary = selected.SingleOrDefault();
        var legacyPattern = primary is null ? LegacyPattern(allWords) : null;
        bool Included(IWord word) => primary is null
            ? word.RawPattern == legacyPattern
            : SameParadigm(word, primary);

        _words = allWords.Where(Included)
            .OrderByDescending(w => w.PracticePrimary)
            .ThenByDescending(w => w.EbtCount)
            .ThenBy(w => w.Id)
            .ToList();
        _excluded = allWords.Where(w => !Included(w)).OrderBy(w => w.Id).ToList();
    }

    static bool SameParadigm(IWord word, IWord primary) =>
        word.RawPattern == primary.RawPattern && word.Stem == primary.Stem &&
        (word is not Noun noun || primary is not Noun selected || noun.Gender == selected.Gender);

    static string LegacyPattern(List<IWord> words) => words
        .GroupBy(w => w.RawPattern)
        .OrderByDescending(g => g.Count())
        .ThenBy(g => g.Min(w => w.Id))
        .First().Key;

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

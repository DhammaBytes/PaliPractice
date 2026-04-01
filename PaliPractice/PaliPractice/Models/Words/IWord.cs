namespace PaliPractice.Models.Words;

/// <summary>
/// Slim interface for word entities. Contains only core properties needed
/// for queue building and inflection generation.
/// </summary>
public interface IWord
{
    int Id { get; }
    int EbtCount { get; }
    int LemmaId { get; }

    /// <summary>
    /// The lemma (dictionary form) of this word, e.g., "dhamma".
    /// </summary>
    string Lemma { get; }

    string? Stem { get; }
    string RawPattern { get; }

    /// <summary>
    /// Whether this word uses an irregular inflection pattern.
    /// </summary>
    bool Irregular { get; }

    /// <summary>
    /// Display details for this word. Lazy-loaded when showing flashcards.
    /// Null until fetched from the database.
    /// </summary>
    IWordDetails? Details { get; set; }
}

/// <summary>
/// Display details for a word. Lazy-loaded when showing flashcards.
/// </summary>
public interface IWordDetails
{
    /// <summary>
    /// DPD headword ID. Matches IWord.Id for 1:1 relationship.
    /// </summary>
    int Id { get; }

    int LemmaId { get; }

    /// <summary>
    /// The variant identifier within the lemma group, e.g., "1.1" or empty.
    /// </summary>
    string Variant { get; }

    /// <summary>
    /// The root family this word belongs to, e.g., "√kar" or empty.
    /// </summary>
    string Root { get; }

    /// <summary>
    /// English meaning/translation. Never null - extraction filters for words with meanings.
    /// </summary>
    string MeaningEn { get; }

    /// <summary>
    /// Russian meaning/translation when available, otherwise empty.
    /// </summary>
    string MeaningRu { get; }

    /// <summary>
    /// Gets the best available meaning for a language code.
    /// Falls back to English for unknown languages or missing translations.
    /// </summary>
    string GetMeaning(string languageCode)
    {
        if (!string.IsNullOrWhiteSpace(languageCode) &&
            languageCode.StartsWith("ru", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(MeaningRu))
            return MeaningRu;

        return MeaningEn;
    }

    string Source1 { get; }
    string Sutta1 { get; }
    string Example1 { get; }
    string Source2 { get; }
    string Sutta2 { get; }
    string Example2 { get; }
}

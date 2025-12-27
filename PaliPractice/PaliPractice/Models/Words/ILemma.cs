namespace PaliPractice.Models.Words;

/// <summary>
/// Represents a practice unit grouping all IWord instances that share the same lemma.
/// The practice session iterates by lemma, not individual word.
/// </summary>
public interface ILemma
{
    /// <summary>
    /// Stable ID for this lemma group (10001-69999 for nouns, 70001-99999 for verbs).
    /// </summary>
    int LemmaId { get; }

    /// <summary>
    /// The dictionary form (lemma) used as practice unit identifier, e.g., "dhamma".
    /// </summary>
    string BaseForm { get; }

    /// <summary>
    /// Words sharing this lemma with the dominant inflection pattern.
    /// Multiple entries may exist for different meanings.
    /// </summary>
    IReadOnlyList<IWord> Words { get; }

    /// <summary>
    /// Words with minority inflection patterns, excluded from the main practice.
    /// These have the same lemma but different Pattern values.
    /// </summary>
    IReadOnlyList<IWord> ExcludedWords { get; }

    /// <summary>
    /// EbtCount from the first word, used for ranking/ordering.
    /// All words in a lemma share the same frequency count.
    /// </summary>
    int EbtCount { get; }
}

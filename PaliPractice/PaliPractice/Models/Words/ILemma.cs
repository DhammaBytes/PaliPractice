namespace PaliPractice.Models.Words;

/// <summary>
/// Represents a practice unit grouping all IWord instances that share the same lemma_clean.
/// The practice session iterates by lemma, not individual word.
/// </summary>
public interface ILemma
{
    /// <summary>
    /// The clean lemma form used as practice unit identifier.
    /// </summary>
    string LemmaClean { get; }

    /// <summary>
    /// Words sharing this lemma_clean with the dominant inflection pattern.
    /// Multiple entries may exist for different meanings.
    /// </summary>
    IReadOnlyList<IWord> Words { get; }

    /// <summary>
    /// Words with minority inflection patterns, excluded from the main practice.
    /// These have the same lemma_clean but different Pattern values.
    /// </summary>
    IReadOnlyList<IWord> Variants { get; }

    /// <summary>
    /// EbtCount from the first word, used for ranking/ordering.
    /// All words in a lemma share the same frequency count.
    /// </summary>
    int EbtCount { get; }
}

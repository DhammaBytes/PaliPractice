namespace PaliPractice.Models;

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
    /// All words (noun/verb entries) sharing this lemma_clean.
    /// Multiple entries may exist for different meanings or variants.
    /// </summary>
    IReadOnlyList<IWord> Words { get; }

    /// <summary>
    /// The primary word (highest EbtCount) used for ranking display.
    /// </summary>
    IWord PrimaryWord { get; }

    /// <summary>
    /// EbtCount of the primary word, used for ranking/ordering.
    /// </summary>
    int EbtCount { get; }
}

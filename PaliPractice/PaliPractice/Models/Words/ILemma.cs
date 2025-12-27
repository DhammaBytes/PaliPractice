namespace PaliPractice.Models.Words;

/// <summary>
/// Represents a lemma (dictionary entry) grouping all IWord variants.
/// This is the root abstraction for practice, dictionary views, and history.
/// Details can be lazy-loaded via LoadDetails().
/// </summary>
public interface ILemma
{
    /// <summary>
    /// Stable ID for this lemma group (10001-69999 for nouns, 70001-99999 for verbs).
    /// Used for form_id encoding and SRS tracking.
    /// </summary>
    int LemmaId { get; }

    /// <summary>
    /// EbtCount from the primary word, used for ranking/ordering.
    /// </summary>
    int EbtCount { get; }

    /// <summary>
    /// The dictionary form (lemma), e.g., "dhamma", "hoti".
    /// </summary>
    string BaseForm { get; }

    /// <summary>
    /// Words sharing this lemma with the dominant inflection pattern.
    /// Multiple entries may exist for different meanings.
    /// </summary>
    IReadOnlyList<IWord> Words { get; }

    /// <summary>
    /// The primary word variant (first by EbtCount).
    /// Use this for inflection generation.
    /// </summary>
    IWord Primary { get; }

    /// <summary>
    /// Words with minority inflection patterns, excluded from the main practice.
    /// These have the same lemma but different Pattern values.
    /// </summary>
    IReadOnlyList<IWord> ExcludedWords { get; }

    /// <summary>
    /// Whether details have been loaded for this lemma's words.
    /// </summary>
    bool HasDetails { get; }

    /// <summary>
    /// Load details into each word's Details property.
    /// Called by database service after fetching details from DB.
    /// </summary>
    /// <param name="details">All details for this lemma (matched by DPD id).</param>
    void LoadDetails(IReadOnlyList<IWordDetails> details);
}

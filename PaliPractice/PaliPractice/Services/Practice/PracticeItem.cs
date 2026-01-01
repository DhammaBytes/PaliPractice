using PaliPractice.Services.UserData;

namespace PaliPractice.Services.Practice;

/// <summary>
/// Source bucket for a practice item.
/// </summary>
public enum PracticeItemSource
{
    /// <summary>
    /// Form that was on cooldown and is now due for review.
    /// </summary>
    DueForReview,

    /// <summary>
    /// Form that has never been practiced before.
    /// </summary>
    NewForm,

    /// <summary>
    /// Review form from a difficult grammatical combination (meta-bucket boost).
    /// </summary>
    DifficultCombo
}

/// <summary>
/// Represents a form to practice with its source bucket and priority.
/// </summary>
/// <param name="FormId">The unique form ID (EndingId=0 for combinations).</param>
/// <param name="Type">Declension or Conjugation.</param>
/// <param name="LemmaId">The lemma ID for looking up the word.</param>
/// <param name="Source">Where this item came from (review, new, etc.).</param>
/// <param name="Priority">Higher = more urgent (0.0 to 1.0 range).</param>
/// <param name="MasteryLevel">Current mastery level (0=unpracticed, 1-10=practicing, 11=retired).</param>
public record PracticeItem(
    long FormId,
    PracticeType Type,
    int LemmaId,
    PracticeItemSource Source,
    double Priority,
    int MasteryLevel = CooldownCalculator.UnpracticedLevel
)
{
    /// <summary>
    /// Creates a practice item for a form that has never been practiced.
    /// </summary>
    public static PracticeItem NewForm(long formId, PracticeType type, int lemmaId) =>
        new(formId, type, lemmaId, PracticeItemSource.NewForm, Priority: 0.5);
}

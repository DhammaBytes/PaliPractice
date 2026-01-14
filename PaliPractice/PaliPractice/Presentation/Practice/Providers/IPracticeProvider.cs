using PaliPractice.Models.Words;
using PaliPractice.Services.Practice;

namespace PaliPractice.Presentation.Practice.Providers;

/// <summary>
/// Provides practice items using the spaced repetition queue.
/// </summary>
public interface IPracticeProvider
{
    /// <summary>
    /// Current practice item (form to practice).
    /// </summary>
    PracticeItem? Current { get; }

    /// <summary>
    /// Current index in the queue (0-based).
    /// </summary>
    int CurrentIndex { get; }

    /// <summary>
    /// Total items in the queue.
    /// </summary>
    int TotalCount { get; }

    /// <summary>
    /// Whether there are more items in the queue.
    /// </summary>
    bool HasNext { get; }

    /// <summary>
    /// Load the practice queue for a session.
    /// </summary>
    Task LoadAsync(CancellationToken ct = default);

    /// <summary>
    /// Move to the next item in the queue.
    /// Silently rebuilds the queue if exhausted but more forms are available.
    /// Returns false only when the pool is truly exhausted (no due or new forms).
    /// </summary>
    bool MoveNext();

    /// <summary>
    /// Get the current lemma with details loaded.
    /// </summary>
    ILemma? GetCurrentLemma();

    /// <summary>
    /// Get the grammatical parameters for the current form.
    /// For declension: returns (Case, Gender, Number).
    /// For conjugation: returns (Tense, Person, Number, Reflexive).
    /// </summary>
    object GetCurrentParameters();
}

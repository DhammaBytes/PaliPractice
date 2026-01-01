namespace PaliPractice.Services.Practice;

/// <summary>
/// Builds practice queues using spaced repetition logic.
/// Mixes review forms and new forms based on settings and mastery state.
/// </summary>
public interface IPracticeQueueBuilder
{
    /// <summary>
    /// Build a practice queue for a session.
    /// Mixes review forms and new forms according to the 4-6:1 ratio.
    /// </summary>
    /// <param name="type">Declension or Conjugation</param>
    /// <param name="count">Number of items to queue (typically daily goal)</param>
    /// <param name="seedDate">Optional date for deterministic seeding (default: today UTC)</param>
    /// <returns>List of practice items ordered for the session.</returns>
    List<PracticeItem> BuildQueue(PracticeType type, int count, DateTime? seedDate = null);

    /// <summary>
    /// Get all eligible form IDs (corpus-attested, matching current settings).
    /// Useful for determining untried forms.
    /// </summary>
    List<long> GetEligibleFormIds(PracticeType type);
}

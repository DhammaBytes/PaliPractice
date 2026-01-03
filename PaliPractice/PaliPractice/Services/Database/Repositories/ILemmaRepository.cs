using PaliPractice.Models.Words;

namespace PaliPractice.Services.Database.Repositories;

/// <summary>
/// Base interface for lemma repository operations.
/// Shared by noun and verb repositories.
/// </summary>
public interface ILemmaRepository
{
    /// <summary>
    /// Get total count of lemmas.
    /// </summary>
    int GetCount();

    /// <summary>
    /// Get lemma by lemma ID.
    /// </summary>
    ILemma? GetLemma(int lemmaId);

    /// <summary>
    /// Get lemmas within a rank range, ordered by EbtCount descending.
    /// Rank 1 = most common.
    /// </summary>
    List<ILemma> GetLemmasByRank(int minRank, int maxRank);

    /// <summary>
    /// Ensure details are loaded for the lemma.
    /// </summary>
    void EnsureDetails(ILemma lemma);

    /// <summary>
    /// Preload caches to avoid lazy loading delay on first access.
    /// </summary>
    void Preload();
}

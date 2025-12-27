using PaliPractice.Models.Words;

namespace PaliPractice.Presentation.Practice.Providers;

public interface ILemmaProvider
{
    /// <summary>
    /// Lemmas grouped by lemma, ordered by EbtCount.
    /// </summary>
    IReadOnlyList<ILemma> Lemmas { get; }

    Task LoadAsync(CancellationToken ct = default);
}

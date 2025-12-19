namespace PaliPractice.Presentation.Practice.Providers;

public interface IWordProvider
{
    /// <summary>
    /// Lemmas grouped by lemma_clean, ordered by EbtCount.
    /// </summary>
    IReadOnlyList<ILemma> Lemmas { get; }

    Task LoadAsync(CancellationToken ct = default);
}

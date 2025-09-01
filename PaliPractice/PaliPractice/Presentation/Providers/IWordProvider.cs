namespace PaliPractice.Presentation.Providers;

public interface IWordProvider
{
    IReadOnlyList<Headword> Words { get; }
    Task LoadAsync(CancellationToken ct = default);
}
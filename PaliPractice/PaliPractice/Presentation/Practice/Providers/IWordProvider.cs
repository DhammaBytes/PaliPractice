namespace PaliPractice.Presentation.Practice.Providers;

public interface IWordProvider
{
    IReadOnlyList<IWord> Words { get; }
    Task LoadAsync(CancellationToken ct = default);
}
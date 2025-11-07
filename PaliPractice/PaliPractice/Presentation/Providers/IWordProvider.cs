using PaliPractice.Models;

namespace PaliPractice.Presentation.Providers;

public interface IWordProvider
{
    IReadOnlyList<IWord> Words { get; }
    Task LoadAsync(CancellationToken ct = default);
}
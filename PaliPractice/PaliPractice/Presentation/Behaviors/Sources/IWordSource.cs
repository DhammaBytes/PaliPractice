namespace PaliPractice.Presentation.Behaviors.Sources;

public interface IWordSource
{
    List<Headword> Words { get; }
    int CurrentIndex { get; set; }
    Task LoadAsync(CancellationToken ct = default);
}
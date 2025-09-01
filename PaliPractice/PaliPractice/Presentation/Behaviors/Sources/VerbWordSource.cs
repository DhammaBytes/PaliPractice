namespace PaliPractice.Presentation.Behaviors.Sources;

public sealed class VerbWordSource : IWordSource
{
    readonly IDatabaseService _db;
    public List<Headword> Words { get; } = new();
    public int CurrentIndex { get; set; }

    public VerbWordSource(IDatabaseService db) => _db = db;

    public async Task LoadAsync(CancellationToken ct = default)
    {
        await _db.InitializeAsync();
        Words.Clear();
        Words.AddRange(await _db.GetRandomVerbsAsync(100));
    }
}
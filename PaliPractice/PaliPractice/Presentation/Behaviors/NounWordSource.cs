namespace PaliPractice.Presentation.Behaviors;

public sealed class NounWordSource : IWordSource
{
    readonly IDatabaseService _db;
    public List<Headword> Words { get; } = [];
    public int CurrentIndex { get; set; }

    public NounWordSource(IDatabaseService db) => _db = db;

    public async Task LoadAsync(CancellationToken ct = default)
    {
        await _db.InitializeAsync();
        Words.Clear();
        Words.AddRange(await _db.GetRandomNounsAsync(100));
    }
}

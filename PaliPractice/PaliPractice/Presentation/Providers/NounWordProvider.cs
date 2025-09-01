namespace PaliPractice.Presentation.Providers;

public sealed class NounWordProvider : IWordProvider
{
    readonly IDatabaseService _db;
    readonly List<Headword> _words = [];
    public IReadOnlyList<Headword> Words => _words;

    public NounWordProvider(IDatabaseService db) => _db = db;

    public async Task LoadAsync(CancellationToken ct = default)
    {
        await _db.InitializeAsync();
        _words.Clear();
        _words.AddRange(await _db.GetRandomNounsAsync(100));
    }
}
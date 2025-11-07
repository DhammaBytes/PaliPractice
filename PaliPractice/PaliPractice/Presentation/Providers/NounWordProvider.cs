using PaliPractice.Models;

namespace PaliPractice.Presentation.Providers;

public sealed class NounWordProvider : IWordProvider
{
    readonly IDatabaseService _db;
    readonly List<Noun> _words = [];
    public IReadOnlyList<IWord> Words => _words;

    public NounWordProvider(IDatabaseService db) => _db = db;

    public async Task LoadAsync(CancellationToken ct = default)
    {
        await _db.InitializeAsync();
        _words.Clear();
        _words.AddRange(await _db.GetRandomNounsAsync(100));
    }
}
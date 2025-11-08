using PaliPractice.Models;

namespace PaliPractice.Presentation.Providers;

public sealed class NounWordProvider : IWordProvider
{
    readonly IDatabaseService _db;
    readonly List<Noun> _words = [];
    public IReadOnlyList<IWord> Words => _words;

    public NounWordProvider(IDatabaseService db) => _db = db;

    public Task LoadAsync(CancellationToken ct = default)
    {
        _db.Initialize();
        _words.Clear();
        _words.AddRange(_db.GetRandomNouns(100));
        return Task.CompletedTask;
    }
}
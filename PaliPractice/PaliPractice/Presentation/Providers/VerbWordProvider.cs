using PaliPractice.Models;

namespace PaliPractice.Presentation.Providers;

public sealed class VerbWordProvider : IWordProvider
{
    readonly IDatabaseService _db;
    readonly List<Verb> _words = [];
    public IReadOnlyList<IWord> Words => _words;

    public VerbWordProvider(IDatabaseService db) => _db = db;

    public async Task LoadAsync(CancellationToken ct = default)
    {
        await _db.InitializeAsync();
        _words.Clear();
        _words.AddRange(await _db.GetRandomVerbsAsync(100));
    }
}
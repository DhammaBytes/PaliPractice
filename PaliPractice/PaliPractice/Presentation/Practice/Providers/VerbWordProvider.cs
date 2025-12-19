namespace PaliPractice.Presentation.Practice.Providers;

public sealed class VerbWordProvider : IWordProvider
{
    readonly IDatabaseService _db;
    readonly List<Verb> _words = [];
    public IReadOnlyList<IWord> Words => _words;

    public VerbWordProvider(IDatabaseService db) => _db = db;

    public Task LoadAsync(CancellationToken ct = default)
    {
        _db.Initialize();
        _words.Clear();
        _words.AddRange(_db.GetRandomVerbs(100));
        return Task.CompletedTask;
    }
}
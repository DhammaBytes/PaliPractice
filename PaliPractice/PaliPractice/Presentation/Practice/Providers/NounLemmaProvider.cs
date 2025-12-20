namespace PaliPractice.Presentation.Practice.Providers;

public sealed class NounLemmaProvider : ILemmaProvider
{
    readonly IDatabaseService _db;
    readonly List<ILemma> _lemmas = [];
    public IReadOnlyList<ILemma> Lemmas => _lemmas;

    public NounLemmaProvider(IDatabaseService db) => _db = db;

    public Task LoadAsync(CancellationToken ct = default)
    {
        _db.Initialize();
        _lemmas.Clear();

        // Get nouns, group by lemma_clean, create Lemma instances
        var nouns = _db.GetRandomNouns(100);
        var grouped = nouns
            .GroupBy(n => n.LemmaClean)
            .Select(g => new Lemma(g.Key, g.Cast<IWord>()))
            .OrderByDescending(l => l.EbtCount)
            .ToList();

        _lemmas.AddRange(grouped);
        return Task.CompletedTask;
    }
}

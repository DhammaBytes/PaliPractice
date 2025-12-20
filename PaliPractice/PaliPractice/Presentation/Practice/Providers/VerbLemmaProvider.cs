namespace PaliPractice.Presentation.Practice.Providers;

public sealed class VerbLemmaProvider : ILemmaProvider
{
    readonly IDatabaseService _db;
    readonly List<ILemma> _lemmas = [];
    public IReadOnlyList<ILemma> Lemmas => _lemmas;

    public VerbLemmaProvider(IDatabaseService db) => _db = db;

    public Task LoadAsync(CancellationToken ct = default)
    {
        _db.Initialize();
        _lemmas.Clear();

        // Get verbs, group by lemma_clean, create Lemma instances
        var verbs = _db.GetRandomVerbs(100);
        var grouped = verbs
            .GroupBy(v => v.LemmaClean)
            .Select(g => new Lemma(g.Key, g.Cast<IWord>()))
            .OrderByDescending(l => l.EbtCount)
            .ToList();

        _lemmas.AddRange(grouped);
        return Task.CompletedTask;
    }
}

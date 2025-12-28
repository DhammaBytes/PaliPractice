using PaliPractice.Models.Words;
using PaliPractice.Services.Database;

namespace PaliPractice.Presentation.Practice.Providers;

public sealed class NounLemmaProvider : ILemmaProvider
{
    readonly IDatabaseService _db;
    readonly List<ILemma> _lemmas = [];
    public IReadOnlyList<ILemma> Lemmas => _lemmas;

    public NounLemmaProvider(IDatabaseService db) => _db = db;

    public Task LoadAsync(CancellationToken ct = default)
    {
        _lemmas.Clear();

        // Get top 100 noun lemmas by rank
        var lemmas = _db.Nouns.GetLemmasByRank(1, 100);
        _lemmas.AddRange(lemmas);
        return Task.CompletedTask;
    }
}

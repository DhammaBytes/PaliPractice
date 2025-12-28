using PaliPractice.Models.Words;
using PaliPractice.Services.Database;

namespace PaliPractice.Presentation.Practice.Providers;

public sealed class VerbLemmaProvider : ILemmaProvider
{
    readonly IDatabaseService _db;
    readonly List<ILemma> _lemmas = [];
    public IReadOnlyList<ILemma> Lemmas => _lemmas;

    public VerbLemmaProvider(IDatabaseService db) => _db = db;

    public Task LoadAsync(CancellationToken ct = default)
    {
        _lemmas.Clear();

        // Get top 100 verb lemmas by rank
        var lemmas = _db.Verbs.GetLemmasByRank(1, 100);
        _lemmas.AddRange(lemmas);
        return Task.CompletedTask;
    }
}

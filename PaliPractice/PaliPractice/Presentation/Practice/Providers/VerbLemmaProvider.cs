using PaliPractice.Models.Words;
using PaliPractice.Services.UserData;

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

        // Get verb lemmas by rank using user settings
        var minRank = _db.UserData.GetSetting(SettingsKeys.VerbsLemmaMin, SettingsKeys.DefaultLemmaMin);
        var maxRank = _db.UserData.GetSetting(SettingsKeys.VerbsLemmaMax, SettingsKeys.DefaultLemmaMax);
        var lemmas = _db.Verbs.GetLemmasByRank(minRank, maxRank);
        _lemmas.AddRange(lemmas);
        return Task.CompletedTask;
    }
}

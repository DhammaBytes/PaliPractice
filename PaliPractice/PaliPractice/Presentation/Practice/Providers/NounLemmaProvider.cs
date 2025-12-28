using PaliPractice.Models.Words;
using PaliPractice.Services.Database;
using PaliPractice.Services.UserData;

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

        // Get noun lemmas by rank using user settings
        var minRank = _db.UserData.GetSetting(SettingsKeys.NounsLemmaMin, SettingsKeys.DefaultLemmaMin);
        var maxRank = _db.UserData.GetSetting(SettingsKeys.NounsLemmaMax, SettingsKeys.DefaultLemmaMax);
        var lemmas = _db.Nouns.GetLemmasByRank(minRank, maxRank);
        _lemmas.AddRange(lemmas);
        return Task.CompletedTask;
    }
}

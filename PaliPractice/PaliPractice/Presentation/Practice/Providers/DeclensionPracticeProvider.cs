using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;
using PaliPractice.Services.Database;
using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.Practice;

namespace PaliPractice.Presentation.Practice.Providers;

/// <summary>
/// Provides declension practice items using the SRS queue.
/// Includes staleness detection to rebuild queue when SRS data becomes stale.
/// </summary>
public sealed class DeclensionPracticeProvider : IPracticeProvider
{
    static readonly TimeSpan StalenessThreshold = TimeSpan.FromHours(1);

    readonly IPracticeQueueBuilder _queueBuilder;
    readonly UserDataRepository _userData;
    readonly NounRepository _nouns;

    List<PracticeItem> _queue = [];
    int _currentIndex = -1;
    DateTime _queueBuiltUtc;

    public DeclensionPracticeProvider(
        IPracticeQueueBuilder queueBuilder,
        IDatabaseService db)
    {
        _queueBuilder = queueBuilder;
        _userData = db.UserData;
        _nouns = db.Nouns;
    }

    public PracticeItem? Current => _currentIndex >= 0 && _currentIndex < _queue.Count
        ? _queue[_currentIndex]
        : null;

    public int CurrentIndex => _currentIndex;
    public int TotalCount => _queue.Count;
    public bool HasNext => _currentIndex < _queue.Count - 1;

    public Task LoadAsync(CancellationToken ct = default)
    {
        var dailyGoal = _userData.GetDailyGoal(PracticeType.Declension);
        _queue = _queueBuilder.BuildQueue(PracticeType.Declension, dailyGoal);
        _currentIndex = _queue.Count > 0 ? 0 : -1;
        _queueBuiltUtc = DateTime.UtcNow;

        return Task.CompletedTask;
    }

    public bool MoveNext()
    {
        if (_currentIndex >= _queue.Count - 1)
        {
            // Queue exhausted - check if we should rebuild due to staleness
            if (DateTime.UtcNow - _queueBuiltUtc > StalenessThreshold)
            {
                System.Diagnostics.Debug.WriteLine("[DeclensionProvider] Queue stale, rebuilding...");
                LoadAsync().GetAwaiter().GetResult();
                return _queue.Count > 0;
            }
            return false;
        }

        _currentIndex++;
        return true;
    }

    public ILemma? GetCurrentLemma()
    {
        var item = Current;
        if (item == null) return null;

        var lemma = _nouns.GetLemma(item.LemmaId);
        if (lemma == null) return null;

        // Ensure details are loaded
        _nouns.EnsureDetails(lemma);
        return lemma;
    }

    public object GetCurrentParameters()
    {
        var item = Current;
        if (item == null)
            return (Case.None, Gender.None, Number.None);

        // Parse the FormId to extract grammatical parameters
        var parsed = Declension.ParseId((int)item.FormId);
        return (parsed.Case, parsed.Gender, parsed.Number);
    }
}

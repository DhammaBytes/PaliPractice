using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;
using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.Practice;

namespace PaliPractice.Presentation.Practice.Providers;

/// <summary>
/// Provides declension practice items using the SRS queue.
/// Builds queue at 120% of daily goal and silently rebuilds when exhausted.
/// </summary>
public sealed class DeclensionPracticeProvider : IPracticeProvider
{
    /// <summary>
    /// Build queue with 20% buffer over daily goal to avoid hitting the edge exactly.
    /// </summary>
    const double QueueBufferMultiplier = 1.2;

    readonly IPracticeQueueBuilder _queueBuilder;
    readonly IUserDataRepository _userData;
    readonly INounRepository _nouns;

    List<PracticeItem> _queue = [];
    int _currentIndex = -1;

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
        var queueSize = (int)(dailyGoal * QueueBufferMultiplier);
        _queue = _queueBuilder.BuildQueue(PracticeType.Declension, queueSize);
        _currentIndex = _queue.Count > 0 ? 0 : -1;

        System.Diagnostics.Debug.WriteLine($"[DeclensionProvider] Built queue with {_queue.Count} items (goal={dailyGoal}, requested={queueSize})");
        return Task.CompletedTask;
    }

    public bool MoveNext()
    {
        if (_currentIndex < _queue.Count - 1)
        {
            _currentIndex++;
            return true;
        }

        // Queue exhausted - silently try to rebuild
        System.Diagnostics.Debug.WriteLine("[DeclensionProvider] Queue exhausted, attempting silent rebuild...");
        var dailyGoal = _userData.GetDailyGoal(PracticeType.Declension);
        var queueSize = (int)(dailyGoal * QueueBufferMultiplier);
        _queue = _queueBuilder.BuildQueue(PracticeType.Declension, queueSize);

        if (_queue.Count > 0)
        {
            _currentIndex = 0;
            System.Diagnostics.Debug.WriteLine($"[DeclensionProvider] Silent rebuild successful: {_queue.Count} items");
            return true;
        }

        // Pool truly exhausted - no more forms available
        System.Diagnostics.Debug.WriteLine("[DeclensionProvider] Pool exhausted - no forms available");
        return false;
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

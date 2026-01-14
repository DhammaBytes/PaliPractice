using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;
using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.Practice;

namespace PaliPractice.Presentation.Practice.Providers;

/// <summary>
/// Provides conjugation practice items using the SRS queue.
/// Builds queue at 120% of daily goal and silently rebuilds when exhausted.
/// </summary>
public sealed class ConjugationPracticeProvider : IPracticeProvider
{
    /// <summary>
    /// Build queue with 20% buffer over daily goal to avoid hitting the edge exactly.
    /// </summary>
    const double QueueBufferMultiplier = 1.2;

    readonly IPracticeQueueBuilder _queueBuilder;
    readonly IUserDataRepository _userData;
    readonly IVerbRepository _verbs;

    List<PracticeItem> _queue = [];
    int _currentIndex = -1;

    public ConjugationPracticeProvider(
        IPracticeQueueBuilder queueBuilder,
        IDatabaseService db)
    {
        _queueBuilder = queueBuilder;
        _userData = db.UserData;
        _verbs = db.Verbs;
    }

    public PracticeItem? Current => _currentIndex >= 0 && _currentIndex < _queue.Count
        ? _queue[_currentIndex]
        : null;

    public int CurrentIndex => _currentIndex;
    public int TotalCount => _queue.Count;
    public bool HasNext => _currentIndex < _queue.Count - 1;

    public Task LoadAsync(CancellationToken ct = default)
    {
        var dailyGoal = _userData.GetDailyGoal(PracticeType.Conjugation);
        var queueSize = (int)(dailyGoal * QueueBufferMultiplier);
        _queue = _queueBuilder.BuildQueue(PracticeType.Conjugation, queueSize);
        _currentIndex = _queue.Count > 0 ? 0 : -1;

        System.Diagnostics.Debug.WriteLine($"[ConjugationProvider] Built queue with {_queue.Count} items (goal={dailyGoal}, requested={queueSize})");
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
        System.Diagnostics.Debug.WriteLine("[ConjugationProvider] Queue exhausted, attempting silent rebuild...");
        var dailyGoal = _userData.GetDailyGoal(PracticeType.Conjugation);
        var queueSize = (int)(dailyGoal * QueueBufferMultiplier);
        _queue = _queueBuilder.BuildQueue(PracticeType.Conjugation, queueSize);

        if (_queue.Count > 0)
        {
            _currentIndex = 0;
            System.Diagnostics.Debug.WriteLine($"[ConjugationProvider] Silent rebuild successful: {_queue.Count} items");
            return true;
        }

        // Pool truly exhausted - no more forms available
        System.Diagnostics.Debug.WriteLine("[ConjugationProvider] Pool exhausted - no forms available");
        return false;
    }

    public ILemma? GetCurrentLemma()
    {
        var item = Current;
        if (item == null) return null;

        var lemma = _verbs.GetLemma(item.LemmaId);
        if (lemma == null) return null;

        // Ensure details are loaded
        _verbs.EnsureDetails(lemma);
        return lemma;
    }

    public object GetCurrentParameters()
    {
        var item = Current;
        if (item == null)
            return (Tense.None, Person.None, Number.None, Voice.None);

        // Parse the FormId to extract grammatical parameters
        var parsed = Conjugation.ParseId(item.FormId);
        return (parsed.Tense, parsed.Person, parsed.Number, parsed.Voice);
    }
}

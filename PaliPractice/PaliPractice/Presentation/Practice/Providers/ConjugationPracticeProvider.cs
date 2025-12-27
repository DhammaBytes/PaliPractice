using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;
using PaliPractice.Services.Practice;
using PaliPractice.Services.UserData;

namespace PaliPractice.Presentation.Practice.Providers;

/// <summary>
/// Provides conjugation practice items using the SRS queue.
/// </summary>
public sealed class ConjugationPracticeProvider : IPracticeProvider
{
    readonly IPracticeQueueBuilder _queueBuilder;
    readonly IUserDataService _userData;
    readonly IDatabaseService _db;

    List<PracticeItem> _queue = [];
    int _currentIndex = -1;

    public ConjugationPracticeProvider(
        IPracticeQueueBuilder queueBuilder,
        IUserDataService userData,
        IDatabaseService db)
    {
        _queueBuilder = queueBuilder;
        _userData = userData;
        _db = db;
    }

    public PracticeItem? Current => _currentIndex >= 0 && _currentIndex < _queue.Count
        ? _queue[_currentIndex]
        : null;

    public int CurrentIndex => _currentIndex;
    public int TotalCount => _queue.Count;
    public bool HasNext => _currentIndex < _queue.Count - 1;

    public Task LoadAsync(CancellationToken ct = default)
    {
        _db.Initialize();
        _userData.Initialize();

        var dailyGoal = _userData.GetDailyGoal(PracticeType.Conjugation);
        _queue = _queueBuilder.BuildQueue(PracticeType.Conjugation, dailyGoal);
        _currentIndex = _queue.Count > 0 ? 0 : -1;

        return Task.CompletedTask;
    }

    public bool MoveNext()
    {
        if (_currentIndex >= _queue.Count - 1)
            return false;

        _currentIndex++;
        return true;
    }

    public ILemma? GetCurrentLemma()
    {
        var item = Current;
        if (item == null) return null;

        var lemma = _db.GetVerbLemma(item.LemmaId);
        if (lemma == null) return null;

        // Ensure details are loaded
        _db.EnsureDetails(lemma);
        return lemma;
    }

    public object GetCurrentParameters()
    {
        var item = Current;
        if (item == null)
            return (Tense.None, Person.None, Number.None, false);

        // Parse the FormId to extract grammatical parameters
        var parsed = Conjugation.ParseId(item.FormId);
        return (parsed.Tense, parsed.Person, parsed.Number, parsed.Reflexive);
    }
}

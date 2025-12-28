using PaliPractice.Models;
using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;
using PaliPractice.Services.Database;
using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.Practice;

namespace PaliPractice.Presentation.Practice.Providers;

/// <summary>
/// Provides conjugation practice items using the SRS queue.
/// </summary>
public sealed class ConjugationPracticeProvider : IPracticeProvider
{
    readonly IPracticeQueueBuilder _queueBuilder;
    readonly UserDataRepository _userData;
    readonly VerbRepository _verbs;

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

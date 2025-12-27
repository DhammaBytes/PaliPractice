using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;
using PaliPractice.Services.Practice;
using PaliPractice.Services.UserData;

namespace PaliPractice.Presentation.Practice.Providers;

/// <summary>
/// Provides declension practice items using the SRS queue.
/// </summary>
public sealed class DeclensionPracticeProvider : IPracticeProvider
{
    readonly IPracticeQueueBuilder _queueBuilder;
    readonly IUserDataService _userData;
    readonly IDatabaseService _db;

    List<PracticeItem> _queue = [];
    int _currentIndex = -1;

    public DeclensionPracticeProvider(
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

        var dailyGoal = _userData.GetDailyGoal(PracticeType.Declension);
        _queue = _queueBuilder.BuildQueue(PracticeType.Declension, dailyGoal);
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

    public IWord? GetCurrentWord()
    {
        var item = Current;
        if (item == null) return null;

        return _db.GetNounByLemmaId(item.LemmaId);
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

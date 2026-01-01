using PaliPractice.Models.Words;
using PaliPractice.Services.Database.Repositories;

namespace PaliPractice.Presentation.Practice.ViewModels.Common;

/// <summary>
/// Tracks daily practice goal progress from persisted user data.
/// </summary>
[Bindable]
public partial class DailyGoalViewModel : ObservableObject
{
    readonly IUserDataRepository _userData;
    readonly PracticeType _practiceType;

    [ObservableProperty] string _dailyGoalText = "0/50";
    [ObservableProperty] double _dailyProgress = 0.0;

    public DailyGoalViewModel(IUserDataRepository userData, PracticeType practiceType)
    {
        _userData = userData;
        _practiceType = practiceType;
        Refresh();
    }

    /// <summary>
    /// Refreshes progress from persisted data.
    /// </summary>
    public void Refresh()
    {
        var progress = _userData.GetTodayProgress();
        var goal = _userData.GetDailyGoal(_practiceType);
        var completed = _practiceType == PracticeType.Declension
            ? progress.DeclensionsCompleted
            : progress.ConjugationsCompleted;

        DailyGoalText = $"{completed}/{goal}";
        DailyProgress = goal > 0 ? 100.0 * Math.Min(completed, goal) / goal : 0;
    }
}

/// <summary>
/// Unified flashcard view model for practice screens.
/// Manages the question display, answer reveal state, and card metadata.
/// </summary>
[Bindable]
public partial class FlashCardViewModel : ObservableObject
{
    // Question side
    [ObservableProperty] string _question = string.Empty;
    [ObservableProperty] string _root = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LevelText))]
    int _level;
    public string LevelText => Level.ToString();
    [ObservableProperty] string _progressText = "1/50";
    [ObservableProperty] bool _isLoading = true;
    [ObservableProperty] string _errorMessage = string.Empty;

    // Answer side
    [ObservableProperty] bool _isRevealed;
    [ObservableProperty] string _answer = string.Empty;
    [ObservableProperty] string _answerStem = string.Empty;
    [ObservableProperty] string _answerEnding = string.Empty;

    string _inflectedForm = string.Empty;
    string _inflectedEnding = string.Empty;

    public void DisplayWord(IWord word, int currentIndex, int totalCount, int masteryLevel, string? root = null)
    {
        Question = word.Lemma;
        ProgressText = $"{currentIndex + 1}/{totalCount}";
        Level = masteryLevel;
        Root = root ?? string.Empty;
    }

    /// <summary>
    /// Sets the inflected form and ending that will be shown when revealed.
    /// </summary>
    public void SetAnswer(string inflectedForm, string ending)
    {
        _inflectedForm = inflectedForm;
        _inflectedEnding = ending;
    }

    /// <summary>
    /// Reveals the answer by showing the inflected form.
    /// </summary>
    public void Reveal()
    {
        Answer = _inflectedForm;
        AnswerEnding = _inflectedEnding;
        // Stem is the form minus the ending
        AnswerStem = _inflectedEnding.Length > 0 && _inflectedForm.EndsWith(_inflectedEnding)
            ? _inflectedForm[..^_inflectedEnding.Length]
            : _inflectedForm;
        IsRevealed = true;
    }

    /// <summary>
    /// Resets to hidden state for the next card.
    /// </summary>
    public void Reset()
    {
        IsRevealed = false;
        Answer = string.Empty;
        AnswerStem = string.Empty;
        AnswerEnding = string.Empty;
        _inflectedForm = string.Empty;
        _inflectedEnding = string.Empty;
    }
}

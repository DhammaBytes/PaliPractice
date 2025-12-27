using PaliPractice.Models.Words;

namespace PaliPractice.Presentation.Practice.ViewModels.Common;

/// <summary>
/// Tracks daily practice goal progress.
/// </summary>
[Bindable]
public partial class DailyGoalViewModel : ObservableObject
{
    const int DailyTarget = 50;

    [ObservableProperty] string _dailyGoalText = "0/50";
    [ObservableProperty] double _dailyProgress = 0.0;

    int _completedToday;

    /// <summary>
    /// Advances the daily goal by one completed card.
    /// </summary>
    public void Advance()
    {
        _completedToday = Math.Min(DailyTarget, _completedToday + 1);
        DailyGoalText = $"{_completedToday}/{DailyTarget}";
        DailyProgress = 100.0 * _completedToday / DailyTarget;
    }

    /// <summary>
    /// Resets daily progress (e.g., at start of new day).
    /// </summary>
    public void Reset()
    {
        _completedToday = 0;
        DailyGoalText = "0/50";
        DailyProgress = 0.0;
    }
}

/// <summary>
/// Manages flashcard reveal state for practice screens.
/// Controls when the answer is shown and tracks the current inflected form.
/// </summary>
[Bindable]
public partial class FlashcardStateViewModel : ObservableObject
{
    [ObservableProperty] bool _isRevealed;
    [ObservableProperty] string _answer = string.Empty;
    [ObservableProperty] string _answerStem = string.Empty;
    [ObservableProperty] string _answerEnding = string.Empty;

    string _inflectedForm = string.Empty;
    string _inflectedEnding = string.Empty;

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

/// <summary>
/// Displays the current word being practiced.
/// </summary>
[Bindable]
public partial class WordCardViewModel : ObservableObject
{
    [ObservableProperty] string _currentWord = string.Empty;
    [ObservableProperty] string _rankText = "Top-100";
    [ObservableProperty] string _ankiState = "Anki state: 6/10";
    [ObservableProperty] bool _isLoading = true;
    [ObservableProperty] string _errorMessage = string.Empty;

    public void DisplayCurrentCard(ILemma lemma)
    {
        CurrentWord = lemma.BaseForm;
        RankText = lemma.EbtCount switch
        {
            > 1000 => "Top-100",
            > 500 => "Top-300",
            > 200 => "Top-500",
            _ => "Top-1000"
        };
    }
}

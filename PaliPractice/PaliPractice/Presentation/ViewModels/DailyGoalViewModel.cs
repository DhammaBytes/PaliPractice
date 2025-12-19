namespace PaliPractice.Presentation.ViewModels;

/// <summary>
/// Tracks daily practice goal progress.
/// Extracted from CardViewModel for cleaner separation of concerns.
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

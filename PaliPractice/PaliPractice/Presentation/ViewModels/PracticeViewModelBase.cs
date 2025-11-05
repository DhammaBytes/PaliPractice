using System.ComponentModel;
using PaliPractice.Presentation.Providers;
using Uno.Extensions.Navigation;

namespace PaliPractice.Presentation.ViewModels;

/// <summary>
/// Base class for practice ViewModels (Conjugation and Declension).
/// Encapsulates shared logic: card loading, answer validation, navigation, and daily goal tracking.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public abstract partial class PracticeViewModelBase<TAnswer> : ObservableObject
{
    protected readonly IWordProvider Words;
    protected readonly INavigator Navigator;
    protected readonly ILogger Logger;

    protected int _currentIndex;
    [ObservableProperty] private bool _canRateCard;

    public CardViewModel Card { get; }

    // Commands - stored as fields to maintain reference for NotifyCanExecuteChanged
    private readonly RelayCommand _hardCommand;
    private readonly RelayCommand _easyCommand;

    /// <summary>
    /// All toggle groups for this practice type. Used to check if all selections are correct.
    /// </summary>
    protected abstract IReadOnlyList<IValidatableChoice> Groups { get; }

    /// <summary>
    /// Builds the answer key for a given word (deterministic or from database).
    /// </summary>
    protected abstract TAnswer BuildAnswerFor(Headword w);

    /// <summary>
    /// Applies the answer to all toggle groups by calling SetExpected on each.
    /// </summary>
    protected abstract void ApplyAnswerToGroups(TAnswer answer);

    /// <summary>
    /// Resets all toggle groups to their default state.
    /// </summary>
    protected abstract void ResetAllGroups();

    /// <summary>
    /// Optional: Set usage examples specific to the word type (verb/noun).
    /// </summary>
    protected virtual void SetExamples(Headword w) { }

    protected PracticeViewModelBase(
        IWordProvider words,
        CardViewModel card,
        INavigator navigator,
        ILogger logger)
    {
        Words = words;
        Card = card;
        Navigator = navigator;
        Logger = logger;

        // Initialize commands with CanExecute predicates
        _hardCommand = new RelayCommand(MarkAsHard, () => CanRateCard);
        _easyCommand = new RelayCommand(MarkAsEasy, () => CanRateCard);

        // Initialize daily goal immediately (don't wait for async initialization)
        InitializeDailyGoal();
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            Card.IsLoading = true;

            await Words.LoadAsync(ct);
            if (Words.Words.Count == 0)
            {
                Card.ErrorMessage = "No words found";
                return;
            }

            DisplayAndBindCurrentCard();
            WireGroupEvents();
            UpdateNavigationState();
        }
        catch (OperationCanceledException)
        {
            // Expected during navigation
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load words");
            Card.ErrorMessage = $"Failed to load data: {ex.Message}";
        }
        finally
        {
            Card.IsLoading = false;
        }
    }

    void DisplayAndBindCurrentCard()
    {
        var w = Words.Words[_currentIndex];
        Card.DisplayCurrentCard(Words.Words, _currentIndex, SetExamples);
        var answer = BuildAnswerFor(w);
        ApplyAnswerToGroups(answer);
        UpdateNavigationState();
    }

    void WireGroupEvents()
    {
        foreach (var group in Groups)
        {
            if (group is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged += (_, __) => UpdateNavigationState();
            }
        }
    }

    protected bool AllGroupsCorrect()
    {
        var allCorrect = Groups.All(g => g.Validation == ValidationState.Correct);
        Logger.LogDebug("AllGroupsCorrect check: {Result}. States: [{States}]",
            allCorrect,
            string.Join(", ", Groups.Select(g => g.Validation)));
        return allCorrect;
    }

    protected void UpdateNavigationState()
    {
        var hasNext = _currentIndex < Words.Words.Count - 1;
        var allCorrect = AllGroupsCorrect();
        CanRateCard = hasNext && allCorrect;

        Logger.LogDebug("UpdateNavigationState: hasNext={HasNext}, allCorrect={AllCorrect}, CanRateCard={CanRate}",
            hasNext, allCorrect, CanRateCard);

        // Notify commands to re-evaluate their CanExecute
        _hardCommand.NotifyCanExecuteChanged();
        _easyCommand.NotifyCanExecuteChanged();
    }

    // Commands - expose the stored instances
    public ICommand GoBackCommand => new AsyncRelayCommand(() => Navigator.NavigateBackAsync(this));
    public ICommand HardCommand => _hardCommand;
    public ICommand EasyCommand => _easyCommand;

    void MarkAsHard()
    {
        if (!CanRateCard) return;
        Logger.LogInformation("Marked hard: {Word}", Card.CurrentWord);
        AdvanceDailyGoal();
        MoveToNextCard();
    }

    void MarkAsEasy()
    {
        if (!CanRateCard) return;
        Logger.LogInformation("Marked easy: {Word}", Card.CurrentWord);
        AdvanceDailyGoal();
        MoveToNextCard();
    }

    void MoveToNextCard()
    {
        if (_currentIndex >= Words.Words.Count - 1) return;

        _currentIndex++;
        ResetAllGroups();          // Clear toggles
        DisplayAndBindCurrentCard();
    }

    // Simple daily goal math; tune targets as needed
    int _dailyTarget = 50;
    int _completedToday = 0;

    void InitializeDailyGoal()
    {
        Card.DailyGoalText = $"{_completedToday}/{_dailyTarget}";
        Card.DailyProgress = 100.0 * _completedToday / _dailyTarget;
        Logger.LogInformation("InitializeDailyGoal: Set to {Text}, Progress={Progress}%",
            Card.DailyGoalText, Card.DailyProgress);
    }

    protected void AdvanceDailyGoal()
    {
        _completedToday = Math.Min(_dailyTarget, _completedToday + 1);
        Card.DailyGoalText = $"{_completedToday}/{_dailyTarget}";
        Card.DailyProgress = 100.0 * _completedToday / _dailyTarget;
    }
}

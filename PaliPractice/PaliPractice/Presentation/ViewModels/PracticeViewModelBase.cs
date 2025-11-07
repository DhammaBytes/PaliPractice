using System.ComponentModel;
using PaliPractice.Models;
using PaliPractice.Presentation.Providers;

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

    protected int CurrentIndex;
    [ObservableProperty] bool _canRateCard;

    public CardViewModel Card { get; }

    // Commands - stored as fields to maintain reference for NotifyCanExecuteChanged
    readonly RelayCommand _hardCommand;
    readonly RelayCommand _easyCommand;

    /// <summary>
    /// All toggle groups for this practice type. Used to check if all selections are correct.
    /// </summary>
    protected abstract IReadOnlyList<IValidatableChoice> Groups { get; }

    /// <summary>
    /// Builds the answer key for a given word (deterministic or from database).
    /// </summary>
    protected abstract TAnswer BuildAnswerFor(IWord w);

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
    protected virtual void SetExamples(IWord w) { }

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
        var w = Words.Words[CurrentIndex];
        Card.DisplayCurrentCard(Words.Words, CurrentIndex, SetExamples);
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
        var hasNext = CurrentIndex < Words.Words.Count - 1;
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
        if (CurrentIndex >= Words.Words.Count - 1) return;

        CurrentIndex++;
        ResetAllGroups();          // Clear toggles
        DisplayAndBindCurrentCard();
    }

    // Simple daily goal math; tune targets as needed
    const int DailyTarget = 50;
    int _completedToday = 0;

    void InitializeDailyGoal()
    {
        Card.DailyGoalText = $"{_completedToday}/{DailyTarget}";
        Card.DailyProgress = 100.0 * _completedToday / DailyTarget;
        Logger.LogInformation("InitializeDailyGoal: Set to {Text}, Progress={Progress}%",
            Card.DailyGoalText, Card.DailyProgress);
    }

    protected void AdvanceDailyGoal()
    {
        _completedToday = Math.Min(DailyTarget, _completedToday + 1);
        Card.DailyGoalText = $"{_completedToday}/{DailyTarget}";
        Card.DailyProgress = 100.0 * _completedToday / DailyTarget;
    }
}

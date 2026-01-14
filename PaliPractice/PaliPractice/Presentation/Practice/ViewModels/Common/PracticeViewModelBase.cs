using PaliPractice.Models.Words;
using PaliPractice.Presentation.Grammar.ViewModels;
using PaliPractice.Presentation.Practice.Providers;
using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.UserData;

namespace PaliPractice.Presentation.Practice.ViewModels.Common;

/// <summary>
/// Base class for practice ViewModels (Conjugation and Declension).
/// Implements flashcard reveal mechanics: user sees a dictionary form,
/// guesses the inflected form, reveals the answer, then rates Easy/Hard.
/// Uses IPracticeProvider for SRS-aware practice queue.
/// </summary>
[Bindable]
public abstract partial class PracticeViewModelBase : ObservableObject
{
    protected readonly ILogger Logger;
    protected readonly IUserDataRepository UserData;

    [ObservableProperty] bool _canRateCard;
    [ObservableProperty] string _alternativeForms = string.Empty;

    public FlashCardViewModel FlashCard { get; }
    public DailyGoalViewModel DailyGoal { get; }
    public ExampleCarouselViewModel ExampleCarousel { get; }

    /// <summary>
    /// Raised when the practice pool is completely exhausted (no due or new forms).
    /// </summary>
    public event EventHandler? QueueExhausted;

    /// <summary>
    /// Raised when the daily goal is reached for the first time this session.
    /// </summary>
    public event EventHandler? DailyGoalReached;

    readonly IPracticeProvider _provider;
    protected readonly INavigator Navigator;

    // Track if we've already shown the daily goal congratulations this session
    bool _dailyGoalNotified;

    // Commands - stored as fields to maintain reference for NotifyCanExecuteChanged
    readonly RelayCommand _hardCommand;
    readonly RelayCommand _easyCommand;
    readonly RelayCommand _revealCommand;

    /// <summary>
    /// Called when a new card is displayed. Subclasses should use the lemma
    /// and grammatical parameters to set up badge display properties.
    /// </summary>
    /// <param name="lemma">The lemma with details loaded.</param>
    /// <param name="parameters">Grammatical parameters from GetCurrentParameters().</param>
    protected abstract void PrepareCardAnswer(ILemma lemma, object parameters);

    /// <summary>
    /// Returns the inflected form to display when the answer is revealed.
    /// </summary>
    protected abstract string GetInflectedForm();

    /// <summary>
    /// Returns the ending portion of the inflected form (for highlighting).
    /// </summary>
    protected abstract string GetInflectedEnding();

    /// <summary>
    /// Returns all inflected forms (not just primary) to avoid in example selection.
    /// </summary>
    protected abstract IReadOnlyList<string> GetAllInflectedForms();

    /// <summary>
    /// Returns formatted alternative forms string (other InCorpus forms besides Primary).
    /// </summary>
    protected abstract string GetAlternativeForms();

    /// <summary>
    /// Returns the practice type (Declension or Conjugation) for history navigation.
    /// </summary>
    protected abstract PracticeType CurrentPracticeType { get; }

    protected PracticeViewModelBase(
        IPracticeProvider provider,
        IUserDataRepository userData,
        FlashCardViewModel flashCard,
        INavigator navigator,
        ILogger logger)
    {
        _provider = provider;
        UserData = userData;
        FlashCard = flashCard;
        Navigator = navigator;
        Logger = logger;
        DailyGoal = new DailyGoalViewModel(userData, CurrentPracticeType);
        ExampleCarousel = new ExampleCarouselViewModel();

        // Initialize commands with CanExecute predicates
        _hardCommand = new RelayCommand(MarkAsHard, () => CanRateCard);
        _easyCommand = new RelayCommand(MarkAsEasy, () => CanRateCard);
        _revealCommand = new RelayCommand(RevealAnswer, () => !FlashCard.IsRevealed);

        // Subscribe to flashcard state changes to update navigation
        FlashCard.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FlashCardViewModel.IsRevealed))
            {
                UpdateNavigationState();
            }
        };
    }

    protected async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            FlashCard.IsLoading = true;

            await _provider.LoadAsync(ct);
            if (_provider.TotalCount == 0)
            {
                QueueExhausted?.Invoke(this, EventArgs.Empty);
                return;
            }

            DisplayCurrentCard();
            UpdateNavigationState();
        }
        catch (OperationCanceledException)
        {
            // Expected during navigation
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load practice queue");
            FlashCard.ErrorMessage = $"Failed to load data: {ex.Message}";
        }
        finally
        {
            FlashCard.IsLoading = false;
        }
    }

    void DisplayCurrentCard()
    {
        var lemma = _provider.GetCurrentLemma();
        if (lemma == null)
        {
            Logger.LogWarning("No lemma for current form");
            return;
        }

        var masteryLevel = _provider.Current?.MasteryLevel ?? CooldownCalculator.UnpracticedLevel;
        var root = lemma.Primary.Details?.Root;
        FlashCard.DisplayWord(lemma.Primary, _provider.CurrentIndex, _provider.TotalCount, masteryLevel, root);

        // Initialize carousel with all word variants for this lemma
        ExampleCarousel.Initialize(lemma.Words);

        var parameters = _provider.GetCurrentParameters();
        PrepareCardAnswer(lemma, parameters);
        FlashCard.SetAnswer(GetInflectedForm(), GetInflectedEnding());
        AlternativeForms = GetAlternativeForms();

        // Filter examples to avoid those containing answer forms
        ExampleCarousel.SetFormsToAvoid(GetAllInflectedForms());
    }

    void RevealAnswer()
    {
        FlashCard.Reveal();
        ExampleCarousel.IsRevealed = true;
        Logger.LogDebug("Answer revealed: {Form}", FlashCard.Answer);
    }

    void UpdateNavigationState()
    {
        var hasNext = _provider.HasNext;
        var isRevealed = FlashCard.IsRevealed;
        CanRateCard = isRevealed;

        Logger.LogDebug("UpdateNavigationState: hasNext={HasNext}, isRevealed={IsRevealed}, CanRateCard={CanRate}",
            hasNext, isRevealed, CanRateCard);

        // Notify commands to re-evaluate their CanExecute
        _hardCommand.NotifyCanExecuteChanged();
        _easyCommand.NotifyCanExecuteChanged();
        _revealCommand.NotifyCanExecuteChanged();
    }

    // Commands
    public ICommand GoBackCommand => new AsyncRelayCommand(() => Navigator.NavigateBackAsync(this));
    public ICommand GoToHistoryCommand => new AsyncRelayCommand(() =>
        Navigator.NavigateViewModelAsync<HistoryViewModel>(this, data: new HistoryNavigationData(CurrentPracticeType)));
    public ICommand GoToInflectionTableCommand => new AsyncRelayCommand(NavigateToInflectionTable);
    public abstract ICommand GoToSettingsCommand { get; }
    public ICommand HardCommand => _hardCommand;
    public ICommand EasyCommand => _easyCommand;
    public ICommand RevealCommand => _revealCommand;

    /// <summary>
    /// Command for the Continue button in the daily goal dialog.
    /// Does nothing - practice continues automatically.
    /// </summary>
    public ICommand ContinuePracticeCommand => new RelayCommand(() => { });

    async Task NavigateToInflectionTable()
    {
        var lemma = _provider.GetCurrentLemma();
        if (lemma == null) return;

        await Navigator.NavigateViewModelAsync<InflectionTableViewModel>(
            this, data: new InflectionTableNavigationData(lemma, CurrentPracticeType));
    }

    void MarkAsHard()
    {
        if (!CanRateCard) return;
        RecordResult(wasEasy: false);
        MoveToNextCard();
    }

    void MarkAsEasy()
    {
        if (!CanRateCard) return;
        RecordResult(wasEasy: true);
        MoveToNextCard();
    }

    void RecordResult(bool wasEasy)
    {
        var current = _provider.Current;
        if (current == null) return;

        Logger.LogInformation("Marked {Result}: FormId={FormId}",
            wasEasy ? "easy" : "hard", current.FormId);

        // Record to SRS system (FormText resolved on history load, not stored)
        UserData.RecordPracticeResult(current.FormId, CurrentPracticeType, wasEasy);

        // Update daily progress
        UserData.IncrementProgress(CurrentPracticeType);
        DailyGoal.Refresh();

        // Check if daily goal was just reached (first time this session)
        if (!_dailyGoalNotified && UserData.IsDailyGoalMet(CurrentPracticeType))
        {
            _dailyGoalNotified = true;
            Logger.LogInformation("Daily goal reached!");
            DailyGoalReached?.Invoke(this, EventArgs.Empty);
        }
    }

    void MoveToNextCard()
    {
        if (!_provider.MoveNext())
        {
            Logger.LogInformation("Practice queue exhausted");
            CanRateCard = false;
            _hardCommand.NotifyCanExecuteChanged();
            _easyCommand.NotifyCanExecuteChanged();
            QueueExhausted?.Invoke(this, EventArgs.Empty);
            return;
        }

        FlashCard.Reset();
        ExampleCarousel.Reset();
        DisplayCurrentCard();
        UpdateNavigationState();
    }
}

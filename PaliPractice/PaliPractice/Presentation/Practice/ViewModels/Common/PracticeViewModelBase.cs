using PaliPractice.Models.Words;
using PaliPractice.Localization;
using PaliPractice.Presentation.Grammar.ViewModels;
using PaliPractice.Presentation.Practice.Providers;
using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.Feedback;
using PaliPractice.Services.UserData;
using PaliPractice.Services.UserData.Entities;

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
#if DEBUG
    protected ILemma? ScreenshotLemma;
#endif

    [ObservableProperty] bool _canRateCard;
    [ObservableProperty] string _alternativeForms = string.Empty;
    [ObservableProperty] bool _useAbbreviatedLabels;

    /// <summary>
    /// Returns the practice type for this ViewModel. Used for badge width calculations.
    /// </summary>
    public abstract PracticeType PracticeTypePublic { get; }

    /// <summary>
    /// Called when UseAbbreviatedLabels changes. Override in derived classes to refresh badge labels.
    /// </summary>
    partial void OnUseAbbreviatedLabelsChanged(bool value) => OnAbbreviationModeChanged();

    /// <summary>
    /// Override in derived classes to refresh badge labels when abbreviation mode changes.
    /// </summary>
    protected virtual void OnAbbreviationModeChanged() { }

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
    readonly IStoreReviewService _storeReviewService;
    protected readonly INavigator Navigator;

    // Track if we've already shown the daily goal congratulations this session
    bool _dailyGoalNotified;
    readonly SemaphoreSlim _sessionGate = new(1, 1);
    Dictionary<string, string>? _sessionSettings;
    int _sessionDay;

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
        IStoreReviewService storeReviewService,
        ILogger logger)
    {
        _provider = provider;
        UserData = userData;
        FlashCard = flashCard;
        Navigator = navigator;
        _storeReviewService = storeReviewService;
        Logger = logger;
        DailyGoal = new DailyGoalViewModel(userData, CurrentPracticeType);
        ExampleCarousel = new ExampleCarouselViewModel();

        // Initialize commands with CanExecute predicates
        _hardCommand = new RelayCommand(MarkAsHard, () => CanRateCard);
        _easyCommand = new RelayCommand(MarkAsEasy, () => CanRateCard);
        _revealCommand = new RelayCommand(RevealAnswer, () => HasLoadedCard && !FlashCard.IsRevealed);

        // Subscribe to flashcard state changes to update navigation
        FlashCard.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(FlashCardViewModel.IsRevealed) or nameof(FlashCardViewModel.IsLoading))
            {
                UpdateNavigationState();
            }
        };
    }

    bool HasLoadedCard => !FlashCard.IsLoading && FlashCard.ErrorMessage.Length == 0 &&
#if DEBUG
        (_provider.Current != null || ScreenshotLemma != null);
#else
        _provider.Current != null;
#endif

    /// <summary>Called by the page after subscribing to session notifications.</summary>
    public virtual async Task StartAsync(CancellationToken ct = default)
    {
        try
        {
            await _sessionGate.WaitAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }

        try
        {
            var day = UserData.GetTodayProgress().Date;
            var settings = GetSessionSettings();
            DailyGoal.Refresh();
            if (_sessionDay == day && _sessionSettings != null &&
                _sessionSettings.Count == settings.Count &&
                _sessionSettings.All(pair => settings.TryGetValue(pair.Key, out var value) && value == pair.Value) &&
                _provider.Current != null && FlashCard.ErrorMessage.Length == 0)
            {
                RefreshMeanings();
                return;
            }

            if (_sessionDay != day)
                _dailyGoalNotified = UserData.IsDailyGoalMet(CurrentPracticeType);
            FlashCard.Reset();
            FlashCard.Question = string.Empty;
            FlashCard.Root = string.Empty;
            FlashCard.ErrorMessage = string.Empty;
            AlternativeForms = string.Empty;
            ExampleCarousel.Reset();
            await InitializeAsync(ct);
            if (!ct.IsCancellationRequested && FlashCard.ErrorMessage.Length == 0)
            {
                _sessionDay = day;
                // Queue construction may repair invalid settings.
                _sessionSettings = GetSessionSettings();
            }
        }
        finally
        {
            _sessionGate.Release();
        }
    }

    Dictionary<string, string> GetSessionSettings()
    {
        var prefix = CurrentPracticeType == PracticeType.Declension ? "nouns." : "verbs.";
        return UserData.GetAllSettings().Where(pair => pair.Key.StartsWith(prefix, StringComparison.Ordinal))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    protected async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            FlashCard.IsLoading = true;

            await _provider.LoadAsync(ct);
            ct.ThrowIfCancellationRequested();
            if (_provider.TotalCount == 0)
            {
                QueueExhausted?.Invoke(this, EventArgs.Empty);
                return;
            }

            DisplayCurrentCard();
            UpdateNavigationState();

            // Check if we should prompt for a store review (fire-and-forget, don't block UI)
            _ = TryPromptForReviewAsync();
        }
        catch (OperationCanceledException)
        {
            // Expected during navigation
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load practice queue");
            FlashCard.ErrorMessage = AppText.Format("Practice.Error.LoadData", ex.Message);
        }
        finally
        {
            FlashCard.IsLoading = false;
        }
    }

    async Task TryPromptForReviewAsync()
    {
        try
        {
            await _storeReviewService.TryPromptForReviewAsync();
        }
        catch (Exception ex)
        {
            // Don't let review prompt errors affect practice
            Logger.LogWarning(ex, "Failed to check review prompt eligibility");
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

    /// <summary>Refresh cached pages after a language setting changes, without advancing practice.</summary>
    public void RefreshMeanings()
    {
        var lemma = _provider.GetCurrentLemma();
        if (lemma is null) return;
        ExampleCarousel.Initialize(lemma.Words);
        ExampleCarousel.SetFormsToAvoid(GetAllInflectedForms());
    }

    void RevealAnswer()
    {
        if (!HasLoadedCard || FlashCard.IsRevealed) return;
        FlashCard.Reveal();
        ExampleCarousel.IsRevealed = true;
        Logger.LogDebug("Answer revealed: {Form}", FlashCard.Answer);
    }

    void UpdateNavigationState()
    {
        var hasNext = _provider.HasNext;
        var isRevealed = FlashCard.IsRevealed;
        CanRateCard = HasLoadedCard && isRevealed;

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
#if DEBUG
        var lemma = ScreenshotLemma ?? _provider.GetCurrentLemma();
#else
        var lemma = _provider.GetCurrentLemma();
#endif
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

        // Capture the answer actually displayed before advancing the queue.
        var lemma = _provider.GetCurrentLemma()
            ?? throw new InvalidOperationException("Current practice lemma is unavailable");
        var snapshot = PracticeSnapshot.Capture(current.FormId, CurrentPracticeType,
            GetInflectedForm(), lemma.BaseForm);
        UserData.RecordPracticeResult(current.FormId, CurrentPracticeType, wasEasy, snapshot);

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
            UpdateNavigationState();
            QueueExhausted?.Invoke(this, EventArgs.Empty);
            return;
        }

        FlashCard.Reset();
        ExampleCarousel.Reset();
        DisplayCurrentCard();
        UpdateNavigationState();
    }
}

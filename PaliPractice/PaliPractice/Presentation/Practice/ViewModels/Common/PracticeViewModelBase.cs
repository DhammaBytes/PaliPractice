using PaliPractice.Models.Words;
using PaliPractice.Presentation.Practice.Providers;
using PaliPractice.Services.Practice;
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
    protected readonly IUserDataService UserData;

    [ObservableProperty] bool _canRateCard;

    public FlashCardViewModel FlashCard { get; }
    public FlashcardStateViewModel Flashcard { get; }
    public DailyGoalViewModel DailyGoal { get; }
    public ExampleCarouselViewModel ExampleCarousel { get; }

    readonly IPracticeProvider _provider;
    readonly INavigator _navigator;

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
    /// Returns the practice type (Declension or Conjugation) for history navigation.
    /// </summary>
    protected abstract PracticeType CurrentPracticeType { get; }

    /// <summary>
    /// Record the practice result with combination difficulty update.
    /// Subclasses implement to call the appropriate difficulty update method.
    /// </summary>
    protected abstract void RecordCombinationDifficulty(bool wasHard);

    protected PracticeViewModelBase(
        IPracticeProvider provider,
        IUserDataService userData,
        FlashCardViewModel flashCard,
        INavigator navigator,
        ILogger logger)
    {
        _provider = provider;
        UserData = userData;
        FlashCard = flashCard;
        _navigator = navigator;
        Logger = logger;
        Flashcard = new FlashcardStateViewModel();
        DailyGoal = new DailyGoalViewModel(userData, CurrentPracticeType);
        ExampleCarousel = new ExampleCarouselViewModel();

        // Initialize commands with CanExecute predicates
        _hardCommand = new RelayCommand(MarkAsHard, () => CanRateCard);
        _easyCommand = new RelayCommand(MarkAsEasy, () => CanRateCard);
        _revealCommand = new RelayCommand(RevealAnswer, () => !Flashcard.IsRevealed);

        // Subscribe to flashcard state changes to update navigation
        Flashcard.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FlashcardStateViewModel.IsRevealed))
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
                FlashCard.ErrorMessage = "No forms to practice. Check your settings.";
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

        var masteryLevel = _provider.Current?.MasteryLevel ?? 1;
        FlashCard.DisplayWord(lemma.Primary, _provider.CurrentIndex, _provider.TotalCount, masteryLevel);

        // Initialize carousel with all word variants for this lemma
        ExampleCarousel.Initialize(lemma.Words);

        var parameters = _provider.GetCurrentParameters();
        PrepareCardAnswer(lemma, parameters);
        Flashcard.SetAnswer(GetInflectedForm(), GetInflectedEnding());
    }

    void RevealAnswer()
    {
        Flashcard.Reveal();
        ExampleCarousel.IsRevealed = true;
        Logger.LogDebug("Answer revealed: {Form}", Flashcard.Answer);
    }

    void UpdateNavigationState()
    {
        var hasNext = _provider.HasNext;
        var isRevealed = Flashcard.IsRevealed;
        CanRateCard = isRevealed;

        Logger.LogDebug("UpdateNavigationState: hasNext={HasNext}, isRevealed={IsRevealed}, CanRateCard={CanRate}",
            hasNext, isRevealed, CanRateCard);

        // Notify commands to re-evaluate their CanExecute
        _hardCommand.NotifyCanExecuteChanged();
        _easyCommand.NotifyCanExecuteChanged();
        _revealCommand.NotifyCanExecuteChanged();
    }

    // Commands
    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));
    public ICommand GoToHistoryCommand => new AsyncRelayCommand(() =>
        _navigator.NavigateViewModelAsync<HistoryViewModel>(this, data: new HistoryNavigationData(CurrentPracticeType)));
    public ICommand HardCommand => _hardCommand;
    public ICommand EasyCommand => _easyCommand;
    public ICommand RevealCommand => _revealCommand;

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

        var formText = GetInflectedForm();
        Logger.LogInformation("Marked {Result}: {Form} (FormId={FormId})",
            wasEasy ? "easy" : "hard", formText, current.FormId);

        // Record to SRS system
        UserData.RecordPracticeResult(current.FormId, CurrentPracticeType, wasEasy, formText);

        // Update combination difficulty
        RecordCombinationDifficulty(wasHard: !wasEasy);

        // Update daily progress
        UserData.IncrementProgress(CurrentPracticeType);
        DailyGoal.Refresh();
    }

    void MoveToNextCard()
    {
        if (!_provider.MoveNext())
        {
            // Queue exhausted - could show completion screen
            Logger.LogInformation("Practice queue exhausted");
            CanRateCard = false;
            _hardCommand.NotifyCanExecuteChanged();
            _easyCommand.NotifyCanExecuteChanged();
            return;
        }

        Flashcard.Reset();
        ExampleCarousel.Reset();
        DisplayCurrentCard();
        UpdateNavigationState();
    }
}

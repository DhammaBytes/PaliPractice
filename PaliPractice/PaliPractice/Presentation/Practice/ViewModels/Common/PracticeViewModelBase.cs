using PaliPractice.Models.Words;
using PaliPractice.Presentation.Practice.Providers;

namespace PaliPractice.Presentation.Practice.ViewModels.Common;

/// <summary>
/// Base class for practice ViewModels (Conjugation and Declension).
/// Implements flashcard reveal mechanics: user sees a dictionary form,
/// guesses the inflected form, reveals the answer, then rates Easy/Hard.
/// </summary>
[Bindable]
public abstract partial class PracticeViewModelBase : ObservableObject
{
    protected readonly ILogger Logger;

    [ObservableProperty] bool _canRateCard;

    public Common.WordCardViewModel WordCard { get; }
    public Common.FlashcardStateViewModel Flashcard { get; }
    public Common.DailyGoalViewModel DailyGoal { get; }
    public Common.ExampleCarouselViewModel ExampleCarousel { get; }
    
    readonly ILemmaProvider _lemmas;
    readonly INavigator _navigator;
    
    // Commands - stored as fields to maintain reference for NotifyCanExecuteChanged
    readonly RelayCommand _hardCommand;
    readonly RelayCommand _easyCommand;
    readonly RelayCommand _revealCommand;
    
    int _currentIndex;

    /// <summary>
    /// Called when a new card is displayed. Subclasses should generate the inflected form
    /// for the current lemma and set up badge display properties.
    /// </summary>
    protected abstract void PrepareCardAnswer(ILemma lemma);

    /// <summary>
    /// Returns the inflected form to display when the answer is revealed.
    /// </summary>
    protected abstract string GetInflectedForm();

    /// <summary>
    /// Returns the practice type (Declension or Conjugation) for history navigation.
    /// </summary>
    protected abstract PracticeType CurrentPracticeType { get; }

    protected PracticeViewModelBase(
        ILemmaProvider lemmas,
        Common.WordCardViewModel wordCard,
        INavigator navigator,
        ILogger logger)
    {
        _lemmas = lemmas;
        WordCard = wordCard;
        _navigator = navigator;
        Logger = logger;
        Flashcard = new Common.FlashcardStateViewModel();
        DailyGoal = new Common.DailyGoalViewModel();
        ExampleCarousel = new Common.ExampleCarouselViewModel();

        // Initialize commands with CanExecute predicates
        _hardCommand = new RelayCommand(MarkAsHard, () => CanRateCard);
        _easyCommand = new RelayCommand(MarkAsEasy, () => CanRateCard);
        _revealCommand = new RelayCommand(RevealAnswer, () => !Flashcard.IsRevealed);

        // Subscribe to flashcard state changes to update navigation
        Flashcard.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Common.FlashcardStateViewModel.IsRevealed))
            {
                UpdateNavigationState();
            }
        };
    }

    protected async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            WordCard.IsLoading = true;

            await _lemmas.LoadAsync(ct);
            if (_lemmas.Lemmas.Count == 0)
            {
                WordCard.ErrorMessage = "No words found";
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
            Logger.LogError(ex, "Failed to load words");
            WordCard.ErrorMessage = $"Failed to load data: {ex.Message}";
        }
        finally
        {
            WordCard.IsLoading = false;
        }
    }

    void DisplayCurrentCard()
    {
        var lemma = _lemmas.Lemmas[_currentIndex];
        WordCard.DisplayCurrentCard(lemma);
        ExampleCarousel.Initialize(lemma);
        PrepareCardAnswer(lemma);
        Flashcard.SetAnswer(GetInflectedForm());
    }

    void RevealAnswer()
    {
        Flashcard.Reveal();
        ExampleCarousel.IsRevealed = true;
        Logger.LogDebug("Answer revealed: {Form}", Flashcard.Answer);
    }

    void UpdateNavigationState()
    {
        var hasNext = _currentIndex < _lemmas.Lemmas.Count - 1;
        var isRevealed = Flashcard.IsRevealed;
        CanRateCard = hasNext && isRevealed;

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
        Logger.LogInformation("Marked hard: {Word}", WordCard.CurrentWord);
        DailyGoal.Advance();
        MoveToNextCard();
    }

    void MarkAsEasy()
    {
        if (!CanRateCard) return;
        Logger.LogInformation("Marked easy: {Word}", WordCard.CurrentWord);
        DailyGoal.Advance();
        MoveToNextCard();
    }

    void MoveToNextCard()
    {
        if (_currentIndex >= _lemmas.Lemmas.Count - 1) return;

        _currentIndex++;
        Flashcard.Reset();
        ExampleCarousel.Reset();
        DisplayCurrentCard();
        UpdateNavigationState();
    }
}

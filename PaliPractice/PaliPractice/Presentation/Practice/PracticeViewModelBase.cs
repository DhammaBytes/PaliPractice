using PaliPractice.Presentation.Practice.Controls;
using PaliPractice.Presentation.Practice.Providers;

namespace PaliPractice.Presentation.Practice;

/// <summary>
/// Base class for practice ViewModels (Conjugation and Declension).
/// Implements flashcard reveal mechanics: user sees dictionary form, guesses inflected form,
/// reveals answer, then rates Easy/Hard.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public abstract partial class PracticeViewModelBase : ObservableObject
{
    protected readonly ILemmaProvider Lemmas;
    protected readonly INavigator Navigator;
    protected readonly ILogger Logger;

    protected int CurrentIndex;
    [ObservableProperty] bool _canRateCard;

    public WordCardViewModel WordCard { get; }
    public FlashcardStateViewModel Flashcard { get; }
    public DailyGoalViewModel DailyGoal { get; }
    public ExampleCarouselViewModel ExampleCarousel { get; }

    // Commands - stored as fields to maintain reference for NotifyCanExecuteChanged
    readonly RelayCommand _hardCommand;
    readonly RelayCommand _easyCommand;
    readonly RelayCommand _revealCommand;

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
    public abstract PracticeType CurrentPracticeType { get; }

    protected PracticeViewModelBase(
        ILemmaProvider lemmas,
        WordCardViewModel wordCard,
        INavigator navigator,
        ILogger logger)
    {
        Lemmas = lemmas;
        WordCard = wordCard;
        Navigator = navigator;
        Logger = logger;
        Flashcard = new FlashcardStateViewModel();
        DailyGoal = new DailyGoalViewModel();
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

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            WordCard.IsLoading = true;

            await Lemmas.LoadAsync(ct);
            if (Lemmas.Lemmas.Count == 0)
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
        var lemma = Lemmas.Lemmas[CurrentIndex];
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
        var hasNext = CurrentIndex < Lemmas.Lemmas.Count - 1;
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
    public ICommand GoBackCommand => new AsyncRelayCommand(() => Navigator.NavigateBackAsync(this));
    public ICommand GoToHistoryCommand => new AsyncRelayCommand(() =>
        Navigator.NavigateViewModelAsync<HistoryViewModel>(this, data: new HistoryNavigationData(CurrentPracticeType)));
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
        if (CurrentIndex >= Lemmas.Lemmas.Count - 1) return;

        CurrentIndex++;
        Flashcard.Reset();
        ExampleCarousel.Reset();
        DisplayCurrentCard();
        UpdateNavigationState();
    }
}

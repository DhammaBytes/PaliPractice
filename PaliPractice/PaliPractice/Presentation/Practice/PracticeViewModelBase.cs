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
    protected readonly IWordProvider Words;
    protected readonly INavigator Navigator;
    protected readonly ILogger Logger;

    protected int CurrentIndex;
    [ObservableProperty] bool _canRateCard;

    public CardViewModel Card { get; }
    public FlashcardStateViewModel Flashcard { get; }
    public DailyGoalViewModel DailyGoal { get; }

    // Commands - stored as fields to maintain reference for NotifyCanExecuteChanged
    readonly RelayCommand _hardCommand;
    readonly RelayCommand _easyCommand;
    readonly RelayCommand _revealCommand;

    /// <summary>
    /// Called when a new card is displayed. Subclasses should generate the inflected form
    /// for the current word and set up badge display properties.
    /// </summary>
    protected abstract void PrepareCardAnswer(IWord word);

    /// <summary>
    /// Returns the inflected form to display when the answer is revealed.
    /// </summary>
    protected abstract string GetInflectedForm();

    /// <summary>
    /// Returns the practice type (Declension or Conjugation) for history navigation.
    /// </summary>
    public abstract PracticeType CurrentPracticeType { get; }

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
        Flashcard = new FlashcardStateViewModel();
        DailyGoal = new DailyGoalViewModel();

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
            Card.IsLoading = true;

            await Words.LoadAsync(ct);
            if (Words.Words.Count == 0)
            {
                Card.ErrorMessage = "No words found";
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
            Card.ErrorMessage = $"Failed to load data: {ex.Message}";
        }
        finally
        {
            Card.IsLoading = false;
        }
    }

    void DisplayCurrentCard()
    {
        var word = Words.Words[CurrentIndex];
        Card.DisplayCurrentCard(Words.Words, CurrentIndex, SetExamples);
        PrepareCardAnswer(word);
        Flashcard.SetAnswer(GetInflectedForm());
    }

    void RevealAnswer()
    {
        Flashcard.Reveal();
        Logger.LogDebug("Answer revealed: {Form}", Flashcard.Answer);
    }

    void UpdateNavigationState()
    {
        var hasNext = CurrentIndex < Words.Words.Count - 1;
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
        Logger.LogInformation("Marked hard: {Word}", Card.CurrentWord);
        DailyGoal.Advance();
        MoveToNextCard();
    }

    void MarkAsEasy()
    {
        if (!CanRateCard) return;
        Logger.LogInformation("Marked easy: {Word}", Card.CurrentWord);
        DailyGoal.Advance();
        MoveToNextCard();
    }

    void MoveToNextCard()
    {
        if (CurrentIndex >= Words.Words.Count - 1) return;

        CurrentIndex++;
        Flashcard.Reset();
        DisplayCurrentCard();
        UpdateNavigationState();
    }
}

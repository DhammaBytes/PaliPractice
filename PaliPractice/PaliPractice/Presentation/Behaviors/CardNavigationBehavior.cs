namespace PaliPractice.Presentation.Behaviors;

[Bindable]
public partial class CardNavigationBehavior : ObservableObject
{
    readonly IWordSource _wordSource;
    readonly CardStateBehavior _cardState;
    readonly Action<Headword> _setExamples;
    readonly Func<bool> _canProceedNext;

    [ObservableProperty] bool canGoToPrevious;
    [ObservableProperty] bool canGoToNext;

    public ICommand PreviousCommand { get; }
    public ICommand NextCommand { get; }

    public CardNavigationBehavior(
        IWordSource wordSource,
        CardStateBehavior cardState,
        Action<Headword> setExamples,
        Func<bool>? canProceedNext = null)
    {
        _wordSource = wordSource;
        _cardState = cardState;
        _setExamples = setExamples;
        _canProceedNext = canProceedNext ?? (() => true);

        PreviousCommand = new RelayCommand(GoToPrevious, () => CanGoToPrevious);
        NextCommand = new RelayCommand(GoToNext, () => CanGoToNext);

        UpdateNavigationState();
    }

    void GoToPrevious()
    {
        if (_wordSource.CurrentIndex > 0)
        {
            _wordSource.CurrentIndex--;
            _cardState.DisplayCurrentCard(_wordSource.Words, _wordSource.CurrentIndex, _setExamples);
            UpdateNavigationState();
        }
    }

    void GoToNext()
    {
        if (_wordSource.CurrentIndex < _wordSource.Words.Count - 1)
        {
            _wordSource.CurrentIndex++;
            _cardState.DisplayCurrentCard(_wordSource.Words, _wordSource.CurrentIndex, _setExamples);
            UpdateNavigationState();
        }
    }

    public void UpdateNavigationState()
    {
        CanGoToPrevious = _wordSource.CurrentIndex > 0;
        CanGoToNext = _wordSource.CurrentIndex < _wordSource.Words.Count - 1 && _canProceedNext();
        
        ((RelayCommand)PreviousCommand).NotifyCanExecuteChanged();
        ((RelayCommand)NextCommand).NotifyCanExecuteChanged();
    }
}
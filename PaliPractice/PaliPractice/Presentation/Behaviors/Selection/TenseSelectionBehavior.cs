namespace PaliPractice.Presentation.Behaviors.Selection;

[Bindable]
public partial class TenseSelectionBehavior : ObservableObject
{
    [ObservableProperty] bool _isPresentSelected;
    [ObservableProperty] bool _isImperativeSelected;
    [ObservableProperty] bool _isAoristSelected;
    [ObservableProperty] bool _isOptativeSelected;
    [ObservableProperty] bool _isFutureSelected;

    public ICommand SelectPresentCommand { get; }
    public ICommand SelectImperativeCommand { get; }
    public ICommand SelectAoristCommand { get; }
    public ICommand SelectOptativeCommand { get; }
    public ICommand SelectFutureCommand { get; }

    public TenseSelectionBehavior()
    {
        SelectPresentCommand = new RelayCommand(() => Set(true, false, false, false, false));
        SelectImperativeCommand = new RelayCommand(() => Set(false, true, false, false, false));
        SelectAoristCommand = new RelayCommand(() => Set(false, false, true, false, false));
        SelectOptativeCommand = new RelayCommand(() => Set(false, false, false, true, false));
        SelectFutureCommand = new RelayCommand(() => Set(false, false, false, false, true));
    }

    void Set(bool present, bool imperative, bool aorist, bool optative, bool future)
    {
        IsPresentSelected = present;
        IsImperativeSelected = imperative;
        IsAoristSelected = aorist;
        IsOptativeSelected = optative;
        IsFutureSelected = future;
    }
}

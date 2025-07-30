namespace PaliPractice.Presentation.Behaviors;

[Bindable]
public partial class TenseSelectionBehavior : ObservableObject
{
    [ObservableProperty] bool isPresentSelected;
    [ObservableProperty] bool isImperativeSelected;
    [ObservableProperty] bool isAoristSelected;
    [ObservableProperty] bool isOptativeSelected;
    [ObservableProperty] bool isFutureSelected;

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

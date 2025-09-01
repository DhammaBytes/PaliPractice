namespace PaliPractice.Presentation.Behaviors.Selection;

[Bindable]
public partial class GenderSelectionBehavior : ObservableObject
{
    [ObservableProperty] bool _isMasculineSelected;
    [ObservableProperty] bool _isNeuterSelected;
    [ObservableProperty] bool _isFeminineSelected;

    public ICommand SelectMasculineCommand { get; }
    public ICommand SelectNeuterCommand { get; }
    public ICommand SelectFeminineCommand { get; }

    public GenderSelectionBehavior()
    {
        SelectMasculineCommand = new RelayCommand(() => Set(true, false, false));
        SelectNeuterCommand = new RelayCommand(() => Set(false, true, false));
        SelectFeminineCommand = new RelayCommand(() => Set(false, false, true));
    }

    void Set(bool masculine, bool neuter, bool feminine)
    {
        IsMasculineSelected = masculine;
        IsNeuterSelected = neuter;
        IsFeminineSelected = feminine;
    }
}
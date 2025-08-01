namespace PaliPractice.Presentation.Behaviors;

[Bindable]
public partial class PersonSelectionBehavior : ObservableObject
{
    [ObservableProperty] bool isFirstPersonSelected;
    [ObservableProperty] bool isSecondPersonSelected;
    [ObservableProperty] bool isThirdPersonSelected;

    public ICommand SelectFirstPersonCommand { get; }
    public ICommand SelectSecondPersonCommand { get; }
    public ICommand SelectThirdPersonCommand { get; }

    public PersonSelectionBehavior()
    {
        SelectFirstPersonCommand = new RelayCommand(() => Set(true, false, false));
        SelectSecondPersonCommand = new RelayCommand(() => Set(false, true, false));
        SelectThirdPersonCommand = new RelayCommand(() => Set(false, false, true));
    }

    void Set(bool first, bool second, bool third)
    {
        IsFirstPersonSelected = first;
        IsSecondPersonSelected = second;
        IsThirdPersonSelected = third;
    }
}
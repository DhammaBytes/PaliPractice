namespace PaliPractice.Presentation.Behaviors;

[Bindable]
public partial class NumberSelectionBehavior : ObservableObject
{
    [ObservableProperty] bool isSingularSelected;
    [ObservableProperty] bool isPluralSelected;

    public ICommand SelectSingularCommand { get; }
    public ICommand SelectPluralCommand { get; }

    public NumberSelectionBehavior()
    {
        SelectSingularCommand = new RelayCommand(() => { IsSingularSelected = true; IsPluralSelected = false; });
        SelectPluralCommand = new RelayCommand(() => { IsPluralSelected = true; IsSingularSelected = false; });
    }
}

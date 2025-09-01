namespace PaliPractice.Presentation.Behaviors.Selection;

[Bindable]
public partial class NumberSelectionBehavior : ObservableObject
{
    [ObservableProperty] bool _isSingularSelected;
    [ObservableProperty] bool _isPluralSelected;

    public ICommand SelectSingularCommand { get; }
    public ICommand SelectPluralCommand { get; }

    public NumberSelectionBehavior()
    {
        SelectSingularCommand = new RelayCommand(() => { IsSingularSelected = true; IsPluralSelected = false; });
        SelectPluralCommand = new RelayCommand(() => { IsPluralSelected = true; IsSingularSelected = false; });
    }
}

using System.ComponentModel;

namespace PaliPractice.Presentation.ViewModels.ButtonGroups;

[Microsoft.UI.Xaml.Data.Bindable]
public partial class PersonButtonGroupViewModel : ButtonGroupViewModel<Person>
{
    public PersonButtonGroupViewModel() : base(Person.None) 
    {
        SelectFirstPersonCommand = new RelayCommand(() => Select(Person.First));
        SelectSecondPersonCommand = new RelayCommand(() => Select(Person.Second));
        SelectThirdPersonCommand = new RelayCommand(() => Select(Person.Third));
        PropertyChanged += OnSelectionPropertyChanged;
    }

    void OnSelectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Selected))
        {
            OnPropertyChanged(nameof(IsFirstPersonSelected));
            OnPropertyChanged(nameof(IsSecondPersonSelected));
            OnPropertyChanged(nameof(IsThirdPersonSelected));
        }
    }

    public bool IsFirstPersonSelected => IsSelected(Person.First);
    public bool IsSecondPersonSelected => IsSelected(Person.Second);
    public bool IsThirdPersonSelected => IsSelected(Person.Third);

    public ICommand SelectFirstPersonCommand { get; }
    public ICommand SelectSecondPersonCommand { get; }
    public ICommand SelectThirdPersonCommand { get; }
}
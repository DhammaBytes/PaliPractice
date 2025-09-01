using System.ComponentModel;

namespace PaliPractice.Presentation.Shared.ViewModels;

[Microsoft.UI.Xaml.Data.Bindable]
public partial class GenderSelection : Selection<Gender>
{
    public GenderSelection() : base(Gender.None) 
    {
        SelectMasculineCommand = new RelayCommand(() => Select(Gender.Masculine));
        SelectNeuterCommand = new RelayCommand(() => Select(Gender.Neuter));
        SelectFeminineCommand = new RelayCommand(() => Select(Gender.Feminine));
        PropertyChanged += OnSelectionPropertyChanged;
    }

    void OnSelectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Selected))
        {
            OnPropertyChanged(nameof(IsMasculineSelected));
            OnPropertyChanged(nameof(IsNeuterSelected));
            OnPropertyChanged(nameof(IsFeminineSelected));
        }
    }

    public bool IsMasculineSelected => IsSelected(Gender.Masculine);
    public bool IsNeuterSelected => IsSelected(Gender.Neuter);
    public bool IsFeminineSelected => IsSelected(Gender.Feminine);

    public ICommand SelectMasculineCommand { get; }
    public ICommand SelectNeuterCommand { get; }
    public ICommand SelectFeminineCommand { get; }
}
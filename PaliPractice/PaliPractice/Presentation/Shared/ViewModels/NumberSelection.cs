using System.ComponentModel;

namespace PaliPractice.Presentation.Shared.ViewModels;

[Microsoft.UI.Xaml.Data.Bindable]
public partial class NumberSelection : Selection<Number>
{
    public NumberSelection() : base(Number.None) 
    {
        SelectSingularCommand = new RelayCommand(() => Select(Number.Singular));
        SelectPluralCommand = new RelayCommand(() => Select(Number.Plural));
        PropertyChanged += OnSelectionPropertyChanged;
    }

    void OnSelectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Selected))
        {
            OnPropertyChanged(nameof(IsSingularSelected));
            OnPropertyChanged(nameof(IsPluralSelected));
        }
    }

    public bool IsSingularSelected => IsSelected(Number.Singular);
    public bool IsPluralSelected => IsSelected(Number.Plural);

    public ICommand SelectSingularCommand { get; }
    public ICommand SelectPluralCommand { get; }
}
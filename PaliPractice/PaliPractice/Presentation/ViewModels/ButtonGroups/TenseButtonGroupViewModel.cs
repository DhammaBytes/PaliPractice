using System.ComponentModel;

namespace PaliPractice.Presentation.ViewModels.ButtonGroups;

[Microsoft.UI.Xaml.Data.Bindable]
public partial class TenseButtonGroupViewModel : ButtonGroupViewModel<Tense>
{
    public TenseButtonGroupViewModel() : base(Tense.None) 
    {
        SelectPresentCommand = new RelayCommand(() => Select(Tense.Present));
        SelectImperativeCommand = new RelayCommand(() => Select(Tense.Imperative));
        SelectAoristCommand = new RelayCommand(() => Select(Tense.Aorist));
        SelectOptativeCommand = new RelayCommand(() => Select(Tense.Optative));
        SelectFutureCommand = new RelayCommand(() => Select(Tense.Future));
        PropertyChanged += OnSelectionPropertyChanged;
    }

    void OnSelectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Selected))
        {
            OnPropertyChanged(nameof(IsPresentSelected));
            OnPropertyChanged(nameof(IsImperativeSelected));
            OnPropertyChanged(nameof(IsAoristSelected));
            OnPropertyChanged(nameof(IsOptativeSelected));
            OnPropertyChanged(nameof(IsFutureSelected));
        }
    }

    public bool IsPresentSelected => IsSelected(Tense.Present);
    public bool IsImperativeSelected => IsSelected(Tense.Imperative);
    public bool IsAoristSelected => IsSelected(Tense.Aorist);
    public bool IsOptativeSelected => IsSelected(Tense.Optative);
    public bool IsFutureSelected => IsSelected(Tense.Future);

    public ICommand SelectPresentCommand { get; }
    public ICommand SelectImperativeCommand { get; }
    public ICommand SelectAoristCommand { get; }
    public ICommand SelectOptativeCommand { get; }
    public ICommand SelectFutureCommand { get; }
}
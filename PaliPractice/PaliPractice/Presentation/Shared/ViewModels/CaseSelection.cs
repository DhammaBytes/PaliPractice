using System.ComponentModel;

namespace PaliPractice.Presentation.Shared.ViewModels;

[Microsoft.UI.Xaml.Data.Bindable]
public partial class CaseSelection : Selection<NounCase>
{
    public CaseSelection() : base(NounCase.None) 
    {
        SelectNominativeCommand = new RelayCommand(() => Select(NounCase.Nominative));
        SelectAccusativeCommand = new RelayCommand(() => Select(NounCase.Accusative));
        SelectInstrumentalCommand = new RelayCommand(() => Select(NounCase.Instrumental));
        SelectDativeCommand = new RelayCommand(() => Select(NounCase.Dative));
        SelectAblativeCommand = new RelayCommand(() => Select(NounCase.Ablative));
        SelectGenitiveCommand = new RelayCommand(() => Select(NounCase.Genitive));
        SelectLocativeCommand = new RelayCommand(() => Select(NounCase.Locative));
        SelectVocativeCommand = new RelayCommand(() => Select(NounCase.Vocative));
        PropertyChanged += OnSelectionPropertyChanged;
    }

    void OnSelectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Selected))
        {
            OnPropertyChanged(nameof(IsNominativeSelected));
            OnPropertyChanged(nameof(IsAccusativeSelected));
            OnPropertyChanged(nameof(IsInstrumentalSelected));
            OnPropertyChanged(nameof(IsDativeSelected));
            OnPropertyChanged(nameof(IsAblativeSelected));
            OnPropertyChanged(nameof(IsGenitiveSelected));
            OnPropertyChanged(nameof(IsLocativeSelected));
            OnPropertyChanged(nameof(IsVocativeSelected));
        }
    }

    public bool IsNominativeSelected => IsSelected(NounCase.Nominative);
    public bool IsAccusativeSelected => IsSelected(NounCase.Accusative);
    public bool IsInstrumentalSelected => IsSelected(NounCase.Instrumental);
    public bool IsDativeSelected => IsSelected(NounCase.Dative);
    public bool IsAblativeSelected => IsSelected(NounCase.Ablative);
    public bool IsGenitiveSelected => IsSelected(NounCase.Genitive);
    public bool IsLocativeSelected => IsSelected(NounCase.Locative);
    public bool IsVocativeSelected => IsSelected(NounCase.Vocative);

    public ICommand SelectNominativeCommand { get; }
    public ICommand SelectAccusativeCommand { get; }
    public ICommand SelectInstrumentalCommand { get; }
    public ICommand SelectDativeCommand { get; }
    public ICommand SelectAblativeCommand { get; }
    public ICommand SelectGenitiveCommand { get; }
    public ICommand SelectLocativeCommand { get; }
    public ICommand SelectVocativeCommand { get; }
}

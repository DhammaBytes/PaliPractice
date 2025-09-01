namespace PaliPractice.Presentation.Behaviors.Selection;

[Bindable]
public partial class CaseSelectionBehavior : ObservableObject
{
    [ObservableProperty] bool _isNominativeSelected;
    [ObservableProperty] bool _isAccusativeSelected;
    [ObservableProperty] bool _isInstrumentalSelected;
    [ObservableProperty] bool _isDativeSelected;
    [ObservableProperty] bool _isAblativeSelected;
    [ObservableProperty] bool _isGenitiveSelected;
    [ObservableProperty] bool _isLocativeSelected;
    [ObservableProperty] bool _isVocativeSelected;

    public ICommand SelectNominativeCommand { get; }
    public ICommand SelectAccusativeCommand { get; }
    public ICommand SelectInstrumentalCommand { get; }
    public ICommand SelectDativeCommand { get; }
    public ICommand SelectAblativeCommand { get; }
    public ICommand SelectGenitiveCommand { get; }
    public ICommand SelectLocativeCommand { get; }
    public ICommand SelectVocativeCommand { get; }

    public CaseSelectionBehavior()
    {
        SelectNominativeCommand = new RelayCommand(() => Set(true, false, false, false, false, false, false, false));
        SelectAccusativeCommand = new RelayCommand(() => Set(false, true, false, false, false, false, false, false));
        SelectInstrumentalCommand = new RelayCommand(() => Set(false, false, true, false, false, false, false, false));
        SelectDativeCommand = new RelayCommand(() => Set(false, false, false, true, false, false, false, false));
        SelectAblativeCommand = new RelayCommand(() => Set(false, false, false, false, true, false, false, false));
        SelectGenitiveCommand = new RelayCommand(() => Set(false, false, false, false, false, true, false, false));
        SelectLocativeCommand = new RelayCommand(() => Set(false, false, false, false, false, false, true, false));
        SelectVocativeCommand = new RelayCommand(() => Set(false, false, false, false, false, false, false, true));
    }

    void Set(bool nom, bool acc, bool ins, bool dat, bool abl, bool gen, bool loc, bool voc)
    {
        IsNominativeSelected = nom;
        IsAccusativeSelected = acc;
        IsInstrumentalSelected = ins;
        IsDativeSelected = dat;
        IsAblativeSelected = abl;
        IsGenitiveSelected = gen;
        IsLocativeSelected = loc;
        IsVocativeSelected = voc;
    }
}

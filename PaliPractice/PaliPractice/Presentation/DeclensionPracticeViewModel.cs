using PaliPractice.Services;
using System.Windows.Input;

namespace PaliPractice.Presentation;

public partial class DeclensionPracticeViewModel : ObservableObject
{
    readonly IDatabaseService _databaseService;
    readonly ILogger<DeclensionPracticeViewModel> _logger;
    readonly INavigator _navigator;
    List<Headword> _nouns = new();
    int _currentIndex = 0;

    [ObservableProperty] string currentWord = string.Empty;
    [ObservableProperty] string rankText = "Top-100";
    [ObservableProperty] string ankiState = "Anki state: 6/10";
    [ObservableProperty] string usageExample = "virūp'akkhehi me mettaṃ, mettaṃ erāpathehi me";
    [ObservableProperty] string suttaReference = "AN4.67 ahirājasuttaṃ";
    [ObservableProperty] string dailyGoalText = "25/50";
    [ObservableProperty] double dailyProgress = 50.0;
    
    [ObservableProperty] bool isLoading = true;
    [ObservableProperty] string errorMessage = string.Empty;
    
    // Number selection
    [ObservableProperty] bool isSingularSelected = false;
    [ObservableProperty] bool isPluralSelected = false;
    
    // Gender selection
    [ObservableProperty] bool isMasculineSelected = false;
    [ObservableProperty] bool isNeuterSelected = false;
    [ObservableProperty] bool isFeminineSelected = false;
    
    // Case selection
    [ObservableProperty] bool isNominativeSelected = false;
    [ObservableProperty] bool isAccusativeSelected = false;
    [ObservableProperty] bool isInstrumentalSelected = false;
    [ObservableProperty] bool isDativeSelected = false;
    [ObservableProperty] bool isAblativeSelected = false;
    [ObservableProperty] bool isGenitiveSelected = false;
    [ObservableProperty] bool isLocativeSelected = false;
    [ObservableProperty] bool isVocativeSelected = false;
    
    public DeclensionPracticeViewModel(
        IDatabaseService databaseService, 
        ILogger<DeclensionPracticeViewModel> logger,
        INavigator navigator)
    {
        _databaseService = databaseService;
        _logger = logger;
        _navigator = navigator;
        
        // Navigation commands
        GoBackCommand = new AsyncRelayCommand(GoBack);
        
        // Number selection commands
        SelectSingularCommand = new RelayCommand(SelectSingular);
        SelectPluralCommand = new RelayCommand(SelectPlural);
        
        // Gender selection commands
        SelectMasculineCommand = new RelayCommand(SelectMasculine);
        SelectNeuterCommand = new RelayCommand(SelectNeuter);
        SelectFeminineCommand = new RelayCommand(SelectFeminine);
        
        // Case selection commands
        SelectNominativeCommand = new RelayCommand(SelectNominative);
        SelectAccusativeCommand = new RelayCommand(SelectAccusative);
        SelectInstrumentalCommand = new RelayCommand(SelectInstrumental);
        SelectDativeCommand = new RelayCommand(SelectDative);
        SelectAblativeCommand = new RelayCommand(SelectAblative);
        SelectGenitiveCommand = new RelayCommand(SelectGenitive);
        SelectLocativeCommand = new RelayCommand(SelectLocative);
        SelectVocativeCommand = new RelayCommand(SelectVocative);
        
        _ = Task.Run(LoadNounsAsync);
    }

    public ICommand GoBackCommand { get; }
    
    public ICommand SelectSingularCommand { get; }
    public ICommand SelectPluralCommand { get; }
    
    public ICommand SelectMasculineCommand { get; }
    public ICommand SelectNeuterCommand { get; }
    public ICommand SelectFeminineCommand { get; }
    
    public ICommand SelectNominativeCommand { get; }
    public ICommand SelectAccusativeCommand { get; }
    public ICommand SelectInstrumentalCommand { get; }
    public ICommand SelectDativeCommand { get; }
    public ICommand SelectAblativeCommand { get; }
    public ICommand SelectGenitiveCommand { get; }
    public ICommand SelectLocativeCommand { get; }
    public ICommand SelectVocativeCommand { get; }

    async Task LoadNounsAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            
            await _databaseService.InitializeAsync();
            _nouns = await _databaseService.GetRandomNounsAsync(100); // Load 100 cards
            
            if (_nouns.Count > 0)
            {
                DisplayCurrentCard();
            }
            else
            {
                ErrorMessage = "No nouns found in database";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load nouns");
            ErrorMessage = $"Failed to load data: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    void DisplayCurrentCard()
    {
        if (_nouns.Count == 0) return;
        
        var noun = _nouns[_currentIndex];
        CurrentWord = noun.LemmaClean ?? noun.Lemma1;
        
        // Update rank based on frequency (placeholder logic)
        RankText = noun.EbtCount switch
        {
            > 1000 => "Top-100",
            > 500 => "Top-300", 
            > 200 => "Top-500",
            _ => "Top-1000"
        };
    }

    async Task GoBack()
    {
        await _navigator.NavigateBackAsync(this);
    }

    // Number selection methods
    void SelectSingular()
    {
        IsSingularSelected = true;
        IsPluralSelected = false;
    }

    void SelectPlural()
    {
        IsPluralSelected = true;
        IsSingularSelected = false;
    }

    // Gender selection methods
    void SelectMasculine()
    {
        IsMasculineSelected = true;
        IsNeuterSelected = false;
        IsFeminineSelected = false;
    }

    void SelectNeuter()
    {
        IsNeuterSelected = true;
        IsMasculineSelected = false;
        IsFeminineSelected = false;
    }

    void SelectFeminine()
    {
        IsFeminineSelected = true;
        IsMasculineSelected = false;
        IsNeuterSelected = false;
    }

    // Case selection methods
    void SelectNominative()
    {
        ClearAllCases();
        IsNominativeSelected = true;
    }

    void SelectAccusative()
    {
        ClearAllCases();
        IsAccusativeSelected = true;
    }

    void SelectInstrumental()
    {
        ClearAllCases();
        IsInstrumentalSelected = true;
    }

    void SelectDative()
    {
        ClearAllCases();
        IsDativeSelected = true;
    }

    void SelectAblative()
    {
        ClearAllCases();
        IsAblativeSelected = true;
    }

    void SelectGenitive()
    {
        ClearAllCases();
        IsGenitiveSelected = true;
    }

    void SelectLocative()
    {
        ClearAllCases();
        IsLocativeSelected = true;
    }

    void SelectVocative()
    {
        ClearAllCases();
        IsVocativeSelected = true;
    }

    void ClearAllCases()
    {
        IsNominativeSelected = false;
        IsAccusativeSelected = false;
        IsInstrumentalSelected = false;
        IsDativeSelected = false;
        IsAblativeSelected = false;
        IsGenitiveSelected = false;
        IsLocativeSelected = false;
        IsVocativeSelected = false;
    }
}
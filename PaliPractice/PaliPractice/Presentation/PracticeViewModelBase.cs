using PaliPractice.Services;
using System.Windows.Input;

namespace PaliPractice.Presentation;

public abstract partial class PracticeViewModelBase : ObservableObject
{
    protected readonly IDatabaseService _databaseService;
    protected readonly ILogger _logger;
    protected readonly INavigator _navigator;
    protected List<Headword> _words = new();
    protected int _currentIndex = 0;

    [ObservableProperty] string currentWord = string.Empty;
    [ObservableProperty] string rankText = "Top-100";
    [ObservableProperty] string ankiState = "Anki state: 6/10";
    [ObservableProperty] string usageExample = string.Empty;
    [ObservableProperty] string suttaReference = string.Empty;
    [ObservableProperty] string dailyGoalText = "25/50";
    [ObservableProperty] double dailyProgress = 50.0;
    
    [ObservableProperty] bool isLoading = true;
    [ObservableProperty] string errorMessage = string.Empty;
    
    // Number selection (shared between nouns and verbs)
    [ObservableProperty] bool isSingularSelected = false;
    [ObservableProperty] bool isPluralSelected = false;

    public PracticeViewModelBase(
        IDatabaseService databaseService, 
        ILogger logger,
        INavigator navigator)
    {
        _databaseService = databaseService;
        _logger = logger;
        _navigator = navigator;
        
        // Navigation command
        GoBackCommand = new AsyncRelayCommand(GoBack);
        
        // Number selection commands
        SelectSingularCommand = new RelayCommand(SelectSingular);
        SelectPluralCommand = new RelayCommand(SelectPlural);
    }

    public ICommand GoBackCommand { get; }
    public ICommand SelectSingularCommand { get; }
    public ICommand SelectPluralCommand { get; }

    protected abstract Task LoadWordsAsync();
    protected abstract string GetRankPrefix();

    protected virtual void DisplayCurrentCard()
    {
        if (_words.Count == 0) return;
        
        var word = _words[_currentIndex];
        CurrentWord = word.LemmaClean ?? word.Lemma1;
        
        // Update rank based on frequency
        RankText = word.EbtCount switch
        {
            > 1000 => "Top-100",
            > 500 => "Top-300", 
            > 200 => "Top-500",
            _ => "Top-1000"
        };
        
        // Update usage example (placeholder for now)
        UpdateUsageExample(word);
    }

    protected virtual void UpdateUsageExample(Headword word)
    {
        // This would be populated from actual data
        UsageExample = "Example sentence with " + word.LemmaClean;
        SuttaReference = "AN4.67 sutta reference";
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
        OnSelectionChanged();
    }

    void SelectPlural()
    {
        IsPluralSelected = true;
        IsSingularSelected = false;
        OnSelectionChanged();
    }

    protected virtual void OnSelectionChanged()
    {
        // Override in derived classes to handle selection changes
    }

    public async Task InitializeAsync()
    {
        await LoadWordsAsync();
    }
}
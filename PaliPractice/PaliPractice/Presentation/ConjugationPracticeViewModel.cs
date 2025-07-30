using PaliPractice.Services;
using System.Windows.Input;

namespace PaliPractice.Presentation;

public partial class ConjugationPracticeViewModel : PracticeViewModelBase
{
    // Person selection
    [ObservableProperty] bool isFirstPersonSelected = false;
    [ObservableProperty] bool isSecondPersonSelected = false;
    [ObservableProperty] bool isThirdPersonSelected = false;
    
    // Voice selection
    [ObservableProperty] bool isNormalSelected = true; // Active voice
    [ObservableProperty] bool isReflexiveSelected = false; // Reflexive/middle voice
    
    // Tense/Mood selection
    [ObservableProperty] bool isPresentSelected = false;
    [ObservableProperty] bool isImperativeSelected = false;
    [ObservableProperty] bool isAoristSelected = false;
    [ObservableProperty] bool isOptativeSelected = false;
    [ObservableProperty] bool isFutureSelected = false;
    
    public ConjugationPracticeViewModel(
        IDatabaseService databaseService, 
        ILogger<ConjugationPracticeViewModel> logger,
        INavigator navigator) 
        : base(databaseService, logger, navigator)
    {
        // Person selection commands
        SelectFirstPersonCommand = new RelayCommand(SelectFirstPerson);
        SelectSecondPersonCommand = new RelayCommand(SelectSecondPerson);
        SelectThirdPersonCommand = new RelayCommand(SelectThirdPerson);
        
        // Voice selection commands
        SelectNormalCommand = new RelayCommand(SelectNormal);
        SelectReflexiveCommand = new RelayCommand(SelectReflexive);
        
        // Tense/Mood selection commands
        SelectPresentCommand = new RelayCommand(SelectPresent);
        SelectImperativeCommand = new RelayCommand(SelectImperative);
        SelectAoristCommand = new RelayCommand(SelectAorist);
        SelectOptativeCommand = new RelayCommand(SelectOptative);
        SelectFutureCommand = new RelayCommand(SelectFuture);
        
        _ = Task.Run(LoadWordsAsync);
    }

    // Person commands
    public ICommand SelectFirstPersonCommand { get; }
    public ICommand SelectSecondPersonCommand { get; }
    public ICommand SelectThirdPersonCommand { get; }
    
    // Voice commands
    public ICommand SelectNormalCommand { get; }
    public ICommand SelectReflexiveCommand { get; }
    
    // Tense/Mood commands
    public ICommand SelectPresentCommand { get; }
    public ICommand SelectImperativeCommand { get; }
    public ICommand SelectAoristCommand { get; }
    public ICommand SelectOptativeCommand { get; }
    public ICommand SelectFutureCommand { get; }

    protected override async Task LoadWordsAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            
            await _databaseService.InitializeAsync();
            _words = await _databaseService.GetRandomVerbsAsync(100); // Load 100 verb cards
            
            if (_words.Count > 0)
            {
                DisplayCurrentCard();
            }
            else
            {
                ErrorMessage = "No verbs found in database";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load verbs");
            ErrorMessage = $"Failed to load data: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected override string GetRankPrefix() => "V"; // V for Verb

    protected override void UpdateUsageExample(Headword word)
    {
        // Verb-specific example
        UsageExample = "bhagavā etadavoca: sādhu sādhu, bhikkhu";
        SuttaReference = "MN1 mūlapariyāya";
    }

    // Person selection methods
    void SelectFirstPerson()
    {
        IsFirstPersonSelected = true;
        IsSecondPersonSelected = false;
        IsThirdPersonSelected = false;
        OnSelectionChanged();
    }

    void SelectSecondPerson()
    {
        IsSecondPersonSelected = true;
        IsFirstPersonSelected = false;
        IsThirdPersonSelected = false;
        OnSelectionChanged();
    }

    void SelectThirdPerson()
    {
        IsThirdPersonSelected = true;
        IsFirstPersonSelected = false;
        IsSecondPersonSelected = false;
        OnSelectionChanged();
    }

    // Voice selection methods
    void SelectNormal()
    {
        IsNormalSelected = true;
        IsReflexiveSelected = false;
        OnSelectionChanged();
    }

    void SelectReflexive()
    {
        IsReflexiveSelected = true;
        IsNormalSelected = false;
        OnSelectionChanged();
    }

    // Tense/Mood selection methods
    void SelectPresent()
    {
        ClearAllTenses();
        IsPresentSelected = true;
        OnSelectionChanged();
    }

    void SelectImperative()
    {
        ClearAllTenses();
        IsImperativeSelected = true;
        OnSelectionChanged();
    }

    void SelectAorist()
    {
        ClearAllTenses();
        IsAoristSelected = true;
        OnSelectionChanged();
    }

    void SelectOptative()
    {
        ClearAllTenses();
        IsOptativeSelected = true;
        OnSelectionChanged();
    }

    void SelectFuture()
    {
        ClearAllTenses();
        IsFutureSelected = true;
        OnSelectionChanged();
    }

    void ClearAllTenses()
    {
        IsPresentSelected = false;
        IsImperativeSelected = false;
        IsAoristSelected = false;
        IsOptativeSelected = false;
        IsFutureSelected = false;
    }

    protected override void OnSelectionChanged()
    {
        // Here we would check the answer and provide feedback
        // For now, just log the selection
        base.OnSelectionChanged();
    }
}
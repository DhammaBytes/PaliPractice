using PaliPractice.Presentation.Behaviors;
using System.ComponentModel;
using PaliPractice.Presentation.Providers;
using PaliPractice.Presentation.Shared.ViewModels;

namespace PaliPractice.Presentation;

[Microsoft.UI.Xaml.Data.Bindable]
public partial class ConjugationPracticeViewModel : ObservableObject
{
    readonly IWordProvider _words;
    int _currentIndex;
    readonly ILogger<ConjugationPracticeViewModel> _logger;

    public CardStateBehavior Card { get; }
    public NumberSelection Number { get; }
    public PersonSelection Person { get; }
    public VoiceSelection Voice { get; }
    public TenseSelection Tense { get; }
    public NavigationBehavior Nav { get; }
    [ObservableProperty] bool _canGoToPrevious;
    [ObservableProperty] bool _canGoToNext;

    public ICommand PreviousCommand { get; }
    public ICommand NextCommand { get; }
    
    public ConjugationPracticeViewModel(
        VerbWordProvider words,
        CardStateBehavior card,
        NavigationBehavior nav,
        ILogger<ConjugationPracticeViewModel> logger)
    {
        _words = words;
        Card = card; Nav = nav;
        _logger = logger;
        
        Number = new NumberSelection();
        Person = new PersonSelection();
        Voice = new VoiceSelection();
        Tense = new TenseSelection();
        
        PreviousCommand = new RelayCommand(GoToPrevious, () => CanGoToPrevious);
        NextCommand = new RelayCommand(GoToNext, () => CanGoToNext);
        
        SetupSelectionChangeHandlers();
        _ = InitializeAsync();
    }


    async Task InitializeAsync()
    {
        try
        {
            Card.IsLoading = true;
            await _words.LoadAsync();
            if (_words.Words.Count == 0) { Card.ErrorMessage = "No verbs found in database"; return; }
            Card.DisplayCurrentCard(_words.Words, _currentIndex, SetVerbExamples);
            UpdateNavigationState();
        }
        catch (Exception ex) { _logger.LogError(ex, "Failed to load verbs"); Card.ErrorMessage = $"Failed to load data: {ex.Message}"; }
        finally { Card.IsLoading = false; }
    }

    void SetVerbExamples(Headword w)
    {
        Card.UsageExample = "bhagavā etadavoca: sādhu sādhu, bhikkhu";
        Card.SuttaReference = "MN1 mūlapariyāya";
    }

    bool CanProceedToNext()
    {
        return HasAnyNumberSelected() && HasAnyPersonSelected() && HasAnyVoiceSelected() && HasAnyTenseSelected();
    }

    bool HasAnyNumberSelected() => Number.Selected != Shared.ViewModels.Number.None;
    
    bool HasAnyPersonSelected() => Person.Selected != Shared.ViewModels.Person.None;
    
    bool HasAnyVoiceSelected() => Voice.Selected != Shared.ViewModels.Voice.None;
    
    bool HasAnyTenseSelected() => Tense.Selected != Shared.ViewModels.Tense.None;

    void SetupSelectionChangeHandlers()
    {
        Number.PropertyChanged += OnSelectionChanged;
        Person.PropertyChanged += OnSelectionChanged;
        Voice.PropertyChanged += OnSelectionChanged;
        Tense.PropertyChanged += OnSelectionChanged;
    }

    void OnSelectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateNavigationState();
    }

    void GoToPrevious()
    {
        if (_currentIndex > 0)
        {
            _currentIndex--;
            Card.DisplayCurrentCard(_words.Words, _currentIndex, SetVerbExamples);
            UpdateNavigationState();
        }
    }

    void GoToNext()
    {
        if (_currentIndex < _words.Words.Count - 1)
        {
            _currentIndex++;
            Card.DisplayCurrentCard(_words.Words, _currentIndex, SetVerbExamples);
            UpdateNavigationState();
        }
    }

    void UpdateNavigationState()
    {
        CanGoToPrevious = _currentIndex > 0;
        CanGoToNext = _currentIndex < _words.Words.Count - 1 && CanProceedToNext();
        
        ((RelayCommand)PreviousCommand).NotifyCanExecuteChanged();
        ((RelayCommand)NextCommand).NotifyCanExecuteChanged();
    }
}

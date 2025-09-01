using System.ComponentModel;
using PaliPractice.Presentation.Providers;
using PaliPractice.Presentation.ViewModels.ButtonGroups;
using CardViewModel = PaliPractice.Presentation.ViewModels.CardViewModel;

namespace PaliPractice.Presentation;

[Microsoft.UI.Xaml.Data.Bindable]
public partial class ConjugationPracticeViewModel : ObservableObject
{
    readonly IWordProvider _words;
    int _currentIndex;
    readonly ILogger<ConjugationPracticeViewModel> _logger;

    public CardViewModel Card { get; }
    public NumberButtonGroupViewModel Number { get; }
    public PersonButtonGroupViewModel Person { get; }
    public VoiceButtonGroupViewModel Voice { get; }
    public TenseButtonGroupViewModel Tense { get; }
    readonly INavigator _navigator;
    [ObservableProperty] bool _canGoToPrevious;
    [ObservableProperty] bool _canGoToNext;

    public ICommand GoBackCommand { get; }
    public ICommand PreviousCommand { get; }
    public ICommand NextCommand { get; }
    
    public ConjugationPracticeViewModel(
        [FromKeyedServices("verb")] IWordProvider words,
        CardViewModel card,
        INavigator navigator,
        ILogger<ConjugationPracticeViewModel> logger)
    {
        _words = words;
        Card = card; 
        _navigator = navigator;
        _logger = logger;
        
        Number = new NumberButtonGroupViewModel();
        Person = new PersonButtonGroupViewModel();
        Voice = new VoiceButtonGroupViewModel();
        Tense = new TenseButtonGroupViewModel();
        
        GoBackCommand = new AsyncRelayCommand(GoBack);
        PreviousCommand = new RelayCommand(GoToPrevious, () => CanGoToPrevious);
        NextCommand = new RelayCommand(GoToNext, () => CanGoToNext);
        
        SetupSelectionChangeHandlers();
    }


    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Card.IsLoading = true;
            await _words.LoadAsync(cancellationToken);
            if (_words.Words.Count == 0) { Card.ErrorMessage = "No verbs found in database"; return; }
            Card.DisplayCurrentCard(_words.Words, _currentIndex, SetVerbExamples);
            UpdateNavigationState();
        }
        catch (OperationCanceledException) { /* Expected during navigation */ }
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

    bool HasAnyNumberSelected() => Number.Selected != Models.Number.None;
    
    bool HasAnyPersonSelected() => Person.Selected != Models.Person.None;
    
    bool HasAnyVoiceSelected() => Voice.Selected != Models.Voice.None;
    
    bool HasAnyTenseSelected() => Tense.Selected != Models.Tense.None;

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

    async Task GoBack()
    {
        await _navigator.NavigateBackAsync(this);
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

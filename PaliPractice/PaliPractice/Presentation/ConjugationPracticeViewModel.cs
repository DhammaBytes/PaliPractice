using System.ComponentModel;
using PaliPractice.Presentation.Providers;
using PaliPractice.Presentation.ViewModels;
using CardViewModel = PaliPractice.Presentation.ViewModels.CardViewModel;

namespace PaliPractice.Presentation;

[Microsoft.UI.Xaml.Data.Bindable]
public partial class ConjugationPracticeViewModel : ObservableObject
{
    readonly IWordProvider _words;
    int _currentIndex;
    readonly ILogger<ConjugationPracticeViewModel> _logger;

    public CardViewModel Card { get; }
    public EnumChoiceViewModel<Number> Number { get; }
    public EnumChoiceViewModel<Person> Person { get; }
    public EnumChoiceViewModel<Voice> Voice { get; }
    public EnumChoiceViewModel<Tense> Tense { get; }
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
        
        Number = new EnumChoiceViewModel<Number>(new[] {
            new EnumOption<Number>(Models.Number.Singular, "Singular", "\uE77B", Color.FromArgb(255, 230, 230, 255)),
            new EnumOption<Number>(Models.Number.Plural, "Plural", "\uE716", Color.FromArgb(255, 230, 230, 255))
        }, Models.Number.None);
        
        Person = new EnumChoiceViewModel<Person>(new[] {
            new EnumOption<Person>(Models.Person.First, "1st", "\uE77B", Color.FromArgb(255, 230, 255, 230)),
            new EnumOption<Person>(Models.Person.Second, "2nd", "\uE748", Color.FromArgb(255, 230, 255, 230)),
            new EnumOption<Person>(Models.Person.Third, "3rd", "\uE716", Color.FromArgb(255, 230, 255, 230))
        }, Models.Person.None);
        
        Voice = new EnumChoiceViewModel<Voice>(new[] {
            new EnumOption<Voice>(Models.Voice.Normal, "Active", "\uE768", Color.FromArgb(255, 255, 230, 230)),
            new EnumOption<Voice>(Models.Voice.Reflexive, "Reflexive", "\uE74C", Color.FromArgb(255, 255, 230, 230))
        }, Models.Voice.None);
        
        Tense = new EnumChoiceViewModel<Tense>(new[] {
            new EnumOption<Tense>(Models.Tense.Present, "Present", null, Color.FromArgb(255, 240, 240, 255)),
            new EnumOption<Tense>(Models.Tense.Imperative, "Imper.", null, Color.FromArgb(255, 240, 240, 255)),
            new EnumOption<Tense>(Models.Tense.Aorist, "Aorist", null, Color.FromArgb(255, 240, 240, 255)),
            new EnumOption<Tense>(Models.Tense.Optative, "Opt.", null, Color.FromArgb(255, 240, 240, 255)),
            new EnumOption<Tense>(Models.Tense.Future, "Future", null, Color.FromArgb(255, 240, 240, 255))
        }, Models.Tense.None);
        
        GoBackCommand = new AsyncRelayCommand(GoBack);
        PreviousCommand = new RelayCommand(GoToPrevious, () => CanGoToPrevious);
        NextCommand = new RelayCommand(GoToNext, () => CanGoToNext);
        
        SetupSelectionChangeHandlers();
        _ = InitializeAsync();
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

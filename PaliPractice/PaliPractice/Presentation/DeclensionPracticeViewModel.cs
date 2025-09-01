using PaliPractice.Presentation.Behaviors;
using System.ComponentModel;
using PaliPractice.Presentation.Providers;
using PaliPractice.Presentation.Shared.ViewModels;

namespace PaliPractice.Presentation;

[Microsoft.UI.Xaml.Data.Bindable]
public partial class DeclensionPracticeViewModel : ObservableObject
{
    readonly IWordProvider _words;
    int _currentIndex;
    readonly ILogger<DeclensionPracticeViewModel> _logger;

    public CardStateBehavior Card { get; }
    public NumberSelection Number { get; }
    public GenderSelection Gender { get; }
    public CaseSelection Cases { get; }
    public NavigationBehavior Nav { get; }
    [ObservableProperty] bool _canGoToPrevious;
    [ObservableProperty] bool _canGoToNext;

    public ICommand PreviousCommand { get; }
    public ICommand NextCommand { get; }
    
    public DeclensionPracticeViewModel(
        NounWordProvider words,
        CardStateBehavior card,
        NavigationBehavior nav,
        ILogger<DeclensionPracticeViewModel> logger)
    {
        _words = words;
        Card = card; Nav = nav;
        _logger = logger;
        
        Number = new NumberSelection();
        Gender = new GenderSelection();
        Cases = new CaseSelection();
        
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
            if (_words.Words.Count == 0) { Card.ErrorMessage = "No nouns found in database"; return; }
            Card.DisplayCurrentCard(_words.Words, _currentIndex, SetNounExamples);
            UpdateNavigationState();
        }
        catch (Exception ex) { _logger.LogError(ex, "Failed to load nouns"); Card.ErrorMessage = $"Failed to load data: {ex.Message}"; }
        finally { Card.IsLoading = false; }
    }

    void SetNounExamples(Headword w)
    {
        Card.UsageExample = "virūp'akkhehi me mettaṃ, mettaṃ erāpathehi me";
        Card.SuttaReference = "AN4.67 ahirājasuttaṃ";
    }

    bool CanProceedToNext()
    {
        return HasAnyNumberSelected() && HasAnyGenderSelected() && HasAnyCaseSelected();
    }

    bool HasAnyNumberSelected() => Number.Selected != Shared.ViewModels.Number.None;
    
    bool HasAnyGenderSelected() => Gender.Selected != Shared.ViewModels.Gender.None;
    
    bool HasAnyCaseSelected() => Cases.Selected != NounCase.None;

    void SetupSelectionChangeHandlers()
    {
        Number.PropertyChanged += OnSelectionChanged;
        Gender.PropertyChanged += OnSelectionChanged;
        Cases.PropertyChanged += OnSelectionChanged;
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
            Card.DisplayCurrentCard(_words.Words, _currentIndex, SetNounExamples);
            UpdateNavigationState();
        }
    }

    void GoToNext()
    {
        if (_currentIndex < _words.Words.Count - 1)
        {
            _currentIndex++;
            Card.DisplayCurrentCard(_words.Words, _currentIndex, SetNounExamples);
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

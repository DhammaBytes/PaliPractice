using System.ComponentModel;
using PaliPractice.Presentation.Providers;
using PaliPractice.Presentation.ViewModels;

namespace PaliPractice.Presentation;

[Microsoft.UI.Xaml.Data.Bindable]
public partial class DeclensionPracticeViewModel : ObservableObject
{
    readonly IWordProvider _words;
    int _currentIndex;
    readonly ILogger<DeclensionPracticeViewModel> _logger;

    public CardViewModel Card { get; }
    public EnumChoiceViewModel<Number> Number { get; }
    public EnumChoiceViewModel<Gender> Gender { get; }
    public EnumChoiceViewModel<NounCase> Cases { get; }
    readonly INavigator _navigator;
    [ObservableProperty] bool _canGoToPrevious;
    [ObservableProperty] bool _canGoToNext;

    public ICommand GoBackCommand { get; }
    public ICommand PreviousCommand { get; }
    public ICommand NextCommand { get; }
    
    public DeclensionPracticeViewModel(
        [FromKeyedServices("noun")] IWordProvider words,
        CardViewModel card,
        INavigator navigator,
        ILogger<DeclensionPracticeViewModel> logger)
    {
        _words = words;
        Card = card; 
        _navigator = navigator;
        _logger = logger;
        
        Number = new EnumChoiceViewModel<Number>(new[] {
            new EnumOption<Number>(Models.Number.Singular, "Singular", "\uE77B", Color.FromArgb(255, 230, 230, 255)),
            new EnumOption<Number>(Models.Number.Plural, "Plural", "\uE716", Color.FromArgb(255, 230, 230, 255))
        }, Models.Number.None);
        
        Gender = new EnumChoiceViewModel<Gender>(new[] {
            new EnumOption<Gender>(Models.Gender.Masculine, "Masculine", "\uE71A", Color.FromArgb(255, 220, 255, 220)),
            new EnumOption<Gender>(Models.Gender.Neuter, "Neuter", "\uE734", Color.FromArgb(255, 220, 255, 220)),
            new EnumOption<Gender>(Models.Gender.Feminine, "Feminine", "\uE716", Color.FromArgb(255, 220, 255, 220))
        }, Models.Gender.None);
        
        Cases = new EnumChoiceViewModel<NounCase>(new[] {
            new EnumOption<NounCase>(NounCase.Nominative, "Nom", null, Color.FromArgb(255, 255, 243, 224)),
            new EnumOption<NounCase>(NounCase.Accusative, "Acc", null, Color.FromArgb(255, 255, 243, 224)),
            new EnumOption<NounCase>(NounCase.Instrumental, "Ins", null, Color.FromArgb(255, 255, 243, 224)),
            new EnumOption<NounCase>(NounCase.Dative, "Dat", null, Color.FromArgb(255, 255, 243, 224)),
            new EnumOption<NounCase>(NounCase.Ablative, "Abl", null, Color.FromArgb(255, 255, 243, 224)),
            new EnumOption<NounCase>(NounCase.Genitive, "Gen", null, Color.FromArgb(255, 255, 243, 224)),
            new EnumOption<NounCase>(NounCase.Locative, "Loc", null, Color.FromArgb(255, 255, 243, 224)),
            new EnumOption<NounCase>(NounCase.Vocative, "Voc", null, Color.FromArgb(255, 255, 243, 224))
        }, NounCase.None);
        
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
            if (_words.Words.Count == 0) { Card.ErrorMessage = "No nouns found in database"; return; }
            Card.DisplayCurrentCard(_words.Words, _currentIndex, SetNounExamples);
            UpdateNavigationState();
        }
        catch (OperationCanceledException) { /* Expected during navigation */ }
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

    bool HasAnyNumberSelected() => Number.Selected != Models.Number.None;
    
    bool HasAnyGenderSelected() => Gender.Selected != Models.Gender.None;
    
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

    async Task GoBack()
    {
        await _navigator.NavigateBackAsync(this);
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

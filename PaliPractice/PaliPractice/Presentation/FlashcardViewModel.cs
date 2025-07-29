using PaliPractice.Services;
using System.Windows.Input;

namespace PaliPractice.Presentation;

public partial class FlashcardViewModel : ObservableObject
{
    readonly IDatabaseService _databaseService;
    readonly ILogger<FlashcardViewModel> _logger;
    List<Headword> _nouns = new();
    int _currentIndex = 0;

    [ObservableProperty] string currentNoun = string.Empty;
    [ObservableProperty] string currentTranslation = string.Empty;
    [ObservableProperty] bool isTranslationVisible = false;
    [ObservableProperty] bool isLoading = true;
    [ObservableProperty] string errorMessage = string.Empty;
    [ObservableProperty] string posInfo = string.Empty;
    
    public FlashcardViewModel(IDatabaseService databaseService, ILogger<FlashcardViewModel> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
        
        RevealCommand = new AsyncRelayCommand(RevealTranslation);
        NextCommand = new AsyncRelayCommand(NextCard);
        PreviousCommand = new AsyncRelayCommand(PreviousCard);
        
        Title = "Pali Noun Flashcards";
        
        _ = Task.Run(LoadNounsAsync);
    }

    public string Title { get; }
    public ICommand RevealCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand PreviousCommand { get; }

    async Task LoadNounsAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            
            await _databaseService.InitializeAsync();
            _nouns = await _databaseService.GetRandomNounsAsync(50); // Load 50 cards
            
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
        CurrentNoun = noun.LemmaClean ?? noun.Lemma1;
        CurrentTranslation = noun.Meaning1 ?? "No translation available";
        PosInfo = $"{noun.Pos} • Frequency: {noun.EbtCount}";
        IsTranslationVisible = false;
    }

    Task RevealTranslation()
    {
        IsTranslationVisible = true;
        return Task.CompletedTask;
    }

    Task NextCard()
    {
        if (_nouns.Count == 0) return Task.CompletedTask;
        
        _currentIndex = (_currentIndex + 1) % _nouns.Count;
        DisplayCurrentCard();
        return Task.CompletedTask;
    }

    Task PreviousCard()
    {
        if (_nouns.Count == 0) return Task.CompletedTask;
        
        _currentIndex = _currentIndex == 0 ? _nouns.Count - 1 : _currentIndex - 1;
        DisplayCurrentCard();
        return Task.CompletedTask;
    }
}
namespace PaliPractice.Presentation.Practice.Controls;

/// <summary>
/// Manages carousel navigation through unique translations for a lemma.
/// Arrows cycle through translations (not examples).
/// Reference is always visible; translation+arrows hidden until revealed.
/// </summary>
[Bindable]
public partial class ExampleCarouselViewModel : ObservableObject
{
    static readonly Random _random = new();

    IReadOnlyList<TranslationEntry> _entries = [];

    [ObservableProperty] int _currentIndex;
    [ObservableProperty] int _totalTranslations;
    [ObservableProperty] string _currentMeaning = string.Empty;
    [ObservableProperty] string _currentReference = string.Empty;
    [ObservableProperty] string _currentExample = string.Empty;
    [ObservableProperty] bool _hasMultipleTranslations;
    [ObservableProperty] string _paginationText = string.Empty;
    [ObservableProperty] bool _isRevealed;

    /// <summary>
    /// Initialize carousel with translations from all words under a lemma.
    /// Picks a random starting translation.
    /// </summary>
    public void Initialize(ILemma lemma)
    {
        _entries = TranslationEntry.BuildFromLemma(lemma);

        TotalTranslations = _entries.Count;
        HasMultipleTranslations = TotalTranslations > 1;

        // Pick random starting translation
        CurrentIndex = TotalTranslations > 1 ? _random.Next(TotalTranslations) : 0;

        UpdateCurrentDisplay();
    }

    void UpdateCurrentDisplay()
    {
        if (_entries.Count == 0)
        {
            CurrentMeaning = string.Empty;
            CurrentReference = string.Empty;
            CurrentExample = string.Empty;
            PaginationText = string.Empty;
            return;
        }

        var entry = _entries[CurrentIndex];
        CurrentMeaning = entry.Meaning;
        CurrentReference = entry.CurrentExample.Reference;
        CurrentExample = entry.CurrentExample.Example;
        PaginationText = $"{CurrentIndex + 1} of {TotalTranslations}";
    }

    [RelayCommand]
    void Previous()
    {
        if (_entries.Count == 0) return;
        CurrentIndex = (CurrentIndex - 1 + _entries.Count) % _entries.Count;
        // Shuffle reference for the new translation
        _entries[CurrentIndex].ShuffleReference();
        UpdateCurrentDisplay();
    }

    [RelayCommand]
    void Next()
    {
        if (_entries.Count == 0) return;
        CurrentIndex = (CurrentIndex + 1) % _entries.Count;
        // Shuffle reference for the new translation
        _entries[CurrentIndex].ShuffleReference();
        UpdateCurrentDisplay();
    }

    /// <summary>
    /// Gets the current word (for inflection generation).
    /// </summary>
    public IWord? CurrentWord => _entries.Count > 0 ? _entries[CurrentIndex].CurrentExample.Word : null;

    /// <summary>
    /// Reset with a new random translation and hide the answer.
    /// </summary>
    public void Reset()
    {
        IsRevealed = false;

        if (_entries.Count == 0) return;

        // Pick new random translation
        CurrentIndex = _entries.Count > 1 ? _random.Next(_entries.Count) : 0;
        // Shuffle reference for the selected translation
        _entries[CurrentIndex].ShuffleReference();
        UpdateCurrentDisplay();
    }
}

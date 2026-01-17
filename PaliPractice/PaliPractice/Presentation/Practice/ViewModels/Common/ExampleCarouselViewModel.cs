using Microsoft.UI.Xaml.Media;
using PaliPractice.Models.Words;
using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Practice.Common;
using PaliPractice.Presentation.Practice.ViewModels.Common.Entries;
using PaliPractice.Themes;

namespace PaliPractice.Presentation.Practice.ViewModels.Common;

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
    IReadOnlyList<string> _formsToAvoid = [];
    string _rawMeaning = string.Empty;

    // Text balancing parameters (set by UI layer)
    double _availableWidth;
    double _fontSize = 18;
    FontFamily _fontFamily = new(FontPaths.SourceSans);

    [ObservableProperty] int _currentIndex;
    [ObservableProperty] int _totalTranslations;
    [ObservableProperty] string _currentMeaning = string.Empty;
    [ObservableProperty] string _currentReference = string.Empty;
    [ObservableProperty] string _currentExample = string.Empty;
    [ObservableProperty] bool _hasMultipleTranslations;
    [ObservableProperty] string _paginationText = string.Empty;
    [ObservableProperty] bool _isRevealed;

    /// <summary>
    /// Initialize carousel with translations from a lemma (all word variants).
    /// </summary>
    public void Initialize(ILemma lemma) => Initialize(lemma.Words);

    /// <summary>
    /// Initialize carousel with translations from word variants (extracts Details from each).
    /// </summary>
    public void Initialize(IEnumerable<IWord> words)
    {
        var allDetails = words
            .Select(w => w.Details)
            .Where(d => d != null)
            .Cast<IWordDetails>();
        InitializeEntries(TranslationEntry.BuildFromAllDetails(allDetails));
    }

    void InitializeEntries(IReadOnlyList<TranslationEntry> entries)
    {
        _entries = entries;
        _formsToAvoid = [];

        TotalTranslations = _entries.Count;
        HasMultipleTranslations = TotalTranslations > 1;

        // Pick random starting translation
        CurrentIndex = TotalTranslations > 1 ? _random.Next(TotalTranslations) : 0;

        UpdateCurrentDisplay();
    }

    /// <summary>
    /// Sets the inflected forms to avoid when picking examples.
    /// Call this after Initialize and before the example is displayed.
    /// </summary>
    public void SetFormsToAvoid(IReadOnlyList<string> forms)
    {
        _formsToAvoid = forms;
        // Re-pick example for current translation with the new filter
        if (_entries.Count > 0)
        {
            _entries[CurrentIndex].ShuffleReference(_formsToAvoid);
            UpdateCurrentDisplay();
        }
    }

    /// <summary>
    /// Updates the available width for text balancing.
    /// Call this when the translation container width changes.
    /// </summary>
    public void UpdateAvailableWidth(double width, double fontSize, FontFamily fontFamily)
    {
        _availableWidth = width;
        _fontSize = fontSize;
        _fontFamily = fontFamily;
        RebalanceMeaning();
    }

    void UpdateCurrentDisplay()
    {
        if (_entries.Count == 0)
        {
            _rawMeaning = string.Empty;
            CurrentMeaning = string.Empty;
            CurrentReference = string.Empty;
            CurrentExample = string.Empty;
            PaginationText = string.Empty;
            return;
        }

        var entry = _entries[CurrentIndex];
        _rawMeaning = entry.Meaning;
        RebalanceMeaning();
        CurrentReference = entry.CurrentExample.Reference;
        CurrentExample = entry.CurrentExample.Example;
        PaginationText = $"{CurrentIndex + 1} of {TotalTranslations}";
    }

    void RebalanceMeaning()
    {
        if (_availableWidth > 0 && !string.IsNullOrEmpty(_rawMeaning))
            CurrentMeaning = TextBalancer.Balance(_rawMeaning, _availableWidth, _fontSize, _fontFamily);
        else
            CurrentMeaning = _rawMeaning;
    }

    [RelayCommand]
    void Previous()
    {
        if (_entries.Count == 0) return;
        CurrentIndex = (CurrentIndex - 1 + _entries.Count) % _entries.Count;
        // Shuffle reference for the new translation, avoiding answer forms
        _entries[CurrentIndex].ShuffleReference(_formsToAvoid);
        UpdateCurrentDisplay();
    }

    [RelayCommand]
    void Next()
    {
        if (_entries.Count == 0) return;
        CurrentIndex = (CurrentIndex + 1) % _entries.Count;
        // Shuffle reference for the new translation, avoiding answer forms
        _entries[CurrentIndex].ShuffleReference(_formsToAvoid);
        UpdateCurrentDisplay();
    }

    /// <summary>
    /// Reset with a new random translation and hide the answer.
    /// </summary>
    public void Reset()
    {
        IsRevealed = false;

        if (_entries.Count == 0) return;

        // Pick new random translation
        CurrentIndex = _entries.Count > 1 ? _random.Next(_entries.Count) : 0;
        // Shuffle reference for the selected translation, avoiding answer forms
        _entries[CurrentIndex].ShuffleReference(_formsToAvoid);
        UpdateCurrentDisplay();
    }
}

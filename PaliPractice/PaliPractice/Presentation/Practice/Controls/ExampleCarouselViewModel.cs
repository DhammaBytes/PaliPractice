namespace PaliPractice.Presentation.Practice.Controls;

/// <summary>
/// Manages carousel navigation through all examples from all words under a lemma.
/// Updates translation display when carousel index changes.
/// </summary>
[Bindable]
public partial class ExampleCarouselViewModel : ObservableObject
{
    List<ExampleEntry> _entries = [];

    [ObservableProperty] int _currentExampleIndex;
    [ObservableProperty] int _totalExamples;
    [ObservableProperty] string _currentExample = string.Empty;
    [ObservableProperty] string _currentReference = string.Empty;
    [ObservableProperty] string _currentMeaning = string.Empty;
    [ObservableProperty] bool _hasMultipleExamples;
    [ObservableProperty] string _paginationText = string.Empty;

    /// <summary>
    /// Initialize carousel with all examples from all words under a lemma.
    /// </summary>
    public void Initialize(ILemma lemma)
    {
        _entries = BuildExampleEntries(lemma.Words).ToList();

        TotalExamples = _entries.Count;
        HasMultipleExamples = TotalExamples > 1;
        CurrentExampleIndex = 0;

        UpdateCurrentDisplay();
    }

    static IEnumerable<ExampleEntry> BuildExampleEntries(IReadOnlyList<IWord> words)
    {
        foreach (var word in words)
        {
            if (!string.IsNullOrEmpty(word.Example1))
                yield return new ExampleEntry(word, 0);
            if (!string.IsNullOrEmpty(word.Example2))
                yield return new ExampleEntry(word, 1);
        }
    }

    void UpdateCurrentDisplay()
    {
        if (_entries.Count == 0)
        {
            CurrentExample = string.Empty;
            CurrentReference = string.Empty;
            CurrentMeaning = string.Empty;
            PaginationText = string.Empty;
            return;
        }

        var entry = _entries[CurrentExampleIndex];
        CurrentExample = entry.Example;
        CurrentReference = entry.Reference;
        CurrentMeaning = entry.Meaning ?? string.Empty;
        PaginationText = $"{CurrentExampleIndex + 1} of {TotalExamples}";
    }

    [RelayCommand]
    void Previous()
    {
        if (_entries.Count == 0) return;
        CurrentExampleIndex = (CurrentExampleIndex - 1 + _entries.Count) % _entries.Count;
        UpdateCurrentDisplay();
    }

    [RelayCommand]
    void Next()
    {
        if (_entries.Count == 0) return;
        CurrentExampleIndex = (CurrentExampleIndex + 1) % _entries.Count;
        UpdateCurrentDisplay();
    }

    /// <summary>
    /// Gets the current word (for inflection generation).
    /// </summary>
    public IWord? CurrentWord => _entries.Count > 0 ? _entries[CurrentExampleIndex].Word : null;

    /// <summary>
    /// Reset to first example.
    /// </summary>
    public void Reset()
    {
        CurrentExampleIndex = 0;
        if (_entries.Count > 0)
            UpdateCurrentDisplay();
    }
}

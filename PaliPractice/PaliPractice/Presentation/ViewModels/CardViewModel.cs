namespace PaliPractice.Presentation.ViewModels;

[Bindable]
public partial class CardViewModel : ObservableObject
{
    [ObservableProperty] string _currentWord = string.Empty;
    [ObservableProperty] string _rankText = "Top-100";
    [ObservableProperty] string _ankiState = "Anki state: 6/10";
    [ObservableProperty] string _usageExample = string.Empty;
    [ObservableProperty] string _suttaReference = string.Empty;
    [ObservableProperty] string _dailyGoalText = "25/50";
    [ObservableProperty] double _dailyProgress = 50.0;
    [ObservableProperty] bool _isLoading = true;
    [ObservableProperty] string _errorMessage = string.Empty;

    public void DisplayCurrentCard(IReadOnlyList<Headword> words, int index, Action<Headword>? setExamples = null)
    {
        if (words.Count == 0) return;
        var word = words[index];
        CurrentWord = word.LemmaClean ?? word.Lemma1;
        RankText = word.EbtCount switch { > 1000 => "Top-100", > 500 => "Top-300", > 200 => "Top-500", _ => "Top-1000" };
        setExamples?.Invoke(word);
    }
}

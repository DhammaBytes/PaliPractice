namespace PaliPractice.Presentation.Behaviors;

[Bindable]
public partial class CardStateBehavior : ObservableObject
{
    [ObservableProperty] string currentWord = string.Empty;
    [ObservableProperty] string rankText = "Top-100";
    [ObservableProperty] string ankiState = "Anki state: 6/10";
    [ObservableProperty] string usageExample = string.Empty;
    [ObservableProperty] string suttaReference = string.Empty;
    [ObservableProperty] string dailyGoalText = "25/50";
    [ObservableProperty] double dailyProgress = 50.0;
    [ObservableProperty] bool isLoading = true;
    [ObservableProperty] string errorMessage = string.Empty;

    public void DisplayCurrentCard(List<Headword> words, int index, Action<Headword>? setExamples = null)
    {
        if (words.Count == 0) return;
        var word = words[index];
        CurrentWord = word.LemmaClean ?? word.Lemma1;
        RankText = word.EbtCount switch { > 1000 => "Top-100", > 500 => "Top-300", > 200 => "Top-500", _ => "Top-1000" };
        setExamples?.Invoke(word);
    }
}
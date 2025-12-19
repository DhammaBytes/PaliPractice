namespace PaliPractice.Presentation.Practice.Controls;

[Bindable]
public partial class CardViewModel : ObservableObject
{
    [ObservableProperty] string _currentWord = string.Empty;
    [ObservableProperty] string _rankText = "Top-100";
    [ObservableProperty] string _ankiState = "Anki state: 6/10";
    [ObservableProperty] bool _isLoading = true;
    [ObservableProperty] string _errorMessage = string.Empty;

    public void DisplayCurrentCard(ILemma lemma)
    {
        CurrentWord = lemma.LemmaClean;
        RankText = lemma.EbtCount switch
        {
            > 1000 => "Top-100",
            > 500 => "Top-300",
            > 200 => "Top-500",
            _ => "Top-1000"
        };
    }
}

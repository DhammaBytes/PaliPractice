using PaliPractice.Presentation.Behaviors;

namespace PaliPractice.Presentation;

[Bindable]
public class DeclensionPracticeViewModel : ObservableObject
{
    readonly IWordSource _words;
    readonly ILogger<DeclensionPracticeViewModel> _logger;

    public CardStateBehavior Card { get; }
    public NumberSelectionBehavior Number { get; }
    public GenderSelectionBehavior Gender { get; }
    public CaseSelectionBehavior Cases { get; }
    public NavigationBehavior Nav { get; }
    
    public DeclensionPracticeViewModel(
        NounWordSource words,
        CardStateBehavior card,
        NumberSelectionBehavior number,
        GenderSelectionBehavior gender,
        CaseSelectionBehavior @case,
        NavigationBehavior nav,
        ILogger<DeclensionPracticeViewModel> logger)
    {
        _words = words;
        Card = card; Number = number; Gender = gender; Cases = @case; Nav = nav;
        _logger = logger;
        _ = InitializeAsync();
    }


    async Task InitializeAsync()
    {
        try
        {
            Card.IsLoading = true;
            await _words.LoadAsync();
            if (_words.Words.Count == 0) { Card.ErrorMessage = "No nouns found in database"; return; }
            Card.DisplayCurrentCard(_words.Words, _words.CurrentIndex, SetNounExamples);
        }
        catch (Exception ex) { _logger.LogError(ex, "Failed to load nouns"); Card.ErrorMessage = $"Failed to load data: {ex.Message}"; }
        finally { Card.IsLoading = false; }
    }

    void SetNounExamples(Headword w)
    {
        Card.UsageExample = "virūp'akkhehi me mettaṃ, mettaṃ erāpathehi me";
        Card.SuttaReference = "AN4.67 ahirājasuttaṃ";
    }

}

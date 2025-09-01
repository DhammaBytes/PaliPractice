using PaliPractice.Presentation.Behaviors;
using System.ComponentModel;
using PaliPractice.Presentation.Behaviors.Selection;
using PaliPractice.Presentation.Behaviors.Sources;

namespace PaliPractice.Presentation;

[Microsoft.UI.Xaml.Data.Bindable]
public class ConjugationPracticeViewModel : ObservableObject
{
    readonly IWordSource _words;
    readonly ILogger<ConjugationPracticeViewModel> _logger;

    public CardStateBehavior Card { get; }
    public NumberSelectionBehavior Number { get; }
    public PersonSelectionBehavior Person { get; }
    public VoiceSelectionBehavior Voice { get; }
    public TenseSelectionBehavior Tense { get; }
    public NavigationBehavior Nav { get; }
    public CardNavigationBehavior CardNavigation { get; }
    
    public ConjugationPracticeViewModel(
        VerbWordSource words,
        CardStateBehavior card,
        NumberSelectionBehavior number,
        PersonSelectionBehavior person,
        VoiceSelectionBehavior voice,
        TenseSelectionBehavior tense,
        NavigationBehavior nav,
        ILogger<ConjugationPracticeViewModel> logger)
    {
        _words = words;
        Card = card; Number = number; Person = person; Voice = voice; Tense = tense; Nav = nav;
        _logger = logger;
        
        CardNavigation = new CardNavigationBehavior(words, card, SetVerbExamples, CanProceedToNext);
        
        SetupSelectionChangeHandlers();
        _ = InitializeAsync();
    }


    async Task InitializeAsync()
    {
        try
        {
            Card.IsLoading = true;
            await _words.LoadAsync();
            if (_words.Words.Count == 0) { Card.ErrorMessage = "No verbs found in database"; return; }
            Card.DisplayCurrentCard(_words.Words, _words.CurrentIndex, SetVerbExamples);
        }
        catch (Exception ex) { _logger.LogError(ex, "Failed to load verbs"); Card.ErrorMessage = $"Failed to load data: {ex.Message}"; }
        finally { Card.IsLoading = false; }
    }

    void SetVerbExamples(Headword w)
    {
        Card.UsageExample = "bhagavā etadavoca: sādhu sādhu, bhikkhu";
        Card.SuttaReference = "MN1 mūlapariyāya";
    }

    bool CanProceedToNext()
    {
        return HasAnyNumberSelected() && HasAnyPersonSelected() && HasAnyVoiceSelected() && HasAnyTenseSelected();
    }

    bool HasAnyNumberSelected() => Number.IsSingularSelected || Number.IsPluralSelected;
    
    bool HasAnyPersonSelected() => Person.IsFirstPersonSelected || Person.IsSecondPersonSelected || Person.IsThirdPersonSelected;
    
    bool HasAnyVoiceSelected() => Voice.IsNormalSelected || Voice.IsReflexiveSelected;
    
    bool HasAnyTenseSelected() => Tense.IsPresentSelected || Tense.IsImperativeSelected || Tense.IsAoristSelected || 
                                 Tense.IsOptativeSelected || Tense.IsFutureSelected;

    void SetupSelectionChangeHandlers()
    {
        Number.PropertyChanged += OnSelectionChanged;
        Person.PropertyChanged += OnSelectionChanged;
        Voice.PropertyChanged += OnSelectionChanged;
        Tense.PropertyChanged += OnSelectionChanged;
    }

    void OnSelectionChanged(object sender, PropertyChangedEventArgs e)
    {
        CardNavigation.UpdateNavigationState();
    }
}

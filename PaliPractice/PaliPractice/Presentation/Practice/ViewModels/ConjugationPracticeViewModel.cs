using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;
using PaliPractice.Presentation.Practice.Providers;
using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.Database;
using PaliPractice.Themes;

namespace PaliPractice.Presentation.Practice.ViewModels;

[Bindable]
public partial class ConjugationPracticeViewModel : PracticeViewModelBase
{
    readonly IInflectionService _inflectionService;
    Conjugation? _currentConjugation;

    // Current grammatical parameters from the SRS queue
    Tense _currentTense;
    Person _currentPerson;
    Number _currentNumber;
    bool _currentReflexive;

    protected override PracticeType CurrentPracticeType => PracticeType.Conjugation;

    // Badge display properties for Person
    [ObservableProperty] string _personLabel = string.Empty;
    [ObservableProperty] SolidColorBrush _personBrush = new(Colors.Transparent);
    [ObservableProperty] string? _personGlyph;

    // Badge display properties for Number
    [ObservableProperty] string _numberLabel = string.Empty;
    [ObservableProperty] SolidColorBrush _numberBrush = new(Colors.Transparent);
    [ObservableProperty] string? _numberGlyph;

    // Badge display properties for Tense
    [ObservableProperty] string _tenseLabel = string.Empty;
    [ObservableProperty] SolidColorBrush _tenseBrush = new(Colors.Transparent);
    [ObservableProperty] string? _tenseGlyph;

    // Alternative forms (other InCorpus forms besides Primary)
    [ObservableProperty] string _alternativeForms = string.Empty;

    public ConjugationPracticeViewModel(
        [FromKeyedServices("conjugation")] IPracticeProvider provider,
        IDatabaseService db,
        FlashCardViewModel flashCard,
        INavigator navigator,
        ILogger<ConjugationPracticeViewModel> logger,
        IInflectionService inflectionService)
        : base(provider, db.UserData, flashCard, navigator, logger)
    {
        _inflectionService = inflectionService;
        _ = InitializeAsync();
    }

    protected override void PrepareCardAnswer(ILemma lemma, object parameters)
    {
        var verb = (Verb)lemma.Primary;

        // Extract grammatical parameters from the SRS queue
        var (tense, person, number, reflexive) = ((Tense, Person, Number, bool))parameters;
        _currentTense = tense;
        _currentPerson = person;
        _currentNumber = number;
        _currentReflexive = reflexive;

        // Generate the conjugation for the specified grammatical combination
        _currentConjugation = _inflectionService.GenerateVerbForms(verb, person, number, tense, reflexive);

        if (!_currentConjugation.Primary.HasValue)
        {
            Logger.LogError("No attested conjugation for verb {Lemma} (id={Id}) tense={Tense} person={Person} number={Number} reflexive={Reflexive}",
                verb.Lemma, verb.Id, tense, person, number, reflexive);
            _currentConjugation = null;
            SetBadgesFallback();
            return;
        }

        // Update badge display properties
        UpdateBadges(_currentConjugation);
    }

    protected override void RecordCombinationDifficulty(bool wasHard)
    {
        UserData.UpdateConjugationDifficulty(_currentTense, _currentPerson, _currentNumber, _currentReflexive, wasHard);
    }

    void UpdateBadges(Conjugation c)
    {
        // Person badge
        PersonLabel = c.Person switch
        {
            Person.First => "1st",
            Person.Second => "2nd",
            Person.Third => "3rd",
            _ => c.Person.ToString()
        };
        PersonBrush = OptionPresentation.GetChipBrush(c.Person);
        PersonGlyph = OptionPresentation.GetGlyph(c.Person);

        // Number badge
        NumberLabel = c.Number switch
        {
            Number.Singular => "Singular",
            Number.Plural => "Plural",
            _ => c.Number.ToString()
        };
        NumberBrush = OptionPresentation.GetChipBrush(c.Number);
        NumberGlyph = OptionPresentation.GetGlyph(c.Number);

        // Tense badge
        TenseLabel = c.Tense.ToString();
        TenseBrush = OptionPresentation.GetChipBrush(c.Tense);
        TenseGlyph = "\uE8C8"; // Placeholder icon (Tag)

        // Alternative forms (other InCorpus forms besides Primary)
        var primary = c.Primary;
        var alternatives = c.Forms
            .Where(f => f.InCorpus && (!primary.HasValue || f.EndingId != primary.Value.EndingId))
            .Select(f => f.Form)
            .ToList();
        AlternativeForms = alternatives.Count > 0 ? string.Join(", ", alternatives) : string.Empty;
    }

    void SetBadgesFallback()
    {
        PersonLabel = "3rd";
        PersonBrush = OptionPresentation.GetChipBrush(Person.Third);
        PersonGlyph = OptionPresentation.GetGlyph(Person.Third);

        NumberLabel = "Singular";
        NumberBrush = OptionPresentation.GetChipBrush(Number.Singular);
        NumberGlyph = OptionPresentation.GetGlyph(Number.Singular);

        TenseLabel = "Present";
        TenseBrush = OptionPresentation.GetChipBrush(Tense.Present);
        TenseGlyph = "\uE8C8";

        AlternativeForms = string.Empty;
    }

    protected override string GetInflectedForm()
    {
        return _currentConjugation?.Primary?.Form ?? FlashCard.Question;
    }

    protected override string GetInflectedEnding()
    {
        return _currentConjugation?.Primary?.Ending ?? string.Empty;
    }
}

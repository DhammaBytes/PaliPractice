using PaliPractice.Presentation.Practice.Controls;
using PaliPractice.Presentation.Practice.Providers;
using PaliPractice.Themes;

namespace PaliPractice.Presentation.Practice;

[Bindable]
public partial class ConjugationPracticeViewModel : PracticeViewModelBase
{
    readonly Random _random = new();
    Conjugation? _currentConjugation;

    public override PracticeType CurrentPracticeType => PracticeType.Conjugation;

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

    public ConjugationPracticeViewModel(
        [FromKeyedServices("verb")] ILemmaProvider lemmas,
        WordCardViewModel wordCard,
        INavigator navigator,
        ILogger<ConjugationPracticeViewModel> logger)
        : base(lemmas, wordCard, navigator, logger)
    {
        _ = InitializeAsync();
    }

    protected override void PrepareCardAnswer(ILemma lemma)
    {
        // Use primary word for inflection generation
        var verb = (Verb)lemma.PrimaryWord;

        // TODO: Implement real verb inflection service similar to noun declensions.
        // For now, generate mock conjugation data based on word id.
        var id = verb.Id;

        var person = (Person)((id % 3) + 1);  // 1..3
        var number = (id % 2 == 0) ? Models.Number.Singular : Models.Number.Plural;
        var tense = new[] { Models.Tense.Present, Models.Tense.Aorist, Models.Tense.Future }[id % 3];
        var voice = (id % 2 == 0) ? Models.Voice.Active : Models.Voice.Reflexive;

        // Create mock conjugation
        _currentConjugation = new Conjugation
        {
            Form = GenerateMockForm(verb, person, number, tense),
            Person = person,
            Number = number,
            Tense = tense,
            Voice = voice
        };

        // Update badge display properties
        UpdateBadges(person, number, tense);
    }

    string GenerateMockForm(Verb verb, Person person, Models.Number number, Tense tense)
    {
        // Mock form generation - will be replaced with real inflection service
        var stem = verb.Stem ?? verb.LemmaClean;
        if (stem.EndsWith("ti") || stem.EndsWith("ati") || stem.EndsWith("eti"))
        {
            stem = stem[..^2]; // Remove -ti
        }

        var ending = (person, number, tense) switch
        {
            (Person.First, Models.Number.Singular, Tense.Present) => "āmi",
            (Person.Second, Models.Number.Singular, Tense.Present) => "asi",
            (Person.Third, Models.Number.Singular, Tense.Present) => "ati",
            (Person.First, Models.Number.Plural, Tense.Present) => "āma",
            (Person.Second, Models.Number.Plural, Tense.Present) => "atha",
            (Person.Third, Models.Number.Plural, Tense.Present) => "anti",
            _ => "ati" // Default fallback
        };

        return stem + ending;
    }

    void UpdateBadges(Person person, Models.Number number, Tense tense)
    {
        // Person badge
        PersonLabel = person switch
        {
            Person.First => "1st",
            Person.Second => "2nd",
            Person.Third => "3rd",
            _ => person.ToString()
        };
        PersonBrush = OptionPresentation.GetChipBrush(person);
        PersonGlyph = OptionPresentation.GetGlyph(person);

        // Number badge
        NumberLabel = number switch
        {
            Models.Number.Singular => "Singular",
            Models.Number.Plural => "Plural",
            _ => number.ToString()
        };
        NumberBrush = OptionPresentation.GetChipBrush(number);
        NumberGlyph = OptionPresentation.GetGlyph(number);

        // Tense badge
        TenseLabel = tense.ToString();
        TenseBrush = OptionPresentation.GetChipBrush(tense);
        TenseGlyph = "\uE8C8"; // Placeholder icon (Tag)
    }

    protected override string GetInflectedForm()
    {
        return _currentConjugation?.Form ?? WordCard.CurrentWord;
    }
}

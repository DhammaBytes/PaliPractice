using PaliPractice.Presentation.Providers;
using PaliPractice.Presentation.ViewModels;
using PaliPractice.Themes;

namespace PaliPractice.Presentation;

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

    public ConjugationPracticeViewModel(
        [FromKeyedServices("verb")] IWordProvider words,
        CardViewModel card,
        INavigator navigator,
        ILogger<ConjugationPracticeViewModel> logger)
        : base(words, card, navigator, logger)
    {
        _ = InitializeAsync();
    }

    protected override void PrepareCardAnswer(IWord w)
    {
        var verb = (Verb)w;

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
            Models.Number.Singular => "Sing",
            Models.Number.Plural => "Plur",
            _ => number.ToString()
        };
        NumberBrush = OptionPresentation.GetChipBrush(number);
        NumberGlyph = OptionPresentation.GetGlyph(number);

        // Tense badge
        TenseLabel = tense switch
        {
            Tense.Present => "Pres",
            Tense.Aorist => "Aor",
            Tense.Future => "Fut",
            Tense.Imperative => "Imp",
            Tense.Optative => "Opt",
            _ => tense.ToString()
        };
        TenseBrush = OptionPresentation.GetChipBrush(tense);
    }

    protected override string GetInflectedForm()
    {
        return _currentConjugation?.Form ?? Card.CurrentWord;
    }

    protected override void SetExamples(IWord w)
    {
        var verb = (Verb)w;

        // Use real data from the verb if available
        if (!string.IsNullOrEmpty(verb.Example1))
        {
            Card.UsageExample = verb.Example1;

            var reference = "";
            if (!string.IsNullOrEmpty(verb.Source1))
                reference = verb.Source1;
            if (!string.IsNullOrEmpty(verb.Sutta1))
                reference = string.IsNullOrEmpty(reference) ? verb.Sutta1 : $"{reference} {verb.Sutta1}";

            Card.SuttaReference = reference;
        }
        else if (!string.IsNullOrEmpty(verb.Example2))
        {
            Card.UsageExample = verb.Example2;

            var reference = "";
            if (!string.IsNullOrEmpty(verb.Source2))
                reference = verb.Source2;
            if (!string.IsNullOrEmpty(verb.Sutta2))
                reference = string.IsNullOrEmpty(reference) ? verb.Sutta2 : $"{reference} {verb.Sutta2}";

            Card.SuttaReference = reference;
        }
        else
        {
            // Fallback mock example
            Card.UsageExample = "bhagavā etadavoca: sādhu sādhu, bhikkhu";
            Card.SuttaReference = "MN1 mūlapariyāya";
        }
    }
}

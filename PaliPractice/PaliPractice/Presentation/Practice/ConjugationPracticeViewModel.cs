using PaliPractice.Presentation.Practice.Controls;
using PaliPractice.Presentation.Practice.Providers;
using PaliPractice.Themes;

namespace PaliPractice.Presentation.Practice;

[Bindable]
public partial class ConjugationPracticeViewModel : PracticeViewModelBase
{
    readonly IInflectionService _inflectionService;
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

    // Alternative forms (other InCorpus forms besides Primary)
    [ObservableProperty] string _alternativeForms = string.Empty;

    public ConjugationPracticeViewModel(
        [FromKeyedServices("verb")] ILemmaProvider lemmas,
        WordCardViewModel wordCard,
        INavigator navigator,
        ILogger<ConjugationPracticeViewModel> logger,
        IInflectionService inflectionService)
        : base(lemmas, wordCard, navigator, logger)
    {
        _inflectionService = inflectionService;
        _ = InitializeAsync();
    }

    protected override void PrepareCardAnswer(ILemma lemma)
    {
        // Use primary word for inflection generation
        var verb = (Verb)lemma.PrimaryWord;

        // Generate all grouped conjugations for this verb (one per person+number+tense+voice combo)
        var allConjugations = new List<Conjugation>();
        foreach (var person in Enum.GetValues<Person>())
        {
            foreach (var number in Enum.GetValues<Models.Number>())
            {
                foreach (var tense in Enum.GetValues<Tense>())
                {
                    foreach (var voice in Enum.GetValues<Voice>())
                    {
                        var conjugation = _inflectionService.GenerateVerbForms(verb, person, number, tense, voice);
                        // Only include if it has an attested primary form
                        if (conjugation.Primary.HasValue)
                        {
                            allConjugations.Add(conjugation);
                        }
                    }
                }
            }
        }

        if (allConjugations.Count == 0)
        {
            Logger.LogError("No attested conjugations for verb {Lemma} (id={Id})", verb.Lemma, verb.Id);
            _currentConjugation = null;
            SetBadgesFallback();
            return;
        }

        // Pick a random conjugation (person+number+tense+voice combo) for the user to produce
        _currentConjugation = allConjugations[_random.Next(allConjugations.Count)];

        // Update badge display properties
        UpdateBadges(_currentConjugation);
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
            Models.Number.Singular => "Singular",
            Models.Number.Plural => "Plural",
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
            .Where(f => f.InCorpus && (!primary.HasValue || f.EndingIndex != primary.Value.EndingIndex))
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
        NumberBrush = OptionPresentation.GetChipBrush(Models.Number.Singular);
        NumberGlyph = OptionPresentation.GetGlyph(Models.Number.Singular);

        TenseLabel = "Present";
        TenseBrush = OptionPresentation.GetChipBrush(Tense.Present);
        TenseGlyph = "\uE8C8";

        AlternativeForms = string.Empty;
    }

    protected override string GetInflectedForm()
    {
        return _currentConjugation?.Primary?.Form ?? WordCard.CurrentWord;
    }
}

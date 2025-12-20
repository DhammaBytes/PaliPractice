using PaliPractice.Presentation.Practice.Controls;
using PaliPractice.Presentation.Practice.Providers;
using PaliPractice.Themes;

namespace PaliPractice.Presentation.Practice;

[Bindable]
public partial class DeclensionPracticeViewModel : PracticeViewModelBase
{
    readonly IInflectionService _inflectionService;
    readonly Random _random = new();
    Declension? _currentDeclension;

    public override PracticeType CurrentPracticeType => PracticeType.Declension;

    // Badge display properties for Gender
    [ObservableProperty] string _genderLabel = string.Empty;
    [ObservableProperty] SolidColorBrush _genderBrush = new(Colors.Transparent);
    [ObservableProperty] string? _genderGlyph;

    // Badge display properties for Number
    [ObservableProperty] string _numberLabel = string.Empty;
    [ObservableProperty] SolidColorBrush _numberBrush = new(Colors.Transparent);
    [ObservableProperty] string? _numberGlyph;

    // Badge display properties for Case
    [ObservableProperty] string _caseLabel = string.Empty;
    [ObservableProperty] SolidColorBrush _caseBrush = new(Colors.Transparent);
    [ObservableProperty] string? _caseGlyph;
    [ObservableProperty] string _caseHint = string.Empty;

    public DeclensionPracticeViewModel(
        [FromKeyedServices("noun")] ILemmaProvider lemmas,
        WordCardViewModel wordCard,
        INavigator navigator,
        ILogger<DeclensionPracticeViewModel> logger,
        IInflectionService inflectionService)
        : base(lemmas, wordCard, navigator, logger)
    {
        _inflectionService = inflectionService;
        _ = InitializeAsync();
    }

    protected override void PrepareCardAnswer(ILemma lemma)
    {
        // Use primary word for inflection generation
        var noun = (Noun)lemma.PrimaryWord;

        // Generate all possible declensions for this noun
        var allDeclensions = new List<Declension>();
        foreach (var nounCase in Enum.GetValues<NounCase>())
        {
            foreach (var number in Enum.GetValues<Models.Number>())
            {
                var declensions = _inflectionService.GenerateNounForms(noun, nounCase, number);
                allDeclensions.AddRange(declensions);
            }
        }

        // Filter to only InCorpus forms
        var corpusForms = allDeclensions.Where(d => d.InCorpus).ToList();

        if (corpusForms.Count == 0)
        {
            Logger.LogWarning("No corpus forms found for noun {Lemma} (id={Id}), falling back to all forms",
                noun.Lemma, noun.Id);
            corpusForms = allDeclensions;
        }

        if (corpusForms.Count == 0)
        {
            Logger.LogError("No declensions generated for noun {Lemma} (id={Id})", noun.Lemma, noun.Id);
            // Use a fallback
            _currentDeclension = null;
            SetBadgesFallback(noun);
            return;
        }

        // Pick a random corpus form for the user to produce
        _currentDeclension = corpusForms[_random.Next(corpusForms.Count)];

        // Set badge display properties
        UpdateBadges(_currentDeclension);
    }

    void UpdateBadges(Declension d)
    {
        // Gender badge
        GenderLabel = d.Gender switch
        {
            Gender.Masculine => "Masculine",
            Gender.Neuter => "Neuter",
            Gender.Feminine => "Feminine",
            _ => d.Gender.ToString()
        };
        GenderBrush = OptionPresentation.GetChipBrush(d.Gender);
        GenderGlyph = OptionPresentation.GetGlyph(d.Gender);

        // Number badge
        NumberLabel = d.Number switch
        {
            Models.Number.Singular => "Singular",
            Models.Number.Plural => "Plural",
            _ => d.Number.ToString()
        };
        NumberBrush = OptionPresentation.GetChipBrush(d.Number);
        NumberGlyph = OptionPresentation.GetGlyph(d.Number);

        // Case badge
        CaseLabel = d.CaseName.ToString();
        CaseBrush = OptionPresentation.GetChipBrush(d.CaseName);
        CaseGlyph = "\uE8C8"; // Placeholder icon (Tag)
        CaseHint = d.CaseName switch
        {
            NounCase.Nominative => "who? what? (subject)",
            NounCase.Accusative => "whom? what? (object)",
            NounCase.Instrumental => "with whom? with what? by what means?",
            NounCase.Dative => "to whom? to what? for whom?",
            NounCase.Ablative => "from whom? from what?",
            NounCase.Genitive => "whose? of whom? of what?",
            NounCase.Locative => "in/at/on whom? where?",
            NounCase.Vocative => "O...! (direct address)",
            _ => string.Empty
        };
    }

    void SetBadgesFallback(Noun noun)
    {
        GenderLabel = noun.Gender.ToString();
        GenderBrush = OptionPresentation.GetChipBrush(noun.Gender);
        GenderGlyph = OptionPresentation.GetGlyph(noun.Gender);

        NumberLabel = "Singular";
        NumberBrush = OptionPresentation.GetChipBrush(Models.Number.Singular);
        NumberGlyph = OptionPresentation.GetGlyph(Models.Number.Singular);

        CaseLabel = "Nominative";
        CaseBrush = OptionPresentation.GetChipBrush(NounCase.Nominative);
        CaseGlyph = "\uE8C8";
        CaseHint = "who? what? (subject)";
    }

    protected override string GetInflectedForm()
    {
        return _currentDeclension?.Form ?? WordCard.CurrentWord;
    }
}

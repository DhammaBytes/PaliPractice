using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;
using PaliPractice.Presentation.Practice.Providers;
using PaliPractice.Themes;
using WordCardViewModel = PaliPractice.Presentation.Practice.Controls.ViewModels.WordCardViewModel;

namespace PaliPractice.Presentation.Practice.ViewModels;

[Bindable]
public partial class DeclensionPracticeViewModel : PracticeViewModelBase
{
    readonly IInflectionService _inflectionService;
    readonly Random _random = new();
    Declension? _currentDeclension;

    protected override PracticeType CurrentPracticeType => PracticeType.Declension;

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

    // Alternative forms (other InCorpus forms besides Primary)
    [ObservableProperty] string _alternativeForms = string.Empty;

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
        // Use the first word (all share the same dominant pattern)
        var noun = (Noun)lemma.Words.First();

        // Generate all grouped declensions for this noun (one per case+number combo)
        var allDeclensions = new List<Declension>();
        foreach (var nounCase in Enum.GetValues<NounCase>())
        {
            foreach (var number in Enum.GetValues<Number>())
            {
                var declension = _inflectionService.GenerateNounForms(noun, nounCase, number);
                // Only include if it has an attested primary form
                if (declension.Primary.HasValue)
                {
                    allDeclensions.Add(declension);
                }
            }
        }

        if (allDeclensions.Count == 0)
        {
            Logger.LogError("No attested declensions for noun {Lemma} (id={Id})", noun.Lemma, noun.Id);
            _currentDeclension = null;
            SetBadgesFallback(noun);
            return;
        }

        // Pick a random declension (case+number combo) for the user to produce
        _currentDeclension = allDeclensions[_random.Next(allDeclensions.Count)];

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
            Number.Singular => "Singular",
            Number.Plural => "Plural",
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

        // Alternative forms (other InCorpus forms besides Primary)
        var primary = d.Primary;
        var alternatives = d.Forms
            .Where(f => f.InCorpus && (!primary.HasValue || f.EndingIndex != primary.Value.EndingIndex))
            .Select(f => f.Form)
            .ToList();
        AlternativeForms = alternatives.Count > 0 ? string.Join(", ", alternatives) : string.Empty;
    }

    void SetBadgesFallback(Noun noun)
    {
        GenderLabel = noun.Gender.ToString();
        GenderBrush = OptionPresentation.GetChipBrush(noun.Gender);
        GenderGlyph = OptionPresentation.GetGlyph(noun.Gender);

        NumberLabel = "Singular";
        NumberBrush = OptionPresentation.GetChipBrush(Number.Singular);
        NumberGlyph = OptionPresentation.GetGlyph(Number.Singular);

        CaseLabel = "Nominative";
        CaseBrush = OptionPresentation.GetChipBrush(NounCase.Nominative);
        CaseGlyph = "\uE8C8";
        CaseHint = "who? what? (subject)";

        AlternativeForms = string.Empty;
    }

    protected override string GetInflectedForm()
    {
        return _currentDeclension?.Primary?.Form ?? WordCard.CurrentWord;
    }
}

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

    public DeclensionPracticeViewModel(
        [FromKeyedServices("noun")] IWordProvider words,
        CardViewModel card,
        INavigator navigator,
        ILogger<DeclensionPracticeViewModel> logger,
        IInflectionService inflectionService)
        : base(words, card, navigator, logger)
    {
        _inflectionService = inflectionService;
        _ = InitializeAsync();
    }

    protected override void PrepareCardAnswer(IWord w)
    {
        var noun = (Noun)w;

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
            Gender.Masculine => "Masc",
            Gender.Neuter => "Neut",
            Gender.Feminine => "Fem",
            _ => d.Gender.ToString()
        };
        GenderBrush = OptionPresentation.GetChipBrush(d.Gender);
        GenderGlyph = OptionPresentation.GetGlyph(d.Gender);

        // Number badge
        NumberLabel = d.Number switch
        {
            Models.Number.Singular => "Sing",
            Models.Number.Plural => "Plur",
            _ => d.Number.ToString()
        };
        NumberBrush = OptionPresentation.GetChipBrush(d.Number);
        NumberGlyph = OptionPresentation.GetGlyph(d.Number);

        // Case badge
        CaseLabel = d.CaseName switch
        {
            NounCase.Nominative => "Nom",
            NounCase.Accusative => "Acc",
            NounCase.Instrumental => "Ins",
            NounCase.Dative => "Dat",
            NounCase.Ablative => "Abl",
            NounCase.Genitive => "Gen",
            NounCase.Locative => "Loc",
            NounCase.Vocative => "Voc",
            _ => d.CaseName.ToString()
        };
        CaseBrush = OptionPresentation.GetChipBrush(d.CaseName);
    }

    void SetBadgesFallback(Noun noun)
    {
        GenderLabel = noun.Gender.ToString();
        GenderBrush = OptionPresentation.GetChipBrush(noun.Gender);
        GenderGlyph = OptionPresentation.GetGlyph(noun.Gender);

        NumberLabel = "Sing";
        NumberBrush = OptionPresentation.GetChipBrush(Models.Number.Singular);
        NumberGlyph = OptionPresentation.GetGlyph(Models.Number.Singular);

        CaseLabel = "Nom";
        CaseBrush = OptionPresentation.GetChipBrush(NounCase.Nominative);
    }

    protected override string GetInflectedForm()
    {
        return _currentDeclension?.Form ?? Card.CurrentWord;
    }

    protected override void SetExamples(IWord w)
    {
        var noun = (Noun)w;

        // Use real data from the noun
        if (!string.IsNullOrEmpty(noun.Example1))
        {
            Card.UsageExample = noun.Example1;

            // Combine source and sutta reference if available
            var reference = "";
            if (!string.IsNullOrEmpty(noun.Source1))
                reference = noun.Source1;
            if (!string.IsNullOrEmpty(noun.Sutta1))
                reference = string.IsNullOrEmpty(reference) ? noun.Sutta1 : $"{reference} {noun.Sutta1}";

            Card.SuttaReference = reference;
        }
        else if (!string.IsNullOrEmpty(noun.Example2))
        {
            // Fall back to second example if first is not available
            Card.UsageExample = noun.Example2;

            var reference = "";
            if (!string.IsNullOrEmpty(noun.Source2))
                reference = noun.Source2;
            if (!string.IsNullOrEmpty(noun.Sutta2))
                reference = string.IsNullOrEmpty(reference) ? noun.Sutta2 : $"{reference} {noun.Sutta2}";

            Card.SuttaReference = reference;
        }
        else
        {
            // No examples available
            Card.UsageExample = "";
            Card.SuttaReference = "";
        }
    }
}

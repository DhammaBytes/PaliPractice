using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;
using PaliPractice.Presentation.Practice.Providers;
using PaliPractice.Services.Grammar;
using PaliPractice.Themes;

namespace PaliPractice.Presentation.Practice.ViewModels;

[Bindable]
public partial class DeclensionPracticeViewModel : PracticeViewModelBase
{
    readonly IInflectionService _inflectionService;
    Declension? _currentDeclension;

    // Current grammatical parameters from the SRS queue
    Case _currentCase;
    Gender _currentGender;
    Number _currentNumber;

    protected override PracticeType CurrentPracticeType => PracticeType.Declension;

    // Badge display properties for Gender
    [ObservableProperty] string _genderLabel = string.Empty;
    [ObservableProperty] Color _genderColor = Colors.Transparent;
    [ObservableProperty] string? _genderGlyph;

    // Badge display properties for Number
    [ObservableProperty] string _numberLabel = string.Empty;
    [ObservableProperty] Color _numberColor = Colors.Transparent;
    [ObservableProperty] string? _numberGlyph;

    // Badge display properties for Case
    [ObservableProperty] string _caseLabel = string.Empty;
    [ObservableProperty] Color _caseColor = Colors.Transparent;
    [ObservableProperty] string? _caseGlyph;
    [ObservableProperty] string _caseHint = string.Empty;

    public DeclensionPracticeViewModel(
        [FromKeyedServices("declension")] IPracticeProvider provider,
        IDatabaseService db,
        FlashCardViewModel flashCard,
        INavigator navigator,
        ILogger<DeclensionPracticeViewModel> logger,
        IInflectionService inflectionService)
        : base(provider, db.UserData, flashCard, navigator, logger)
    {
        _inflectionService = inflectionService;
        _ = InitializeAsync();
    }

    protected override void PrepareCardAnswer(ILemma lemma, object parameters)
    {
        var noun = (Noun)lemma.Primary;

        // Extract grammatical parameters from the SRS queue
        var (nounCase, gender, number) = ((Case, Gender, Number))parameters;
        _currentCase = nounCase;
        _currentGender = gender;
        _currentNumber = number;

        // Generate the declension for the specified grammatical combination
        _currentDeclension = _inflectionService.GenerateNounForms(noun, nounCase, number);

        if (!_currentDeclension.Primary.HasValue)
        {
            Logger.LogError("No attested declension for noun {Lemma} (id={Id}) case={Case} number={Number}",
                noun.Lemma, noun.Id, nounCase, number);
            _currentDeclension = null;
            SetBadgesFallback(noun);
            return;
        }

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
        GenderColor = OptionPresentation.GetChipColor(d.Gender);
        GenderGlyph = OptionPresentation.GetGlyph(d.Gender);

        // Number badge
        NumberLabel = d.Number switch
        {
            Number.Singular => "Singular",
            Number.Plural => "Plural",
            _ => d.Number.ToString()
        };
        NumberColor = OptionPresentation.GetChipColor(d.Number);
        NumberGlyph = OptionPresentation.GetGlyph(d.Number);

        // Case badge
        CaseLabel = d.Case.ToString();
        CaseColor = OptionPresentation.GetChipColor(d.Case);
        CaseGlyph = "\uE8C8"; // Placeholder icon (Tag)
        CaseHint = d.Case switch
        {
            Case.Nominative => "who? what? (subject)",
            Case.Accusative => "whom? what? (object)",
            Case.Instrumental => "with whom? by whom? by what means?",
            Case.Dative => "for whom? to whom? to what?",
            Case.Ablative => "from whom? from where? from what?",
            Case.Genitive => "whose? of whom? of what?",
            Case.Locative => "in whom? where? when?",
            Case.Vocative => "O, …! (direct address)",
            _ => string.Empty
        };
    }

    protected override string GetAlternativeForms()
    {
        if (_currentDeclension == null) return string.Empty;

        var primary = _currentDeclension.Primary;
        var alternatives = _currentDeclension.Forms
            .Where(f => f.InCorpus && (!primary.HasValue || f.EndingId != primary.Value.EndingId))
            .Select(f => f.Form)
            .ToList();
        return alternatives.Count > 0 ? string.Join(", ", alternatives) : string.Empty;
    }

    void SetBadgesFallback(Noun noun)
    {
        GenderLabel = noun.Gender.ToString();
        GenderColor = OptionPresentation.GetChipColor(noun.Gender);
        GenderGlyph = OptionPresentation.GetGlyph(noun.Gender);

        NumberLabel = "Singular";
        NumberColor = OptionPresentation.GetChipColor(Number.Singular);
        NumberGlyph = OptionPresentation.GetGlyph(Number.Singular);

        CaseLabel = "Nominative";
        CaseColor = OptionPresentation.GetChipColor(Case.Nominative);
        CaseGlyph = "\uE8C8";
        CaseHint = "who? what? (subject)";
    }

    protected override string GetInflectedForm()
    {
        return _currentDeclension?.Primary?.Form ?? FlashCard.Question;
    }

    protected override string GetInflectedEnding()
    {
        return _currentDeclension?.Primary?.Ending ?? string.Empty;
    }

    protected override IReadOnlyList<string> GetAllInflectedForms()
    {
        if (_currentDeclension == null)
            return [];

        return _currentDeclension.Forms
            .Select(f => f.Form)
            .Where(f => !string.IsNullOrEmpty(f))
            .ToList();
    }
}

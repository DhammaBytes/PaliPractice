using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;
using PaliPractice.Presentation.Practice.Providers;
using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.UserData;
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
        [FromKeyedServices("declension")] IPracticeProvider provider,
        IUserDataService userData,
        FlashCardViewModel flashCard,
        INavigator navigator,
        ILogger<DeclensionPracticeViewModel> logger,
        IInflectionService inflectionService)
        : base(provider, userData, flashCard, navigator, logger)
    {
        _inflectionService = inflectionService;
        _ = InitializeAsync();
    }

    protected override void PrepareCardAnswer(IWord word, object parameters)
    {
        var noun = (Noun)word;

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

    protected override void RecordCombinationDifficulty(bool wasHard)
    {
        UserData.UpdateDeclensionDifficulty(_currentCase, _currentGender, _currentNumber, wasHard);
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
        CaseLabel = d.Case.ToString();
        CaseBrush = OptionPresentation.GetChipBrush(d.Case);
        CaseGlyph = "\uE8C8"; // Placeholder icon (Tag)
        CaseHint = d.Case switch
        {
            Case.Nominative => "who? what? (subject)",
            Case.Accusative => "whom? what? (object)",
            Case.Instrumental => "with whom? with what? by what means?",
            Case.Dative => "to whom? to what? for whom?",
            Case.Ablative => "from whom? from what?",
            Case.Genitive => "whose? of whom? of what?",
            Case.Locative => "in/at/on whom? where?",
            Case.Vocative => "O...! (direct address)",
            _ => string.Empty
        };

        // Alternative forms (other InCorpus forms besides Primary)
        var primary = d.Primary;
        var alternatives = d.Forms
            .Where(f => f.InCorpus && (!primary.HasValue || f.EndingId != primary.Value.EndingId))
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
        CaseBrush = OptionPresentation.GetChipBrush(Case.Nominative);
        CaseGlyph = "\uE8C8";
        CaseHint = "who? what? (subject)";

        AlternativeForms = string.Empty;
    }

    protected override string GetInflectedForm()
    {
        return _currentDeclension?.Primary?.Form ?? FlashCard.Question;
    }

    protected override string GetInflectedEnding()
    {
        return _currentDeclension?.Primary?.Ending ?? string.Empty;
    }
}

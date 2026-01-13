using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;
using PaliPractice.Presentation.Practice.Providers;
using PaliPractice.Presentation.Settings.ViewModels;
using PaliPractice.Services.Grammar;
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
    Voice _currentVoice;

    protected override PracticeType CurrentPracticeType => PracticeType.Conjugation;

    // Badge display properties for Person
    [ObservableProperty] string _personLabel = string.Empty;
    [ObservableProperty] Color _personColor = Colors.Transparent;
    [ObservableProperty] string? _personIconPath;

    // Badge display properties for Number
    [ObservableProperty] string _numberLabel = string.Empty;
    [ObservableProperty] Color _numberColor = Colors.Transparent;
    [ObservableProperty] string? _numberIconPath;

    // Badge display properties for Tense
    [ObservableProperty] string _tenseLabel = string.Empty;
    [ObservableProperty] Color _tenseColor = Colors.Transparent;
    [ObservableProperty] string? _tenseIconPath;

    // Badge display properties for Voice (only shown for reflexive)
    [ObservableProperty] string _voiceLabel = string.Empty;
    [ObservableProperty] Color _voiceColor = Colors.Transparent;
    [ObservableProperty] string? _voiceIconPath;
    [ObservableProperty] bool _isReflexive;

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

    public override ICommand GoToSettingsCommand =>
        new AsyncRelayCommand(() => Navigator.NavigateViewModelAsync<ConjugationSettingsViewModel>(this));

    protected override void PrepareCardAnswer(ILemma lemma, object parameters)
    {
        var verb = (Verb)lemma.Primary;

        // Extract grammatical parameters from the SRS queue
        var (tense, person, number, voice) = ((Tense, Person, Number, Voice))parameters;
        _currentTense = tense;
        _currentPerson = person;
        _currentNumber = number;
        _currentVoice = voice;

        // Generate the conjugation for the specified grammatical combination
        var reflexive = voice == Voice.Reflexive;
        _currentConjugation = _inflectionService.GenerateVerbForms(verb, person, number, tense, reflexive);

        if (!_currentConjugation.Primary.HasValue)
        {
            Logger.LogError("No attested conjugation for verb {Lemma} (id={Id}) tense={Tense} person={Person} number={Number} voice={Voice}",
                verb.Lemma, verb.Id, tense, person, number, voice);
            _currentConjugation = null;
            SetBadgesFallback();
            return;
        }

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
        PersonColor = OptionPresentation.GetChipColor(c.Person);
        PersonIconPath = BadgeIcons.GetIconPath(c.Person);

        // Number badge
        NumberLabel = c.Number switch
        {
            Number.Singular => "Singular",
            Number.Plural => "Plural",
            _ => c.Number.ToString()
        };
        NumberColor = OptionPresentation.GetChipColor(c.Number);
        NumberIconPath = BadgeIcons.GetIconPath(c.Number);

        // Tense badge
        TenseLabel = c.Tense.ToString();
        TenseColor = OptionPresentation.GetChipColor(c.Tense);
        TenseIconPath = BadgeIcons.GetIconPath(c.Tense);

        // Voice badge (only visible for reflexive)
        IsReflexive = c.Voice == Voice.Reflexive;
        VoiceLabel = "Reflexive";
        VoiceColor = OptionPresentation.GetChipColor(Voice.Reflexive);
        VoiceIconPath = BadgeIcons.GetIconPath(Voice.Reflexive);
    }

    protected override string GetAlternativeForms()
    {
        if (_currentConjugation == null) return string.Empty;

        var primary = _currentConjugation.Primary;
        var alternatives = _currentConjugation.Forms
            .Where(f => f.InCorpus && (!primary.HasValue || f.EndingId != primary.Value.EndingId))
            .Select(f => f.Form)
            .ToList();
        return alternatives.Count > 0 ? string.Join(", ", alternatives) : string.Empty;
    }

    void SetBadgesFallback()
    {
        PersonLabel = "3rd";
        PersonColor = OptionPresentation.GetChipColor(Person.Third);
        PersonIconPath = BadgeIcons.GetIconPath(Person.Third);

        NumberLabel = "Singular";
        NumberColor = OptionPresentation.GetChipColor(Number.Singular);
        NumberIconPath = BadgeIcons.GetIconPath(Number.Singular);

        TenseLabel = "Present";
        TenseColor = OptionPresentation.GetChipColor(Tense.Present);
        TenseIconPath = BadgeIcons.GetIconPath(Tense.Present);

        IsReflexive = false;
    }

    protected override string GetInflectedForm()
    {
        return _currentConjugation?.Primary?.Form ?? FlashCard.Question;
    }

    protected override string GetInflectedEnding()
    {
        return _currentConjugation?.Primary?.Ending ?? string.Empty;
    }

    protected override IReadOnlyList<string> GetAllInflectedForms()
    {
        if (_currentConjugation == null)
            return [];

        return _currentConjugation.Forms
            .Select(f => f.Form)
            .Where(f => !string.IsNullOrEmpty(f))
            .ToList();
    }
}

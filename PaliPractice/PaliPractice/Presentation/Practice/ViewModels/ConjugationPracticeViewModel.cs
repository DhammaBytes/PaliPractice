using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;
using PaliPractice.Presentation.Practice.Common;
using PaliPractice.Presentation.Practice.Providers;
using PaliPractice.Presentation.Settings.ViewModels;
using PaliPractice.Services.Feedback;
using PaliPractice.Services.Grammar;
using PaliPractice.Themes;
using PaliPractice.Themes.Icons;

namespace PaliPractice.Presentation.Practice.ViewModels;

[Bindable]
public partial class ConjugationPracticeViewModel : PracticeViewModelBase
{
    readonly IInflectionService _inflectionService;
    readonly IDatabaseService _db;
    Conjugation? _currentConjugation;

    // Current grammatical parameters from the SRS queue
    Tense _currentTense;
    Person _currentPerson;
    Number _currentNumber;
    Voice _currentVoice;

    protected override PracticeType CurrentPracticeType => PracticeType.Conjugation;
    public override PracticeType PracticeTypePublic => PracticeType.Conjugation;

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
        IStoreReviewService storeReviewService,
        ILogger<ConjugationPracticeViewModel> logger,
        IInflectionService inflectionService)
        : base(provider, db.UserData, flashCard, navigator, storeReviewService, logger)
    {
        _inflectionService = inflectionService;
        _db = db;

#if DEBUG
        if (ScreenshotMode.IsEnabled)
            _ = InitializeForScreenshotAsync();
        else
#endif
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
        // Person badge (always full - already short: 1st, 2nd, 3rd)
        PersonLabel = c.Person switch
        {
            Person.First => "1st",
            Person.Second => "2nd",
            Person.Third => "3rd",
            _ => c.Person.ToString()
        };
        PersonColor = BadgePresentation.GetChipColor(c.Person);
        PersonIconPath = BadgeIcons.GetIconPath(c.Person);

        // Number badge (abbreviatable)
        NumberLabel = UseAbbreviatedLabels
            ? BadgeLabelMaps.GetAbbreviated(c.Number)
            : BadgeLabelMaps.GetFull(c.Number);
        NumberColor = BadgePresentation.GetChipColor(c.Number);
        NumberIconPath = BadgeIcons.GetIconPath(c.Number);

        // Tense badge (always full - never abbreviated)
        TenseLabel = c.Tense.ToString();
        TenseColor = BadgePresentation.GetChipColor(c.Tense);
        TenseIconPath = BadgeIcons.GetIconPath(c.Tense);

        // Voice badge (abbreviatable, only visible for reflexive)
        IsReflexive = c.Voice == Voice.Reflexive;
        VoiceLabel = UseAbbreviatedLabels
            ? BadgeLabelMaps.GetAbbreviated(Voice.Reflexive)
            : BadgeLabelMaps.GetFull(Voice.Reflexive);
        VoiceColor = BadgePresentation.GetChipColor(Voice.Reflexive);
        VoiceIconPath = BadgeIcons.GetIconPath(Voice.Reflexive);
    }

    /// <summary>
    /// Called when UseAbbreviatedLabels changes. Refreshes badge labels.
    /// </summary>
    protected override void OnAbbreviationModeChanged()
    {
        if (_currentConjugation != null)
            UpdateBadges(_currentConjugation);
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
        PersonColor = BadgePresentation.GetChipColor(Person.Third);
        PersonIconPath = BadgeIcons.GetIconPath(Person.Third);

        NumberLabel = "Singular";
        NumberColor = BadgePresentation.GetChipColor(Number.Singular);
        NumberIconPath = BadgeIcons.GetIconPath(Number.Singular);

        TenseLabel = "Present";
        TenseColor = BadgePresentation.GetChipColor(Tense.Present);
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

#if DEBUG
    #region Screenshot Mode

    /// <summary>
    /// Initialize with predetermined content for App Store screenshots.
    /// Loads "bhavati" (LemmaId: 70008) with Optative Second Plural Active → "bhavetha".
    /// </summary>
    async Task InitializeForScreenshotAsync()
    {
        try
        {
            FlashCard.IsLoading = true;

            // Load the specific lemma for screenshots
            var lemma = _db.Verbs.GetLemma(ScreenshotMode.BhavatiLemmaId);
            if (lemma == null)
            {
                Logger.LogError("Screenshot mode: Failed to load bhavati lemma");
                await InitializeAsync(); // Fallback to normal initialization
                return;
            }

            _db.Verbs.EnsureDetails(lemma);
            ScreenshotLemma = lemma;

            // Set the predetermined grammatical parameters
            _currentTense = Tense.Optative;
            _currentPerson = Person.Second;
            _currentNumber = Number.Plural;
            _currentVoice = Voice.Active;

            // Display the word
            var verb = (Verb)lemma.Primary;
            FlashCard.DisplayWord(verb, 1, 1, masteryLevel: 5, verb.Details?.Root);

            // Generate the conjugation
            _currentConjugation = _inflectionService.GenerateVerbForms(verb, _currentPerson, _currentNumber, _currentTense, reflexive: false);
            UpdateBadges(_currentConjugation!);

            // Set the answer
            FlashCard.SetAnswer(GetInflectedForm(), GetInflectedEnding());
            AlternativeForms = GetAlternativeForms();

            // Initialize example carousel
            ExampleCarousel.Initialize(lemma.Words);
            ExampleCarousel.SetFormsToAvoid(GetAllInflectedForms());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Screenshot mode initialization failed");
            await InitializeAsync(); // Fallback to normal initialization
        }
        finally
        {
            FlashCard.IsLoading = false;
        }
    }

    #endregion
#endif
}

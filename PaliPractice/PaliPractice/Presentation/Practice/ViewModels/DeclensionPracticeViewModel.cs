using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;
using PaliPractice.Localization;
using PaliPractice.Presentation.Practice.Common;
using PaliPractice.Presentation.Practice.Providers;
using PaliPractice.Presentation.Settings.ViewModels;
using PaliPractice.Services.Feedback;
using PaliPractice.Services.Grammar;
using PaliPractice.Themes.Icons;

namespace PaliPractice.Presentation.Practice.ViewModels;

[Bindable]
public partial class DeclensionPracticeViewModel : PracticeViewModelBase
{
    readonly IInflectionService _inflectionService;
    readonly IDatabaseService _db;
    Declension? _currentDeclension;

    // Current grammatical parameters from the SRS queue
    Case _currentCase;
    Gender _currentGender;
    Number _currentNumber;

    protected override PracticeType CurrentPracticeType => PracticeType.Declension;

    public override IReadOnlyList<BadgeLabelOption> GetBadgeLabelOptions()
    {
        if (_currentDeclension is not { } d) return [];
        return
        [
            new(GrammarText.GetCase(d.Case), GrammarText.GetCaseShort(d.Case)),
            new(BadgeLabelMaps.GetFull(d.Gender), BadgeLabelMaps.GetAbbreviated(d.Gender)),
            new(BadgeLabelMaps.GetFull(d.Number), BadgeLabelMaps.GetAbbreviated(d.Number))
        ];
    }

    // Badge display properties for Gender
    [ObservableProperty] string _genderLabel = string.Empty;
    [ObservableProperty] string? _genderIconPath;

    // Badge display properties for Number
    [ObservableProperty] string _numberLabel = string.Empty;
    [ObservableProperty] string? _numberIconPath;

    // Badge display properties for Case
    [ObservableProperty] string _caseLabel = string.Empty;
    [ObservableProperty] string? _caseIconPath;
    [ObservableProperty] string _caseHint = string.Empty;

    public DeclensionPracticeViewModel(
        [FromKeyedServices("declension")] IPracticeProvider provider,
        IDatabaseService db,
        FlashCardViewModel flashCard,
        INavigator navigator,
        IStoreReviewService storeReviewService,
        ILogger<DeclensionPracticeViewModel> logger,
        IInflectionService inflectionService)
        : base(provider, db.UserData, flashCard, navigator, storeReviewService, logger)
    {
        _inflectionService = inflectionService;
        _db = db;
    }

    public override Task StartAsync(CancellationToken ct = default)
    {
#if DEBUG
        if (ScreenshotMode.IsEnabled)
            return InitializeForScreenshotAsync();
#endif
        return base.StartAsync(ct);
    }

    public override ICommand GoToSettingsCommand =>
        new AsyncRelayCommand(() => Navigator.NavigateViewModelAsync<DeclensionSettingsViewModel>(this));

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
        // Gender badge (abbreviatable)
        GenderLabel = IsBadgeAbbreviated(1)
            ? BadgeLabelMaps.GetAbbreviated(d.Gender)
            : BadgeLabelMaps.GetFull(d.Gender);
        GenderIconPath = BadgeIcons.GetIconPath(d.Gender);

        // Number badge (abbreviatable)
        NumberLabel = IsBadgeAbbreviated(2)
            ? BadgeLabelMaps.GetAbbreviated(d.Number)
            : BadgeLabelMaps.GetFull(d.Number);
        NumberIconPath = BadgeIcons.GetIconPath(d.Number);

        // Case badge
        CaseLabel = IsBadgeAbbreviated(0)
            ? GrammarText.GetCaseShort(d.Case)
            : GrammarText.GetCase(d.Case);
        CaseIconPath = BadgeIcons.GetIconPath(d.Case);
        CaseHint = GrammarText.GetCasePracticeHint(d.Case);
    }

    /// <summary>
    /// Refreshes badge labels when their selected forms change.
    /// </summary>
    protected override void OnAbbreviationModeChanged()
    {
        if (_currentDeclension != null)
            UpdateBadges(_currentDeclension);
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
        GenderLabel = GrammarText.GetGender(noun.Gender);
        GenderIconPath = BadgeIcons.GetIconPath(noun.Gender);

        NumberLabel = GrammarText.GetNumber(Number.Singular);
        NumberIconPath = BadgeIcons.GetIconPath(Number.Singular);

        CaseLabel = GrammarText.GetCase(Case.Nominative);
        CaseIconPath = BadgeIcons.GetIconPath(Case.Nominative);
        CaseHint = GrammarText.GetCasePracticeHint(Case.Nominative);
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

#if DEBUG
    #region Screenshot Mode

    /// <summary>
    /// Initialize with predetermined content for App Store screenshots.
    /// Loads "dhamma" (LemmaId: 10005) with Ablative Masculine Singular → "dhammato".
    /// </summary>
    async Task InitializeForScreenshotAsync()
    {
        try
        {
            FlashCard.IsLoading = true;

            // Load the specific lemma for screenshots
            var lemma = _db.Nouns.GetLemma(ScreenshotMode.DhammaLemmaId);
            if (lemma == null)
            {
                Logger.LogError("Screenshot mode: Failed to load dhamma lemma");
                await InitializeAsync(); // Fallback to normal initialization
                return;
            }

            _db.Nouns.EnsureDetails(lemma);
            ScreenshotLemma = lemma;

            // Set the predetermined grammatical parameters
            _currentCase = Case.Ablative;
            _currentGender = Gender.Masculine;
            _currentNumber = Number.Singular;

            // Display the word
            var noun = (Noun)lemma.Primary;
            FlashCard.DisplayWord(noun, 1, 1, masteryLevel: 5, noun.Details?.Root);

            // Generate the declension
            _currentDeclension = _inflectionService.GenerateNounForms(noun, _currentCase, _currentNumber);
            UpdateBadges(_currentDeclension!);
            NotifyBadgeOptionsChanged();

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

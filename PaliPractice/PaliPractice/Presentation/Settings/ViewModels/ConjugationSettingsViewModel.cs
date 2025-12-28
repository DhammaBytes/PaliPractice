using PaliPractice.Models;
using PaliPractice.Services.Database;
using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.Practice;
using PaliPractice.Services.UserData;

namespace PaliPractice.Presentation.Settings.ViewModels;

[Bindable]
public partial class ConjugationSettingsViewModel : ObservableObject
{
    readonly INavigator _navigator;
    readonly UserDataRepository _userData;
    bool _isLoading = true;

    public ConjugationSettingsViewModel(INavigator navigator, IDatabaseService db)
    {
        _navigator = navigator;
        _userData = db.UserData;

        LoadSettings();
        _isLoading = false;
    }

    void LoadSettings()
    {
        // Load daily goal
        DailyGoal = _userData.GetSetting(SettingsKeys.VerbsDailyGoal, SettingsKeys.DefaultDailyGoal);

        // Load person settings
        var persons = SettingsHelpers.FromCsvSet<Person>(
            _userData.GetSetting(SettingsKeys.VerbsPersons,
                SettingsHelpers.ToCsv(SettingsKeys.VerbsDefaultPersons)));
        FirstPerson = persons.Contains(Person.First);
        SecondPerson = persons.Contains(Person.Second);
        ThirdPerson = persons.Contains(Person.Third);

        // Load number setting (index-based: 0=both, 1=singular, 2=plural)
        var numbersCsv = _userData.GetSetting(SettingsKeys.VerbsNumbers,
            SettingsHelpers.ToCsv(SettingsKeys.DefaultNumbers));
        NumberIndex = SettingsHelpers.NumberIndexFromCsv(numbersCsv);

        // Load enabled patterns (positive list)
        var enabled = SettingsHelpers.FromCsvSet<VerbPattern>(
            _userData.GetSetting(SettingsKeys.VerbsPatterns,
                SettingsHelpers.ToCsv(SettingsKeys.VerbsDefaultPatterns)));
        PatternAti = enabled.Contains(VerbPattern.Ati);
        PatternEti = enabled.Contains(VerbPattern.Eti);
        PatternOti = enabled.Contains(VerbPattern.Oti);
        PatternAtiLong = enabled.Contains(VerbPattern.Āti);

        // Load tense settings
        var tenses = SettingsHelpers.FromCsvSet<Tense>(
            _userData.GetSetting(SettingsKeys.VerbsTenses,
                SettingsHelpers.ToCsv(SettingsKeys.VerbsDefaultTenses)));
        Present = tenses.Contains(Tense.Present);
        Imperative = tenses.Contains(Tense.Imperative);
        Optative = tenses.Contains(Tense.Optative);
        Future = tenses.Contains(Tense.Future);

        // Load voice setting (index-based: 0=both, 1=normal, 2=reflexive)
        var voicesCsv = _userData.GetSetting(SettingsKeys.VerbsVoices,
            SettingsHelpers.ToCsv(SettingsKeys.VerbsDefaultVoices));
        VoiceIndex = VoiceIndexFromCsv(voicesCsv);

        // Load lemma range settings
        LemmaMin = _userData.GetSetting(SettingsKeys.VerbsLemmaMin, SettingsKeys.DefaultLemmaMin);
        LemmaMax = _userData.GetSetting(SettingsKeys.VerbsLemmaMax, SettingsKeys.DefaultLemmaMax);
    }

    void SaveSettings()
    {
        if (_isLoading) return;

        // Save daily goal
        _userData.SetSetting(SettingsKeys.VerbsDailyGoal, DailyGoal);

        // Save persons
        var persons = new List<Person>();
        if (FirstPerson) persons.Add(Person.First);
        if (SecondPerson) persons.Add(Person.Second);
        if (ThirdPerson) persons.Add(Person.Third);
        _userData.SetSetting(SettingsKeys.VerbsPersons, SettingsHelpers.ToCsv(persons));

        // Save number setting
        _userData.SetSetting(SettingsKeys.VerbsNumbers, SettingsHelpers.NumberIndexToCsv(NumberIndex));

        // Save enabled patterns (positive list)
        var enabled = new List<VerbPattern>();
        if (PatternAti) enabled.Add(VerbPattern.Ati);
        if (PatternEti) enabled.Add(VerbPattern.Eti);
        if (PatternOti) enabled.Add(VerbPattern.Oti);
        if (PatternAtiLong) enabled.Add(VerbPattern.Āti);
        _userData.SetSetting(SettingsKeys.VerbsPatterns, SettingsHelpers.ToCsv(enabled));

        // Save tenses
        var tenses = new List<Tense>();
        if (Present) tenses.Add(Tense.Present);
        if (Imperative) tenses.Add(Tense.Imperative);
        if (Optative) tenses.Add(Tense.Optative);
        if (Future) tenses.Add(Tense.Future);
        _userData.SetSetting(SettingsKeys.VerbsTenses, SettingsHelpers.ToCsv(tenses));

        // Save voice setting
        _userData.SetSetting(SettingsKeys.VerbsVoices, VoiceIndexToCsv(VoiceIndex));

        // Save lemma range
        _userData.SetSetting(SettingsKeys.VerbsLemmaMin, LemmaMin);
        _userData.SetSetting(SettingsKeys.VerbsLemmaMax, LemmaMax);
    }

    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));

    public ICommand GoToLemmaRangeCommand => new AsyncRelayCommand(() =>
        _navigator.NavigateViewModelAsync<LemmaRangeSettingsViewModel>(this,
            data: new LemmaRangeNavigationData(PracticeType.Conjugation)));

    #region Person settings with "at least one enabled" validation

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisableFirstPerson))]
    [NotifyPropertyChangedFor(nameof(CanDisableSecondPerson))]
    [NotifyPropertyChangedFor(nameof(CanDisableThirdPerson))]
    bool _firstPerson;
    partial void OnFirstPersonChanged(bool value) => SaveSettings();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisableFirstPerson))]
    [NotifyPropertyChangedFor(nameof(CanDisableSecondPerson))]
    [NotifyPropertyChangedFor(nameof(CanDisableThirdPerson))]
    bool _secondPerson;
    partial void OnSecondPersonChanged(bool value) => SaveSettings();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisableFirstPerson))]
    [NotifyPropertyChangedFor(nameof(CanDisableSecondPerson))]
    [NotifyPropertyChangedFor(nameof(CanDisableThirdPerson))]
    bool _thirdPerson;
    partial void OnThirdPersonChanged(bool value) => SaveSettings();

    public bool CanDisableFirstPerson => SecondPerson || ThirdPerson;
    public bool CanDisableSecondPerson => FirstPerson || ThirdPerson;
    public bool CanDisableThirdPerson => FirstPerson || SecondPerson;

    #endregion

    #region Pattern settings with "at least one enabled" validation

    /// <summary>
    /// Total count of enabled patterns.
    /// Used to ensure at least one pattern remains enabled.
    /// </summary>
    int EnabledPatternCount =>
        (PatternAti ? 1 : 0) + (PatternEti ? 1 : 0) + (PatternOti ? 1 : 0) + (PatternAtiLong ? 1 : 0);

    // Pattern: ati (includes hoti pr, atthi pr)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternAti))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternEti))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternOti))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternAtiLong))]
    bool _patternAti = true;
    partial void OnPatternAtiChanged(bool value) => SaveSettings();

    // Pattern: eti
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternAti))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternEti))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternOti))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternAtiLong))]
    bool _patternEti = true;
    partial void OnPatternEtiChanged(bool value) => SaveSettings();

    // Pattern: oti (includes karoti pr, brūti pr)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternAti))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternEti))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternOti))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternAtiLong))]
    bool _patternOti = true;
    partial void OnPatternOtiChanged(bool value) => SaveSettings();

    // Pattern: āti
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternAti))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternEti))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternOti))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternAtiLong))]
    bool _patternAtiLong = true;
    partial void OnPatternAtiLongChanged(bool value) => SaveSettings();

    // CanDisable properties
    public bool CanDisablePatternAti => EnabledPatternCount > 1 || !PatternAti;
    public bool CanDisablePatternEti => EnabledPatternCount > 1 || !PatternEti;
    public bool CanDisablePatternOti => EnabledPatternCount > 1 || !PatternOti;
    public bool CanDisablePatternAtiLong => EnabledPatternCount > 1 || !PatternAtiLong;

    #endregion

    #region Daily goal settings

    [ObservableProperty]
    int _dailyGoal = SettingsKeys.DefaultDailyGoal;
    partial void OnDailyGoalChanged(int value) => SaveSettings();

    /// <summary>
    /// Validates and corrects the daily goal when the field loses focus.
    /// </summary>
    [RelayCommand]
    void ValidateDailyGoal()
    {
        if (_isLoading) return;

        var validated = SettingsHelpers.ValidateDailyGoal(DailyGoal);
        if (validated != DailyGoal)
        {
            _isLoading = true;
            DailyGoal = validated;
            _isLoading = false;
            SaveSettings();
        }
    }

    #endregion

    #region Number settings (dropdown)

    /// <summary>
    /// Index for ComboBox binding. Maps to Number CSV: 0=both, 1=singular, 2=plural.
    /// </summary>
    [ObservableProperty]
    int _numberIndex;
    partial void OnNumberIndexChanged(int value) => SaveSettings();

    /// <summary>Whether to include singular forms in practice.</summary>
    public bool IncludeSingular => NumberIndex is 0 or 1;

    /// <summary>Whether to include plural forms in practice.</summary>
    public bool IncludePlural => NumberIndex is 0 or 2;

    #endregion

    #region Tense settings with "at least one enabled" validation

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisablePresent))]
    [NotifyPropertyChangedFor(nameof(CanDisableImperative))]
    [NotifyPropertyChangedFor(nameof(CanDisableOptative))]
    [NotifyPropertyChangedFor(nameof(CanDisableFuture))]
    bool _present;
    partial void OnPresentChanged(bool value) => SaveSettings();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisablePresent))]
    [NotifyPropertyChangedFor(nameof(CanDisableImperative))]
    [NotifyPropertyChangedFor(nameof(CanDisableOptative))]
    [NotifyPropertyChangedFor(nameof(CanDisableFuture))]
    bool _imperative;
    partial void OnImperativeChanged(bool value) => SaveSettings();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisablePresent))]
    [NotifyPropertyChangedFor(nameof(CanDisableImperative))]
    [NotifyPropertyChangedFor(nameof(CanDisableOptative))]
    [NotifyPropertyChangedFor(nameof(CanDisableFuture))]
    bool _optative;
    partial void OnOptativeChanged(bool value) => SaveSettings();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisablePresent))]
    [NotifyPropertyChangedFor(nameof(CanDisableImperative))]
    [NotifyPropertyChangedFor(nameof(CanDisableOptative))]
    [NotifyPropertyChangedFor(nameof(CanDisableFuture))]
    bool _future;
    partial void OnFutureChanged(bool value) => SaveSettings();

    int EnabledTenseCount => (Present ? 1 : 0) + (Imperative ? 1 : 0) + (Optative ? 1 : 0) + (Future ? 1 : 0);

    public bool CanDisablePresent => EnabledTenseCount > 1 || !Present;
    public bool CanDisableImperative => EnabledTenseCount > 1 || !Imperative;
    public bool CanDisableOptative => EnabledTenseCount > 1 || !Optative;
    public bool CanDisableFuture => EnabledTenseCount > 1 || !Future;

    #endregion

    #region Voice settings (dropdown)

    /// <summary>
    /// Labels for the voice dropdown. Index maps to: 0=both, 1=normal only, 2=reflexive only.
    /// </summary>
    public static readonly string[] VoiceOptions = ["Normal & Reflexive", "Normal only", "Reflexive only"];

    /// <summary>
    /// Index for ComboBox binding. Maps to Voice CSV: 0="1,2", 1="1", 2="2".
    /// </summary>
    [ObservableProperty]
    int _voiceIndex;
    partial void OnVoiceIndexChanged(int value) => SaveSettings();

    /// <summary>Whether to include normal (active) forms.</summary>
    public bool IncludeNormal => VoiceIndex is 0 or 1;

    /// <summary>Whether to include reflexive forms.</summary>
    public bool IncludeReflexive => VoiceIndex is 0 or 2;

    /// <summary>Converts dropdown index to CSV for storage.</summary>
    static string VoiceIndexToCsv(int index) => index switch
    {
        1 => SettingsHelpers.ToCsv([Voice.Normal]),
        2 => SettingsHelpers.ToCsv([Voice.Reflexive]),
        _ => SettingsHelpers.ToCsv([Voice.Normal, Voice.Reflexive])
    };

    /// <summary>Converts CSV to dropdown index.</summary>
    static int VoiceIndexFromCsv(string csv)
    {
        var normal = SettingsHelpers.IncludesNormal(csv);
        var reflexive = SettingsHelpers.IncludesReflexive(csv);
        return (normal, reflexive) switch
        {
            (true, true) => 0,
            (true, false) => 1,
            (false, true) => 2,
            _ => 0
        };
    }

    #endregion

    // Lemma range settings
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RangeText))]
    int _lemmaMin;
    partial void OnLemmaMinChanged(int value) => SaveSettings();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RangeText))]
    int _lemmaMax;
    partial void OnLemmaMaxChanged(int value) => SaveSettings();

    public string RangeText => $"{LemmaMin}–{LemmaMax}";

    public void RefreshRange()
    {
        _isLoading = true;
        LemmaMin = _userData.GetSetting(SettingsKeys.VerbsLemmaMin, SettingsKeys.DefaultLemmaMin);
        LemmaMax = _userData.GetSetting(SettingsKeys.VerbsLemmaMax, SettingsKeys.DefaultLemmaMax);
        _isLoading = false;
        OnPropertyChanged(nameof(RangeText));
    }
}

using PaliPractice.Models;
using PaliPractice.Services.Practice;
using PaliPractice.Services.UserData;

namespace PaliPractice.Presentation.Settings.ViewModels;

[Bindable]
public partial class ConjugationSettingsViewModel : ObservableObject
{
    readonly INavigator _navigator;
    readonly IUserDataService _userData;
    bool _isLoading = true;

    public ConjugationSettingsViewModel(INavigator navigator, IUserDataService userData)
    {
        _navigator = navigator;
        _userData = userData;
        _userData.Initialize();

        LoadSettings();
        _isLoading = false;
    }

    void LoadSettings()
    {
        // Load daily goal
        DailyGoal = _userData.GetSetting(SettingsKeys.ConjugationDailyGoal, SettingsKeys.DefaultDailyGoal);

        // Load person settings
        var persons = ParseEnumSet<Person>(_userData.GetSetting(SettingsKeys.ConjugationPersons, SettingsKeys.DefaultConjugationPersons));
        FirstPerson = persons.Contains(Person.First);
        SecondPerson = persons.Contains(Person.Second);
        ThirdPerson = persons.Contains(Person.Third);

        // Load number setting (migrate old values)
        var numberRaw = _userData.GetSetting(SettingsKeys.ConjugationNumberSetting, "Singular & Plural");
        NumberSetting = MigrateNumberSetting(numberRaw);

        // Load pattern settings (stored as excluded patterns)
        var excluded = ParsePatternSet(_userData.GetSetting(SettingsKeys.ConjugationExcludedPatterns, ""));
        PatternAti = !excluded.Contains("ati");
        PatternEti = !excluded.Contains("eti");
        PatternOti = !excluded.Contains("oti");
        PatternAtiLong = !excluded.Contains("āti");

        // Load tense settings
        var tenses = ParseEnumSet<Tense>(_userData.GetSetting(SettingsKeys.ConjugationTenses, SettingsKeys.DefaultConjugationTenses));
        Present = tenses.Contains(Tense.Present);
        Imperative = tenses.Contains(Tense.Imperative);
        Optative = tenses.Contains(Tense.Optative);
        Future = tenses.Contains(Tense.Future);

        // Load reflexive setting
        ReflexiveSetting = _userData.GetSetting(SettingsKeys.ConjugationReflexive, SettingsKeys.DefaultReflexive);

        // Load lemma range settings
        LemmaMin = _userData.GetSetting(SettingsKeys.ConjugationLemmaMin, SettingsKeys.DefaultLemmaMin);
        LemmaMax = _userData.GetSetting(SettingsKeys.ConjugationLemmaMax, SettingsKeys.DefaultLemmaMax);
    }

    void SaveSettings()
    {
        if (_isLoading) return;

        // Save daily goal
        _userData.SetSetting(SettingsKeys.ConjugationDailyGoal, DailyGoal);

        // Save persons
        var persons = new List<Person>();
        if (FirstPerson) persons.Add(Person.First);
        if (SecondPerson) persons.Add(Person.Second);
        if (ThirdPerson) persons.Add(Person.Third);
        _userData.SetSetting(SettingsKeys.ConjugationPersons, ToEnumString(persons));

        // Save number setting
        _userData.SetSetting(SettingsKeys.ConjugationNumberSetting, NumberSetting);

        // Save pattern exclusions
        var excluded = new List<string>();
        if (!PatternAti) excluded.Add("ati");
        if (!PatternEti) excluded.Add("eti");
        if (!PatternOti) excluded.Add("oti");
        if (!PatternAtiLong) excluded.Add("āti");
        _userData.SetSetting(SettingsKeys.ConjugationExcludedPatterns, string.Join(",", excluded));

        // Save tenses
        var tenses = new List<Tense>();
        if (Present) tenses.Add(Tense.Present);
        if (Imperative) tenses.Add(Tense.Imperative);
        if (Optative) tenses.Add(Tense.Optative);
        if (Future) tenses.Add(Tense.Future);
        _userData.SetSetting(SettingsKeys.ConjugationTenses, ToEnumString(tenses));

        // Save reflexive
        _userData.SetSetting(SettingsKeys.ConjugationReflexive, ReflexiveSetting);

        // Save lemma range
        _userData.SetSetting(SettingsKeys.ConjugationLemmaMin, LemmaMin);
        _userData.SetSetting(SettingsKeys.ConjugationLemmaMax, LemmaMax);
    }

    static HashSet<T> ParseEnumSet<T>(string csv) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(csv))
            return [];

        return csv.Split(',')
            .Select(s => s.Trim())
            .Where(s => int.TryParse(s, out _))
            .Select(s => (T)Enum.ToObject(typeof(T), int.Parse(s)))
            .ToHashSet();
    }

    static string ToEnumString<T>(IEnumerable<T> values) where T : Enum
        => string.Join(",", values.Select(v => Convert.ToInt32(v)));

    static HashSet<string> ParsePatternSet(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return [];

        return csv.Split(',')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToHashSet();
    }

    /// <summary>
    /// Migrates old number setting values to new format.
    /// </summary>
    static string MigrateNumberSetting(string value) => value switch
    {
        "Both" => "Singular & Plural",
        "Singular" => "Singular only",
        "Plural" => "Plural only",
        _ => value
    };

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

    #region Daily goal settings (dropdown)

    public static readonly int[] DailyGoalOptions = [25, 50, 100];

    [ObservableProperty]
    int _dailyGoal = SettingsKeys.DefaultDailyGoal;
    partial void OnDailyGoalChanged(int value) => SaveSettings();

    #endregion

    #region Number settings (dropdown)

    public static readonly string[] NumberOptions = ["Singular & Plural", "Singular only", "Plural only"];

    [ObservableProperty]
    string _numberSetting = "Singular & Plural";
    partial void OnNumberSettingChanged(string value) => SaveSettings();

    /// <summary>Whether to include singular forms in practice.</summary>
    public bool IncludeSingular => NumberSetting is "Singular & Plural" or "Singular only";

    /// <summary>Whether to include plural forms in practice.</summary>
    public bool IncludePlural => NumberSetting is "Singular & Plural" or "Plural only";

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

    // Reflexive setting: "both", "active", "reflexive"
    [ObservableProperty]
    string _reflexiveSetting = SettingsKeys.DefaultReflexive;
    partial void OnReflexiveSettingChanged(string value) => SaveSettings();

    /// <summary>
    /// Whether to include active (non-reflexive) forms.
    /// </summary>
    public bool IncludeActive => ReflexiveSetting is "both" or "active";

    /// <summary>
    /// Whether to include reflexive forms.
    /// </summary>
    public bool IncludeReflexive => ReflexiveSetting is "both" or "reflexive";

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
        LemmaMin = _userData.GetSetting(SettingsKeys.ConjugationLemmaMin, SettingsKeys.DefaultLemmaMin);
        LemmaMax = _userData.GetSetting(SettingsKeys.ConjugationLemmaMax, SettingsKeys.DefaultLemmaMax);
        _isLoading = false;
        OnPropertyChanged(nameof(RangeText));
    }
}

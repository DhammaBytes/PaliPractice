using PaliPractice.Models;
using PaliPractice.Services.Practice;
using PaliPractice.Services.UserData;

namespace PaliPractice.Presentation.Settings.ViewModels;

[Bindable]
public partial class DeclensionSettingsViewModel : ObservableObject
{
    readonly INavigator _navigator;
    readonly IUserDataService _userData;
    bool _isLoading = true;

    // Pattern labels by gender (derived from NounPattern enum)
    public static readonly string[] MascPatterns = NounPatternHelper.MasculinePatterns
        .Select(p => p.ToDisplayLabel()).ToArray();
    public static readonly string[] NtPatterns = NounPatternHelper.NeuterPatterns
        .Select(p => p.ToDisplayLabel()).ToArray();
    public static readonly string[] FemPatterns = NounPatternHelper.FemininePatterns
        .Select(p => p.ToDisplayLabel()).ToArray();

    public DeclensionSettingsViewModel(INavigator navigator, IUserDataService userData)
    {
        _navigator = navigator;
        _userData = userData;
        _userData.Initialize();

        LoadSettings();
        _isLoading = false;
    }

    void LoadSettings()
    {
        // Load pattern settings (stored as excluded patterns per gender)
        var mascExcluded = ParsePatternSet(_userData.GetSetting(SettingsKeys.DeclensionMascExcludedPatterns, ""));
        PatternMascA = !mascExcluded.Contains("a");
        PatternMascI = !mascExcluded.Contains("i");
        PatternMascILong = !mascExcluded.Contains("ī");
        PatternMascU = !mascExcluded.Contains("u");
        PatternMascULong = !mascExcluded.Contains("ū");
        PatternMascAs = !mascExcluded.Contains("as");
        PatternMascAr = !mascExcluded.Contains("ar");
        PatternMascAnt = !mascExcluded.Contains("ant");

        var ntExcluded = ParsePatternSet(_userData.GetSetting(SettingsKeys.DeclensionNtExcludedPatterns, ""));
        PatternNtA = !ntExcluded.Contains("a");
        PatternNtI = !ntExcluded.Contains("i");
        PatternNtU = !ntExcluded.Contains("u");

        var femExcluded = ParsePatternSet(_userData.GetSetting(SettingsKeys.DeclensionFemExcludedPatterns, ""));
        PatternFemA = !femExcluded.Contains("ā");
        PatternFemI = !femExcluded.Contains("i");
        PatternFemILong = !femExcluded.Contains("ī");
        PatternFemU = !femExcluded.Contains("u");
        PatternFemAr = !femExcluded.Contains("ar");

        // Load irregular setting
        IncludeIrregular = _userData.GetSetting(SettingsKeys.DeclensionIncludeIrregular, true);

        // Load number setting (migrate old values)
        var numberRaw = _userData.GetSetting(SettingsKeys.DeclensionNumberSetting, "Singular & Plural");
        NumberSetting = MigrateNumberSetting(numberRaw);

        // Load case settings
        var cases = ParseEnumSet<Case>(_userData.GetSetting(SettingsKeys.DeclensionCases, SettingsKeys.DefaultDeclensionCases));
        Nominative = cases.Contains(Case.Nominative);
        Accusative = cases.Contains(Case.Accusative);
        Instrumental = cases.Contains(Case.Instrumental);
        Dative = cases.Contains(Case.Dative);
        Ablative = cases.Contains(Case.Ablative);
        Genitive = cases.Contains(Case.Genitive);
        Locative = cases.Contains(Case.Locative);
        Vocative = cases.Contains(Case.Vocative);

        // Load lemma range settings
        LemmaMin = _userData.GetSetting(SettingsKeys.DeclensionLemmaMin, SettingsKeys.DefaultLemmaMin);
        LemmaMax = _userData.GetSetting(SettingsKeys.DeclensionLemmaMax, SettingsKeys.DefaultLemmaMax);
    }

    void SaveSettings()
    {
        if (_isLoading) return;

        // Save pattern exclusions
        var mascExcluded = new List<string>();
        if (!PatternMascA) mascExcluded.Add("a");
        if (!PatternMascI) mascExcluded.Add("i");
        if (!PatternMascILong) mascExcluded.Add("ī");
        if (!PatternMascU) mascExcluded.Add("u");
        if (!PatternMascULong) mascExcluded.Add("ū");
        if (!PatternMascAs) mascExcluded.Add("as");
        if (!PatternMascAr) mascExcluded.Add("ar");
        if (!PatternMascAnt) mascExcluded.Add("ant");
        _userData.SetSetting(SettingsKeys.DeclensionMascExcludedPatterns, string.Join(",", mascExcluded));

        var ntExcluded = new List<string>();
        if (!PatternNtA) ntExcluded.Add("a");
        if (!PatternNtI) ntExcluded.Add("i");
        if (!PatternNtU) ntExcluded.Add("u");
        _userData.SetSetting(SettingsKeys.DeclensionNtExcludedPatterns, string.Join(",", ntExcluded));

        var femExcluded = new List<string>();
        if (!PatternFemA) femExcluded.Add("ā");
        if (!PatternFemI) femExcluded.Add("i");
        if (!PatternFemILong) femExcluded.Add("ī");
        if (!PatternFemU) femExcluded.Add("u");
        if (!PatternFemAr) femExcluded.Add("ar");
        _userData.SetSetting(SettingsKeys.DeclensionFemExcludedPatterns, string.Join(",", femExcluded));

        // Save irregular setting
        _userData.SetSetting(SettingsKeys.DeclensionIncludeIrregular, IncludeIrregular);

        // Save number setting
        _userData.SetSetting(SettingsKeys.DeclensionNumberSetting, NumberSetting);

        // Save cases
        var cases = new List<Case>();
        if (Nominative) cases.Add(Case.Nominative);
        if (Accusative) cases.Add(Case.Accusative);
        if (Instrumental) cases.Add(Case.Instrumental);
        if (Dative) cases.Add(Case.Dative);
        if (Ablative) cases.Add(Case.Ablative);
        if (Genitive) cases.Add(Case.Genitive);
        if (Locative) cases.Add(Case.Locative);
        if (Vocative) cases.Add(Case.Vocative);
        _userData.SetSetting(SettingsKeys.DeclensionCases, ToEnumString(cases));

        // Save lemma range
        _userData.SetSetting(SettingsKeys.DeclensionLemmaMin, LemmaMin);
        _userData.SetSetting(SettingsKeys.DeclensionLemmaMax, LemmaMax);
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

    static string ToEnumString<T>(IEnumerable<T> values) where T : Enum
        => string.Join(",", values.Select(v => Convert.ToInt32(v)));

    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));

    public ICommand GoToLemmaRangeCommand => new AsyncRelayCommand(() =>
        _navigator.NavigateViewModelAsync<LemmaRangeSettingsViewModel>(this,
            data: new LemmaRangeNavigationData(PracticeType.Declension)));

    #region Pattern settings with "at least one enabled" validation

    /// <summary>
    /// Total count of enabled patterns across all genders.
    /// Used to ensure at least one pattern remains enabled.
    /// </summary>
    int EnabledPatternCount =>
        (PatternMascA ? 1 : 0) + (PatternMascI ? 1 : 0) + (PatternMascILong ? 1 : 0) +
        (PatternMascU ? 1 : 0) + (PatternMascULong ? 1 : 0) + (PatternMascAs ? 1 : 0) +
        (PatternMascAr ? 1 : 0) + (PatternMascAnt ? 1 : 0) +
        (PatternNtA ? 1 : 0) + (PatternNtI ? 1 : 0) + (PatternNtU ? 1 : 0) +
        (PatternFemA ? 1 : 0) + (PatternFemI ? 1 : 0) + (PatternFemILong ? 1 : 0) +
        (PatternFemU ? 1 : 0) + (PatternFemAr ? 1 : 0);

    // Masculine patterns
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascULong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAs))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAr))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAnt))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemU))]
    bool _patternMascA = true;
    partial void OnPatternMascAChanged(bool value) => SaveSettings();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascULong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAs))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAr))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAnt))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemU))]
    bool _patternMascI = true;
    partial void OnPatternMascIChanged(bool value) => SaveSettings();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascULong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAs))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAr))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAnt))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemU))]
    bool _patternMascILong = true;
    partial void OnPatternMascILongChanged(bool value) => SaveSettings();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascULong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAs))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAr))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAnt))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemU))]
    bool _patternMascU = true;
    partial void OnPatternMascUChanged(bool value) => SaveSettings();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascULong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAs))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAr))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAnt))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemU))]
    bool _patternMascULong = true;
    partial void OnPatternMascULongChanged(bool value) => SaveSettings();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascULong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAs))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAr))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAnt))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemU))]
    bool _patternMascAs = true;
    partial void OnPatternMascAsChanged(bool value) => SaveSettings();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascULong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAs))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAr))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAnt))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemU))]
    bool _patternMascAr = true;
    partial void OnPatternMascArChanged(bool value) => SaveSettings();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascULong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAs))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAr))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAnt))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemU))]
    bool _patternMascAnt = true;
    partial void OnPatternMascAntChanged(bool value) => SaveSettings();

    // Neuter patterns
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascULong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAs))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAr))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAnt))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemU))]
    bool _patternNtA = true;
    partial void OnPatternNtAChanged(bool value) => SaveSettings();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascULong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAs))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAr))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAnt))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemU))]
    bool _patternNtI = true;
    partial void OnPatternNtIChanged(bool value) => SaveSettings();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascULong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAs))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAr))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAnt))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemU))]
    bool _patternNtU = true;
    partial void OnPatternNtUChanged(bool value) => SaveSettings();

    // Feminine patterns
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascULong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAs))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAr))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAnt))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemU))]
    bool _patternFemA = true;
    partial void OnPatternFemAChanged(bool value) => SaveSettings();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascULong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAs))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAr))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAnt))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemU))]
    bool _patternFemI = true;
    partial void OnPatternFemIChanged(bool value) => SaveSettings();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascULong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAs))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAr))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAnt))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemU))]
    bool _patternFemILong = true;
    partial void OnPatternFemILongChanged(bool value) => SaveSettings();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascULong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAs))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAr))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAnt))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemAr))]
    bool _patternFemU = true;
    partial void OnPatternFemUChanged(bool value) => SaveSettings();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascULong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAs))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAr))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternMascAnt))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternNtU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemA))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemI))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemILong))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemU))]
    [NotifyPropertyChangedFor(nameof(CanDisablePatternFemAr))]
    bool _patternFemAr = true;
    partial void OnPatternFemArChanged(bool value) => SaveSettings();

    // CanDisable properties - disable when this is the last enabled pattern
    public bool CanDisablePatternMascA => EnabledPatternCount > 1 || !PatternMascA;
    public bool CanDisablePatternMascI => EnabledPatternCount > 1 || !PatternMascI;
    public bool CanDisablePatternMascILong => EnabledPatternCount > 1 || !PatternMascILong;
    public bool CanDisablePatternMascU => EnabledPatternCount > 1 || !PatternMascU;
    public bool CanDisablePatternMascULong => EnabledPatternCount > 1 || !PatternMascULong;
    public bool CanDisablePatternMascAs => EnabledPatternCount > 1 || !PatternMascAs;
    public bool CanDisablePatternMascAr => EnabledPatternCount > 1 || !PatternMascAr;
    public bool CanDisablePatternMascAnt => EnabledPatternCount > 1 || !PatternMascAnt;
    public bool CanDisablePatternNtA => EnabledPatternCount > 1 || !PatternNtA;
    public bool CanDisablePatternNtI => EnabledPatternCount > 1 || !PatternNtI;
    public bool CanDisablePatternNtU => EnabledPatternCount > 1 || !PatternNtU;
    public bool CanDisablePatternFemA => EnabledPatternCount > 1 || !PatternFemA;
    public bool CanDisablePatternFemI => EnabledPatternCount > 1 || !PatternFemI;
    public bool CanDisablePatternFemILong => EnabledPatternCount > 1 || !PatternFemILong;
    public bool CanDisablePatternFemU => EnabledPatternCount > 1 || !PatternFemU;
    public bool CanDisablePatternFemAr => EnabledPatternCount > 1 || !PatternFemAr;

    // Include irregular nouns toggle
    [ObservableProperty]
    bool _includeIrregular = true;
    partial void OnIncludeIrregularChanged(bool value) => SaveSettings();

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

    #region Case settings with "at least one enabled" validation

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisableNominative))]
    [NotifyPropertyChangedFor(nameof(CanDisableAccusative))]
    [NotifyPropertyChangedFor(nameof(CanDisableInstrumental))]
    [NotifyPropertyChangedFor(nameof(CanDisableDative))]
    [NotifyPropertyChangedFor(nameof(CanDisableAblative))]
    [NotifyPropertyChangedFor(nameof(CanDisableGenitive))]
    [NotifyPropertyChangedFor(nameof(CanDisableLocative))]
    [NotifyPropertyChangedFor(nameof(CanDisableVocative))]
    bool _nominative;
    partial void OnNominativeChanged(bool value) => SaveSettings();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisableNominative))]
    [NotifyPropertyChangedFor(nameof(CanDisableAccusative))]
    [NotifyPropertyChangedFor(nameof(CanDisableInstrumental))]
    [NotifyPropertyChangedFor(nameof(CanDisableDative))]
    [NotifyPropertyChangedFor(nameof(CanDisableAblative))]
    [NotifyPropertyChangedFor(nameof(CanDisableGenitive))]
    [NotifyPropertyChangedFor(nameof(CanDisableLocative))]
    [NotifyPropertyChangedFor(nameof(CanDisableVocative))]
    bool _accusative;
    partial void OnAccusativeChanged(bool value) => SaveSettings();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisableNominative))]
    [NotifyPropertyChangedFor(nameof(CanDisableAccusative))]
    [NotifyPropertyChangedFor(nameof(CanDisableInstrumental))]
    [NotifyPropertyChangedFor(nameof(CanDisableDative))]
    [NotifyPropertyChangedFor(nameof(CanDisableAblative))]
    [NotifyPropertyChangedFor(nameof(CanDisableGenitive))]
    [NotifyPropertyChangedFor(nameof(CanDisableLocative))]
    [NotifyPropertyChangedFor(nameof(CanDisableVocative))]
    bool _instrumental;
    partial void OnInstrumentalChanged(bool value) => SaveSettings();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisableNominative))]
    [NotifyPropertyChangedFor(nameof(CanDisableAccusative))]
    [NotifyPropertyChangedFor(nameof(CanDisableInstrumental))]
    [NotifyPropertyChangedFor(nameof(CanDisableDative))]
    [NotifyPropertyChangedFor(nameof(CanDisableAblative))]
    [NotifyPropertyChangedFor(nameof(CanDisableGenitive))]
    [NotifyPropertyChangedFor(nameof(CanDisableLocative))]
    [NotifyPropertyChangedFor(nameof(CanDisableVocative))]
    bool _dative;
    partial void OnDativeChanged(bool value) => SaveSettings();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisableNominative))]
    [NotifyPropertyChangedFor(nameof(CanDisableAccusative))]
    [NotifyPropertyChangedFor(nameof(CanDisableInstrumental))]
    [NotifyPropertyChangedFor(nameof(CanDisableDative))]
    [NotifyPropertyChangedFor(nameof(CanDisableAblative))]
    [NotifyPropertyChangedFor(nameof(CanDisableGenitive))]
    [NotifyPropertyChangedFor(nameof(CanDisableLocative))]
    [NotifyPropertyChangedFor(nameof(CanDisableVocative))]
    bool _ablative;
    partial void OnAblativeChanged(bool value) => SaveSettings();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisableNominative))]
    [NotifyPropertyChangedFor(nameof(CanDisableAccusative))]
    [NotifyPropertyChangedFor(nameof(CanDisableInstrumental))]
    [NotifyPropertyChangedFor(nameof(CanDisableDative))]
    [NotifyPropertyChangedFor(nameof(CanDisableAblative))]
    [NotifyPropertyChangedFor(nameof(CanDisableGenitive))]
    [NotifyPropertyChangedFor(nameof(CanDisableLocative))]
    [NotifyPropertyChangedFor(nameof(CanDisableVocative))]
    bool _genitive;
    partial void OnGenitiveChanged(bool value) => SaveSettings();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisableNominative))]
    [NotifyPropertyChangedFor(nameof(CanDisableAccusative))]
    [NotifyPropertyChangedFor(nameof(CanDisableInstrumental))]
    [NotifyPropertyChangedFor(nameof(CanDisableDative))]
    [NotifyPropertyChangedFor(nameof(CanDisableAblative))]
    [NotifyPropertyChangedFor(nameof(CanDisableGenitive))]
    [NotifyPropertyChangedFor(nameof(CanDisableLocative))]
    [NotifyPropertyChangedFor(nameof(CanDisableVocative))]
    bool _locative;
    partial void OnLocativeChanged(bool value) => SaveSettings();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisableNominative))]
    [NotifyPropertyChangedFor(nameof(CanDisableAccusative))]
    [NotifyPropertyChangedFor(nameof(CanDisableInstrumental))]
    [NotifyPropertyChangedFor(nameof(CanDisableDative))]
    [NotifyPropertyChangedFor(nameof(CanDisableAblative))]
    [NotifyPropertyChangedFor(nameof(CanDisableGenitive))]
    [NotifyPropertyChangedFor(nameof(CanDisableLocative))]
    [NotifyPropertyChangedFor(nameof(CanDisableVocative))]
    bool _vocative;
    partial void OnVocativeChanged(bool value) => SaveSettings();

    int EnabledCaseCount => (Nominative ? 1 : 0) + (Accusative ? 1 : 0) + (Instrumental ? 1 : 0) +
                            (Dative ? 1 : 0) + (Ablative ? 1 : 0) + (Genitive ? 1 : 0) +
                            (Locative ? 1 : 0) + (Vocative ? 1 : 0);

    public bool CanDisableNominative => EnabledCaseCount > 1 || !Nominative;
    public bool CanDisableAccusative => EnabledCaseCount > 1 || !Accusative;
    public bool CanDisableInstrumental => EnabledCaseCount > 1 || !Instrumental;
    public bool CanDisableDative => EnabledCaseCount > 1 || !Dative;
    public bool CanDisableAblative => EnabledCaseCount > 1 || !Ablative;
    public bool CanDisableGenitive => EnabledCaseCount > 1 || !Genitive;
    public bool CanDisableLocative => EnabledCaseCount > 1 || !Locative;
    public bool CanDisableVocative => EnabledCaseCount > 1 || !Vocative;

    #endregion

    #region Lemma range settings

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
        LemmaMin = _userData.GetSetting(SettingsKeys.DeclensionLemmaMin, SettingsKeys.DefaultLemmaMin);
        LemmaMax = _userData.GetSetting(SettingsKeys.DeclensionLemmaMax, SettingsKeys.DefaultLemmaMax);
        _isLoading = false;
        OnPropertyChanged(nameof(RangeText));
    }

    #endregion
}

using PaliPractice.Models.Inflection;
using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.UserData;

namespace PaliPractice.Presentation.Settings.ViewModels;

[Bindable]
public partial class DeclensionSettingsViewModel : ObservableObject
{
    readonly INavigator _navigator;
    readonly IUserDataRepository _userData;
    bool _isLoading = true;

    // Pattern labels by gender (base patterns only - variants/irregulars inherit from parent)
    public static readonly string[] MascPatterns = Enum.GetValues<NounPattern>()
        .Where(p => p is > NounPattern.None and < NounPattern._VariantMasc)
        .Select(p => p.ToDisplayLabel()).ToArray();
    public static readonly string[] FemPatterns = Enum.GetValues<NounPattern>()
        .Where(p => p is > NounPattern._BaseFem and < NounPattern._VariantFem)
        .Select(p => p.ToDisplayLabel()).ToArray();
    public static readonly string[] NtPatterns = Enum.GetValues<NounPattern>()
        .Where(p => p is > NounPattern._BaseNeut and < NounPattern._VariantNeut)
        .Select(p => p.ToDisplayLabel()).ToArray();

    public DeclensionSettingsViewModel(INavigator navigator, IDatabaseService db)
    {
        _navigator = navigator;
        _userData = db.UserData;

        LoadSettings();
        _isLoading = false;
    }

    void LoadSettings()
    {
        // Load daily goal
        DailyGoal = _userData.GetSetting(SettingsKeys.NounsDailyGoal, SettingsKeys.DefaultDailyGoal);

        // Load enabled patterns per gender (positive list)
        var mascEnabled = SettingsHelpers.FromCsvSet<NounPattern>(
            _userData.GetSetting(SettingsKeys.NounsMascPatterns,
                SettingsHelpers.ToCsv(SettingsKeys.NounsDefaultMascPatterns)));
        PatternMascA = mascEnabled.Contains(NounPattern.AMasc);
        PatternMascI = mascEnabled.Contains(NounPattern.IMasc);
        PatternMascILong = mascEnabled.Contains(NounPattern.ĪMasc);
        PatternMascU = mascEnabled.Contains(NounPattern.UMasc);
        PatternMascULong = mascEnabled.Contains(NounPattern.ŪMasc);
        PatternMascAs = mascEnabled.Contains(NounPattern.AsMasc);
        PatternMascAr = mascEnabled.Contains(NounPattern.ArMasc);
        PatternMascAnt = mascEnabled.Contains(NounPattern.AntMasc);
        
        var neutEnabled = SettingsHelpers.FromCsvSet<NounPattern>(
            _userData.GetSetting(SettingsKeys.NounsNeutPatterns,
                SettingsHelpers.ToCsv(SettingsKeys.NounsDefaultNeutPatterns)));
        PatternNtA = neutEnabled.Contains(NounPattern.ANeut);
        PatternNtI = neutEnabled.Contains(NounPattern.INeut);
        PatternNtU = neutEnabled.Contains(NounPattern.UNeut);
        
        var femEnabled = SettingsHelpers.FromCsvSet<NounPattern>(
            _userData.GetSetting(SettingsKeys.NounsFemPatterns,
                SettingsHelpers.ToCsv(SettingsKeys.NounsDefaultFemPatterns)));
        PatternFemALong = femEnabled.Contains(NounPattern.ĀFem);
        PatternFemI = femEnabled.Contains(NounPattern.IFem);
        PatternFemILong = femEnabled.Contains(NounPattern.ĪFem);
        PatternFemU = femEnabled.Contains(NounPattern.UFem);
        PatternFemAr = femEnabled.Contains(NounPattern.ArFem);
        
        // Load number setting (index-based: 0=both, 1=singular, 2=plural)
        var numbersCsv = _userData.GetSetting(SettingsKeys.NounsNumbers,
            SettingsHelpers.ToCsv(SettingsKeys.DefaultNumbers));
        NumberIndex = SettingsHelpers.NumberIndexFromCsv(numbersCsv);
        
        // Load case settings
        var cases = SettingsHelpers.FromCsvSet<Case>(
            _userData.GetSetting(SettingsKeys.NounsCases,
                SettingsHelpers.ToCsv(SettingsKeys.NounsDefaultCases)));
        Nominative = cases.Contains(Case.Nominative);
        Accusative = cases.Contains(Case.Accusative);
        Instrumental = cases.Contains(Case.Instrumental);
        Dative = cases.Contains(Case.Dative);
        Ablative = cases.Contains(Case.Ablative);
        Genitive = cases.Contains(Case.Genitive);
        Locative = cases.Contains(Case.Locative);
        Vocative = cases.Contains(Case.Vocative);
        
        // Load lemma range settings
        LemmaMin = _userData.GetSetting(SettingsKeys.NounsLemmaMin, SettingsKeys.DefaultLemmaMin);
        LemmaMax = _userData.GetSetting(SettingsKeys.NounsLemmaMax, SettingsKeys.DefaultLemmaMax);
    }

    void SaveSettings()
    {
        if (_isLoading) return;

        // Save daily goal (convert double to int, handling NaN)
        _userData.SetSetting(SettingsKeys.NounsDailyGoal, SettingsHelpers.ValidateDailyGoalDouble(DailyGoal));

        // Save enabled patterns (positive list)
        var mascEnabled = new List<NounPattern>();
        if (PatternMascA) mascEnabled.Add(NounPattern.AMasc);
        if (PatternMascI) mascEnabled.Add(NounPattern.IMasc);
        if (PatternMascILong) mascEnabled.Add(NounPattern.ĪMasc);
        if (PatternMascU) mascEnabled.Add(NounPattern.UMasc);
        if (PatternMascULong) mascEnabled.Add(NounPattern.ŪMasc);
        if (PatternMascAs) mascEnabled.Add(NounPattern.AsMasc);
        if (PatternMascAr) mascEnabled.Add(NounPattern.ArMasc);
        if (PatternMascAnt) mascEnabled.Add(NounPattern.AntMasc);
        _userData.SetSetting(SettingsKeys.NounsMascPatterns, SettingsHelpers.ToCsv(mascEnabled));
        
        var neutEnabled = new List<NounPattern>();
        if (PatternNtA) neutEnabled.Add(NounPattern.ANeut);
        if (PatternNtI) neutEnabled.Add(NounPattern.INeut);
        if (PatternNtU) neutEnabled.Add(NounPattern.UNeut);
        _userData.SetSetting(SettingsKeys.NounsNeutPatterns, SettingsHelpers.ToCsv(neutEnabled));
        
        var femEnabled = new List<NounPattern>();
        if (PatternFemALong) femEnabled.Add(NounPattern.ĀFem);
        if (PatternFemI) femEnabled.Add(NounPattern.IFem);
        if (PatternFemILong) femEnabled.Add(NounPattern.ĪFem);
        if (PatternFemU) femEnabled.Add(NounPattern.UFem);
        if (PatternFemAr) femEnabled.Add(NounPattern.ArFem);
        _userData.SetSetting(SettingsKeys.NounsFemPatterns, SettingsHelpers.ToCsv(femEnabled));
        
        // Save number setting
        _userData.SetSetting(SettingsKeys.NounsNumbers, SettingsHelpers.NumberIndexToCsv(NumberIndex));
        
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
        _userData.SetSetting(SettingsKeys.NounsCases, SettingsHelpers.ToCsv(cases));
        
        // Save lemma range
        _userData.SetSetting(SettingsKeys.NounsLemmaMin, LemmaMin);
        _userData.SetSetting(SettingsKeys.NounsLemmaMax, LemmaMax);
    }

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
        (PatternFemALong ? 1 : 0) + (PatternFemI ? 1 : 0) + (PatternFemILong ? 1 : 0) +
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
    bool _patternFemALong = true;
    partial void OnPatternFemALongChanged(bool value) => SaveSettings();

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
    public bool CanDisablePatternFemA => EnabledPatternCount > 1 || !PatternFemALong;
    public bool CanDisablePatternFemI => EnabledPatternCount > 1 || !PatternFemI;
    public bool CanDisablePatternFemILong => EnabledPatternCount > 1 || !PatternFemILong;
    public bool CanDisablePatternFemU => EnabledPatternCount > 1 || !PatternFemU;
    public bool CanDisablePatternFemAr => EnabledPatternCount > 1 || !PatternFemAr;
    #endregion

    #region Daily goal settings

    /// <summary>
    /// Daily goal bound to NumberBox.Value (double).
    /// Uses double to handle NaN from cleared/invalid NumberBox input.
    /// </summary>
    [ObservableProperty]
    double _dailyGoal = SettingsKeys.DefaultDailyGoal;
    partial void OnDailyGoalChanged(double value) => SaveSettings();

    /// <summary>
    /// Validates and corrects the daily goal when the field loses focus.
    /// Handles NaN from cleared NumberBox and clamps to valid range.
    /// </summary>
    [RelayCommand]
    void ValidateDailyGoal()
    {
        if (_isLoading) return;

        var validated = SettingsHelpers.ValidateDailyGoalDouble(DailyGoal);
        if (Math.Abs(validated - DailyGoal) > 0.5 || double.IsNaN(DailyGoal))
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
        LemmaMin = _userData.GetSetting(SettingsKeys.NounsLemmaMin, SettingsKeys.DefaultLemmaMin);
        LemmaMax = _userData.GetSetting(SettingsKeys.NounsLemmaMax, SettingsKeys.DefaultLemmaMax);
        _isLoading = false;
        OnPropertyChanged(nameof(RangeText));
    }

    #endregion
}

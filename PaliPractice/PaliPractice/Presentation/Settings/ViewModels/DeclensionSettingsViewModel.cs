using PaliPractice.Services.UserData;

namespace PaliPractice.Presentation.Settings.ViewModels;

[Bindable]
public partial class DeclensionSettingsViewModel : ObservableObject
{
    readonly INavigator _navigator;
    readonly IUserDataService _userData;
    bool _isLoading = true;

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
        // Load gender settings
        var genders = ParseEnumSet<Gender>(_userData.GetSetting(SettingsKeys.DeclensionGenders, SettingsKeys.DefaultDeclensionGenders));
        Masculine = genders.Contains(Gender.Masculine);
        Feminine = genders.Contains(Gender.Feminine);
        Neuter = genders.Contains(Gender.Neuter);

        // Load number settings
        var numbers = ParseEnumSet<Number>(_userData.GetSetting(SettingsKeys.DeclensionNumbers, SettingsKeys.DefaultNumbers));
        Singular = numbers.Contains(Number.Singular);
        Plural = numbers.Contains(Number.Plural);

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

        // Save genders
        var genders = new List<Gender>();
        if (Masculine) genders.Add(Gender.Masculine);
        if (Neuter) genders.Add(Gender.Neuter);
        if (Feminine) genders.Add(Gender.Feminine);
        _userData.SetSetting(SettingsKeys.DeclensionGenders, ToEnumString(genders));

        // Save numbers
        var numbers = new List<Number>();
        if (Singular) numbers.Add(Number.Singular);
        if (Plural) numbers.Add(Number.Plural);
        _userData.SetSetting(SettingsKeys.DeclensionNumbers, ToEnumString(numbers));

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

    static string ToEnumString<T>(IEnumerable<T> values) where T : Enum
        => string.Join(",", values.Select(v => Convert.ToInt32(v)));

    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));

    // Gender settings
    [ObservableProperty]
    bool _masculine;
    partial void OnMasculineChanged(bool value) => SaveSettings();

    [ObservableProperty]
    bool _feminine;
    partial void OnFeminineChanged(bool value) => SaveSettings();

    [ObservableProperty]
    bool _neuter;
    partial void OnNeuterChanged(bool value) => SaveSettings();

    // Number settings
    [ObservableProperty]
    bool _singular;
    partial void OnSingularChanged(bool value) => SaveSettings();

    [ObservableProperty]
    bool _plural;
    partial void OnPluralChanged(bool value) => SaveSettings();

    // Case settings
    [ObservableProperty]
    bool _nominative;
    partial void OnNominativeChanged(bool value) => SaveSettings();

    [ObservableProperty]
    bool _accusative;
    partial void OnAccusativeChanged(bool value) => SaveSettings();

    [ObservableProperty]
    bool _instrumental;
    partial void OnInstrumentalChanged(bool value) => SaveSettings();

    [ObservableProperty]
    bool _dative;
    partial void OnDativeChanged(bool value) => SaveSettings();

    [ObservableProperty]
    bool _ablative;
    partial void OnAblativeChanged(bool value) => SaveSettings();

    [ObservableProperty]
    bool _genitive;
    partial void OnGenitiveChanged(bool value) => SaveSettings();

    [ObservableProperty]
    bool _locative;
    partial void OnLocativeChanged(bool value) => SaveSettings();

    [ObservableProperty]
    bool _vocative;
    partial void OnVocativeChanged(bool value) => SaveSettings();

    // Lemma range settings
    [ObservableProperty]
    int _lemmaMin;
    partial void OnLemmaMinChanged(int value) => SaveSettings();

    [ObservableProperty]
    int _lemmaMax;
    partial void OnLemmaMaxChanged(int value) => SaveSettings();
}

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
        // Load person settings
        var persons = ParseEnumSet<Person>(_userData.GetSetting(SettingsKeys.ConjugationPersons, SettingsKeys.DefaultConjugationPersons));
        FirstPerson = persons.Contains(Person.First);
        SecondPerson = persons.Contains(Person.Second);
        ThirdPerson = persons.Contains(Person.Third);

        // Load number settings
        var numbers = ParseEnumSet<Number>(_userData.GetSetting(SettingsKeys.ConjugationNumbers, SettingsKeys.DefaultNumbers));
        Singular = numbers.Contains(Number.Singular);
        Plural = numbers.Contains(Number.Plural);

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

        // Save persons
        var persons = new List<Person>();
        if (FirstPerson) persons.Add(Person.First);
        if (SecondPerson) persons.Add(Person.Second);
        if (ThirdPerson) persons.Add(Person.Third);
        _userData.SetSetting(SettingsKeys.ConjugationPersons, ToEnumString(persons));

        // Save numbers
        var numbers = new List<Number>();
        if (Singular) numbers.Add(Number.Singular);
        if (Plural) numbers.Add(Number.Plural);
        _userData.SetSetting(SettingsKeys.ConjugationNumbers, ToEnumString(numbers));

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

    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));

    // Person settings
    [ObservableProperty]
    bool _firstPerson;
    partial void OnFirstPersonChanged(bool value) => SaveSettings();

    [ObservableProperty]
    bool _secondPerson;
    partial void OnSecondPersonChanged(bool value) => SaveSettings();

    [ObservableProperty]
    bool _thirdPerson;
    partial void OnThirdPersonChanged(bool value) => SaveSettings();

    // Number settings
    [ObservableProperty]
    bool _singular;
    partial void OnSingularChanged(bool value) => SaveSettings();

    [ObservableProperty]
    bool _plural;
    partial void OnPluralChanged(bool value) => SaveSettings();

    // Tense settings (aligned with Tense enum: Present=1, Imperative=2, Optative=3, Future=4)
    [ObservableProperty]
    bool _present;
    partial void OnPresentChanged(bool value) => SaveSettings();

    [ObservableProperty]
    bool _imperative;
    partial void OnImperativeChanged(bool value) => SaveSettings();

    [ObservableProperty]
    bool _optative;
    partial void OnOptativeChanged(bool value) => SaveSettings();

    [ObservableProperty]
    bool _future;
    partial void OnFutureChanged(bool value) => SaveSettings();

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
    int _lemmaMin;
    partial void OnLemmaMinChanged(int value) => SaveSettings();

    [ObservableProperty]
    int _lemmaMax;
    partial void OnLemmaMaxChanged(int value) => SaveSettings();
}

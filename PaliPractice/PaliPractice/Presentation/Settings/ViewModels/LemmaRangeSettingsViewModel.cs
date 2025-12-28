using PaliPractice.Services.Database;
using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.Practice;
using PaliPractice.Services.UserData;

namespace PaliPractice.Presentation.Settings.ViewModels;

/// <summary>
/// Navigation data for LemmaRangeSettingsPage.
/// </summary>
public record LemmaRangeNavigationData(PracticeType PracticeType);

/// <summary>
/// ViewModel for lemma range selection settings.
/// Supports both Declension and Conjugation practice types.
/// Presets: 0=Top100, 1=Top300, 2=Top500, 3=All, 4=Custom
/// </summary>
[Bindable]
public partial class LemmaRangeSettingsViewModel : ObservableObject
{
    readonly INavigator _navigator;
    readonly UserDataRepository _userData;
    readonly PracticeType _practiceType;
    bool _isLoading = true;

    public LemmaRangeSettingsViewModel(
        INavigator navigator,
        IDatabaseService db,
        LemmaRangeNavigationData data)
    {
        _navigator = navigator;
        _userData = db.UserData;
        _practiceType = data.PracticeType;

        // Get total count for "All words" option
        TotalLemmaCount = _practiceType == PracticeType.Declension
            ? db.Nouns.GetCount()
            : db.Verbs.GetCount();

        LoadSettings();
        _isLoading = false;
    }

    /// <summary>
    /// Total number of available lemmas for this practice type.
    /// </summary>
    public int TotalLemmaCount { get; }

    /// <summary>
    /// Page title based on practice type.
    /// </summary>
    public string Title => _practiceType == PracticeType.Declension
        ? "Noun Practice Range"
        : "Verb Practice Range";

    void LoadSettings()
    {
        var (presetKey, minKey, maxKey) = GetSettingsKeys();

        SelectedPreset = _userData.GetSetting(presetKey, SettingsKeys.DefaultLemmaPreset);
        LemmaMin = _userData.GetSetting(minKey, SettingsKeys.DefaultLemmaMin);
        LemmaMax = _userData.GetSetting(maxKey, SettingsKeys.DefaultLemmaMax);
    }

    void SaveSettings()
    {
        if (_isLoading) return;

        var (presetKey, minKey, maxKey) = GetSettingsKeys();

        _userData.SetSetting(presetKey, SelectedPreset);
        _userData.SetSetting(minKey, LemmaMin);
        _userData.SetSetting(maxKey, LemmaMax);
    }

    (string presetKey, string minKey, string maxKey) GetSettingsKeys()
    {
        return _practiceType == PracticeType.Declension
            ? (SettingsKeys.NounsLemmaPreset, SettingsKeys.NounsLemmaMin, SettingsKeys.NounsLemmaMax)
            : (SettingsKeys.VerbsLemmaPreset, SettingsKeys.VerbsLemmaMin, SettingsKeys.VerbsLemmaMax);
    }

    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));

    /// <summary>
    /// Selected preset index: 0=Top100, 1=Top300, 2=Top500, 3=All, 4=Custom.
    /// </summary>
    [ObservableProperty]
    int _selectedPreset;

    partial void OnSelectedPresetChanged(int value)
    {
        // Auto-set min/max when a preset is selected
        switch (value)
        {
            case 0: // Top 100
                _isLoading = true;
                LemmaMin = 1;
                LemmaMax = 100;
                _isLoading = false;
                break;
            case 1: // Top 300
                _isLoading = true;
                LemmaMin = 1;
                LemmaMax = 300;
                _isLoading = false;
                break;
            case 2: // Top 500
                _isLoading = true;
                LemmaMin = 1;
                LemmaMax = 500;
                _isLoading = false;
                break;
            case 3: // All words
                _isLoading = true;
                LemmaMin = 1;
                LemmaMax = TotalLemmaCount;
                _isLoading = false;
                break;
            // case 4: Custom - don't change values
        }

        OnPropertyChanged(nameof(IsCustomRange));
        SaveSettings();
    }

    /// <summary>
    /// Whether custom range is selected (preset index 4).
    /// </summary>
    public bool IsCustomRange => SelectedPreset == 4;

    /// <summary>
    /// Minimum lemma rank (1 = most common).
    /// </summary>
    [ObservableProperty]
    int _lemmaMin = 1;

    partial void OnLemmaMinChanged(int value) => SaveSettings();

    /// <summary>
    /// Maximum lemma rank.
    /// </summary>
    [ObservableProperty]
    int _lemmaMax = 100;

    partial void OnLemmaMaxChanged(int value) => SaveSettings();

    /// <summary>
    /// Validates and corrects the range when a field loses focus.
    /// </summary>
    [RelayCommand]
    void ValidateRange()
    {
        if (_isLoading) return;

        int min = LemmaMin;
        int max = LemmaMax;
        bool changed = false;

        // Ensure min is at least 1
        if (min < 1)
        {
            min = 1;
            changed = true;
        }

        // Ensure max doesn't exceed total
        if (max > TotalLemmaCount)
        {
            max = TotalLemmaCount;
            changed = true;
        }

        // Ensure max is at least 2
        if (max < 2)
        {
            max = 2;
            changed = true;
        }

        // Ensure min < max
        if (min >= max)
        {
            // Prefer adjusting min to be less than max
            min = max - 1;
            if (min < 1)
            {
                min = 1;
                max = 2;
            }
            changed = true;
        }

        if (changed)
        {
            _isLoading = true;
            LemmaMin = min;
            LemmaMax = max;
            _isLoading = false;
            SaveSettings();
        }
    }
}

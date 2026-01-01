using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.UserData;

namespace PaliPractice.Presentation.Settings.ViewModels;

/// <summary>
/// Navigation data for LemmaRangeSettingsPage.
/// </summary>
public record LemmaRangeNavigationData(PracticeType PracticeType);

/// <summary>
/// ViewModel for lemma range selection settings.
/// Supports both Declension and Conjugation practice types.
/// Preset is derived from min/max values: 0=Top100, 1=Top300, 2=Top500, 3=All, 4=Custom.
/// </summary>
[Bindable]
public partial class LemmaRangeSettingsViewModel : ObservableObject
{
    readonly INavigator _navigator;
    readonly IUserDataRepository _userData;
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
        var (minKey, maxKey) = GetSettingsKeys();

        LemmaMin = _userData.GetSetting(minKey, SettingsKeys.DefaultLemmaMin);
        LemmaMax = _userData.GetSetting(maxKey, SettingsKeys.DefaultLemmaMax);

        // Derive preset from loaded min/max values (silent - no side effects during init)
        SetPresetSilently(DerivePresetFromRange(LemmaMin, LemmaMax));
    }

    void SaveSettings()
    {
        if (_isLoading) return;

        var (minKey, maxKey) = GetSettingsKeys();

        _userData.SetSetting(minKey, LemmaMin);
        _userData.SetSetting(maxKey, LemmaMax);
    }

    (string minKey, string maxKey) GetSettingsKeys()
    {
        return _practiceType == PracticeType.Declension
            ? (SettingsKeys.NounsLemmaMin, SettingsKeys.NounsLemmaMax)
            : (SettingsKeys.VerbsLemmaMin, SettingsKeys.VerbsLemmaMax);
    }

    /// <summary>
    /// Derives the preset index from min/max values.
    /// Returns: 0=Top100, 1=Top300, 2=Top500, 3=All, 4=Custom
    /// </summary>
    int DerivePresetFromRange(int min, int max)
    {
        if (min != 1)
            return 4; // Custom

        return max switch
        {
            100 => 0, // Top 100
            300 => 1, // Top 300
            500 => 2, // Top 500
            _ when max == TotalLemmaCount => 3, // All
            _ => 4 // Custom
        };
    }

    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));

    /// <summary>
    /// Selected preset index: 0=Top100, 1=Top300, 2=Top500, 3=All, 4=Custom.
    /// Derived from min/max values, not stored separately.
    /// Manually implemented to control when side effects trigger.
    /// </summary>
    int _selectedPreset;

    public int SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (!SetProperty(ref _selectedPreset, value))
                return;

            // Auto-set min/max when a preset is selected by user
            ApplyPresetRange(value);
            OnPropertyChanged(nameof(IsCustomRange));
            SaveSettings();
        }
    }

    /// <summary>
    /// Sets the preset value without triggering side effects.
    /// Used during initialization and when deriving preset from range changes.
    /// </summary>
    void SetPresetSilently(int value)
    {
        if (_selectedPreset == value)
            return;

        _selectedPreset = value;
        OnPropertyChanged(nameof(SelectedPreset));
        OnPropertyChanged(nameof(IsCustomRange));
    }

    /// <summary>
    /// Applies min/max values for a preset selection.
    /// </summary>
    void ApplyPresetRange(int preset)
    {
        _isLoading = true;
        switch (preset)
        {
            case 0: // Top 100
                LemmaMin = 1;
                LemmaMax = 100;
                break;
            case 1: // Top 300
                LemmaMin = 1;
                LemmaMax = 300;
                break;
            case 2: // Top 500
                LemmaMin = 1;
                LemmaMax = 500;
                break;
            case 3: // All words
                LemmaMin = 1;
                LemmaMax = TotalLemmaCount;
                break;
            // case 4: Custom - don't change values
        }
        _isLoading = false;
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

    partial void OnLemmaMinChanged(int value)
    {
        UpdatePresetFromRange();
        SaveSettings();
    }

    /// <summary>
    /// Maximum lemma rank.
    /// </summary>
    [ObservableProperty]
    int _lemmaMax = 100;

    partial void OnLemmaMaxChanged(int value)
    {
        UpdatePresetFromRange();
        SaveSettings();
    }

    /// <summary>
    /// Updates the preset selection based on current min/max values.
    /// Called when min or max changes.
    /// </summary>
    void UpdatePresetFromRange()
    {
        if (_isLoading) return;

        // Silent update - avoids triggering ApplyPresetRange back
        SetPresetSilently(DerivePresetFromRange(LemmaMin, LemmaMax));
    }

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
            UpdatePresetFromRange();
            SaveSettings();
        }
    }
}

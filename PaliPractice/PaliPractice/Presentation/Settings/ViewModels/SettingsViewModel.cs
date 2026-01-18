using PaliPractice.Presentation.Main.ViewModels;
using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.Feedback;
using PaliPractice.Services.UserData;
using Uno.Toolkit.UI;

namespace PaliPractice.Presentation.Settings.ViewModels;

[Bindable]
public partial class SettingsViewModel : ObservableObject
{
    readonly INavigator _navigator;
    readonly IFeedbackService _feedbackService;
    readonly IStoreReviewService _storeReviewService;
    readonly IUserDataRepository _userData;
    bool _isLoading = true;

    public static readonly string[] ThemeOptions = ["System", "Light", "Dark"];

    public SettingsViewModel(
        INavigator navigator,
        IFeedbackService feedbackService,
        IStoreReviewService storeReviewService,
        IDatabaseService db)
    {
        _navigator = navigator;
        _feedbackService = feedbackService;
        _storeReviewService = storeReviewService;
        _userData = db.UserData;

        LoadSettings();
        _isLoading = false;
    }

    void LoadSettings()
    {
        ThemeIndex = _userData.GetSetting(SettingsKeys.AppearanceTheme, SettingsKeys.DefaultAppearanceTheme);
    }

    #region Theme settings

    /// <summary>
    /// Theme selection index: 0=System, 1=Light, 2=Dark
    /// </summary>
    [ObservableProperty]
    int _themeIndex;

    partial void OnThemeIndexChanged(int value)
    {
        if (_isLoading) return;

        _userData.SetSetting(SettingsKeys.AppearanceTheme, value);

        if (App.MainWindow?.Content is not FrameworkElement root) return;

        switch (value)
        {
            case 1: // Light
                SystemThemeHelper.SetRootTheme(root.XamlRoot, darkMode: false);
                break;
            case 2: // Dark
                SystemThemeHelper.SetRootTheme(root.XamlRoot, darkMode: true);
                break;
            default: // System - reset to default
                root.RequestedTheme = ElementTheme.Default;
                break;
        }
    }

    #endregion

    /// <summary>
    /// Returns true if the store review feature is available on this platform.
    /// </summary>
    public bool IsStoreReviewAvailable => _storeReviewService.IsAvailable;

    /// <summary>
    /// Returns true if the review explanation footer should be shown.
    /// Hidden after user has manually opened the store page.
    /// </summary>
    public bool ShouldShowReviewExplanation =>
        _storeReviewService.IsAvailable && !_storeReviewService.HasUserOpenedStore;

    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));
    public ICommand GoToConjugationSettingsCommand => new AsyncRelayCommand(GoToConjugationSettings);
    public ICommand GoToDeclensionSettingsCommand => new AsyncRelayCommand(GoToDeclensionSettings);
    public ICommand GoToAboutCommand => new AsyncRelayCommand(GoToAbout);
    public ICommand ContactUsCommand => new AsyncRelayCommand(ContactUsAsync);
    public ICommand RateAppCommand => new AsyncRelayCommand(RateAppAsync);

    async Task GoToAbout()
    {
        await _navigator.NavigateViewModelAsync<AboutViewModel>(this);
    }

    async Task GoToConjugationSettings()
    {
        await _navigator.NavigateViewModelAsync<ConjugationSettingsViewModel>(this);
    }

    async Task GoToDeclensionSettings()
    {
        await _navigator.NavigateViewModelAsync<DeclensionSettingsViewModel>(this);
    }

    async Task ContactUsAsync()
    {
        await _feedbackService.SendFeedbackAsync();
    }

    async Task RateAppAsync()
    {
        await _storeReviewService.OpenStorePageAsync();
    }
}

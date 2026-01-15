using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.UserData;
using Uno.Toolkit.UI;

namespace PaliPractice.Presentation.Settings.ViewModels;

[Bindable]
public partial class AppearanceSettingsViewModel : ObservableObject
{
    readonly INavigator _navigator;
    readonly IUserDataRepository _userData;
    bool _isLoading = true;

    public AppearanceSettingsViewModel(INavigator navigator, IDatabaseService db)
    {
        _navigator = navigator;
        _userData = db.UserData;

        LoadSettings();
        _isLoading = false;
    }

    void LoadSettings()
    {
        // Load theme setting (0=System, 1=Light, 2=Dark)
        ThemeIndex = _userData.GetSetting(SettingsKeys.AppearanceTheme, SettingsKeys.DefaultAppearanceTheme);
    }

    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));

    #region Theme settings (RadioButtons SelectedIndex)

    /// <summary>
    /// Theme selection index: 0=System, 1=Light, 2=Dark
    /// </summary>
    [ObservableProperty]
    int _themeIndex;

    partial void OnThemeIndexChanged(int value)
    {
        if (_isLoading) return;

        // Save to database
        _userData.SetSetting(SettingsKeys.AppearanceTheme, value);

        // Apply theme immediately using SystemThemeHelper (per Uno docs)
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
}

namespace PaliPractice.Presentation.Settings.ViewModels;

[Bindable]
public partial class DeclensionSettingsViewModel : ObservableObject
{
    readonly INavigator _navigator;

    public DeclensionSettingsViewModel(INavigator navigator)
    {
        _navigator = navigator;

        // Initialize with mock values
        Masculine = true;
        Feminine = true;
        Neuter = true;

        Singular = true;
        Plural = true;

        Nominative = true;
        Accusative = true;
        Instrumental = true;
        Dative = true;
        Ablative = true;
        Genitive = true;
        Locative = true;
        Vocative = false;
    }

    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));

    // Gender settings
    [ObservableProperty]
    bool _masculine;

    [ObservableProperty]
    bool _feminine;

    [ObservableProperty]
    bool _neuter;

    // Number settings
    [ObservableProperty]
    bool _singular;

    [ObservableProperty]
    bool _plural;

    // Case settings
    [ObservableProperty]
    bool _nominative;

    [ObservableProperty]
    bool _accusative;

    [ObservableProperty]
    bool _instrumental;

    [ObservableProperty]
    bool _dative;

    [ObservableProperty]
    bool _ablative;

    [ObservableProperty]
    bool _genitive;

    [ObservableProperty]
    bool _locative;

    [ObservableProperty]
    bool _vocative;
}

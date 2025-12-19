namespace PaliPractice.Presentation.Settings;

[Bindable]
public partial class ConjugationSettingsViewModel : ObservableObject
{
    readonly INavigator _navigator;

    public ConjugationSettingsViewModel(INavigator navigator)
    {
        _navigator = navigator;

        // Initialize with mock values
        FirstPerson = true;
        SecondPerson = true;
        ThirdPerson = true;

        Singular = true;
        Plural = true;

        Present = true;
        Imperfect = true;
        Aorist = true;
        Future = true;
        Optative = true;
        Imperative = true;
        Conditional = false;
    }

    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));

    // Person settings
    [ObservableProperty]
    bool _firstPerson;

    [ObservableProperty]
    bool _secondPerson;

    [ObservableProperty]
    bool _thirdPerson;

    // Number settings
    [ObservableProperty]
    bool _singular;

    [ObservableProperty]
    bool _plural;

    // Tense settings
    [ObservableProperty]
    bool _present;

    [ObservableProperty]
    bool _imperfect;

    [ObservableProperty]
    bool _aorist;

    [ObservableProperty]
    bool _future;

    [ObservableProperty]
    bool _optative;

    [ObservableProperty]
    bool _imperative;

    [ObservableProperty]
    bool _conditional;
}

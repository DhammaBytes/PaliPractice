using PaliPractice.Presentation.Settings;

namespace PaliPractice.Presentation.Main;

[Bindable]
public class StartViewModel : ObservableObject
{
    readonly INavigator _navigator;

    public StartViewModel(INavigator navigator)
    {
        _navigator = navigator;

        GoToDeclensionCommand = new AsyncRelayCommand(GoToDeclension);
        GoToConjugationCommand = new AsyncRelayCommand(GoToConjugation);
        GoToSettingsCommand = new AsyncRelayCommand(GoToSettings);
        GoToHelpCommand = new AsyncRelayCommand(GoToHelp);
        GoToAboutCommand = new AsyncRelayCommand(GoToAbout);
    }

    public string Title => "Pali Practice";

    public ICommand GoToDeclensionCommand { get; }
    public ICommand GoToConjugationCommand { get; }
    public ICommand GoToSettingsCommand { get; }
    public ICommand GoToHelpCommand { get; }
    public ICommand GoToAboutCommand { get; }

    async Task GoToDeclension()
    {
        await _navigator.NavigateViewModelAsync<Practice.DeclensionPracticeViewModel>(this);
    }

    async Task GoToConjugation()
    {
        await _navigator.NavigateViewModelAsync<Practice.ConjugationPracticeViewModel>(this);
    }

    async Task GoToSettings()
    {
        await _navigator.NavigateViewModelAsync<SettingsViewModel>(this);
    }

    async Task GoToHelp()
    {
        await _navigator.NavigateViewModelAsync<HelpViewModel>(this);
    }

    async Task GoToAbout()
    {
        await _navigator.NavigateViewModelAsync<AboutViewModel>(this);
    }
}

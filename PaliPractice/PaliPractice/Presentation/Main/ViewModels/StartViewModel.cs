using PaliPractice.Presentation.Practice.ViewModels;
using PaliPractice.Presentation.Settings.ViewModels;

namespace PaliPractice.Presentation.Main.ViewModels;

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

    public string Title => "Pāli Practice";

    public ICommand GoToDeclensionCommand { get; }
    public ICommand GoToConjugationCommand { get; }
    public ICommand GoToSettingsCommand { get; }
    public ICommand GoToHelpCommand { get; }
    public ICommand GoToAboutCommand { get; }

    async Task GoToDeclension()
    {
        await _navigator.NavigateViewModelAsync<DeclensionPracticeViewModel>(this);
    }

    async Task GoToConjugation()
    {
        await _navigator.NavigateViewModelAsync<ConjugationPracticeViewModel>(this);
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

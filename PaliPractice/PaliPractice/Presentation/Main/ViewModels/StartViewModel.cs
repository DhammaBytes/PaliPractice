using PaliPractice.Presentation.Practice.ViewModels;
using PaliPractice.Presentation.Settings.ViewModels;
using PaliPractice.Presentation.Statistics.ViewModels;
using PaliPractice.Services.Database;

namespace PaliPractice.Presentation.Main.ViewModels;

[Bindable]
public class StartViewModel : ObservableObject
{
    readonly INavigator _navigator;

    public StartViewModel(INavigator navigator, IDatabaseService databaseService)
    {
        _navigator = navigator;

        GoToDeclensionCommand = new AsyncRelayCommand(GoToDeclension);
        GoToConjugationCommand = new AsyncRelayCommand(GoToConjugation);
        GoToStatisticsCommand = new AsyncRelayCommand(GoToStatistics);
        GoToSettingsCommand = new AsyncRelayCommand(GoToSettings);
        GoToHelpCommand = new AsyncRelayCommand(GoToHelp);

        // Preload repository caches in background (fire-and-forget)
        _ = Task.Run(databaseService.PreloadCaches);
    }

    public string Title => "Pāli Practice";

    public ICommand GoToDeclensionCommand { get; }
    public ICommand GoToConjugationCommand { get; }
    public ICommand GoToStatisticsCommand { get; }
    public ICommand GoToSettingsCommand { get; }
    public ICommand GoToHelpCommand { get; }

    async Task GoToDeclension()
    {
        await _navigator.NavigateViewModelAsync<DeclensionPracticeViewModel>(this);
    }

    async Task GoToConjugation()
    {
        await _navigator.NavigateViewModelAsync<ConjugationPracticeViewModel>(this);
    }

    async Task GoToStatistics()
    {
        await _navigator.NavigateViewModelAsync<StatisticsViewModel>(this);
    }

    async Task GoToSettings()
    {
        await _navigator.NavigateViewModelAsync<SettingsViewModel>(this);
    }

    async Task GoToHelp()
    {
        await _navigator.NavigateViewModelAsync<HelpViewModel>(this);
    }
}

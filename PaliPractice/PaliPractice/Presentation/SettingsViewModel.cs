namespace PaliPractice.Presentation;

[Bindable]
public partial class SettingsViewModel : ObservableObject
{
    readonly INavigator _navigator;

    public SettingsViewModel(INavigator navigator)
    {
        _navigator = navigator;
    }

    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));
    public ICommand GoToConjugationSettingsCommand => new AsyncRelayCommand(GoToConjugationSettings);
    public ICommand GoToDeclensionSettingsCommand => new AsyncRelayCommand(GoToDeclensionSettings);

    async Task GoToConjugationSettings()
    {
        await _navigator.NavigateViewModelAsync<ConjugationSettingsViewModel>(this);
    }

    async Task GoToDeclensionSettings()
    {
        await _navigator.NavigateViewModelAsync<DeclensionSettingsViewModel>(this);
    }
}

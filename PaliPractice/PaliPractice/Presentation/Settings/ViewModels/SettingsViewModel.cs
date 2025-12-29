using PaliPractice.Services.Feedback;

namespace PaliPractice.Presentation.Settings.ViewModels;

[Bindable]
public partial class SettingsViewModel : ObservableObject
{
    readonly INavigator _navigator;
    readonly IStoreReviewService _storeReviewService;

    public SettingsViewModel(INavigator navigator, IStoreReviewService storeReviewService)
    {
        _navigator = navigator;
        _storeReviewService = storeReviewService;
    }

    /// <summary>
    /// Returns true if the store review feature is available on this platform.
    /// </summary>
    public bool IsStoreReviewAvailable => _storeReviewService.IsAvailable;

    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));
    public ICommand GoToConjugationSettingsCommand => new AsyncRelayCommand(GoToConjugationSettings);
    public ICommand GoToDeclensionSettingsCommand => new AsyncRelayCommand(GoToDeclensionSettings);
    public ICommand RateAppCommand => new AsyncRelayCommand(RateAppAsync);

    async Task GoToConjugationSettings()
    {
        await _navigator.NavigateViewModelAsync<ConjugationSettingsViewModel>(this);
    }

    async Task GoToDeclensionSettings()
    {
        await _navigator.NavigateViewModelAsync<DeclensionSettingsViewModel>(this);
    }

    async Task RateAppAsync()
    {
        await _storeReviewService.OpenStorePageAsync();
    }
}

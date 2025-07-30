namespace PaliPractice.Presentation.Behaviors;

[Bindable]
public class NavigationBehavior : ObservableObject
{
    readonly INavigator _navigator;

    public ICommand GoBackCommand { get; }

    public NavigationBehavior(INavigator navigator)
    {
        _navigator = navigator;
        GoBackCommand = new AsyncRelayCommand(GoBack);
    }

    async Task GoBack()
    {
        await _navigator.NavigateBackAsync(this);
    }
}

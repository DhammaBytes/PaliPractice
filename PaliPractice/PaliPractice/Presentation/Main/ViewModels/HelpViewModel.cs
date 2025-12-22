namespace PaliPractice.Presentation.Main.ViewModels;

[Bindable]
public partial class HelpViewModel : ObservableObject
{
    readonly INavigator _navigator;

    public HelpViewModel(INavigator navigator)
    {
        _navigator = navigator;
    }

    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));
}

namespace PaliPractice.Presentation;

[Bindable]
public partial class AboutViewModel : ObservableObject
{
    readonly INavigator _navigator;

    public AboutViewModel(INavigator navigator)
    {
        _navigator = navigator;
    }

    public string AppName => "Pali Practice";
    public string Version => "1.0.0";
    public string Description => "A language learning app for practicing Pali noun declensions and verb conjugations. Built with Uno Platform.";
    public string DataSource => "Word data from the Digital Pāḷi Dictionary (DPD)";

    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));
}

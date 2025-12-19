namespace PaliPractice.Presentation.Main;

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

    public string Description => """
        An app for practicing Pali grammar using spaced repetition technique

        Maintained by the Dhamma Bytes contributors (dhammabytes.org)
        
        Word and grammar data sourced from the Digital Pāḷi Dictionary (dpdict.net)
        """;

    public string LicenseTitle => "License";
    public string LicenseText => """
        Released under CC BY-NC-SA 4.0:
        • CC: Free to share and adapt
        • BY: Attribute the source
        • NC: Non-commercial use only
        • SA: Share under same conditions

        Source code: github.com/DhammaBytes/PaliPractice
        """;

    // Pali blessing - display in italic
    public string Blessing => """
                              Sabbe sattā sukhitā hontu
                              May all beings be happy
                              """;

    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));
}

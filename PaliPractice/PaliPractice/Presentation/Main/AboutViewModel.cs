namespace PaliPractice.Presentation.Main;

[Bindable]
public partial class AboutViewModel : ObservableObject
{
    readonly INavigator _navigator;

    public AboutViewModel(INavigator navigator)
    {
        _navigator = navigator;
    }

    public string AppName => $"Pali Practice v{Version}";
    string Version => "1.0.0";

    public string Description => """
        An app for practicing Pali grammar using spaced repetition technique

        Maintained by the Dhamma Bytes contributors (dhammabytes.org)
        
        Word and grammar data sourced from the Digital Pāḷi Dictionary (dpdict.net)
        
        The app's icon is the quail from SN 47:6 Sakuṇagghi Sutta (The Hawk), hiding behind the rocks in its ancestral territory – a newly plowed field with clumps of earth all turned up.
        
        “Wander, monks, in what is your proper range, your own ancestral territory. In one who wanders in what is his proper range, his own ancestral territory, Māra gains no opening, Māra gains no foothold. And what, for a monk, is his proper range, his own ancestral territory? The four establishings of mindfulness.”
        
        You can read the full sutta here: https://www.dhammatalks.org/suttas/SN/SN47_6.html
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

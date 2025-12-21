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
    string Version => "1.0";

    public string Description => """
        An app for practicing Pāli grammar using spaced repetition, a method based on repeated review over time

        Maintained by the DhammaBytes contributors (https://dhammabytes.org)
        We warmly welcome feedback and help with translating the app into other languages. Please contact us at contact@qotoqot.com
        
        Word and grammar data are sourced from the Digital Pāli Dictionary (https://dpdict.net)
        """;

    public string IconDescription => """
        The app's icon shows the quail from SN 47:6, the Sakuṇagghi Sutta ("The Hawk"), hiding behind rocks in its ancestral territory – a newly plowed field with clumps of earth all turned up.

        "Wander, monks, in what is your proper range, your own ancestral territory. In one who wanders in what is his proper range, his own ancestral territory, Māra gains no opening, Māra gains no foothold. And what, for a monk, is his proper range, his own ancestral territory? The four establishings of mindfulness."

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

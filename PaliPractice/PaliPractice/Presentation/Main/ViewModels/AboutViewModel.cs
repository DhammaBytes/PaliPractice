using PaliPractice.Services.Feedback;

namespace PaliPractice.Presentation.Main.ViewModels;

[Bindable]
public partial class AboutViewModel : ObservableObject
{
    readonly INavigator _navigator;
    readonly IFeedbackService _feedbackService;

    public AboutViewModel(INavigator navigator, IFeedbackService feedbackService)
    {
        _navigator = navigator;
        _feedbackService = feedbackService;
    }

    public string AppName => $"Pāli Practice v{Version}";

    static string Version
    {
        get
        {
            var v = Package.Current.Id.Version;
            return v.Build > 0
                ? $"{v.Major}.{v.Minor}.{v.Build}"
                : $"{v.Major}.{v.Minor}";
        }
    }

    // Use markdown-style links: [text](url) or [text](mailto:email)
    // Static because content doesn't change - allows direct access from the page
    public static string Description => """
An app for practicing Pāli grammar using spaced repetition, a method based on repeated review over time.

Maintained by the [DhammaBytes](https://dhammabytes.org) contributors. We warmly welcome feedback and help with translating the app into other languages.

Word and grammar data are sourced from the [Digital Pāli Dictionary](https://dpdict.net).
""";

    public static string IconTitle => "App Icon";
    public static string IconDescription => """
The icon shows the quail from SN 47:6, the Sakuṇagghi Sutta ("The Hawk"), hiding behind rocks in its ancestral territory – a newly plowed field with clumps of earth all turned up.

*"Wander, monks, in what is your proper range, your own ancestral territory. In one who wanders in what is his proper range, his own ancestral territory, Māra gains no opening, Māra gains no foothold. And what, for a monk, is his proper range, his own ancestral territory? The four establishings of mindfulness."*

You can [read the full sutta here](https://www.dhammatalks.org/suttas/SN/SN47_6.html).

Illustration by Irina Mir ([@irmirx](https://www.instagram.com/irmirx)).
""";

    public static string LicenseTitle => "App License";
    public static string LicenseText => """
Released under CC BY-NC-SA 4.0:
• CC: Free to share and adapt
• BY: Attribute the source
• NC: Non-commercial use only
• SA: Share under same conditions

[Source code on GitHub](https://github.com/DhammaBytes/PaliPractice).
""";

    public static string FontsTitle => "Fonts";
    public static string FontsText => """
Libertinus Sans is used for Pāli because its serif-like pronounced rhythm and differentiated lowercase characters help with letter-by-letter decoding of unfamiliar words.

Source Sans 3 is used for the rest of the texts and UI elements.

Both fonts are licensed under the SIL Open Font License (OFL 1.1).
""";

    public static string LibrariesTitle => "Libraries & Frameworks";
    public static string LibrariesText => """
• [Uno Platform](https://github.com/unoplatform/uno): Apache License 2.0, © Uno Platform Inc.
• [Markdig](https://github.com/xoofx/markdig): BSD 2-Clause License, © Alexandre Mutel.
• [sqlite-net](https://github.com/praeclarum/sqlite-net): MIT License, © Krueger Systems, Inc.
""";

    public static string Blessing => """
*Sabbe sattā sukhitā hontu*
May all beings be happy
""";

    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));
    public ICommand ContactUsCommand => new AsyncRelayCommand(() => _feedbackService.SendFeedbackAsync());
}

using PaliPractice.Services.Feedback;
using PaliPractice.Localization;

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

    public string AppName => AppTextFormatter.FormatAppNameWithVersion(Version);

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
    public static string Description => AppText.Get("About.Description");

    public static string IconTitle => AppText.Get("About.IconTitle");
    public static string IconDescription => AppText.Get("About.IconDescription");

    public static string LicenseTitle => AppText.Get("About.LicenseTitle");
    public static string LicenseText => AppText.Get("About.LicenseText");

    public static string FontsTitle => AppText.Get("About.FontsTitle");
    public static string FontsText => AppText.Get("About.FontsText");

    public static string LibrariesTitle => AppText.Get("About.LibrariesTitle");
    public static string LibrariesText => AppText.Get("About.LibrariesText");

    public const string BlessingPali = "sabbe sattā sukhitā hontu";
    public static string BlessingEnglish => AppText.Get("About.BlessingTranslation");

    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));
    public ICommand ContactUsCommand => new AsyncRelayCommand(() => _feedbackService.SendFeedbackAsync());
}

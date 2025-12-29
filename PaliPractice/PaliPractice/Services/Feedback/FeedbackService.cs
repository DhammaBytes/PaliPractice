using System.Globalization;
using System.Text;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Email;
using Windows.System.Profile;

namespace PaliPractice.Services.Feedback;

public interface IFeedbackService
{
    Task SendFeedbackAsync();
}

public sealed class FeedbackService : IFeedbackService
{
    const string SupportEmail = "contact@dhammabytes.org";

    readonly ILogger<FeedbackService> _logger;
    readonly IDatabaseService _databaseService;

    public FeedbackService(ILogger<FeedbackService> logger, IDatabaseService databaseService)
    {
        _logger = logger;
        _databaseService = databaseService;
    }

    public async Task SendFeedbackAsync()
    {
        try
        {
            var body = BuildEmailBody();
            var email = new EmailMessage
            {
                Body = body
            };
            email.To.Add(new EmailRecipient(SupportEmail));

            await EmailManager.ShowComposeNewEmailAsync(email);
            _logger.LogInformation("Email composer opened");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open email composer");
        }
    }

    string BuildEmailBody()
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine("Diagnostic information (please keep for support):");
        sb.AppendLine();

        try
        {
            sb.AppendLine($"App: {GetAppVersion()}");
            sb.AppendLine($"Device: {GetDeviceInfo()}");
            sb.AppendLine($"OS: {GetOsInfo()}");
            sb.AppendLine($"Locale: {GetLocaleInfo()}");
            sb.AppendLine();
            sb.AppendLine("Settings:");
            AppendSettings(sb);
        }
        catch (Exception ex)
        {
            sb.AppendLine($"(Could not gather diagnostics: {ex.Message})");
        }

        return sb.ToString();
    }

    void AppendSettings(StringBuilder sb)
    {
        try
        {
            var settings = _databaseService.UserData.GetAllSettings();
            if (settings.Count == 0)
            {
                sb.AppendLine("  (default settings)");
                return;
            }

            foreach (var (key, value) in settings.OrderBy(kv => kv.Key))
            {
                sb.AppendLine($"  {key}: {value}");
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"  (Could not read settings: {ex.Message})");
        }
    }

    static string GetAppVersion()
    {
        try
        {
            var version = Package.Current.Id.Version;
            var versionString = $"{version.Major}.{version.Minor}.{version.Build}";

#if DEBUG
            return $"{versionString} (DEBUG)";
#else
            return versionString;
#endif
        }
        catch
        {
            return "Unknown";
        }
    }

    static string GetDeviceInfo()
    {
        try
        {
            // AnalyticsInfo.DeviceForm returns: Mobile, Tablet, Desktop, etc.
            var deviceForm = AnalyticsInfo.DeviceForm;
            return string.IsNullOrEmpty(deviceForm) ? "Unknown" : deviceForm;
        }
        catch
        {
            return "Unknown";
        }
    }

    static string GetOsInfo()
    {
        try
        {
            // VersionInfo.DeviceFamily is like "iOS.Mobile" or "Android.Mobile"
            var deviceFamily = AnalyticsVersionInfo.DeviceFamily;

            // VersionInfo.DeviceFamilyVersion gives OS version
            var osVersion = AnalyticsVersionInfo.DeviceFamilyVersion;

            if (!string.IsNullOrEmpty(deviceFamily))
            {
                // For WASM, osVersion is the full user agent - truncate it
                if (osVersion?.Length > 50)
                    osVersion = osVersion[..50] + "...";

                return string.IsNullOrEmpty(osVersion)
                    ? deviceFamily
                    : $"{deviceFamily} {osVersion}";
            }

            return GetOsPlatform();
        }
        catch
        {
            return GetOsPlatform();
        }
    }

    static string GetOsPlatform()
    {
#if __IOS__
        return "iOS";
#elif __ANDROID__
        return "Android";
#elif HAS_UNO_SKIA
        if (OperatingSystem.IsMacOS()) return "macOS";
        if (OperatingSystem.IsWindows()) return "Windows";
        if (OperatingSystem.IsLinux()) return "Linux";
        return "Desktop";
#else
        return "Unknown";
#endif
    }

    static AnalyticsVersionInfo AnalyticsVersionInfo =>
        AnalyticsInfo.VersionInfo;

    static string GetLocaleInfo()
    {
        try
        {
            var culture = CultureInfo.CurrentCulture;
            var region = "??";
            try
            {
                region = RegionInfo.CurrentRegion.TwoLetterISORegionName;
            }
            catch { /* keep ?? */ }

            return $"{culture.Name}, {region}";
        }
        catch
        {
            return "Unknown";
        }
    }
}

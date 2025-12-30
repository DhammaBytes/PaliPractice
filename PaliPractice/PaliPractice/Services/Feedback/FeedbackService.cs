using System.Globalization;
using System.Text;
using PaliPractice.Services.Feedback.Providers;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Email;

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
    readonly IDeviceInfoProvider _deviceInfo;

    public FeedbackService(
        ILogger<FeedbackService> logger,
        IDatabaseService databaseService,
        IDeviceInfoProvider deviceInfo)
    {
        _logger = logger;
        _databaseService = databaseService;
        _deviceInfo = deviceInfo;
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
            var appVersion = GetAppVersion();
            if (appVersion != null)
                sb.AppendLine($"App: {appVersion}");

            var deviceLine = BuildDeviceLine();
            if (deviceLine != null)
                sb.AppendLine($"Device: {deviceLine}");

            if (_deviceInfo.OsVersion != null)
                sb.AppendLine($"OS: {_deviceInfo.OsVersion}");

            var locale = GetLocaleInfo();
            if (locale != null)
                sb.AppendLine($"Locale: {locale}");

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

    string? BuildDeviceLine()
    {
        var parts = new List<string>();

        // "Apple iPhone13,4" or "Samsung Galaxy S21" or just "iPhone13,4"
        if (_deviceInfo.Manufacturer != null && _deviceInfo.Model != null)
        {
            // Skip manufacturer if model already contains it (e.g., "Samsung" in "Samsung Galaxy")
            if (_deviceInfo.Model.Contains(_deviceInfo.Manufacturer, StringComparison.OrdinalIgnoreCase))
                parts.Add(_deviceInfo.Model);
            else
                parts.Add($"{_deviceInfo.Manufacturer} {_deviceInfo.Model}");
        }
        else if (_deviceInfo.Model != null)
        {
            parts.Add(_deviceInfo.Model);
        }
        else if (_deviceInfo.Manufacturer != null)
        {
            parts.Add(_deviceInfo.Manufacturer);
        }

        if (parts.Count == 0)
            return null;

        // Add device type in parentheses if available: "Apple iPhone13,4 (Phone)"
        if (_deviceInfo.DeviceType != null)
            return $"{string.Join(" ", parts)} ({_deviceInfo.DeviceType})";

        return string.Join(" ", parts);
    }

    static string? GetAppVersion()
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
            return null;
        }
    }

    static string? GetLocaleInfo()
    {
        try
        {
            var culture = CultureInfo.CurrentCulture;
            if (string.IsNullOrEmpty(culture.Name))
                return null;

            try
            {
                var region = RegionInfo.CurrentRegion.TwoLetterISORegionName;
                return $"{culture.Name}, {region}";
            }
            catch
            {
                return culture.Name;
            }
        }
        catch
        {
            return null;
        }
    }
}

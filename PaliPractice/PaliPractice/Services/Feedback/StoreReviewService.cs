using Windows.Services.Store;
using Windows.System;

namespace PaliPractice.Services.Feedback;

/// <summary>
/// Service for store reviews and opening store pages.
/// </summary>
public interface IStoreReviewService
{
    /// <summary>
    /// Returns true if the current platform supports opening the store page.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Opens the app's store page in the platform's app store.
    /// </summary>
    Task OpenStorePageAsync();

    /// <summary>
    /// Requests an in-app review dialog (for use after positive user interactions).
    /// Available on iOS, Android (Google Play), and macOS (Mac App Store).
    /// </summary>
    Task RequestInAppReviewAsync();
}

public sealed class StoreReviewService : IStoreReviewService
{
    readonly ILogger<StoreReviewService> _logger;

    // App Store IDs - update these when publishing
    const string AppleAppId = "0000000000"; // TODO: Replace with actual App Store ID
    const string AndroidPackageId = "org.dhammabytes.palipractice";

    public StoreReviewService(ILogger<StoreReviewService> logger)
    {
        _logger = logger;
    }

    public bool IsAvailable => CheckAvailability();

    public async Task OpenStorePageAsync()
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("Store page not available on this platform");
            return;
        }

        var url = GetStoreUrl();
        if (string.IsNullOrEmpty(url))
        {
            _logger.LogWarning("No store URL configured for this platform");
            return;
        }

        try
        {
            _logger.LogInformation("Opening store page: {Url}", url);
            await Launcher.LaunchUriAsync(new Uri(url));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open store page");
        }
    }

    public async Task RequestInAppReviewAsync()
    {
        if (!IsInAppReviewAvailable())
        {
            _logger.LogWarning("In-app review not available on this platform");
            return;
        }

        try
        {
            var result = await StoreContext.GetDefault().RequestRateAndReviewAppAsync();
            _logger.LogInformation("In-app review result: {Status}", result.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request in-app review");
        }
    }

    static string? GetStoreUrl()
    {
#if __IOS__
        // iOS App Store
        return $"itms-apps://itunes.apple.com/app/id{AppleAppId}?action=write-review";
#elif __ANDROID__
        // Google Play Store - market:// URL opens Play Store app directly
        return $"market://details?id={AndroidPackageId}";
#elif HAS_UNO_SKIA
        // Desktop platforms
        if (OperatingSystem.IsMacOS())
        {
            // Mac App Store
            return $"macappstore://itunes.apple.com/app/id{AppleAppId}?action=write-review";
        }
        // Linux/Windows - no store support yet
        return null;
#else
        return null;
#endif
    }

    static bool CheckAvailability()
    {
#if __IOS__
        return true;
#elif __ANDROID__
        return true;
#elif HAS_UNO_SKIA
        // On desktop, only available on macOS (for Mac App Store)
        return OperatingSystem.IsMacOS() && IsMacAppStoreInstall();
#else
        return false;
#endif
    }

    static bool IsInAppReviewAvailable()
    {
#if __IOS__
        return true;
#elif __ANDROID__
        return true;
#elif HAS_UNO_SKIA
        return OperatingSystem.IsMacOS() && IsMacAppStoreInstall();
#else
        return false;
#endif
    }

#if HAS_UNO_SKIA
    static bool IsMacAppStoreInstall()
    {
        try
        {
            // Mac App Store installs have a receipt at Contents/_MASReceipt/receipt
            var bundlePath = AppContext.BaseDirectory;

            // Navigate up from the executable to find Contents folder
            // Typical path: MyApp.app/Contents/MacOS/MyApp
            var contentsPath = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(bundlePath));
            if (string.IsNullOrEmpty(contentsPath))
                return false;

            var receiptPath = System.IO.Path.Combine(contentsPath, "_MASReceipt", "receipt");
            return System.IO.File.Exists(receiptPath);
        }
        catch
        {
            return false;
        }
    }
#endif
}

namespace PaliPractice.Services.Feedback.Helpers;

/// <summary>
/// Platform-specific helpers for store review functionality.
/// Contains all preprocessor-gated code for cleaner separation.
/// </summary>
public static class StorePlatformHelper
{
    // App Store IDs
    public const string AppleAppId = "6742040410";
    public const string AndroidPackageId = "org.dhammabytes.palipractice";

    /// <summary>
    /// Returns true if the current platform supports opening the store page.
    /// </summary>
    public static bool IsStoreAvailable()
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

    /// <summary>
    /// Returns true if in-app review prompt is available on this platform.
    /// </summary>
    public static bool IsInAppReviewAvailable()
    {
#if __IOS__
        return true;
#elif __ANDROID__
        return true;
#elif HAS_UNO_SKIA
        // macOS in-app review not implemented yet
        return false;
#else
        return false;
#endif
    }

    /// <summary>
    /// Gets the platform-specific store URL for opening the app's store page.
    /// Returns null if not available on this platform.
    /// </summary>
    public static string? GetStoreUrl()
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

#if HAS_UNO_SKIA
    /// <summary>
    /// Checks if the app was installed from the Mac App Store by looking for the receipt.
    /// </summary>
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

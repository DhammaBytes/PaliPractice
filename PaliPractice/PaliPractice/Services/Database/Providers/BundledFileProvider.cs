using Windows.Storage;
using IOPath = System.IO.Path;

namespace PaliPractice.Services.Database.Providers;

/// <summary>
/// Platform-specific implementation of bundled file access.
/// Handles the different ways each platform provides access to app-bundled assets.
/// </summary>
public class BundledFileProvider : IBundledFileProvider
{
    readonly string _userDataDirectory;

    public BundledFileProvider()
    {
        // Use Uno's ApplicationData API for consistent cross-platform storage
        // Note: macOS path override is configured in Program.cs to use ~/Library/Application Support/
        _userDataDirectory = ApplicationData.Current.LocalFolder.Path;
    }

    public string GetUserDataDirectory() => _userDataDirectory;

    public string? TryGetReadOnlyPath(string relativePath)
    {
#if __IOS__
        // iOS: Get path from app bundle
        var fileName = IOPath.GetFileNameWithoutExtension(relativePath);
        var extension = IOPath.GetExtension(relativePath).TrimStart('.');
        var directory = IOPath.GetDirectoryName(relativePath);

        // NSBundle.MainBundle.PathForResource can find files in subdirectories
        var bundlePath = Foundation.NSBundle.MainBundle.PathForResource(
            fileName,
            extension,
            directory);

        return bundlePath;
#elif __ANDROID__
        // Android: Assets are inside APK, cannot be accessed via path
        // Must copy to local storage
        return null;
#else
        // Desktop (Windows/Linux/macOS) and WASM:
        // Check if running on desktop where files are in app directory
        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            // Files are deployed alongside the executable
            var basePath = AppContext.BaseDirectory;
            var fullPath = IOPath.Combine(basePath, relativePath);

            if (File.Exists(fullPath))
                return fullPath;
        }

        // WASM or file not found - need to copy
        return null;
#endif
    }

    public async Task<Stream> OpenReadStreamAsync(string relativePath)
    {
#if __ANDROID__
        // Android: Open from assets
        var assets = Android.App.Application.Context.Assets
            ?? throw new InvalidOperationException("Android assets not available");

        // Android assets use forward slashes
        var assetPath = relativePath.Replace('\\', '/');
        return assets.Open(assetPath)
            ?? throw new FileNotFoundException($"Asset not found: {assetPath}");
#else
        // For all other platforms, use StorageFile API
        // This works for Desktop, WASM, and iOS (fallback)
        var uri = new Uri($"ms-appx:///{relativePath.Replace('\\', '/')}");
        var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
        return await file.OpenStreamForReadAsync();
#endif
    }
}

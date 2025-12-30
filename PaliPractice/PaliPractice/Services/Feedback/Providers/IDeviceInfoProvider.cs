namespace PaliPractice.Services.Feedback.Providers;

/// <summary>
/// Platform abstraction for retrieving device information.
/// Different platforms expose device details through different APIs.
/// Returns null when information is unavailable.
/// </summary>
public interface IDeviceInfoProvider
{
    /// <summary>
    /// Gets the device model identifier.
    /// iOS: "iPhone13,4", Android: "Galaxy S21", Windows: "Surface Pro 8"
    /// </summary>
    string? Model { get; }

    /// <summary>
    /// Gets the device manufacturer.
    /// iOS: "Apple", Android: "Samsung", Windows: "Microsoft"
    /// </summary>
    string? Manufacturer { get; }

    /// <summary>
    /// Gets the operating system name and version.
    /// iOS: "iOS 17.2", Android: "Android 14 (API 34)", Windows: "Windows 10.0.22621"
    /// </summary>
    string? OsVersion { get; }

    /// <summary>
    /// Gets the device form factor.
    /// "Phone", "Tablet", "Desktop", etc.
    /// </summary>
    string? DeviceType { get; }
}

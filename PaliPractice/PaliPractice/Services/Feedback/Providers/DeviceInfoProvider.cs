using System.Runtime.InteropServices;
using Windows.System.Profile;
#if __IOS__
using UIKit;
using ObjCRuntime;
#elif __ANDROID__
using Android.OS;
#elif WINDOWS
using Windows.Security.ExchangeActiveSyncProvisioning;
#endif

namespace PaliPractice.Services.Feedback.Providers;

/// <summary>
/// Platform-specific implementation of device information retrieval.
/// Uses native APIs on each platform for accurate device details.
/// </summary>
public sealed class DeviceInfoProvider : IDeviceInfoProvider
{
    public string? Model => GetModel();
    public string? Manufacturer => GetManufacturer();
    public string? OsVersion => GetOsVersion();
    public string? DeviceType => GetDeviceType();

    static string? GetModel()
    {
        try
        {
#if __IOS__
            return GetSystemProperty("hw.machine");
#elif __ANDROID__
            return NullIfEmpty(Build.Model);
#elif WINDOWS
            if (OperatingSystem.IsWindows())
            {
                var deviceInfo = new EasClientDeviceInformation();
                return NullIfEmpty(deviceInfo.SystemProductName);
            }
            return null;
#else
            return null;
#endif
        }
        catch
        {
            return null;
        }
    }

    static string? GetManufacturer()
    {
        try
        {
#if __IOS__
            return "Apple";
#elif __ANDROID__
            return NullIfEmpty(Build.Manufacturer);
#elif WINDOWS
            if (OperatingSystem.IsWindows())
            {
                var deviceInfo = new EasClientDeviceInformation();
                return NullIfEmpty(deviceInfo.SystemManufacturer);
            }
            return null;
#else
            return null;
#endif
        }
        catch
        {
            return null;
        }
    }

    static string? GetOsVersion()
    {
        try
        {
#if __IOS__
            var version = UIDevice.CurrentDevice.SystemVersion;
            return string.IsNullOrEmpty(version) ? null : $"iOS {version}";
#elif __ANDROID__
            var version = Build.VERSION.Release;
            if (string.IsNullOrEmpty(version)) return null;
            var apiLevel = (int)Build.VERSION.SdkInt;
            return $"Android {version} (API {apiLevel})";
#elif WINDOWS
            if (OperatingSystem.IsWindows())
            {
                var versionInfo = AnalyticsInfo.VersionInfo;
                if (ulong.TryParse(versionInfo.DeviceFamilyVersion, out var version))
                {
                    var major = (version >> 48) & 0xFFFF;
                    var minor = (version >> 32) & 0xFFFF;
                    var build = (version >> 16) & 0xFFFF;
                    return $"Windows {major}.{minor}.{build}";
                }
                return "Windows";
            }
            return GetDesktopOsVersion();
#else
            return GetDesktopOsVersion();
#endif
        }
        catch
        {
            return GetDesktopOsFallback();
        }
    }

    static string? GetDeviceType()
    {
        try
        {
#if __IOS__
            return UIDevice.CurrentDevice.UserInterfaceIdiom switch
            {
                UIUserInterfaceIdiom.Phone => "Phone",
                UIUserInterfaceIdiom.Pad => "Tablet",
                UIUserInterfaceIdiom.TV => "TV",
                UIUserInterfaceIdiom.CarPlay => "CarPlay",
                UIUserInterfaceIdiom.Mac => "Desktop",
                _ => null
            };
#elif __ANDROID__
            var context = Android.App.Application.Context;
            var config = context.Resources?.Configuration;
            if (config != null)
            {
                var smallestWidth = config.SmallestScreenWidthDp;
                return smallestWidth >= 600 ? "Tablet" : "Phone";
            }
            return null;
#elif WINDOWS
            if (OperatingSystem.IsWindows())
            {
                var deviceFamily = AnalyticsInfo.VersionInfo.DeviceFamily;
                return deviceFamily switch
                {
                    "Windows.Mobile" => "Phone",
                    "Windows.Desktop" => "Desktop",
                    "Windows.Xbox" => "Console",
                    "Windows.Team" => "Hub",
                    "Windows.IoT" => "IoT",
                    _ => "Desktop"
                };
            }
            return "Desktop";
#else
            return "Desktop";
#endif
        }
        catch
        {
            return null;
        }
    }

#if __IOS__
    [DllImport(Constants.SystemLibrary, EntryPoint = "sysctlbyname")]
    static extern int SysctlByName(
        [MarshalAs(UnmanagedType.LPStr)] string property,
        IntPtr output,
        IntPtr oldLen,
        IntPtr newp,
        uint newlen);

    static string? GetSystemProperty(string property)
    {
        var lengthPtr = Marshal.AllocHGlobal(sizeof(int));
        try
        {
            SysctlByName(property, IntPtr.Zero, lengthPtr, IntPtr.Zero, 0);
            var propertyLength = Marshal.ReadInt32(lengthPtr);

            if (propertyLength == 0)
                return null;

            var valuePtr = Marshal.AllocHGlobal(propertyLength);
            try
            {
                SysctlByName(property, valuePtr, lengthPtr, IntPtr.Zero, 0);
                return Marshal.PtrToStringAnsi(valuePtr);
            }
            finally
            {
                Marshal.FreeHGlobal(valuePtr);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(lengthPtr);
        }
    }
#endif

    static string? GetDesktopOsVersion()
    {
        if (OperatingSystem.IsMacOS())
            return $"macOS {System.Environment.OSVersion.Version}";
        if (OperatingSystem.IsLinux())
            return $"Linux {System.Environment.OSVersion.Version}";
        if (OperatingSystem.IsWindows())
            return $"Windows {System.Environment.OSVersion.Version}";
        return null;
    }

    static string? GetDesktopOsFallback()
    {
        if (OperatingSystem.IsMacOS()) return "macOS";
        if (OperatingSystem.IsWindows()) return "Windows";
        if (OperatingSystem.IsLinux()) return "Linux";
        return null;
    }

    static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}

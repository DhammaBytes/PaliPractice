using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace PaliPractice.Localization;

internal static class MacSystemLanguage
{
    const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    const uint Utf8Encoding = 0x08000100;

    public static void Apply()
    {
        if (!OperatingSystem.IsMacOS()) return;

        var culture = ReadPreferredCulture();
        if (culture is null) return;

        // .NET desktop can inherit an English LC_ALL even when macOS prefers
        // Spanish or Russian. Keep regional formatting independent of UI text.
        Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = culture.Name;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    static CultureInfo? ReadPreferredCulture()
    {
        var bundle = CFBundleGetMainBundle();
        if (bundle == IntPtr.Zero) return null;

        var bundleId = CFBundleGetIdentifier(bundle);
        if (bundleId == IntPtr.Zero) return null;

        var key = CFStringCreateWithCString(IntPtr.Zero, "AppleLanguages", Utf8Encoding);
        if (key == IntPtr.Zero) return null;

        try
        {
            var languages = CFPreferencesCopyAppValue(key, bundleId);
            if (languages == IntPtr.Zero) return null;

            try
            {
                if (CFGetTypeID(languages) != CFArrayGetTypeID()) return null;

                for (nint index = 0; index < CFArrayGetCount(languages); index++)
                {
                    var value = CFArrayGetValueAtIndex(languages, index);
                    if (value == IntPtr.Zero || CFGetTypeID(value) != CFStringGetTypeID())
                        continue;

                    var buffer = new byte[256];
                    if (!CFStringGetCString(value, buffer, buffer.Length, Utf8Encoding))
                        continue;

                    var length = Array.IndexOf(buffer, (byte)0);
                    if (length <= 0) continue;

                    try
                    {
                        var culture = CultureInfo.GetCultureInfo(Encoding.UTF8.GetString(buffer, 0, length));
                        if (culture.TwoLetterISOLanguageName is "en" or "es" or "ru")
                            return culture;
                    }
                    catch (CultureNotFoundException)
                    {
                        // Ignore an unknown language tag and try the next preference.
                    }
                }

                return CultureInfo.GetCultureInfo("en");
            }
            finally
            {
                CFRelease(languages);
            }
        }
        finally
        {
            CFRelease(key);
        }
    }

    [DllImport(CoreFoundation)]
    static extern IntPtr CFBundleGetMainBundle();

    [DllImport(CoreFoundation)]
    static extern IntPtr CFBundleGetIdentifier(IntPtr bundle);

    [DllImport(CoreFoundation)]
    static extern IntPtr CFStringCreateWithCString(IntPtr allocator, string value, uint encoding);

    [DllImport(CoreFoundation)]
    static extern IntPtr CFPreferencesCopyAppValue(IntPtr key, IntPtr applicationId);

    [DllImport(CoreFoundation)]
    static extern nuint CFGetTypeID(IntPtr value);

    [DllImport(CoreFoundation)]
    static extern nuint CFArrayGetTypeID();

    [DllImport(CoreFoundation)]
    static extern nuint CFStringGetTypeID();

    [DllImport(CoreFoundation)]
    static extern nint CFArrayGetCount(IntPtr array);

    [DllImport(CoreFoundation)]
    static extern IntPtr CFArrayGetValueAtIndex(IntPtr array, nint index);

    [DllImport(CoreFoundation)]
    [return: MarshalAs(UnmanagedType.I1)]
    static extern bool CFStringGetCString(IntPtr value, byte[] buffer, nint bufferSize, uint encoding);

    [DllImport(CoreFoundation)]
    static extern void CFRelease(IntPtr value);
}

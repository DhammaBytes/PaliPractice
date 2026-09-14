using Android.App;
using Android.OS;
using Android.Views;
using System.Globalization;

namespace PaliPractice.Droid;

[Activity(
    MainLauncher = true,
    ConfigurationChanges = ActivityHelper.AllConfigChanges,
    WindowSoftInputMode = SoftInput.AdjustNothing | SoftInput.StateHidden
)]
public class MainActivity : ApplicationActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        ApplyAndroidLanguage();

#if DEBUG
        // Global exception handlers for surfacing swallowed exceptions
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Android.Util.Log.Error("PaliPractice", $"[UNHANDLED] {e.ExceptionObject}");
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Android.Util.Log.Error("PaliPractice", $"[UNOBSERVED TASK] {e.Exception}");
            e.SetObserved();
        };

        // Android-specific: catch Java exceptions bridged to .NET
        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (_, e) =>
        {
            Android.Util.Log.Error("PaliPractice", $"[ANDROID UNHANDLED] {e.Exception}");
            e.Handled = false;
        };
#endif

        AndroidX.Core.SplashScreen.SplashScreen.InstallSplashScreen(this);

        base.OnCreate(savedInstanceState);

        // Android 15+ uses predictive back gestures and no longer routes back events
        // through SystemNavigationManager.BackRequested. Handle back directly.
        OnBackPressedDispatcher.AddCallback(this, new NavigationBackCallback(this));
    }

    void ApplyAndroidLanguage()
    {
        // Read native app/system preferences first. Resource configuration can
        // reorder locales to match the language of Android's own resources.
        var localeManager = OperatingSystem.IsAndroidVersionAtLeast(33)
            ? GetSystemService(LocaleService) as LocaleManager
            : null;
        var culture = FirstSupported(localeManager?.ApplicationLocales)
            ?? FirstSupported(localeManager?.SystemLocales)
            ?? FirstSupported(LocaleList.Default)
            ?? FirstSupported(Resources?.Configuration?.Locales)
            ?? FirstSupported(Java.Util.Locale.Default)
            ?? CultureInfo.GetCultureInfo("en");
        SetCulture(culture);
    }

    static CultureInfo? FirstSupported(LocaleList? locales)
    {
        if (locales is null) return null;

        for (var index = 0; index < locales.Size(); index++)
        {
            var culture = FirstSupported(locales.Get(index));
            if (culture is not null) return culture;
        }

        return null;
    }

    static CultureInfo? FirstSupported(Java.Util.Locale? locale)
    {
        if (locale?.Language is not ("en" or "es" or "ru")) return null;
        return CultureInfo.GetCultureInfo(locale.ToLanguageTag());
    }

    static void SetCulture(CultureInfo culture)
    {
        // A persisted WinRT language override can outlive a process. The app's UI
        // language always follows Android's current locale list on a cold start.
        Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = culture.Name;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    sealed class NavigationBackCallback(MainActivity activity)
        : AndroidX.Activity.OnBackPressedCallback(true)
    {
        public override void HandleOnBackPressed()
        {
            if (App.MainWindow?.Content is Presentation.Main.Shell { ContentControl.Content: FrameView { Content: Frame { CanGoBack: true } frame } })
            {
                frame.GoBack();
                if (frame.Content is Page page)
                    page.Focus(FocusState.Pointer);
                return;
            }

            // Root page — disable this callback and re-dispatch so the system closes the app
            Enabled = false;
            activity.OnBackPressedDispatcher.OnBackPressed();
            Enabled = true;
        }
    }
}

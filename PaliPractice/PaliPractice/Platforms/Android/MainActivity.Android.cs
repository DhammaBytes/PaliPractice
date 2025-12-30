using Android.App;
using Android.OS;
using Android.Views;

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
    }
}
using Uno;
using Uno.UI.Hosting;
using IOPath = System.IO.Path;

// ReSharper disable once CheckNamespace
namespace PaliPractice.Desktop;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
#if DEBUG
        // Global exception handlers for surfacing swallowed exceptions
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Console.Error.WriteLine($"[UNHANDLED] {e.ExceptionObject}");
            if (System.Diagnostics.Debugger.IsAttached)
                System.Diagnostics.Debugger.Break();
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Console.Error.WriteLine($"[UNOBSERVED TASK] {e.Exception}");
            e.SetObserved(); // Prevent process termination
        };
#endif
        // Uno's default ApplicationData path nests under Publisher/AppName.
        // Override to use just the app name under each platform's standard data directory:
        //   macOS:   ~/Library/Application Support/PaliPractice
        //   Linux:   ~/.local/share/PaliPractice
        //   Windows: %LOCALAPPDATA%\PaliPractice  (i.e. AppData\Local\PaliPractice)
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        WinRTFeatureConfiguration.ApplicationData.ApplicationDataPathOverride =
            IOPath.Combine(appDataPath, "PaliPractice");

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWin32()
            .Build();

        host.Run();
    }
}

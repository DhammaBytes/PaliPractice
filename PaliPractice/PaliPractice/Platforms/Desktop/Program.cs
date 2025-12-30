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
        // macOS: Override ApplicationData path to use ~/Library/Application Support/
        // instead of Uno's default ~/.local/share/ (which follows Linux XDG, not macOS convention)
        if (OperatingSystem.IsMacOS())
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            WinRTFeatureConfiguration.ApplicationData.ApplicationDataPathOverride =
                IOPath.Combine(appDataPath, "PaliPractice");
        }

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

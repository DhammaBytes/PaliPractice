using Uno.UI.Hosting;

namespace PaliPractice.iOS;

public class EntryPoint
{
    // This is the main entry point of the application.
    public static void Main(string[] args)
    {
#if DEBUG
        // Global exception handlers for surfacing swallowed exceptions
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Console.Error.WriteLine($"[UNHANDLED] {e.ExceptionObject}");
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Console.Error.WriteLine($"[UNOBSERVED TASK] {e.Exception}");
            e.SetObserved();
        };

        // iOS-specific: catch Objective-C exceptions bridged to .NET
        ObjCRuntime.Runtime.MarshalManagedException += (_, args) =>
        {
            Console.Error.WriteLine($"[MARSHALED EXCEPTION] {args.Exception}");
        };
#endif
        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseAppleUIKit()
            .Build();

        host.Run();
    }
}

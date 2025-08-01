using PaliPractice.Presentation.Behaviors;

namespace PaliPractice;

public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }
    protected IHost? Host { get; private set; }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Load WinUI Resources
        Resources.Build(r => r.Merged(
            new XamlControlsResources()));

        // Load Uno.UI.Toolkit and Material Resources
        Resources.Build(r => r.Merged(
            new MaterialToolkitTheme(
                new Styles.ColorPaletteOverride(),
                new Styles.MaterialFontsOverride())));
        var builder = this.CreateBuilder(args)
            // Add navigation support for toolkit controls such as TabBar and NavigationView
            .UseToolkitNavigation()
            .Configure(host => host
#if DEBUG
                // Switch to Development environment when running in DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging(configure: (context, logBuilder) =>
                {
                    // Configure log levels for different categories of logging
                    logBuilder
                        .SetMinimumLevel(
                            context.HostingEnvironment.IsDevelopment() ? LogLevel.Information : LogLevel.Warning)

                        // Default filters for core Uno Platform namespaces
                        .CoreLogLevel(LogLevel.Warning);

                    // Uno Platform namespace filter groups
                    // Uncomment individual methods to see more detailed logging
                    //// Generic Xaml events
                    //logBuilder.XamlLogLevel(LogLevel.Debug);
                    //// Layout specific messages
                    //logBuilder.XamlLayoutLogLevel(LogLevel.Debug);
                    //// Storage messages
                    //logBuilder.StorageLogLevel(LogLevel.Debug);
                    //// Binding related messages
                    //logBuilder.XamlBindingLogLevel(LogLevel.Debug);
                    //// Binder memory references tracking
                    //logBuilder.BinderMemoryReferenceLogLevel(LogLevel.Debug);
                    //// DevServer and HotReload related
                    //logBuilder.HotReloadCoreLogLevel(LogLevel.Information);
                    //// Debug JS interop
                    //logBuilder.WebAssemblyLogLevel(LogLevel.Debug);
                }, enableUnoLogging: true)
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<AppConfig>()
                )
                // Enable localization (see appsettings.json for supported languages)
                .UseLocalization()
                .ConfigureServices((context, services) =>
                {
                    // Register database service
                    services.AddSingleton<IDatabaseService, DatabaseService>();
                    
                    // Behaviors: transient is fine
                    services.AddTransient<CardStateBehavior>();
                    services.AddTransient<NumberSelectionBehavior>();
                    services.AddTransient<PersonSelectionBehavior>();
                    services.AddTransient<VoiceSelectionBehavior>();
                    services.AddTransient<TenseSelectionBehavior>();
                    services.AddTransient<GenderSelectionBehavior>();
                    services.AddTransient<CaseSelectionBehavior>();
                    services.AddTransient<NavigationBehavior>();
                    
                    // Word sources
                    services.AddTransient<NounWordSource>();
                    services.AddTransient<VerbWordSource>();
                    
                    // ViewModels
                    services.AddTransient<DeclensionPracticeViewModel>();
                    services.AddTransient<ConjugationPracticeViewModel>();
                })
                .UseNavigation(RegisterRoutes)
            );
        MainWindow = builder.Window;

#if DEBUG
        MainWindow.UseStudio();
#endif
        // MainWindow.SetWindowIcon();

        Host = await builder.NavigateAsync<Shell>();
    }

    static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap(ViewModel: typeof(ShellViewModel)),
            new ViewMap<StartPage, StartViewModel>(),
            new ViewMap<DeclensionPracticePage, DeclensionPracticeViewModel>(),
            new ViewMap<ConjugationPracticePage, ConjugationPracticeViewModel>()
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
                Nested:
                [
                    new("Start", View: views.FindByViewModel<StartViewModel>(), IsDefault: true),
                    new("DeclensionPractice", View: views.FindByViewModel<DeclensionPracticeViewModel>()),
                    new("ConjugationPractice", View: views.FindByViewModel<ConjugationPracticeViewModel>()),
                ]
            )
        );
    }
}

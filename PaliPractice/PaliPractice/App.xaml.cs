using Windows.Graphics;
using Microsoft.Graphics.Display;
using Microsoft.UI.Windowing;
using PaliPractice.Presentation.Main;
using PaliPractice.Presentation.Main.ViewModels;
using PaliPractice.Presentation.Practice;
using PaliPractice.Presentation.Practice.Providers;
using PaliPractice.Presentation.Practice.ViewModels;
using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Presentation.Settings;
using PaliPractice.Presentation.Settings.ViewModels;
using PaliPractice.Services.Database.Providers;
using PaliPractice.Services.Feedback;
using PaliPractice.Services.Practice;
using PaliPractice.Services.UserData;
using AboutPage = PaliPractice.Presentation.Main.AboutPage;
using ConjugationPracticePage = PaliPractice.Presentation.Practice.ConjugationPracticePage;
using ConjugationPracticeViewModel = PaliPractice.Presentation.Practice.ViewModels.ConjugationPracticeViewModel;
using ConjugationSettingsPage = PaliPractice.Presentation.Settings.ConjugationSettingsPage;
using ConjugationSettingsViewModel = PaliPractice.Presentation.Settings.ViewModels.ConjugationSettingsViewModel;
using DeclensionPracticePage = PaliPractice.Presentation.Practice.DeclensionPracticePage;
using DeclensionPracticeViewModel = PaliPractice.Presentation.Practice.ViewModels.DeclensionPracticeViewModel;
using DeclensionSettingsPage = PaliPractice.Presentation.Settings.DeclensionSettingsPage;
using DeclensionSettingsViewModel = PaliPractice.Presentation.Settings.ViewModels.DeclensionSettingsViewModel;
using LemmaRangeSettingsPage = PaliPractice.Presentation.Settings.LemmaRangeSettingsPage;
using LemmaRangeSettingsViewModel = PaliPractice.Presentation.Settings.ViewModels.LemmaRangeSettingsViewModel;
using LemmaRangeNavigationData = PaliPractice.Presentation.Settings.ViewModels.LemmaRangeNavigationData;
using HelpPage = PaliPractice.Presentation.Main.HelpPage;
using HistoryPage = PaliPractice.Presentation.Practice.HistoryPage;
using SettingsPage = PaliPractice.Presentation.Settings.SettingsPage;
using Shell = PaliPractice.Presentation.Main.Shell;
using StartPage = PaliPractice.Presentation.Main.StartPage;

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

    public static Window? MainWindow { get; private set; }
    protected IHost? Host { get; private set; }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Load WinUI Resources
        Resources.Build(r => r.Merged(
            new XamlControlsResources()));

        // Load Uno.UI.Toolkit Resources
        Resources.Build(r => r.Merged(
            new ToolkitResources()));
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
                .ConfigureServices((_, services) =>
                {
                    // Database services
                    services.AddSingleton<IBundledFileProvider, BundledFileProvider>();
                    services.AddSingleton<IDatabaseService, DatabaseService>();
                    services.AddSingleton<IInflectionService, InflectionService>();
                    services.AddTransient<IPracticeQueueBuilder, PracticeQueueBuilder>();

                    // Platform services
                    services.AddSingleton<IFeedbackService, FeedbackService>();
                    services.AddSingleton<IStoreReviewService, StoreReviewService>();

                    services.AddTransient<FlashCardViewModel>();

                    // Word providers (legacy - to be deprecated)
                    services.AddKeyedTransient<ILemmaProvider, NounLemmaProvider>("noun");
                    services.AddKeyedTransient<ILemmaProvider, VerbLemmaProvider>("verb");

                    // SRS Practice providers (new)
                    services.AddKeyedTransient<IPracticeProvider, DeclensionPracticeProvider>("declension");
                    services.AddKeyedTransient<IPracticeProvider, ConjugationPracticeProvider>("conjugation");

                    // ViewModels
                    services.AddTransient<DeclensionPracticeViewModel>();
                    services.AddTransient<ConjugationPracticeViewModel>();
                    services.AddTransient<SettingsViewModel>();
                    services.AddTransient<ConjugationSettingsViewModel>();
                    services.AddTransient<DeclensionSettingsViewModel>();
                    services.AddTransient<LemmaRangeSettingsViewModel>();
                    services.AddTransient<HistoryViewModel>();
                })
                .UseNavigation(RegisterRoutes)
            );
        
        MainWindow = builder.Window;
        
        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
        {
            // var displayInfo = DisplayInformation.CreateForWindowId(MainWindow.AppWindow.Id);

            // var scale = 1;
            
            // var logicalWidth = 800;
            // var logicalHeight = 600;
            
            // MainWindow.AppWindow.Resize(new SizeInt32
            // {
            //     Width = 800,//(int)(logicalWidth * scale),
            //     Height = 600//(int)(logicalHeight * scale)
            // });
        }

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
            new ViewMap<ConjugationPracticePage, ConjugationPracticeViewModel>(),
            new ViewMap<HelpPage, HelpViewModel>(),
            new ViewMap<AboutPage, AboutViewModel>(),
            new ViewMap<SettingsPage, SettingsViewModel>(),
            new ViewMap<ConjugationSettingsPage, ConjugationSettingsViewModel>(),
            new ViewMap<DeclensionSettingsPage, DeclensionSettingsViewModel>(),
            new DataViewMap<LemmaRangeSettingsPage, LemmaRangeSettingsViewModel, LemmaRangeNavigationData>(),
            new DataViewMap<HistoryPage, HistoryViewModel, HistoryNavigationData>()
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
                Nested:
                [
                    new("Start", View: views.FindByViewModel<StartViewModel>(), IsDefault: true),
                    new("DeclensionPractice", View: views.FindByViewModel<DeclensionPracticeViewModel>()),
                    new("ConjugationPractice", View: views.FindByViewModel<ConjugationPracticeViewModel>()),
                    new("Help", View: views.FindByViewModel<HelpViewModel>()),
                    new("About", View: views.FindByViewModel<AboutViewModel>()),
                    new("Settings", View: views.FindByViewModel<SettingsViewModel>()),
                    new("ConjugationSettings", View: views.FindByViewModel<ConjugationSettingsViewModel>()),
                    new("DeclensionSettings", View: views.FindByViewModel<DeclensionSettingsViewModel>()),
                    new("LemmaRangeSettings", View: views.FindByViewModel<LemmaRangeSettingsViewModel>()),
                    new("History", View: views.FindByViewModel<HistoryViewModel>()),
                ]
            )
        );
    }
}

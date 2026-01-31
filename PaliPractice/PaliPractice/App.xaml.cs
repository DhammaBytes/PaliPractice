using PaliPractice.Presentation.Main.Helpers;
using PaliPractice.Presentation.Main.ViewModels;
using PaliPractice.Presentation.Practice.Providers;
using PaliPractice.Presentation.Practice.ViewModels;
using PaliPractice.Presentation.Settings.ViewModels;
using PaliPractice.Services.Database.Providers;
using PaliPractice.Services.Feedback;
using PaliPractice.Services.Feedback.Providers;
using PaliPractice.Services.Grammar;
using PaliPractice.Services.Practice;
using PaliPractice.Services.UserData;
using PaliPractice.Themes;
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
using InflectionTablePage = PaliPractice.Presentation.Grammar.InflectionTablePage;
using InflectionTableViewModel = PaliPractice.Presentation.Grammar.ViewModels.InflectionTableViewModel;
using InflectionTableNavigationData = PaliPractice.Presentation.Grammar.ViewModels.InflectionTableNavigationData;
using SettingsPage = PaliPractice.Presentation.Settings.SettingsPage;
using Shell = PaliPractice.Presentation.Main.Shell;
using StartPage = PaliPractice.Presentation.Main.StartPage;
using StatisticsPage = PaliPractice.Presentation.Statistics.StatisticsPage;
using StatisticsViewModel = PaliPractice.Presentation.Statistics.ViewModels.StatisticsViewModel;

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

#if DEBUG
        // Catch exceptions on the UI dispatcher thread (XAML binding errors, event handlers, etc.)
        UnhandledException += (_, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"[APP UNHANDLED] {e.Exception}");
            Console.Error.WriteLine($"[APP UNHANDLED] {e.Exception}");
            e.Handled = false; // Set to true if you want to prevent crash and continue
        };
#endif
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

        // Override control accent colors (must be after XamlControlsResources)
        ControlStyling.ApplyAccentColorOverrides(Resources);

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
                    services.AddSingleton<IDeviceInfoProvider, DeviceInfoProvider>();
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
                    services.AddTransient<InflectionTableViewModel>();
                    services.AddTransient<StatisticsViewModel>();
                })
                .UseNavigation(RegisterRoutes)
            );
        
        MainWindow = builder.Window;

        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
        {
            DesktopWindowHelper.ConfigureDesktopWindow(MainWindow);
        }
        else if (OperatingSystem.IsIOS())
        {
            TabletWindowHelper.ConfigureMinimumSize();
        }

#if DEBUG
        // MainWindow.UseStudio();
#endif
        // MainWindow.SetWindowIcon();

        Host = await builder.NavigateAsync<Shell>();

        // Install native macOS menu bar (App/Edit/View/Window/Help).
        // See Services/MacMenu/MacMenuBridge.cs for how the ObjC↔C# bridge works.
        if (OperatingSystem.IsMacOS())
            Services.MacMenu.MacMenuBridge.Initialize();

        // Apply saved theme preference (after navigation so XamlRoot is available)
        ApplySavedTheme();

        // Handle Android hardware back button
        SetupBackButtonHandler();
    }

    static void SetupBackButtonHandler()
    {
        var manager = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
        manager.BackRequested += OnBackRequested;
    }
    
    // Android 7-14, for Android 15+ see MainActivity.Android.cs
    static void OnBackRequested(object? sender, Windows.UI.Core.BackRequestedEventArgs e)
    {
        if (MainWindow?.Content is not Shell { ContentControl.Content: FrameView { Content: Frame { CanGoBack: true } frame } })
            return;

        frame.GoBack();
        e.Handled = true;

        if (frame.Content is Page page)
            page.Focus(FocusState.Pointer);
    }

    void ApplySavedTheme()
    {
        if (Host is null || MainWindow?.Content is not FrameworkElement root) return;

        // Re-apply control styling when theme changes (brushes from ThemeDictionaries update,
        // but control-specific resources need to be re-read)
        root.ActualThemeChanged += (_, _) => ControlStyling.ApplyAccentColorOverrides(Resources);

        var db = Host.Services.GetRequiredService<IDatabaseService>();
        var savedTheme = db.UserData.GetSetting(SettingsKeys.AppearanceTheme, SettingsKeys.DefaultAppearanceTheme);

        // Only apply if not System (0 = follow device)
        if (savedTheme == 0) return;

        // Use SystemThemeHelper from Uno.Toolkit as recommended by docs
        // savedTheme: 1 = Light, 2 = Dark
        SystemThemeHelper.SetRootTheme(root.XamlRoot, darkMode: savedTheme == 2);
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
            new ViewMap<StatisticsPage, StatisticsViewModel>(),
            new ViewMap<ConjugationSettingsPage, ConjugationSettingsViewModel>(),
            new ViewMap<DeclensionSettingsPage, DeclensionSettingsViewModel>(),
            new DataViewMap<LemmaRangeSettingsPage, LemmaRangeSettingsViewModel, LemmaRangeNavigationData>(),
            new DataViewMap<HistoryPage, HistoryViewModel, HistoryNavigationData>(),
            new DataViewMap<InflectionTablePage, InflectionTableViewModel, InflectionTableNavigationData>()
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
                Nested:
                [
                    new RouteMap("Start", View: views.FindByViewModel<StartViewModel>(), IsDefault: true),
                    new RouteMap("DeclensionPractice", View: views.FindByViewModel<DeclensionPracticeViewModel>()),
                    new RouteMap("ConjugationPractice", View: views.FindByViewModel<ConjugationPracticeViewModel>()),
                    new RouteMap("Help", View: views.FindByViewModel<HelpViewModel>()),
                    new RouteMap("About", View: views.FindByViewModel<AboutViewModel>()),
                    new RouteMap("Settings", View: views.FindByViewModel<SettingsViewModel>()),
                    new RouteMap("Statistics", View: views.FindByViewModel<StatisticsViewModel>()),
                    new RouteMap("ConjugationSettings", View: views.FindByViewModel<ConjugationSettingsViewModel>()),
                    new RouteMap("DeclensionSettings", View: views.FindByViewModel<DeclensionSettingsViewModel>()),
                    new RouteMap("LemmaRangeSettings", View: views.FindByViewModel<LemmaRangeSettingsViewModel>()),
                    new RouteMap("History", View: views.FindByViewModel<HistoryViewModel>()),
                    new RouteMap("InflectionTable", View: views.FindByViewModel<InflectionTableViewModel>()),
                ]
            )
        );
    }
}

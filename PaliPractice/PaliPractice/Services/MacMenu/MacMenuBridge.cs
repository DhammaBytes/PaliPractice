using System.Runtime.InteropServices;
using Shell = PaliPractice.Presentation.Main.Shell;

namespace PaliPractice.Services.MacMenu;

/// <summary>
/// C# side of the native macOS menu bar bridge.
///
/// Uno Platform's Skia desktop backend doesn't provide a native macOS menu bar.
/// To add one, we use a small Objective-C library (libPaliMenu.dylib) compiled at
/// build time by MacMenu.targets. The three-part architecture:
///
///   PaliMenu.m  (ObjC)  — builds NSMenu hierarchy, assigns to NSApp.mainMenu
///   MacMenu.targets      — MSBuild target that compiles .m → .dylib with clang
///   MacMenuBridge.cs     — this file; P/Invoke bridge + navigation dispatch
///
/// For standard menu items (Copy, Paste, Full Screen, etc.), the ObjC code uses
/// nil-target items that route through the macOS responder chain directly — no C#
/// involvement needed. For app-specific items (About, Settings, Help), the ObjC
/// code calls back into C# with a string identifier, and this class navigates to
/// the corresponding page via Uno Navigation Extensions.
///
/// This file lives under Services/ (not Platforms/Desktop/) because the Uno SDK
/// automatically excludes Platforms/Desktop/*.cs from non-desktop TFMs (net10.0,
/// net10.0-android, net10.0-ios). Since App.xaml.cs references this class, it must
/// compile on all TFMs. The DllImport is only invoked on macOS at runtime (guarded
/// by OperatingSystem.IsMacOS() in App.xaml.cs), so the missing dylib on other
/// platforms is never a problem.
/// </summary>
static class MacMenuBridge
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void MenuCallback(IntPtr actionUtf8);

    [DllImport("libPaliMenu", CallingConvention = CallingConvention.Cdecl)]
    static extern void pali_menu_install(IntPtr callback);

    // Must be stored in a static field to prevent the GC from collecting the delegate
    // while the native code still holds a pointer to it.
    static MenuCallback? s_callback;

    public static void Initialize()
    {
        s_callback = OnMenuAction;
        pali_menu_install(Marshal.GetFunctionPointerForDelegate(s_callback));
    }

    /// <summary>
    /// Called from native ObjC on the native thread. Marshals the action string
    /// and dispatches to the UI thread for navigation.
    /// </summary>
    static void OnMenuAction(IntPtr actionUtf8)
    {
        var action = Marshal.PtrToStringUTF8(actionUtf8);
        if (string.IsNullOrEmpty(action)) return;

        if (App.MainWindow?.Content is FrameworkElement root)
        {
            root.DispatcherQueue.TryEnqueue(() => HandleAction(action));
        }
    }

    static void HandleAction(string action)
    {
        // Walk the Uno visual tree to find the current page:
        // MainWindow.Content → Shell → ContentControl (ExtendedSplashScreen)
        //   → FrameView → Frame → current Page
        if (App.MainWindow?.Content is not Shell { ContentControl.Content: FrameView frameView })
            return;

        if (frameView.Content is not Frame { Content: Page currentPage })
            return;

        var navigator = currentPage.Navigator();
        if (navigator is null) return;

        // The sender must be the current ViewModel (or page) so that Uno Navigation
        // pushes a new entry onto the back stack. Using the FrameView as sender
        // causes a root-level navigation that replaces the current page without
        // creating a back stack entry.
        var sender = currentPage.DataContext ?? currentPage;

        // Map menu action identifiers to route names registered in App.xaml.cs.
        var route = action switch
        {
            "about" => "About",
            "settings" => "Settings",
            "help" => "Help",
            _ => null
        };

        if (route is null) return;

        // Don't navigate if we're already on that page
        var targetType = action switch
        {
            "about" => typeof(PaliPractice.Presentation.Main.AboutPage),
            "settings" => typeof(PaliPractice.Presentation.Settings.SettingsPage),
            "help" => typeof(PaliPractice.Presentation.Main.HelpPage),
            _ => null
        };
        if (targetType is not null && currentPage.GetType() == targetType)
            return;

        _ = navigator.NavigateRouteAsync(sender: sender, route: route);
    }
}

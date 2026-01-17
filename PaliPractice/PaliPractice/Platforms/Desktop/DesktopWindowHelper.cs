using Windows.Graphics;
using Windows.Graphics.Display;
using Microsoft.UI.Windowing;
using PaliPractice.Presentation.Common;

namespace PaliPractice.Platforms.Desktop;

/// <summary>
/// Helper for configuring desktop window size and constraints.
/// </summary>
public static class DesktopWindowHelper
{
    /// <summary>
    /// Minimum window height in logical pixels.
    /// Matches the smallest CalculateWindowHeight breakpoint.
    /// </summary>
    const int MinHeightLogical = 640;

    /// <summary>
    /// Default window width in logical pixels.
    /// </summary>
    const int DefaultWidthLogical = 600;

    /// <summary>
    /// Configures the desktop window with appropriate initial size, minimum constraints, and position.
    /// </summary>
    public static void ConfigureDesktopWindow(Window window)
    {
        var displayInfo = DisplayInformation.GetForCurrentView();
        var scale = displayInfo.RawPixelsPerViewPixel;

        var rawWidth = (int)displayInfo.ScreenWidthInRawPixels;
        var rawHeight = (int)displayInfo.ScreenHeightInRawPixels;

        // Uno/macOS may report Portrait orientation for landscape displays, swapping dimensions
        var screenWidthPx = Math.Max(rawWidth, rawHeight);
        var screenHeightPx = Math.Min(rawWidth, rawHeight);
        var screenHeightLogical = (int)(screenHeightPx / scale);

        // Set minimum window size to prevent user from shrinking too small and producing responsive layout issues
        SetMinimumSize(window, minWidthLogical: LayoutConstants.ContentMaxWidth, minHeightLogical: MinHeightLogical);

        // Calculate and apply initial window size
        var windowHeightLogical = CalculateWindowHeight(screenHeightLogical);
        var windowWidthPx = (int)(DefaultWidthLogical * scale);
        var windowHeightPx = (int)(windowHeightLogical * scale);

        window.AppWindow.Resize(new SizeInt32 { Width = windowWidthPx, Height = windowHeightPx });

        // Position: horizontally centered, vertically at top for small screens
        var x = (screenWidthPx - windowWidthPx) / 2;
        var y = screenHeightLogical < 864 ? 0 : (screenHeightPx - windowHeightPx) / 2;
        window.AppWindow.Move(new PointInt32 { X = x, Y = y });
    }

    /// <summary>
    /// Sets the minimum window size using OverlappedPresenter.
    /// </summary>
    static void SetMinimumSize(Window window, int minWidthLogical, int minHeightLogical)
    {
        if (window.AppWindow.Presenter is not OverlappedPresenter presenter)
            return;

        presenter.PreferredMinimumWidth = minWidthLogical;
        presenter.PreferredMinimumHeight = minHeightLogical;
    }

    /// <summary>
    /// Calculates optimal window height based on available screen height.
    /// Accounts for OS UI (title bar ~32px, taskbar/dock ~40-60px).
    /// </summary>
    static int CalculateWindowHeight(int screenHeight) => screenHeight switch
    {
        // Tiny laptops (720p): 640px leaves ~80px for title bar + taskbar
        <= 720 => MinHeightLogical,
        // Small screens (768p): 680px leaves ~88px for OS UI
        <= 768 => 680,
        // Medium screens (800-863p)
        <= 863 => 720,
        // Standard+ (864p+): 800px is the ideal app height
        _ => 800
    };
}

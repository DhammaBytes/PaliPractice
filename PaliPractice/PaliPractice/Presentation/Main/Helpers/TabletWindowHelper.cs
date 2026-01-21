using PaliPractice.Presentation.Common;

#if __IOS__
using UIKit;
using CoreGraphics;
#endif

namespace PaliPractice.Presentation.Main.Helpers;

/// <summary>
/// Helper for configuring tablet window size constraints (iPad Stage Manager, Android freeform).
/// </summary>
public static class TabletWindowHelper
{
    /// <summary>
    /// Configures minimum window size for iPad Stage Manager.
    /// Only effective on iPadOS 16+ where windows can be resized.
    /// </summary>
    public static void ConfigureMinimumSize()
    {
#if __IOS__
        // Stage Manager was introduced in iPadOS 16
        if (!OperatingSystem.IsIOSVersionAtLeast(16))
            return;

        // Find the active window scene
        var scene = UIApplication.SharedApplication.ConnectedScenes
            .OfType<UIWindowScene>()
            .FirstOrDefault();

        if (scene?.SizeRestrictions is not { } restrictions)
            return;

        // Set minimum size to prevent responsive layout issues
        restrictions.MinimumSize = new CGSize(
            width: LayoutConstants.ContentMaxWidth,
            height: 720 // iPads start from 744
        );
#endif
    }
}

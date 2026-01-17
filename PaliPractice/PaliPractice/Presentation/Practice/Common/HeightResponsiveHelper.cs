using PaliPractice.Presentation.Common;

namespace PaliPractice.Presentation.Practice.Common;

/// <summary>
/// Helper for height-responsive layouts. Uses window height directly
/// for instant availability (no layout wait needed).
/// </summary>
public static class HeightResponsiveHelper
{
    /// <summary>
    /// Attaches a responsive handler that monitors window height changes.
    /// Fires immediately and on every window SizeChanged.
    /// </summary>
    public static void AttachResponsiveHandler(Action<HeightClass> onHeightClassChanged)
    {
        var window = App.MainWindow;
        if (window is null)
            return;

        HeightClass? lastClass = null;

        // Fire immediately with current window height
        UpdateHeightClass();

        // Also respond to window size changes
        window.SizeChanged += (_, _) => UpdateHeightClass();
        return;

        void UpdateHeightClass()
        {
            var newClass = LayoutConstants.GetCurrentHeightClass();
            if (newClass != lastClass)
            {
                lastClass = newClass;
                onHeightClassChanged(newClass);
            }
        }
    }
}

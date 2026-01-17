using static PaliPractice.Presentation.Common.Text.TextHelpers;

namespace PaliPractice.Presentation.Settings.Controls;

/// <summary>
/// A reusable settings section with a header and content items.
/// </summary>
public static class SettingsSection
{
    public static StackPanel Build(string header, params UIElement[] items)
    {
        var section = new StackPanel()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Spacing(1)
            .Margin(0, 0, 0, 16);

        // Add header
        section.Children.Add(
            RegularText()
                .Text(header)
                .FontSize(14)
                .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                .Margin(16, 8, 16, 4)
        );

        // Add border container for items (edge-to-edge, no rounded corners)
        var itemsContainer = new Border()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .Child(
                new StackPanel()
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Spacing(1)
                    .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
                    .Children(items)
            );

        section.Children.Add(itemsContainer);

        return section;
    }
}

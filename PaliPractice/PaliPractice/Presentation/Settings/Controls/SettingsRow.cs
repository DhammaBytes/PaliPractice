using System.Linq.Expressions;
using PaliPractice.Presentation.Bindings;
using static PaliPractice.Presentation.Common.TextHelpers;

namespace PaliPractice.Presentation.Settings.Controls;

/// <summary>
/// A reusable settings row with a label and toggle switch.
/// </summary>
public static class SettingsRow
{
    public static Grid BuildToggle<TDC>(string label, Expression<Func<TDC, bool>> isOnExpr)
    {
        var toggle = new ToggleSwitch()
            .OnContent("")
            .OffContent("")
            .MinWidth(0)
            .VerticalAlignment(VerticalAlignment.Center)
            .Grid(column: 1);
        toggle.IsOn<TDC>(isOnExpr);

        return new Grid()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .ColumnDefinitions("*,Auto")
            .Padding(16, 12)
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .Children(
                RegularText()
                    .Text(label)
                    .FontSize(16)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Grid(column: 0),
                toggle
            );
    }

    public static Grid BuildNavigation<TDC>(string label, Expression<Func<TDC, ICommand>> commandExpr)
    {
        var button = new Button()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .HorizontalContentAlignment(HorizontalAlignment.Stretch)
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .Padding(16, 12)
            .Command(commandExpr)
            .Content(
                new Grid()
                    .ColumnDefinitions("*,Auto")
                    .Children(
                        RegularText()
                            .Text(label)
                            .FontSize(16)
                            .VerticalAlignment(VerticalAlignment.Center)
                            .Grid(column: 0),
                        new FontIcon()
                            .Glyph("\uE76C") // Chevron right
                            .FontSize(14)
                            .VerticalAlignment(VerticalAlignment.Center)
                            .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                            .Grid(column: 1)
                    )
            );

        return new Grid()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Children(button);
    }

    public static Grid BuildPlaceholder(string label)
    {
        return new Grid()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .ColumnDefinitions("*,Auto")
            .Padding(16, 12)
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .Children(
                RegularText()
                    .Text(label)
                    .FontSize(16)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                    .Grid(column: 0),
                new FontIcon()
                    .Glyph("\uE76C") // Chevron right
                    .FontSize(14)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                    .Grid(column: 1)
            );
    }
}

using System.Linq.Expressions;
using PaliPractice.Presentation.Bindings;
using Microsoft.UI.Xaml.Controls;
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

    public static Grid BuildToggle<TDC>(
        string label,
        Expression<Func<TDC, bool>> isOnExpr,
        Expression<Func<TDC, bool>> isEnabledExpr)
    {
        var toggle = new ToggleSwitch()
            .OnContent("")
            .OffContent("")
            .MinWidth(0)
            .VerticalAlignment(VerticalAlignment.Center)
            .Grid(column: 1);
        toggle.IsOn<TDC>(isOnExpr);
        toggle.SetBinding(Control.IsEnabledProperty, Bind.Path(isEnabledExpr));

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

    public static Grid BuildNavigation<TDC>(
        string label,
        Expression<Func<TDC, ICommand>> commandExpr,
        Action<TextBlock> bindAccessoryText)
    {
        var accessoryText = new TextBlock()
            .FontSize(14)
            .VerticalAlignment(VerticalAlignment.Center)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
            .Margin(0, 0, 8, 0);
        bindAccessoryText(accessoryText);

        var button = new Button()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .HorizontalContentAlignment(HorizontalAlignment.Stretch)
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .Padding(16, 12)
            .Command(commandExpr)
            .Content(
                new Grid()
                    .ColumnDefinitions("*,Auto,Auto")
                    .Children(
                        RegularText()
                            .Text(label)
                            .FontSize(16)
                            .VerticalAlignment(VerticalAlignment.Center)
                            .Grid(column: 0),
                        accessoryText.Grid(column: 1),
                        new FontIcon()
                            .Glyph("\uE76C") // Chevron right
                            .FontSize(14)
                            .VerticalAlignment(VerticalAlignment.Center)
                            .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                            .Grid(column: 2)
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

    /// <summary>
    /// Builds a settings row with a label and dropdown (ComboBox).
    /// </summary>
    public static Grid BuildDropdown(
        string label,
        string[] options,
        Action<ComboBox> bindSelectedItem)
    {
        var comboBox = new ComboBox()
            .VerticalAlignment(VerticalAlignment.Center)
            .MinWidth(160)
            .Grid(column: 1);

        foreach (var option in options)
            comboBox.Items.Add(option);

        bindSelectedItem(comboBox);

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
                comboBox
            );
    }
}

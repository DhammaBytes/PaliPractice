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

    /// <summary>
    /// Builds a settings row with a label and dropdown (ComboBox) for int options.
    /// </summary>
    public static Grid BuildDropdown(
        string label,
        int[] options,
        Action<ComboBox> bindSelectedItem)
    {
        var comboBox = new ComboBox()
            .VerticalAlignment(VerticalAlignment.Center)
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

    /// <summary>
    /// Builds a settings row with a label and dropdown (ComboBox) for enum options.
    /// Each option is a tuple of (enum value, display label).
    /// </summary>
    public static Grid BuildDropdown<T>(
        string label,
        (T Value, string Label)[] options,
        Action<ComboBox> bindSelectedItem) where T : struct, Enum
    {
        var comboBox = new ComboBox()
            .VerticalAlignment(VerticalAlignment.Center)
            .Grid(column: 1);

        foreach (var (value, optionLabel) in options)
        {
            comboBox.Items.Add(new ComboBoxItem
            {
                Tag = value,
                Content = optionLabel
            });
        }

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

    /// <summary>
    /// Builds a settings row with a label and NumberBox for numeric input.
    /// </summary>
    public static Grid BuildNumberBox(
        string label,
        int minimum,
        int maximum,
        Action<NumberBox> bindValue,
        Action<NumberBox>? configure = null)
    {
        var numberBox = new NumberBox()
            .Minimum(minimum)
            .Maximum(maximum)
            .SmallChange(10)
            .LargeChange(50)
            .SpinButtonPlacementMode(NumberBoxSpinButtonPlacementMode.Compact)
            .ValidationMode(NumberBoxValidationMode.InvalidInputOverwritten)
            .MinWidth(72)
            .VerticalAlignment(VerticalAlignment.Center)
            .Grid(column: 1);

        bindValue(numberBox);
        configure?.Invoke(numberBox);

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
                numberBox
            );
    }

    /// <summary>
    /// Builds a settings row with a label, hint, and toggle switch.
    /// </summary>
    public static Grid BuildToggleWithHint<TDC>(
        string label,
        string hint,
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
                new StackPanel()
                    .Spacing(2)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Left)
                    .Grid(column: 0)
                    .Margin(0, 0, 12, 0)
                    .Children(
                        RegularText()
                            .Text(label)
                            .FontSize(16),
                        RegularText()
                            .Text(hint)
                            .FontSize(12)
                            .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                            .TextWrapping(TextWrapping.Wrap)
                    ),
                toggle
            );
    }

    /// <summary>
    /// Builds a settings row with a label, hint, and dropdown (ComboBox).
    /// </summary>
    public static Grid BuildDropdownWithHint(
        string label,
        string hint,
        string[] options,
        Action<ComboBox> bindSelectedItem)
    {
        var comboBox = new ComboBox()
            .VerticalAlignment(VerticalAlignment.Center)
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
                new StackPanel()
                    .Spacing(2)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Left)
                    .Grid(column: 0)
                    .Margin(0, 0, 12, 0)
                    .Children(
                        RegularText()
                            .Text(label)
                            .FontSize(16),
                        RegularText()
                            .Text(hint)
                            .FontSize(12)
                            .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                            .TextWrapping(TextWrapping.Wrap)
                    ),
                comboBox
            );
    }
}

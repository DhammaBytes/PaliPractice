using System.Linq.Expressions;
using Microsoft.UI.Xaml.Markup;
using PaliPractice.Presentation.Bindings;
using static PaliPractice.Presentation.Common.Text.TextHelpers;

namespace PaliPractice.Presentation.Settings.Controls;

/// <summary>
/// A reusable settings row with a label and toggle switch.
/// </summary>
public static class SettingsRow
{
    /// <summary>
    /// Creates a chromeless button template that only uses the Background property,
    /// without any default WinUI button chrome that can cause theme flashing.
    /// Uses semi-transparent black overlay for hover/pressed states (works in both themes).
    /// </summary>
    /// <remarks>
    /// Uses VisualState.Setters instead of Storyboard animations because
    /// DoubleAnimation on Opacity doesn't interpolate correctly on iOS Skia,
    /// causing the overlay to appear at full opacity (black) instead of 4-8%.
    /// Setters provide immediate visual feedback which works on all platforms.
    /// </remarks>
    static ControlTemplate CreateChromelessButtonTemplate()
    {
        const string xaml = """
            <ControlTemplate
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                TargetType="Button">
                <Grid x:Name="RootGrid" Background="{TemplateBinding Background}">
                    <VisualStateManager.VisualStateGroups>
                        <VisualStateGroup x:Name="CommonStates">
                            <VisualState x:Name="Normal">
                                <VisualState.Setters>
                                    <Setter Target="HoverOverlay.Opacity" Value="0"/>
                                </VisualState.Setters>
                            </VisualState>
                            <VisualState x:Name="PointerOver">
                                <VisualState.Setters>
                                    <Setter Target="HoverOverlay.Opacity" Value="0.04"/>
                                </VisualState.Setters>
                            </VisualState>
                            <VisualState x:Name="Pressed">
                                <VisualState.Setters>
                                    <Setter Target="HoverOverlay.Opacity" Value="0.08"/>
                                </VisualState.Setters>
                            </VisualState>
                            <VisualState x:Name="Disabled">
                                <VisualState.Setters>
                                    <Setter Target="RootGrid.Opacity" Value="0.4"/>
                                </VisualState.Setters>
                            </VisualState>
                        </VisualStateGroup>
                    </VisualStateManager.VisualStateGroups>
                    <Border x:Name="HoverOverlay" Background="Black" Opacity="0"/>
                    <ContentPresenter
                        Content="{TemplateBinding Content}"
                        Padding="{TemplateBinding Padding}"
                        HorizontalContentAlignment="{TemplateBinding HorizontalContentAlignment}"
                        VerticalContentAlignment="{TemplateBinding VerticalContentAlignment}"/>
                </Grid>
            </ControlTemplate>
            """;
        return (ControlTemplate)XamlReader.Load(xaml);
    }

    static readonly ControlTemplate ChromelessButtonTemplate = CreateChromelessButtonTemplate();

    /// <summary>
    /// Creates a button with no default chrome - background comes entirely from our ThemeResource.
    /// This prevents flashing when loading in dark mode.
    /// </summary>
    static Button CreateChromelessButton() => new Button()
        .Template(ChromelessButtonTemplate)
        .HorizontalAlignment(HorizontalAlignment.Stretch)
        .HorizontalContentAlignment(HorizontalAlignment.Stretch)
        .Background(ThemeResource.Get<Brush>("SurfaceBrush"));

    public static Grid BuildNavigation<TDC>(
        string label,
        string iconGlyph,
        Expression<Func<TDC, ICommand>> commandExpr)
    {
        var button = CreateChromelessButton()
            .Padding(16, 12)
            .Command(commandExpr)
            .Content(
                new Grid()
                    .ColumnDefinitions("Auto,*,Auto")
                    .Children(
                        new FontIcon()
                            .Glyph(iconGlyph)
                            .FontSize(16)
                            .VerticalAlignment(VerticalAlignment.Center)
                            .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
                            .Margin(0, 0, 12, 0)
                            .Grid(column: 0),
                        RegularText()
                            .Text(label)
                            .FontSize(16)
                            .VerticalAlignment(VerticalAlignment.Center)
                            .Grid(column: 1),
                        new FontIcon()
                            .Glyph("\uE76C") // Chevron right
                            .FontSize(14)
                            .VerticalAlignment(VerticalAlignment.Center)
                            .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
                            .Grid(column: 2)
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
        var accessoryText = RegularText()
            .FontSize(14)
            .VerticalAlignment(VerticalAlignment.Center)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
            .Margin(0, 0, 8, 0);
        bindAccessoryText(accessoryText);

        var button = CreateChromelessButton()
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
                            .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
                            .Grid(column: 2)
                    )
            );

        return new Grid()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Children(button);
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
    /// Builds a settings row with an icon, label, and dropdown (ComboBox).
    /// </summary>
    public static Grid BuildDropdownWithIcon(
        string label,
        string iconGlyph,
        string[] options,
        Action<ComboBox> bindSelectedItem)
    {
        var comboBox = new ComboBox()
            .VerticalAlignment(VerticalAlignment.Center)
            .Grid(column: 2);

        foreach (var option in options)
            comboBox.Items.Add(option);

        bindSelectedItem(comboBox);

        return new Grid()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .ColumnDefinitions("Auto,*,Auto")
            .Padding(16, 12)
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .Children(
                new FontIcon()
                    .Glyph(iconGlyph)
                    .FontSize(16)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
                    .Margin(0, 0, 12, 0)
                    .Grid(column: 0),
                RegularText()
                    .Text(label)
                    .FontSize(16)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Grid(column: 1),
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
    /// Builds a settings row with an SVG icon, label, hint, and toggle switch.
    /// Icon is aligned with the label text (first row).
    /// </summary>
    public static Grid BuildToggleWithIconAndHint<TDC>(
        string? iconPath,
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

        // Build label row with optional icon
        var labelRowChildren = new List<UIElement>();
        if (!string.IsNullOrEmpty(iconPath))
        {
            // Use BitmapIcon with ShowAsMonochrome for Foreground tinting
            // PNG icons are generated from SVGs via Uno.Resizetizer
            var icon = new BitmapIcon()
                .ShowAsMonochrome(true)
                .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
                .Height(16)
                .Width(16)
                .VerticalAlignment(VerticalAlignment.Center)
                .Margin(0, 0, 8, 0)
                .UriSource(new Uri(iconPath));
            labelRowChildren.Add(icon);
        }
        labelRowChildren.Add(
            RegularText()
                .Text(label)
                .FontSize(16)
                .VerticalAlignment(VerticalAlignment.Center)
        );

        var labelRow = new StackPanel()
            .Orientation(Orientation.Horizontal)
            .Children(labelRowChildren.ToArray());

        var labelStack = new StackPanel()
            .Spacing(2)
            .VerticalAlignment(VerticalAlignment.Center)
            .HorizontalAlignment(HorizontalAlignment.Left)
            .Grid(column: 0)
            .Margin(0, 0, 12, 0)
            .Children(
                labelRow,
                RegularText()
                    .Text(hint)
                    .FontSize(13)
                    .Foreground(ThemeResource.Get<Brush>("OnSurfaceSecondaryBrush"))
                    .TextWrapping(TextWrapping.Wrap)
            );

        return new Grid()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .ColumnDefinitions("*,Auto")
            .Padding(16, 12)
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .Children(labelStack, toggle);
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
                            .FontSize(13)
                            .Foreground(ThemeResource.Get<Brush>("OnSurfaceSecondaryBrush"))
                            .TextWrapping(TextWrapping.Wrap)
                    ),
                comboBox
            );
    }

    /// <summary>
    /// Builds a settings row with a label, icon, and action button.
    /// </summary>
    public static Grid BuildAction<TDC>(
        string label,
        string iconGlyph,
        Expression<Func<TDC, ICommand>> commandExpr)
    {
        var button = CreateChromelessButton()
            .Padding(16, 12)
            .Command(commandExpr)
            .Content(
                new Grid()
                    .ColumnDefinitions("Auto,*")
                    .Children(
                        new FontIcon()
                            .Glyph(iconGlyph)
                            .FontSize(16)
                            .VerticalAlignment(VerticalAlignment.Center)
                            .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
                            .Margin(0, 0, 12, 0)
                            .Grid(column: 0),
                        RegularText()
                            .Text(label)
                            .FontSize(16)
                            .VerticalAlignment(VerticalAlignment.Center)
                            .Grid(column: 1)
                    )
            );

        return new Grid()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Children(button);
    }
}

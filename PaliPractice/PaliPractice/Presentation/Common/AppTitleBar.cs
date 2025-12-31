using System.Linq.Expressions;
using PaliPractice.Presentation.Bindings;
using static PaliPractice.Presentation.Common.TextHelpers;

namespace PaliPractice.Presentation.Common;

public static class AppTitleBar
{
    /// <summary>
    /// Builds a title bar with back button only (no history button).
    /// Used for pages like Settings, Help, About, History.
    /// </summary>
    public static Grid Build<TDC>(string title, Expression<Func<TDC, ICommand>> goBackCommand)
    {
        return BuildCore(
            title,
            CreateBackButton(goBackCommand),
            rightButton: null);
    }

    /// <summary>
    /// Builds a title bar with back and history buttons.
    /// Used for practice pages (Declension, Conjugation).
    /// </summary>
    public static Grid BuildWithHistory<TDC>(
        string title,
        Expression<Func<TDC, ICommand>> goBackCommand,
        Expression<Func<TDC, ICommand>> goToHistoryCommand)
    {
        return BuildCore(
            title,
            CreateBackButton(goBackCommand),
            CreateHistoryButton(goToHistoryCommand));
    }

    /// <summary>
    /// Builds a title bar with back button, clickable center button, and history button.
    /// Used for practice pages where the center shows the current lemma as a tappable button.
    /// </summary>
    public static Grid BuildWithCenterButton<TDC>(
        Expression<Func<TDC, ICommand>> goBackCommand,
        Action<TextBlock> bindCenterText,
        Expression<Func<TDC, ICommand>> centerClickCommand,
        Expression<Func<TDC, ICommand>> goToHistoryCommand)
    {
        // Create center button with bound text
        var centerTextBlock = PaliText()
            .FontSize(18)
            .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
            .VerticalAlignment(VerticalAlignment.Center)
            .TextTrimming(TextTrimming.CharacterEllipsis)
            .MaxWidth(180);

        // Apply text binding via callback
        bindCenterText(centerTextBlock);

        var centerButton = new SquircleButton()
            .Fill(ThemeResource.Get<Brush>("BackgroundBrush"))
            .RadiusMode(SquircleRadiusMode.ButtonSmall)
            .Padding(12, 8);
        centerButton.SetBinding(ButtonBase.CommandProperty, Bind.Path(centerClickCommand));
        centerButton.Child(new StackPanel()
            .Orientation(Orientation.Horizontal)
            .Spacing(6)
            .Children(
                centerTextBlock,
                new FontIcon()
                    .Glyph("\uE8A7") // List/table icon
                    .FontSize(14)
                    .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
            ));

        return BuildCoreWithCenterElement(
            centerButton,
            CreateBackButton(goBackCommand),
            CreateHistoryButton(goToHistoryCommand));
    }

    /// <summary>
    /// Core builder with a center element instead of text.
    /// </summary>
    static Grid BuildCoreWithCenterElement(UIElement centerElement, SquircleButton leftButton, SquircleButton? rightButton)
    {
        // Center layer: spans full width, centered element
        var centerLayer = new Grid()
            .HorizontalAlignment(HorizontalAlignment.Center)
            .VerticalAlignment(VerticalAlignment.Center)
            .Children(centerElement);

        // Buttons layer: left and right edges
        var buttonsLayer = new Grid()
            .ColumnDefinitions("Auto,*,Auto")
            .Children(
                leftButton.Grid(column: 0)
            );

        if (rightButton is not null)
            buttonsLayer.Children(rightButton.Grid(column: 2));

        // Stack layers: center behind, buttons on top
        return new Grid()
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .Padding(16, 8)
            .Children(centerLayer, buttonsLayer);
    }

    /// <summary>
    /// Core builder: uses layered Grid to truly center title regardless of button widths.
    /// </summary>
    static Grid BuildCore(string title, SquircleButton leftButton, SquircleButton? rightButton)
    {
        // Title layer: spans full width, centered
        var titleLayer = RegularText()
            .Text(title)
            .FontSize(19)
            .FontWeight(Microsoft.UI.Text.FontWeights.Medium)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .VerticalAlignment(VerticalAlignment.Center);

        // Buttons layer: left and right edges
        var buttonsLayer = new Grid()
            .ColumnDefinitions("Auto,*,Auto")
            .Children(
                leftButton.Grid(column: 0)
            );

        if (rightButton is not null)
            buttonsLayer.Children(rightButton.Grid(column: 2));

        // Stack layers: title behind, buttons on top
        return new Grid()
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .Padding(16, 8)
            .Children(titleLayer, buttonsLayer);
    }

    static SquircleButton CreateBackButton<TDC>(Expression<Func<TDC, ICommand>> commandPath)
    {
        var button = new SquircleButton()
            .Fill(ThemeResource.Get<Brush>("BackgroundBrush"))
            .RadiusMode(SquircleRadiusMode.ButtonSmall)
            .Padding(12, 8);
        button.SetBinding(ButtonBase.CommandProperty, Bind.Path(commandPath));
        return button
            .Child(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .Spacing(6)
                .Children(
                    new FontIcon()
                        .Glyph("\uE72B") // Back arrow
                        .FontSize(16),
                    RegularText()
                        .Text("Back")
                        .VerticalAlignment(VerticalAlignment.Center)
                ));
    }

    static SquircleButton CreateHistoryButton<TDC>(Expression<Func<TDC, ICommand>> commandPath)
    {
        var button = new SquircleButton()
            .Fill(ThemeResource.Get<Brush>("BackgroundBrush"))
            .RadiusMode(SquircleRadiusMode.ButtonSmall)
            .Padding(12, 8);
        button.SetBinding(ButtonBase.CommandProperty, Bind.Path(commandPath));
        return button
            .Child(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .Spacing(6)
                .Children(
                    new FontIcon()
                        .Glyph("\uE81C") // History/Clock icon
                        .FontSize(16),
                    RegularText()
                        .Text("History")
                        .VerticalAlignment(VerticalAlignment.Center)
                ));
    }
}

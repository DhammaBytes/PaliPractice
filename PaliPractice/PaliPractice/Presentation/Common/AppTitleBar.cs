using System.Linq.Expressions;
using PaliPractice.Presentation.Bindings;
using static PaliPractice.Presentation.Common.TextHelpers;
using static PaliPractice.Presentation.Common.ShadowHelper;

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
    /// Builds a title bar with back button and custom center element.
    /// Used when the title needs special formatting (e.g., PaliText for lemma names).
    /// </summary>
    public static Grid BuildWithCenterElement<TDC>(
        UIElement centerElement,
        Expression<Func<TDC, ICommand>> goBackCommand)
    {
        return BuildCoreWithCenterElement(
            centerElement,
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
    /// Used for practice pages where the center shows a button to navigate to the inflection table.
    /// </summary>
    public static Grid BuildWithCenterButton<TDC>(
        Expression<Func<TDC, ICommand>> goBackCommand,
        Expression<Func<TDC, ICommand>> centerClickCommand,
        Expression<Func<TDC, ICommand>> goToHistoryCommand)
    {
        var centerButton = new SquircleButton()
            .Fill(ThemeResource.Get<Brush>("NavigationButtonBrush"))
            .Stroke(ThemeResource.Get<Brush>("NavigationButtonOutlineBrush"))
            .StrokeThickness(LayoutConstants.Sizes.ButtonStrokeThickness)
            .RadiusMode(SquircleRadiusMode.Pill) // More pill-like corners
            .Padding(24, 10);
        centerButton.SetBinding(ButtonBase.CommandProperty, Bind.Path(centerClickCommand));
        centerButton.Child(
            RegularText()
                .Text("All Forms")
                .FontSize(16)
                .FontWeight(Microsoft.UI.Text.FontWeights.Medium)
                .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                .VerticalAlignment(VerticalAlignment.Center)
        );

        return BuildCoreWithCenterElement(
            PillShadow(centerButton),
            CreateBackButton(goBackCommand),
            CreateHistoryButton(goToHistoryCommand));
    }

    /// <summary>
    /// Core builder with a center element instead of text.
    /// </summary>
    static Grid BuildCoreWithCenterElement(UIElement centerElement, UIElement leftButton, UIElement? rightButton)
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
                PillShadow(leftButton).Grid(column: 0)
            );

        if (rightButton is not null)
            buttonsLayer.Children(PillShadow(rightButton).Grid(column: 2));

        // Stack layers: center behind, buttons on top
        // Transparent background - bar blends with page background
        return new Grid()
            .Padding(16, 8)
            .Children(centerLayer, buttonsLayer);
    }

    /// <summary>
    /// Core builder: uses layered Grid to truly center title regardless of button widths.
    /// </summary>
    static Grid BuildCore(string title, UIElement leftButton, UIElement? rightButton)
    {
        // Title layer: spans full width, centered
        var titleLayer = RegularText()
            .Text(title)
            .FontSize(19)
            .FontWeight(Microsoft.UI.Text.FontWeights.Medium)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
            .HorizontalAlignment(HorizontalAlignment.Center)
            .VerticalAlignment(VerticalAlignment.Center);

        // Buttons layer: left and right edges
        var buttonsLayer = new Grid()
            .ColumnDefinitions("Auto,*,Auto")
            .Children(
                PillShadow(leftButton).Grid(column: 0)
            );

        if (rightButton is not null)
            buttonsLayer.Children(PillShadow(rightButton).Grid(column: 2));

        // Stack layers: title behind, buttons on top
        // Transparent background - bar blends with page background
        return new Grid()
            .Padding(16, 8)
            .Children(titleLayer, buttonsLayer);
    }

    // Shared width for Back/History buttons so they match
    const double NavButtonMinWidth = 100;

    static SquircleButton CreateBackButton<TDC>(Expression<Func<TDC, ICommand>> commandPath)
    {
        var button = new SquircleButton()
            .Fill(ThemeResource.Get<Brush>("NavigationButtonBrush"))
            .Stroke(ThemeResource.Get<Brush>("NavigationButtonOutlineBrush"))
            .StrokeThickness(LayoutConstants.Sizes.ButtonStrokeThickness)
            .RadiusMode(SquircleRadiusMode.Pill) // More pill-like corners
            .Padding(12, 10) // +2pt vertical padding
            .MinWidth(NavButtonMinWidth); // Match History button width
        button.SetBinding(ButtonBase.CommandProperty, Bind.Path(commandPath));
        button.Child(new StackPanel()
            .Orientation(Orientation.Horizontal)
            .HorizontalAlignment(HorizontalAlignment.Center) // Center content in button
            .Spacing(6)
            .Children(
                new FontIcon()
                    .Glyph("\uE72B") // Back arrow
                    .FontSize(16)
                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush")),
                RegularText()
                    .Text("Back")
                    .FontSize(16)
                    .FontWeight(Microsoft.UI.Text.FontWeights.Medium)
                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                    .VerticalAlignment(VerticalAlignment.Center)
            ));

        return button;
    }

    static SquircleButton CreateHistoryButton<TDC>(Expression<Func<TDC, ICommand>> commandPath)
    {
        var button = new SquircleButton()
            .Fill(ThemeResource.Get<Brush>("NavigationButtonBrush"))
            .Stroke(ThemeResource.Get<Brush>("NavigationButtonOutlineBrush"))
            .StrokeThickness(LayoutConstants.Sizes.ButtonStrokeThickness)
            .RadiusMode(SquircleRadiusMode.Pill) // More pill-like corners
            .Padding(12, 10) // +2pt vertical padding
            .MinWidth(NavButtonMinWidth); // Match Back button width
        button.SetBinding(ButtonBase.CommandProperty, Bind.Path(commandPath));
        button.Child(new StackPanel()
            .Orientation(Orientation.Horizontal)
            .HorizontalAlignment(HorizontalAlignment.Center) // Center content in button
            .Spacing(6)
            .Children(
                new FontIcon()
                    .Glyph("\uE81C") // History/Clock icon
                    .FontSize(16)
                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush")),
                RegularText()
                    .Text("History")
                    .FontSize(16)
                    .FontWeight(Microsoft.UI.Text.FontWeights.Medium)
                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                    .VerticalAlignment(VerticalAlignment.Center)
            ));

        return button;
    }
}

using System.Linq.Expressions;
using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;

namespace PaliPractice.Presentation.Practice.Controls;

public static class PracticeNavigation
{
    /// <summary>
    /// Legacy overload for backwards compatibility.
    /// </summary>
    public static UIElement Build<TDC>(
        Expression<Func<TDC, ICommand>> hardCommand,
        Expression<Func<TDC, ICommand>> easyCommand) =>
        new Grid()
            .MaxWidth(LayoutConstants.ContentMaxWidth)
            .ColumnDefinitions("*,*")
            .Padding(20, 16)
            .ColumnSpacing(16)
            .Children(
                CreateSquircleButton("Hard", "\uE711", hardCommand, "SurfaceBrush")
                    .Grid(column: 0)
                    .HorizontalAlignment(HorizontalAlignment.Stretch),
                CreateSquircleButton("Easy", "\uE73E", easyCommand, "SurfaceBrush")
                    .Grid(column: 1)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
            );

    /// <summary>
    /// Builds navigation with reveal button that swaps to Easy/Hard after reveal.
    /// </summary>
    /// <param name="revealCommand">Command to reveal the answer</param>
    /// <param name="hardCommand">Command to mark card as hard</param>
    /// <param name="easyCommand">Command to mark card as easy</param>
    /// <param name="isRevealedPath">Path to bool property indicating if answer is revealed</param>
    public static UIElement BuildWithReveal<TDC>(
        Expression<Func<TDC, ICommand>> revealCommand,
        Expression<Func<TDC, ICommand>> hardCommand,
        Expression<Func<TDC, ICommand>> easyCommand,
        Expression<Func<TDC, bool>> isRevealedPath)
    {
        return new Grid()
            .MaxWidth(LayoutConstants.ContentMaxWidth)
            .Padding(20, 16)
            .Children(
                // Reveal button - visible when NOT revealed
                CreateRevealSquircleButton(revealCommand)
                    .BoolToVisibility<SquircleButton, TDC>(isRevealedPath, invert: true),

                // Hard/Easy buttons - visible when revealed
                new Grid()
                    .ColumnDefinitions("*,*")
                    .ColumnSpacing(16)
                    .BoolToVisibility<Grid, TDC>(isRevealedPath)
                    .Children(
                        CreateSquircleButton("Hard", "\uE711", hardCommand, "SurfaceBrush")
                            .Grid(column: 0)
                            .HorizontalAlignment(HorizontalAlignment.Stretch),
                        CreateSquircleButton("Easy", "\uE73E", easyCommand, "SurfaceBrush")
                            .Grid(column: 1)
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                    )
            );
    }

    static SquircleButton CreateRevealSquircleButton<TDC>(Expression<Func<TDC, ICommand>> commandPath)
    {
        var button = new SquircleButton()
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Fill(ThemeResource.Get<Brush>("PrimaryBrush"))
            .RadiusMode(SquircleRadiusMode.ButtonSmall)
            .Padding(32, 16);
        button.SetBinding(ButtonBase.CommandProperty, Bind.Path(commandPath));
        return button
            .Child(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .Spacing(8)
                .Children(
                    new FontIcon()
                        .Glyph("\uE7B3") // Eye icon
                        .FontSize(20)
                        .Foreground(ThemeResource.Get<Brush>("OnPrimaryBrush")),
                    new TextBlock()
                        .Text("Reveal Answer")
                        .FontSize(18)
                        .Foreground(ThemeResource.Get<Brush>("OnPrimaryBrush"))
                ));
    }

    static SquircleButton CreateSquircleButton<TDC>(
        string text,
        string glyph,
        Expression<Func<TDC, ICommand>> commandPath,
        string brushKey)
    {
        var button = new SquircleButton()
            .Fill(ThemeResource.Get<Brush>(brushKey))
            .RadiusMode(SquircleRadiusMode.ButtonSmall)
            .Padding(16, 12);
        button.SetBinding(ButtonBase.CommandProperty, Bind.Path(commandPath));
        return button
            .Child(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .HorizontalAlignment(HorizontalAlignment.Center)
                .Spacing(8)
                .Children(
                    new FontIcon().Glyph(glyph).FontSize(16),
                    new TextBlock().Text(text).FontSize(16)
                ));
    }
}

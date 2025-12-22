using System.Linq.Expressions;
using PaliPractice.Presentation.Bindings;

namespace PaliPractice.Presentation.Common;

public static class AppTitleBar
{
    /// <summary>
    /// Builds a title bar with back button only (no history button).
    /// Used for pages like Settings, Help, About, History.
    /// </summary>
    public static Grid Build<TDC>(string title, Expression<Func<TDC, ICommand>> goBackCommand)
    {
        return
            new Grid()
                .ColumnDefinitions("Auto,*,Auto")
                .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
                .Padding(16, 8)
                .Children(
                    CreateBackButton(goBackCommand)
                        .Grid(column: 0),
                    new TextBlock()
                        .Text(title)
                        .FontSize(20)
                        .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                        .HorizontalAlignment(HorizontalAlignment.Center)
                        .VerticalAlignment(VerticalAlignment.Center)
                        .Grid(column: 1)
                );
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
        return
            new Grid()
                .ColumnDefinitions("Auto,*,Auto")
                .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
                .Padding(16, 8)
                .Children(
                    CreateBackButton(goBackCommand)
                        .Grid(column: 0),
                    new TextBlock()
                        .Text(title)
                        .FontSize(20)
                        .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                        .HorizontalAlignment(HorizontalAlignment.Center)
                        .VerticalAlignment(VerticalAlignment.Center)
                        .Grid(column: 1),
                    CreateHistoryButton(goToHistoryCommand)
                        .Grid(column: 2)
                );
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
                    new TextBlock()
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
                    new TextBlock()
                        .Text("History")
                        .VerticalAlignment(VerticalAlignment.Center)
                ));
    }
}

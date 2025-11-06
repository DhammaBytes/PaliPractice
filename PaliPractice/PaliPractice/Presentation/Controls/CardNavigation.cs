using System.Linq.Expressions;
using PaliPractice.Presentation.Bindings;

namespace PaliPractice.Presentation.Controls;

public static class CardNavigation
{
    public static UIElement Build<TDC>(
        Expression<Func<TDC, ICommand>> hardCommand,
        Expression<Func<TDC, ICommand>> easyCommand) =>
        new Grid()
            .ColumnDefinitions("*,*")
            .Padding(20, 16)
            .ColumnSpacing(16)
            .Children(
                CreateButton("Hard", "\uE711", hardCommand)
                    .Grid(column: 0)
                    .HorizontalAlignment(HorizontalAlignment.Stretch),
                CreateButton("Easy", "\uE73E", easyCommand)
                    .Grid(column: 1)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
            );

    static Button CreateButton<TDC>(
        string text,
        string glyph,
        Expression<Func<TDC, ICommand>> commandPath)
    {
        return new Button()
            .Padding(16, 12)
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .Command(commandPath)
            .Content(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .HorizontalAlignment(HorizontalAlignment.Center)
                .Spacing(8)
                .Children(
                    new FontIcon().Glyph(glyph).FontSize(16),
                    new TextBlock().Text(text).FontSize(16)
                ));
    }
}

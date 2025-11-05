using System.Linq.Expressions;
using PaliPractice.Presentation.Bindings;

namespace PaliPractice.Presentation.Controls;

public static class CardNavigation
{
    // Cached brush for button backgrounds
    private static readonly SolidColorBrush ButtonBackground = new(Color.FromArgb(255, 240, 240, 255));

    public static UIElement Build<TDC>(
        Expression<Func<TDC, ICommand>> previousCommand,
        Expression<Func<TDC, ICommand>> nextCommand) =>
        new Grid()
            .ColumnDefinitions("*,*")
            .Padding(20, 16)
            .ColumnSpacing(16)
            .Children(
                CreateButton("Previous", "\uE72B", 0, previousCommand),
                CreateButton("Next", "\uE72A", 1, nextCommand)
            );

    private static Button CreateButton<TDC>(
        string text,
        string glyph,
        int column,
        Expression<Func<TDC, ICommand>> commandPath)
    {
        return new Button()
            .Grid(column: column)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Padding(16, 12)
            .Background(ButtonBackground)
            .Command<Button, TDC>(commandPath)
            .Content(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .HorizontalAlignment(HorizontalAlignment.Center)
                .Spacing(8)
                .Children(
                    new FontIcon().Glyph(glyph).FontSize(16).Visibility(column == 0 ? Visibility.Visible : Visibility.Collapsed),
                    new TextBlock().Text(text).FontSize(16),
                    new FontIcon().Glyph(glyph).FontSize(16).Visibility(column == 1 ? Visibility.Visible : Visibility.Collapsed)
                ));
    }
}

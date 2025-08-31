using Microsoft.UI;
using PaliPractice.Presentation.Behaviors;

namespace PaliPractice.Presentation.Components.Selectors;

public static class CardNavigationSelector
{
    public static UIElement Build(
        Action<Button> bindPreviousCommand,
        Action<Button> bindNextCommand) =>
        new Grid()
            .ColumnDefinitions("*,*")
            .Padding(20, 16)
            .ColumnSpacing(16)
            .Children(
                CreateButton("Previous", "\uE72B", 0, bindPreviousCommand),
                CreateButton("Next", "\uE72A", 1, bindNextCommand)
            );

    private static Button CreateButton(
        string text,
        string glyph,
        int column,
        Action<Button> bindCommand)
    {
        var button = new Button()
            .Grid(column: column)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Padding(16, 12)
            .Background(new SolidColorBrush(Color.FromArgb(255, 240, 240, 255)))
            .Content(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .HorizontalAlignment(HorizontalAlignment.Center)
                .Spacing(8)
                .Children(
                    new FontIcon().Glyph(glyph).FontSize(16).Visibility(column == 0 ? Visibility.Visible : Visibility.Collapsed),
                    new TextBlock().Text(text).FontSize(16),
                    new FontIcon().Glyph(glyph).FontSize(16).Visibility(column == 1 ? Visibility.Visible : Visibility.Collapsed)
                ));

        bindCommand(button);
        
        return button;
    }
}
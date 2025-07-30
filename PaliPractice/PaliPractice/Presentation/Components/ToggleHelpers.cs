namespace PaliPractice.Presentation.Components;

public static class ToggleHelpers
{
    public static ToggleButton Toggle(string glyph, string text, Func<bool> isChecked, Func<ICommand> command, Color color) =>
        new ToggleButton()
            .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                new FontIcon().Glyph(glyph).FontSize(14),
                new TextBlock().Text(text)))
            .IsChecked(isChecked)
            .Command(command)
            .Padding(12, 6)
            .Background(new SolidColorBrush(color));
}
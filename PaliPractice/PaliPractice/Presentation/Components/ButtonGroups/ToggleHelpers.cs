namespace PaliPractice.Presentation.Components.ButtonGroups;

public static class ToggleHelpers
{
    public static ToggleButton Toggle(string glyph, string text, Action<ToggleButton> bind, Color color)
    {
        var button = new ToggleButton()
            .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                new FontIcon().Glyph(glyph).FontSize(14),
                new TextBlock().Text(text)))
            .Padding(12, 6)
            .Background(new SolidColorBrush(color));
        bind(button);
        return button;
    }
}

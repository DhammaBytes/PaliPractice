namespace PaliPractice.Presentation.Components.ButtonGroups;

public static class NumberButtonGroup
{
    public static UIElement Build(
        Action<ToggleButton> bindSingular,
        Action<ToggleButton> bindPlural)
    {
        var singularButton = new ToggleButton()
            .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                new FontIcon().Glyph("\uE77B").FontSize(14),
                new TextBlock().Text("Singular")))
            .Padding(12, 6)
            .Background(new SolidColorBrush(Color.FromArgb(255, 230,230,255)));
        bindSingular(singularButton);

        var pluralButton = new ToggleButton()
            .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                new FontIcon().Glyph("\uE716").FontSize(14),
                new TextBlock().Text("Plural")))
            .Padding(12, 6)
            .Background(new SolidColorBrush(Color.FromArgb(255, 230,230,255)));
        bindPlural(pluralButton);

        return new StackPanel()
            .Orientation(Orientation.Horizontal)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Spacing(8)
            .Children(
                singularButton,
                pluralButton
            );
    }
}

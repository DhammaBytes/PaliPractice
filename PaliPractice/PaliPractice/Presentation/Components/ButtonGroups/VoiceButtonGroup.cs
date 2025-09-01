namespace PaliPractice.Presentation.Components.ButtonGroups;

public static class VoiceButtonGroup
{
    public static UIElement Build(
        Action<ToggleButton> bindNormal,
        Action<ToggleButton> bindReflexive)
    {
        var normalButton = new ToggleButton()
            .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                new FontIcon().Glyph("\uE719").FontSize(14),
                new TextBlock().Text("Normal")))
            .Padding(12, 6)
            .Background(new SolidColorBrush(Color.FromArgb(255, 230,255,230)));
        bindNormal(normalButton);

        var reflexiveButton = new ToggleButton()
            .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                new FontIcon().Glyph("\uE72A").FontSize(14),
                new TextBlock().Text("Reflexive")))
            .Padding(12, 6)
            .Background(new SolidColorBrush(Color.FromArgb(255, 230,255,230)));
        bindReflexive(reflexiveButton);

        return new StackPanel().Orientation(Orientation.Horizontal).HorizontalAlignment(HorizontalAlignment.Center).Spacing(8).Children(
            normalButton,
            reflexiveButton
        );
    }
}

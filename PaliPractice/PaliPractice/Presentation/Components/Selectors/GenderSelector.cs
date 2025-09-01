namespace PaliPractice.Presentation.Components.Selectors;

public static class GenderSelector
{
    public static UIElement Build(
        Action<ToggleButton> bindMasculine,
        Action<ToggleButton> bindNeuter,
        Action<ToggleButton> bindFeminine)
    {
        var masculineButton = new ToggleButton()
            .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                new FontIcon().Glyph("\uE765").FontSize(14),
                new TextBlock().Text("Masculine")))
            .Padding(12, 6)
            .Background(new SolidColorBrush(Color.FromArgb(255, 255,230,255)));
        bindMasculine(masculineButton);

        var neuterButton = new ToggleButton()
            .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                new FontIcon().Glyph("\uE766").FontSize(14),
                new TextBlock().Text("Neuter")))
            .Padding(12, 6)
            .Background(new SolidColorBrush(Color.FromArgb(255, 255,230,255)));
        bindNeuter(neuterButton);

        var feminineButton = new ToggleButton()
            .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                new FontIcon().Glyph("\uE767").FontSize(14),
                new TextBlock().Text("Feminine")))
            .Padding(12, 6)
            .Background(new SolidColorBrush(Color.FromArgb(255, 255,230,255)));
        bindFeminine(feminineButton);

        return new StackPanel().Orientation(Orientation.Horizontal).HorizontalAlignment(HorizontalAlignment.Center).Spacing(8).Children(
            masculineButton,
            neuterButton,
            feminineButton
        );
    }
}

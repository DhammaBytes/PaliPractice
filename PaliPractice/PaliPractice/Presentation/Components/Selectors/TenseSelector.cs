namespace PaliPractice.Presentation.Components.Selectors;

public static class TenseSelector
{
    public static UIElement Build(
        Action<ToggleButton> bindPresent,
        Action<ToggleButton> bindImperative,
        Action<ToggleButton> bindAorist,
        Action<ToggleButton> bindOptative,
        Action<ToggleButton> bindFuture)
    {
        var presentButton = new ToggleButton()
            .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                new FontIcon().Glyph("\uE121").FontSize(14),
                new TextBlock().Text("Present")))
            .Padding(12, 6)
            .Background(new SolidColorBrush(Color.FromArgb(255,230,255,230)));
        bindPresent(presentButton);

        var imperativeButton = new ToggleButton()
            .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                new FontIcon().Glyph("\uE7BA").FontSize(14),
                new TextBlock().Text("Imperative")))
            .Padding(12, 6)
            .Background(new SolidColorBrush(Color.FromArgb(255,230,255,230)));
        bindImperative(imperativeButton);

        var aoristButton = new ToggleButton()
            .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                new FontIcon().Glyph("\uE7C3").FontSize(14),
                new TextBlock().Text("Aorist")))
            .Padding(12, 6)
            .Background(new SolidColorBrush(Color.FromArgb(255,230,255,230)));
        bindAorist(aoristButton);

        var optativeButton = new ToggleButton()
            .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                new FontIcon().Glyph("\uE7C4").FontSize(14),
                new TextBlock().Text("Optative")))
            .Padding(12, 6)
            .Background(new SolidColorBrush(Color.FromArgb(255,230,255,230)));
        bindOptative(optativeButton);

        var futureButton = new ToggleButton()
            .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                new FontIcon().Glyph("\uE7C5").FontSize(14),
                new TextBlock().Text("Future")))
            .Padding(12, 6)
            .Background(new SolidColorBrush(Color.FromArgb(255,230,255,230)));
        bindFuture(futureButton);

        return new StackPanel().Orientation(Orientation.Horizontal).HorizontalAlignment(HorizontalAlignment.Center).Spacing(8).Children(
            presentButton,
            imperativeButton,
            aoristButton,
            optativeButton,
            futureButton
        );
    }
}
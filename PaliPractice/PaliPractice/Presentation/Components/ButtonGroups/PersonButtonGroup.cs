namespace PaliPractice.Presentation.Components.ButtonGroups;

public static class PersonButtonGroup
{
    public static UIElement Build(
        Action<ToggleButton> bindFirst,
        Action<ToggleButton> bindSecond,
        Action<ToggleButton> bindThird)
    {
        var firstButton = new ToggleButton()
            .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                new FontIcon().Glyph("\uE77A").FontSize(14),
                new TextBlock().Text("1st Person")))
            .Padding(12, 6)
            .Background(new SolidColorBrush(Color.FromArgb(255, 255,230,230)));
        bindFirst(firstButton);

        var secondButton = new ToggleButton()
            .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                new FontIcon().Glyph("\uE77B").FontSize(14),
                new TextBlock().Text("2nd Person")))
            .Padding(12, 6)
            .Background(new SolidColorBrush(Color.FromArgb(255, 255,230,230)));
        bindSecond(secondButton);

        var thirdButton = new ToggleButton()
            .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                new FontIcon().Glyph("\uE77C").FontSize(14),
                new TextBlock().Text("3rd Person")))
            .Padding(12, 6)
            .Background(new SolidColorBrush(Color.FromArgb(255, 255,230,230)));
        bindThird(thirdButton);

        return new StackPanel().Orientation(Orientation.Horizontal).HorizontalAlignment(HorizontalAlignment.Center).Spacing(8).Children(
            firstButton,
            secondButton,
            thirdButton
        );
    }
}

namespace PaliPractice.Presentation.Components.ButtonGroups;

public static class CaseButtonGroup
{
    public static UIElement Build(
        Action<ToggleButton> bindNominative,
        Action<ToggleButton> bindAccusative, 
        Action<ToggleButton> bindInstrumental,
        Action<ToggleButton> bindDative,
        Action<ToggleButton> bindAblative,
        Action<ToggleButton> bindGenitive,
        Action<ToggleButton> bindLocative,
        Action<ToggleButton> bindVocative)
    {
        var nominativeButton = new ToggleButton()
            .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                new FontIcon().Glyph("\uE761").FontSize(14),
                new TextBlock().Text("Nominative")))
            .Padding(12, 6)
            .Background(new SolidColorBrush(Color.FromArgb(255, 255,255,230)));
        bindNominative(nominativeButton);

        var accusativeButton = new ToggleButton()
            .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                new FontIcon().Glyph("\uE762").FontSize(14),
                new TextBlock().Text("Accusative")))
            .Padding(12, 6)
            .Background(new SolidColorBrush(Color.FromArgb(255, 255,255,230)));
        bindAccusative(accusativeButton);

        var instrumentalButton = new ToggleButton()
            .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                new FontIcon().Glyph("\uE763").FontSize(14),
                new TextBlock().Text("Instrumental")))
            .Padding(12, 6)
            .Background(new SolidColorBrush(Color.FromArgb(255, 255,255,230)));
        bindInstrumental(instrumentalButton);

        var dativeButton = new ToggleButton()
            .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                new FontIcon().Glyph("\uE764").FontSize(14),
                new TextBlock().Text("Dative")))
            .Padding(12, 6)
            .Background(new SolidColorBrush(Color.FromArgb(255, 255,255,230)));
        bindDative(dativeButton);

        var ablativeButton = new ToggleButton()
            .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                new FontIcon().Glyph("\uE768").FontSize(14),
                new TextBlock().Text("Ablative")))
            .Padding(12, 6)
            .Background(new SolidColorBrush(Color.FromArgb(255, 255,255,230)));
        bindAblative(ablativeButton);

        var genitiveButton = new ToggleButton()
            .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                new FontIcon().Glyph("\uE769").FontSize(14),
                new TextBlock().Text("Genitive")))
            .Padding(12, 6)
            .Background(new SolidColorBrush(Color.FromArgb(255, 255,255,230)));
        bindGenitive(genitiveButton);

        var locativeButton = new ToggleButton()
            .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                new FontIcon().Glyph("\uE76A").FontSize(14),
                new TextBlock().Text("Locative")))
            .Padding(12, 6)
            .Background(new SolidColorBrush(Color.FromArgb(255, 255,255,230)));
        bindLocative(locativeButton);

        var vocativeButton = new ToggleButton()
            .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                new FontIcon().Glyph("\uE76B").FontSize(14),
                new TextBlock().Text("Vocative")))
            .Padding(12, 6)
            .Background(new SolidColorBrush(Color.FromArgb(255, 255,255,230)));
        bindVocative(vocativeButton);

        return new StackPanel().Spacing(8).Children(
            new StackPanel().Orientation(Orientation.Horizontal).HorizontalAlignment(HorizontalAlignment.Center).Spacing(8).Children(
                nominativeButton,
                accusativeButton,
                instrumentalButton,
                dativeButton
            ),
            new StackPanel().Orientation(Orientation.Horizontal).HorizontalAlignment(HorizontalAlignment.Center).Spacing(8).Children(
                ablativeButton,
                genitiveButton,
                locativeButton,
                vocativeButton
            )
        );
    }
}

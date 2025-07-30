using Microsoft.UI;
using PaliPractice.Presentation.Behaviors;

namespace PaliPractice.Presentation.Components.Selectors;

public static class GenderSelector
{
    public static UIElement Build(Func<GenderSelectionBehavior> gender) =>
        new StackPanel().Orientation(Orientation.Horizontal).HorizontalAlignment(HorizontalAlignment.Center).Spacing(8).Children(
            new ToggleButton()
                .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                    new FontIcon().Glyph("\uE765").FontSize(14),
                    new TextBlock().Text("Masculine")))
                .IsChecked(() => gender().IsMasculineSelected)
                .Command(() => gender().SelectMasculineCommand)
                .Padding(12, 6)
                .Background(new SolidColorBrush(Color.FromArgb(255, 255,230,255))),
            new ToggleButton()
                .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                    new FontIcon().Glyph("\uE766").FontSize(14),
                    new TextBlock().Text("Neuter")))
                .IsChecked(() => gender().IsNeuterSelected)
                .Command(() => gender().SelectNeuterCommand)
                .Padding(12, 6)
                .Background(new SolidColorBrush(Color.FromArgb(255, 255,230,255))),
            new ToggleButton()
                .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                    new FontIcon().Glyph("\uE767").FontSize(14),
                    new TextBlock().Text("Feminine")))
                .IsChecked(() => gender().IsFeminineSelected)
                .Command(() => gender().SelectFeminineCommand)
                .Padding(12, 6)
                .Background(new SolidColorBrush(Color.FromArgb(255, 255,230,255)))
        );
}
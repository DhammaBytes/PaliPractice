using Microsoft.UI;
using PaliPractice.Presentation.Behaviors;

namespace PaliPractice.Presentation.Components.Selectors;

public static class NumberSelector
{
    public static UIElement Build(Func<NumberSelectionBehavior> number) =>
        new StackPanel()
            .Orientation(Orientation.Horizontal)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Spacing(8)
            .Children(
                new ToggleButton()
                    .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                        new FontIcon().Glyph("\uE77B").FontSize(14),
                        new TextBlock().Text("Singular")))
                    .IsChecked(() => number().IsSingularSelected)
                    .Command(() => number().SelectSingularCommand)
                    .Padding(12, 6)
                    .Background(new SolidColorBrush(Color.FromArgb(255, 230,230,255))),
                new ToggleButton()
                    .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                        new FontIcon().Glyph("\uE716").FontSize(14),
                        new TextBlock().Text("Plural")))
                    .IsChecked(() => number().IsPluralSelected)
                    .Command(() => number().SelectPluralCommand)
                    .Padding(12, 6)
                    .Background(new SolidColorBrush(Color.FromArgb(255, 230,230,255)))
            );
}
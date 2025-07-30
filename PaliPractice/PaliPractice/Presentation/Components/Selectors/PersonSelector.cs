using Microsoft.UI;
using PaliPractice.Presentation.Behaviors;

namespace PaliPractice.Presentation.Components.Selectors;

public static class PersonSelector
{
    public static UIElement Build(Func<PersonSelectionBehavior> person) =>
        new StackPanel().Orientation(Orientation.Horizontal).HorizontalAlignment(HorizontalAlignment.Center).Spacing(8).Children(
            new ToggleButton()
                .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                    new FontIcon().Glyph("\uE77A").FontSize(14),
                    new TextBlock().Text("1st Person")))
                .IsChecked(() => person().IsFirstPersonSelected)
                .Command(() => person().SelectFirstPersonCommand)
                .Padding(12, 6)
                .Background(new SolidColorBrush(Color.FromArgb(255, 255,230,230))),
            new ToggleButton()
                .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                    new FontIcon().Glyph("\uE77B").FontSize(14),
                    new TextBlock().Text("2nd Person")))
                .IsChecked(() => person().IsSecondPersonSelected)
                .Command(() => person().SelectSecondPersonCommand)
                .Padding(12, 6)
                .Background(new SolidColorBrush(Color.FromArgb(255, 255,230,230))),
            new ToggleButton()
                .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                    new FontIcon().Glyph("\uE77C").FontSize(14),
                    new TextBlock().Text("3rd Person")))
                .IsChecked(() => person().IsThirdPersonSelected)
                .Command(() => person().SelectThirdPersonCommand)
                .Padding(12, 6)
                .Background(new SolidColorBrush(Color.FromArgb(255, 255,230,230)))
        );
}
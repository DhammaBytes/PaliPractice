using Microsoft.UI;
using PaliPractice.Presentation.Behaviors;

namespace PaliPractice.Presentation.Components.Selectors;

public static class VoiceSelector
{
    public static UIElement Build(Func<VoiceSelectionBehavior> voice) =>
        new StackPanel().Orientation(Orientation.Horizontal).HorizontalAlignment(HorizontalAlignment.Center).Spacing(8).Children(
            new ToggleButton()
                .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                    new FontIcon().Glyph("\uE719").FontSize(14),
                    new TextBlock().Text("Normal")))
                .IsChecked(() => voice().IsNormalSelected)
                .Command(() => voice().SelectNormalCommand)
                .Padding(12, 6)
                .Background(new SolidColorBrush(Color.FromArgb(255, 230,255,230))),
            new ToggleButton()
                .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                    new FontIcon().Glyph("\uE72A").FontSize(14),
                    new TextBlock().Text("Reflexive")))
                .IsChecked(() => voice().IsReflexiveSelected)
                .Command(() => voice().SelectReflexiveCommand)
                .Padding(12, 6)
                .Background(new SolidColorBrush(Color.FromArgb(255, 230,255,230)))
        );
}
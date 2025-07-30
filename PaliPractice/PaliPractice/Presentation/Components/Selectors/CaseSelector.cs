using Microsoft.UI;
using PaliPractice.Presentation.Behaviors;

namespace PaliPractice.Presentation.Components.Selectors;

public static class CaseSelector
{
    public static UIElement Build(Func<CaseSelectionBehavior> cases) =>
        new StackPanel().Spacing(8).Children(
            new StackPanel().Orientation(Orientation.Horizontal).HorizontalAlignment(HorizontalAlignment.Center).Spacing(8).Children(
                new ToggleButton()
                    .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                        new FontIcon().Glyph("\uE761").FontSize(14),
                        new TextBlock().Text("Nominative")))
                    .IsChecked(() => cases().IsNominativeSelected)
                    .Command(() => cases().SelectNominativeCommand)
                    .Padding(12, 6)
                    .Background(new SolidColorBrush(Color.FromArgb(255, 255,255,230))),
                new ToggleButton()
                    .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                        new FontIcon().Glyph("\uE762").FontSize(14),
                        new TextBlock().Text("Accusative")))
                    .IsChecked(() => cases().IsAccusativeSelected)
                    .Command(() => cases().SelectAccusativeCommand)
                    .Padding(12, 6)
                    .Background(new SolidColorBrush(Color.FromArgb(255, 255,255,230))),
                new ToggleButton()
                    .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                        new FontIcon().Glyph("\uE763").FontSize(14),
                        new TextBlock().Text("Instrumental")))
                    .IsChecked(() => cases().IsInstrumentalSelected)
                    .Command(() => cases().SelectInstrumentalCommand)
                    .Padding(12, 6)
                    .Background(new SolidColorBrush(Color.FromArgb(255, 255,255,230))),
                new ToggleButton()
                    .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                        new FontIcon().Glyph("\uE764").FontSize(14),
                        new TextBlock().Text("Dative")))
                    .IsChecked(() => cases().IsDativeSelected)
                    .Command(() => cases().SelectDativeCommand)
                    .Padding(12, 6)
                    .Background(new SolidColorBrush(Color.FromArgb(255, 255,255,230)))
            ),
            new StackPanel().Orientation(Orientation.Horizontal).HorizontalAlignment(HorizontalAlignment.Center).Spacing(8).Children(
                new ToggleButton()
                    .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                        new FontIcon().Glyph("\uE768").FontSize(14),
                        new TextBlock().Text("Ablative")))
                    .IsChecked(() => cases().IsAblativeSelected)
                    .Command(() => cases().SelectAblativeCommand)
                    .Padding(12, 6)
                    .Background(new SolidColorBrush(Color.FromArgb(255, 255,255,230))),
                new ToggleButton()
                    .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                        new FontIcon().Glyph("\uE769").FontSize(14),
                        new TextBlock().Text("Genitive")))
                    .IsChecked(() => cases().IsGenitiveSelected)
                    .Command(() => cases().SelectGenitiveCommand)
                    .Padding(12, 6)
                    .Background(new SolidColorBrush(Color.FromArgb(255, 255,255,230))),
                new ToggleButton()
                    .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                        new FontIcon().Glyph("\uE76A").FontSize(14),
                        new TextBlock().Text("Locative")))
                    .IsChecked(() => cases().IsLocativeSelected)
                    .Command(() => cases().SelectLocativeCommand)
                    .Padding(12, 6)
                    .Background(new SolidColorBrush(Color.FromArgb(255, 255,255,230))),
                new ToggleButton()
                    .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                        new FontIcon().Glyph("\uE76B").FontSize(14),
                        new TextBlock().Text("Vocative")))
                    .IsChecked(() => cases().IsVocativeSelected)
                    .Command(() => cases().SelectVocativeCommand)
                    .Padding(12, 6)
                    .Background(new SolidColorBrush(Color.FromArgb(255, 255,255,230)))
            )
        );
}
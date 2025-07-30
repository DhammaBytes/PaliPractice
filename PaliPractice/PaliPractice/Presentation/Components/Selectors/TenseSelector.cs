using Microsoft.UI;
using PaliPractice.Presentation.Behaviors;

namespace PaliPractice.Presentation.Components.Selectors;

public static class TenseSelector
{
    public static UIElement Build(Func<TenseSelectionBehavior> tense) =>
        new StackPanel().Orientation(Orientation.Horizontal).HorizontalAlignment(HorizontalAlignment.Center).Spacing(8).Children(
            new ToggleButton()
                .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                    new FontIcon().Glyph("\uE121").FontSize(14),
                    new TextBlock().Text("Present")))
                .IsChecked(() => tense().IsPresentSelected)
                .Command(() => tense().SelectPresentCommand)
                .Padding(12, 6)
                .Background(new SolidColorBrush(Color.FromArgb(255,230,255,230))),
            new ToggleButton()
                .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                    new FontIcon().Glyph("\uE7BA").FontSize(14),
                    new TextBlock().Text("Imperative")))
                .IsChecked(() => tense().IsImperativeSelected)
                .Command(() => tense().SelectImperativeCommand)
                .Padding(12, 6)
                .Background(new SolidColorBrush(Color.FromArgb(255,230,255,230))),
            new ToggleButton()
                .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                    new FontIcon().Glyph("\uE7C3").FontSize(14),
                    new TextBlock().Text("Aorist")))
                .IsChecked(() => tense().IsAoristSelected)
                .Command(() => tense().SelectAoristCommand)
                .Padding(12, 6)
                .Background(new SolidColorBrush(Color.FromArgb(255,230,255,230))),
            new ToggleButton()
                .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                    new FontIcon().Glyph("\uE7C4").FontSize(14),
                    new TextBlock().Text("Optative")))
                .IsChecked(() => tense().IsOptativeSelected)
                .Command(() => tense().SelectOptativeCommand)
                .Padding(12, 6)
                .Background(new SolidColorBrush(Color.FromArgb(255,230,255,230))),
            new ToggleButton()
                .Content(new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(
                    new FontIcon().Glyph("\uE7C5").FontSize(14),
                    new TextBlock().Text("Future")))
                .IsChecked(() => tense().IsFutureSelected)
                .Command(() => tense().SelectFutureCommand)
                .Padding(12, 6)
                .Background(new SolidColorBrush(Color.FromArgb(255,230,255,230)))
        );
}
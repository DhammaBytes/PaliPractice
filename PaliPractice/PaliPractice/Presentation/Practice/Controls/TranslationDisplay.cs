using System.Linq.Expressions;
using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;

namespace PaliPractice.Presentation.Practice.Controls;

/// <summary>
/// Displays the meaning/translation in a squircle with navigation arrows on sides.
/// Squircle background is always visible; shows "…" until revealed.
/// Arrows only visible when multiple translations exist.
/// </summary>
public static class TranslationDisplay
{
    public static StackPanel Build<TDC>(
        Expression<Func<TDC, ExampleCarouselViewModel>> carouselPath,
        Expression<Func<TDC, bool>> isRevealedPath)
    {
        return new StackPanel()
            .Orientation(Orientation.Horizontal)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Spacing(8)
            .Children(
                // Previous button - hidden when only one translation
                new Button()
                    .Background(new SolidColorBrush(Colors.Transparent))
                    .Padding(8, 6)
                    .MinWidth(0)
                    .MinHeight(0)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Scope(carouselPath)
                    .CommandWithin<Button, ExampleCarouselViewModel>(c => c.PreviousCommand)
                    .VisibilityWithin<Button, ExampleCarouselViewModel>(c => c.HasMultipleTranslations)
                    .OpacityWithin<Button, ExampleCarouselViewModel>(c => c.IsRevealed)
                    .Content(new FontIcon()
                        .Glyph("\uE76B") // ChevronLeft
                        .FontSize(14)
                        .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))),

                // Translation in squircle - background always visible
                new SquircleBorder()
                    .Fill(ThemeResource.Get<Brush>("SurfaceBrush"))
                    .RadiusMode(SquircleRadiusMode.ButtonSmall)
                    .Child(
                        new Border()
                            .Padding(24, 16)
                            .Child(
                                new Grid()
                                    .Scope(carouselPath)
                                    .Children(
                                        // Placeholder "…" shown when not revealed
                                        new TextBlock()
                                            .Text("…")
                                            .FontSize(24)
                                            .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                                            .HorizontalAlignment(HorizontalAlignment.Center)
                                            .VerticalAlignment(VerticalAlignment.Center)
                                            .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
                                            .VisibilityWithin<TextBlock, ExampleCarouselViewModel>(c => c.IsRevealed, invert: true),

                                        // Translation content - shown when revealed
                                        new StackPanel()
                                            .Spacing(8)
                                            .HorizontalAlignment(HorizontalAlignment.Center)
                                            .VerticalAlignment(VerticalAlignment.Center)
                                            .VisibilityWithin<StackPanel, ExampleCarouselViewModel>(c => c.IsRevealed)
                                            .Children(
                                                // Translation text
                                                new TextBlock()
                                                    .TextWithin<ExampleCarouselViewModel>(c => c.CurrentMeaning)
                                                    .FontSize(16)
                                                    .FontStyle(Windows.UI.Text.FontStyle.Italic)
                                                    .TextWrapping(TextWrapping.Wrap)
                                                    .TextAlignment(TextAlignment.Center)
                                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                                    .MaxWidth(280)
                                                    .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush")),

                                                // Pagination below translation
                                                new TextBlock()
                                                    .TextWithin<ExampleCarouselViewModel>(c => c.PaginationText)
                                                    .FontSize(11)
                                                    .TextAlignment(TextAlignment.Center)
                                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                                    .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
                                                    .VisibilityWithin<TextBlock, ExampleCarouselViewModel>(c => c.HasMultipleTranslations)
                                            )
                                    )
                            )
                    ),

                // Next button - hidden when only one translation
                new Button()
                    .Background(new SolidColorBrush(Colors.Transparent))
                    .Padding(8, 6)
                    .MinWidth(0)
                    .MinHeight(0)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Scope(carouselPath)
                    .CommandWithin<Button, ExampleCarouselViewModel>(c => c.NextCommand)
                    .VisibilityWithin<Button, ExampleCarouselViewModel>(c => c.HasMultipleTranslations)
                    .OpacityWithin<Button, ExampleCarouselViewModel>(c => c.IsRevealed)
                    .Content(new FontIcon()
                        .Glyph("\uE76C") // ChevronRight
                        .FontSize(14)
                        .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush")))
            );
    }
}

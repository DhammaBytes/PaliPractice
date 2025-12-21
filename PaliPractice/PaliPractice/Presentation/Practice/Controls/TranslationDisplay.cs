using System.Linq.Expressions;
using PaliPractice.Presentation.Bindings;

namespace PaliPractice.Presentation.Practice.Controls;

/// <summary>
/// Displays the meaning/translation of the current word.
/// Updates when carousel index changes.
/// </summary>
public static class TranslationDisplay
{
    public static Border Build<TDC>(
        Expression<Func<TDC, ExampleCarouselViewModel>> carouselPath,
        Expression<Func<TDC, bool>> isRevealedPath)
    {
        // Border always visible to maintain layout height
        // Text opacity controlled by isRevealed (0 when hidden, 1 when revealed)
        return new Border()
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .CornerRadius(8)
            .Padding(16, 12)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .MinWidth(200)
            .MaxWidth(500)
            .Child(
                new TextBlock()
                    .Scope(carouselPath)
                    .TextWithin<ExampleCarouselViewModel>(c => c.CurrentMeaning)
                    .FontSize(16)
                    .FontStyle(Windows.UI.Text.FontStyle.Italic)
                    .TextWrapping(TextWrapping.Wrap)
                    .TextAlignment(TextAlignment.Center)
                    .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
                    .BoolToOpacity<TextBlock, TDC>(isRevealedPath)
            );
    }
}

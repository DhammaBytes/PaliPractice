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
        return new Border()
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .CornerRadius(8)
            .Padding(16, 12)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .MinWidth(200)
            .MaxWidth(500)
            .BoolToVisibility<Border, TDC>(isRevealedPath)
            .Child(
                new TextBlock()
                    .Scope(carouselPath)
                    .TextWithin<ExampleCarouselViewModel>(c => c.CurrentMeaning)
                    .FontSize(16)
                    .FontStyle(Windows.UI.Text.FontStyle.Italic)
                    .TextWrapping(TextWrapping.Wrap)
                    .TextAlignment(TextAlignment.Center)
                    .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
            );
    }
}

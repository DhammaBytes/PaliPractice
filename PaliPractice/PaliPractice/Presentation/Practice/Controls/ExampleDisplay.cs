using System.Linq.Expressions;
using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;
using PaliPractice.Themes;

namespace PaliPractice.Presentation.Practice.Controls;

/// <summary>
/// Displays the example sentence and reference for the current translation.
/// Example text has bold highlighting; reference is always visible.
/// </summary>
public static class ExampleSection
{
    public static StackPanel Build<TDC>(Expression<Func<TDC, ViewModels.ExampleCarouselViewModel>> carouselPath)
    {
        return new StackPanel()
            .Spacing(4)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .MaxWidth(LayoutConstants.ReferenceMaxWidth)
            .Scope(carouselPath)
            .Children(
                // Example text (with <b>bold</b> support)
                new TextBlock()
                    .HtmlTextWithin<ViewModels.ExampleCarouselViewModel>(c => c.CurrentExample)
                    .FontSize(14)
                    .FontFamily(FontPaths.LibertinusSans)
                    .TextWrapping(TextWrapping.Wrap)
                    .TextAlignment(TextAlignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush")),

                // Reference text (always visible)
                new TextBlock()
                    .TextWithin<ViewModels.ExampleCarouselViewModel>(c => c.CurrentReference)
                    .FontSize(12)
                    .FontFamily(FontPaths.LibertinusSans)
                    .TextWrapping(TextWrapping.Wrap)
                    .TextAlignment(TextAlignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
            );
    }
}

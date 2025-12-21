using System.Linq.Expressions;
using PaliPractice.Presentation.Bindings;

namespace PaliPractice.Presentation.Practice.Controls;

/// <summary>
/// Builds the example text + reference section.
/// Displays the Pali example sentence with bold highlighting and source reference.
/// </summary>
public static class ExampleSection
{
    public static StackPanel Build<TDC>(Expression<Func<TDC, ExampleCarouselViewModel>> carouselPath)
    {
        return new StackPanel()
            .Spacing(4)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Scope(carouselPath)
            .Children(
                // Example text (with <b>bold</b> support)
                new TextBlock()
                    .HtmlTextWithin<ExampleCarouselViewModel>(c => c.CurrentExample)
                    .FontSize(14)
                    .TextWrapping(TextWrapping.Wrap)
                    .TextAlignment(TextAlignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush")),

                // Reference text
                new TextBlock()
                    .TextWithin<ExampleCarouselViewModel>(c => c.CurrentReference)
                    .FontSize(12)
                    .TextWrapping(TextWrapping.Wrap)
                    .TextAlignment(TextAlignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
            );
    }
}

/// <summary>
/// Builds the carousel pagination controls for navigating between examples.
/// Displays: [&lt;] 1 of 5 [&gt;]
/// </summary>
public static class CarouselPaging
{
    public static StackPanel Build<TDC>(Expression<Func<TDC, ExampleCarouselViewModel>> carouselPath)
    {
        return new StackPanel()
            .Orientation(Orientation.Horizontal)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Spacing(4)
            .Scope(carouselPath)
            .Children(
                // Previous button
                new Button()
                    .Background(new SolidColorBrush(Colors.Transparent))
                    .Padding(6, 4)
                    .MinWidth(0)
                    .MinHeight(0)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .CommandWithin<Button, ExampleCarouselViewModel>(c => c.PreviousCommand)
                    .OpacityWithin<Button, ExampleCarouselViewModel>(c => c.HasMultipleExamples)
                    .Content(new FontIcon()
                        .Glyph("\uE76B") // ChevronLeft
                        .FontSize(12)
                        .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))),

                // Pagination text: "1 of 5"
                new TextBlock()
                    .TextWithin<ExampleCarouselViewModel>(c => c.PaginationText)
                    .FontSize(12)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush")),

                // Next button
                new Button()
                    .Background(new SolidColorBrush(Colors.Transparent))
                    .Padding(6, 4)
                    .MinWidth(0)
                    .MinHeight(0)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .CommandWithin<Button, ExampleCarouselViewModel>(c => c.NextCommand)
                    .OpacityWithin<Button, ExampleCarouselViewModel>(c => c.HasMultipleExamples)
                    .Content(new FontIcon()
                        .Glyph("\uE76C") // ChevronRight
                        .FontSize(12)
                        .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush")))
            );
    }
}

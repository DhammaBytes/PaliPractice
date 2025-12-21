using System.Linq.Expressions;
using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;

namespace PaliPractice.Presentation.Practice.Controls;

/// <summary>
/// Builds just the header row (rank badge + anki state) for the practice card.
/// </summary>
public static class CardHeader
{
    public static Grid Build<TDC>(
        Expression<Func<TDC, WordCardViewModel>> cardPath,
        string rankPrefix)
    {
        return new Grid()
            .ColumnDefinitions("Auto,*,Auto")
            .Children(
                new Border()
                    .Background(ThemeResource.Get<Brush>("PrimaryBrush"))
                    .CornerRadius(12)
                    .Padding(8, 4)
                    .Child(
                        new StackPanel()
                            .Orientation(Orientation.Horizontal)
                            .Spacing(4)
                            .Children(
                                new TextBlock()
                                    .Text(rankPrefix)
                                    .FontSize(12)
                                    .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                                    .Foreground(ThemeResource.Get<Brush>("OnPrimaryBrush")),
                                new TextBlock()
                                    .Scope(cardPath)
                                    .TextWithin<WordCardViewModel>(c => c.RankText)
                                    .FontSize(12)
                                    .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                                    .Foreground(ThemeResource.Get<Brush>("OnPrimaryBrush"))
                            )
                    )
                    .Grid(column: 0),
                new TextBlock()
                    .Scope(cardPath)
                    .TextWithin<WordCardViewModel>(c => c.AnkiState)
                    .FontSize(14)
                    .HorizontalAlignment(HorizontalAlignment.Right)
                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                    .Grid(column: 2)
            );
    }
}

/// <summary>
/// Builds the main word TextBlock for practice cards.
/// </summary>
public static class CardWord
{
    public static TextBlock Build<TDC>(Expression<Func<TDC, WordCardViewModel>> cardPath)
    {
        return new TextBlock()
            .Scope(cardPath)
            .TextWithin<WordCardViewModel>(c => c.CurrentWord)
            .FontSize(48)
            .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .TextAlignment(TextAlignment.Center)
            .TextWrapping(TextWrapping.Wrap)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
            .Margin(0, 16, 0, 8);
    }
}

/// <summary>
/// Builds the example + reference section (without pagination).
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
                // Example text
                new TextBlock()
                    .TextWithin<ExampleCarouselViewModel>(c => c.CurrentExample)
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
/// Builds the carousel pagination controls: &lt;| 1 of 5 |&gt;
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

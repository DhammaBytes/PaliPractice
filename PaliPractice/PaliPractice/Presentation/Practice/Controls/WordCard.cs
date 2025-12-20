using System.Linq.Expressions;
using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;

namespace PaliPractice.Presentation.Practice.Controls;

public static class WordCard
{
    public static Border Build<TDC>(
        Expression<Func<TDC, WordCardViewModel>> cardPath,
        Expression<Func<TDC, ExampleCarouselViewModel>> carouselPath,
        string rankPrefix)
    {
        return new Border()
            .MaxWidth(LayoutConstants.ContentMaxWidth)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .CornerRadius(12)
            .Padding(24)
            .Child(
                new Grid()
                    .RowDefinitions("Auto,Auto,Auto") // Header, Main word, Example with arrows
                    .Children(
                        // Row 0: Header (rank + anki)
                        BuildHeaderRow<TDC>(cardPath, rankPrefix).Grid(row: 0),

                        // Row 1: Main word (lemma)
                        BuildMainWordRow<TDC>(cardPath).Grid(row: 1),

                        // Row 2: Example with arrows (always visible)
                        BuildExampleRow<TDC>(carouselPath).Grid(row: 2)
                    )
            );
    }

    static Grid BuildHeaderRow<TDC>(Expression<Func<TDC, WordCardViewModel>> cardPath, string rankPrefix)
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

    static TextBlock BuildMainWordRow<TDC>(Expression<Func<TDC, WordCardViewModel>> cardPath)
    {
        return new TextBlock()
            .Scope(cardPath)
            .TextWithin<WordCardViewModel>(c => c.CurrentWord)
            .FontSize(48)
            .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .TextAlignment(TextAlignment.Center)
            .TextWrapping(TextWrapping.Wrap)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
            .Margin(0, 16, 0, 8);
    }

    static Grid BuildExampleRow<TDC>(Expression<Func<TDC, ExampleCarouselViewModel>> carouselPath)
    {
        return new Grid()
            .ColumnDefinitions("Auto,*,Auto")
            .Margin(0, 8, 0, 0)
            .Children(
                // Left arrow button (opacity preserves layout space)
                new Button()
                    .Grid(column: 0)
                    .Background(ThemeResource.Get<Brush>("SurfaceVariantBrush"))
                    .CornerRadius(20)
                    .Padding(8)
                    .MinWidth(40)
                    .MinHeight(40)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Scope(carouselPath)
                    .CommandWithin<Button, ExampleCarouselViewModel>(c => c.PreviousCommand)
                    .OpacityWithin<Button, ExampleCarouselViewModel>(c => c.HasMultipleExamples)
                    .Content(new FontIcon()
                        .Glyph("\uE76B") // ChevronLeft
                        .FontSize(16)
                        .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))),

                // Center: Example + Reference
                new StackPanel()
                    .Grid(column: 1)
                    .Spacing(4)
                    .Margin(12, 0)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .Scope(carouselPath)
                    .Children(
                        new TextBlock()
                            .TextWithin<ExampleCarouselViewModel>(c => c.CurrentExample)
                            .FontSize(14)
                            .TextWrapping(TextWrapping.Wrap)
                            .TextAlignment(TextAlignment.Center)
                            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush")),
                        new TextBlock()
                            .TextWithin<ExampleCarouselViewModel>(c => c.CurrentReference)
                            .FontSize(12)
                            .TextWrapping(TextWrapping.Wrap)
                            .TextAlignment(TextAlignment.Center)
                            .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                    ),

                // Right arrow button (opacity preserves layout space)
                new Button()
                    .Grid(column: 2)
                    .Background(ThemeResource.Get<Brush>("SurfaceVariantBrush"))
                    .CornerRadius(20)
                    .Padding(8)
                    .MinWidth(40)
                    .MinHeight(40)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Scope(carouselPath)
                    .CommandWithin<Button, ExampleCarouselViewModel>(c => c.NextCommand)
                    .OpacityWithin<Button, ExampleCarouselViewModel>(c => c.HasMultipleExamples)
                    .Content(new FontIcon()
                        .Glyph("\uE76C") // ChevronRight
                        .FontSize(16)
                        .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush")))
            );
    }
}

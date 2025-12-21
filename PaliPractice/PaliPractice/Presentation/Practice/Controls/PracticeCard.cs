using System.Linq.Expressions;
using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;

namespace PaliPractice.Presentation.Practice.Controls;

/// <summary>
/// Builds the header row for practice cards: [Rank Badge] ... [Anki State]
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

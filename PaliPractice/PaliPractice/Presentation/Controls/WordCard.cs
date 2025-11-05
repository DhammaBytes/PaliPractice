using System.Linq.Expressions;
using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.ViewModels;

namespace PaliPractice.Presentation.Controls;

public static class WordCard
{
    public static Border Build<TDC>(
        Expression<Func<TDC, CardViewModel>> cardPath,
        string rankPrefix)
    {
        return new Border()
            .MinWidth(360)
            .MaxWidth(600)
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .CornerRadius(12)
            .Padding(24)
            .Scope<Border, TDC, CardViewModel>(cardPath) // Bind scope once
            .VisibilityWithin<Border, CardViewModel>(c => !c.IsLoading) // Within the CardViewModel scope
            .Child(
                new Grid()
                    .RowDefinitions("Auto,Auto,Auto")
                    .Children(
                        // Header row: rank + anki
                        new Grid().ColumnDefinitions("Auto,*,Auto").Children(
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
                                                .TextWithin<CardViewModel>(c => c.RankText)
                                                .FontSize(12)
                                                .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                                                .Foreground(ThemeResource.Get<Brush>("OnPrimaryBrush"))
                                        )
                                )
                                .Grid(column: 0),
                            new TextBlock()
                                .TextWithin<CardViewModel>(c => c.AnkiState)
                                .FontSize(14)
                                .HorizontalAlignment(HorizontalAlignment.Right)
                                .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                                .Grid(column: 2)
                        ).Grid(row: 0),

                        // Main word
                        new TextBlock()
                            .TextWithin<CardViewModel>(c => c.CurrentWord)
                            .FontSize(48)
                            .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .TextAlignment(TextAlignment.Center)
                            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                            .Margin(0, 16)
                            .Grid(row: 1),

                        // Example + reference
                        new StackPanel().Spacing(8).Children(
                            new TextBlock()
                                .TextWithin<CardViewModel>(c => c.UsageExample)
                                .FontSize(16)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .TextAlignment(TextAlignment.Center)
                                .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                                .TextWrapping(TextWrapping.Wrap),
                            new TextBlock()
                                .TextWithin<CardViewModel>(c => c.SuttaReference)
                                .FontSize(12)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .TextAlignment(TextAlignment.Center)
                                .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                        ).Grid(row: 2)
                    )
            );
    }
}

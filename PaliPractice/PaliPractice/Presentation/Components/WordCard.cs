namespace PaliPractice.Presentation.Components;

public static class WordCard
{
    public static Border Build(
        Func<bool> isLoading,
        Action<TextBlock> bindRankText,
        Action<TextBlock> bindAnkiState,
        Action<TextBlock> bindCurrentWord,
        Action<TextBlock> bindUsageExample,
        Action<TextBlock> bindSuttaReference,
        string rankPrefix)
    {
        var rankTextBlock = new TextBlock();
        bindRankText(rankTextBlock);

        var ankiTextBlock = new TextBlock();
        bindAnkiState(ankiTextBlock);

        var mainWordTextBlock = new TextBlock();
        bindCurrentWord(mainWordTextBlock);

        var exampleTextBlock = new TextBlock();
        bindUsageExample(exampleTextBlock);

        var refTextBlock = new TextBlock();
        bindSuttaReference(refTextBlock);

        return
        new Border()
            .MinWidth(360)
            .MaxWidth(600)
            .Visibility(isLoading, l => !l ? Visibility.Visible : Visibility.Collapsed)
            .Background(Theme.Brushes.Surface.Default)
            .CornerRadius(12)
            .Padding(24)
            .Child(
                new Grid()
                    .RowDefinitions("Auto,Auto,Auto")
                    .Children(
                        // Header row: rank + anki
                        new Grid().ColumnDefinitions("Auto,*,Auto").Children(
                            new Border().Background(Theme.Brushes.Primary.Default).CornerRadius(12).Padding(8,4).Child(
                                new StackPanel().Orientation(Orientation.Horizontal).Spacing(4).Children(
                                    new TextBlock().Text(rankPrefix).FontSize(12).FontWeight(Microsoft.UI.Text.FontWeights.Bold).Foreground(Theme.Brushes.OnPrimary.Default),
                                    rankTextBlock.FontSize(12).FontWeight(Microsoft.UI.Text.FontWeights.Bold).Foreground(Theme.Brushes.OnPrimary.Default)))
                                .Grid(column:0),
                            ankiTextBlock.FontSize(14)
                                .HorizontalAlignment(HorizontalAlignment.Right)
                                .Foreground(Theme.Brushes.OnBackground.Medium)
                                .Grid(column:2)
                        ).Grid(row:0),

                        // Main word
                        mainWordTextBlock
                            .FontSize(48)
                            .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .TextAlignment(TextAlignment.Center)
                            .Foreground(Theme.Brushes.OnBackground.Default)
                            .Margin(0,16)
                            .Grid(row:1),

                        // Example + reference
                        new StackPanel().Spacing(8).Children(
                            exampleTextBlock
                                .FontSize(16)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .TextAlignment(TextAlignment.Center)
                                .Foreground(Theme.Brushes.OnBackground.Default)
                                .TextWrapping(TextWrapping.Wrap),
                            refTextBlock
                                .FontSize(12)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .TextAlignment(TextAlignment.Center)
                                .Foreground(Theme.Brushes.OnBackground.Medium)
                        ).Grid(row:2)
                    )
            );
    }
}

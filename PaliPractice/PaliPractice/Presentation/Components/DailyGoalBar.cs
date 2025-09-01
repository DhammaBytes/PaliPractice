namespace PaliPractice.Presentation.Components;

public static class DailyGoalBar
{
    public static Border Build(Action<TextBlock> bindDailyGoalText, Action<ProgressBar> bindDailyProgress)
    {
        var goalTextBlock = new TextBlock();
        bindDailyGoalText(goalTextBlock);

        var progressBar = new ProgressBar();
        bindDailyProgress(progressBar);

        return
        new Border().Background(Theme.Brushes.Surface.Default).Padding(20,12).Child(
            new StackPanel().Spacing(8).Children(
                new Grid().ColumnDefinitions("*,Auto").Children(
                    new TextBlock().Text("Daily goal").FontSize(14).Foreground(Theme.Brushes.OnBackground.Medium).Grid(column:0),
                    goalTextBlock.FontSize(14).Foreground(Theme.Brushes.OnBackground.Medium).Grid(column:1)),
                progressBar.Maximum(100).Height(6).CornerRadius(3)
            )
        );
    }
}
using System.Linq.Expressions;
using PaliPractice.Presentation.Bindings;

namespace PaliPractice.Presentation.Controls;

public static class DailyGoalBar
{
    public static Border Build<TDC>(
        Expression<Func<TDC, string>> dailyGoalText,
        Expression<Func<TDC, double>> dailyProgress)
    {
        return new Border()
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .Padding(20, 12)
            .Child(
                new StackPanel().Spacing(8).Children(
                    new Grid().ColumnDefinitions("*,Auto").Children(
                        new TextBlock()
                            .Text("Daily goal")
                            .FontSize(14)
                            .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                            .Grid(column: 0),
                        new TextBlock()
                            .Text(dailyGoalText)
                            .FontSize(14)
                            .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                            .Grid(column: 1)
                    ),
                    new ProgressBar()
                        .Maximum(100)
                        .Height(6)
                        .CornerRadius(3)
                        .Value(dailyProgress)
                )
            );
    }
}

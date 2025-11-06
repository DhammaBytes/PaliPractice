using System.Linq.Expressions;
using PaliPractice.Presentation.Bindings;

namespace PaliPractice.Presentation.Controls;

public static class AppTitleBar
{
    public static Grid Build<TDC>(string title, Expression<Func<TDC, ICommand>> goBackCommand)
    {
        return
            new Grid()
                .ColumnDefinitions("Auto,*,Auto")
                .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
                .Padding(16, 8)
                .Children(
                    new Button()
                        .Content(new FontIcon().Glyph("\uE72B"))
                        .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
                        .Command(goBackCommand)
                        .Grid(column: 0),
                    new TextBlock()
                        .Text(title)
                        .FontSize(20)
                        .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                        .HorizontalAlignment(HorizontalAlignment.Center)
                        .VerticalAlignment(VerticalAlignment.Center)
                        .Grid(column: 1),
                    new Button()
                        .Content(new FontIcon().Glyph("\uE713"))
                        .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
                        .Grid(column: 2)
                );
    }
}

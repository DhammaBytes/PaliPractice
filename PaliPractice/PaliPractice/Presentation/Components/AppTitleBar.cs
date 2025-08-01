namespace PaliPractice.Presentation.Components;

public static class AppTitleBar
{
    public static Grid Build(string title, Action<Button> bindGoBack)
    {
        var backButton = new Button();
        bindGoBack(backButton);

        return
        new Grid().ColumnDefinitions("Auto,*,Auto").Background(Theme.Brushes.Surface.Default).Padding(16,8).Children(
            backButton.Content(new FontIcon().Glyph("\uE72B")).Background(Theme.Brushes.Surface.Default).Grid(column:0),
            new TextBlock().Text(title).FontSize(20).FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                .HorizontalAlignment(HorizontalAlignment.Center).VerticalAlignment(VerticalAlignment.Center).Grid(column:1),
            new Button().Content(new FontIcon().Glyph("\uE734")).Background(Theme.Brushes.Surface.Default).Grid(column:2));
    }
}
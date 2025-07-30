using Microsoft.UI;
using System.Windows.Input;

namespace PaliPractice.Presentation;

public static class SharedPracticeComponents
{
    public static Grid CreateTitleBar(string title, Func<ICommand> goBackCommand)
    {
        return new Grid()
            .ColumnDefinitions("Auto,*,Auto")
            .Background(Theme.Brushes.Surface.Default)
            .Padding(16, 8)
            .Children(
                new Button()
                    .Content(new FontIcon().Glyph("\uE72B")) // Back arrow
                    .Command(goBackCommand)
                    .Background(Theme.Brushes.Surface.Default)
                    .Grid(column: 0),
                new TextBlock()
                    .Text(title)
                    .FontSize(20)
                    .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Grid(column: 1),
                new Button()
                    .Content(new FontIcon().Glyph("\uE734")) // Star icon
                    .Background(Theme.Brushes.Surface.Default)
                    .Grid(column: 2)
            );
    }

    public static Border CreateWordCard(
        Func<bool> isLoading,
        Func<string> currentWord,
        Func<string> rankText,
        Func<string> ankiState,
        Func<string> usageExample,
        Func<string> suttaReference,
        string rankPrefix = "N")
    {
        return new Border()
            .Visibility(isLoading, loading => !loading ? Visibility.Visible : Visibility.Collapsed)
            .Background(Theme.Brushes.Surface.Default)
            .CornerRadius(12)
            .Padding(24)
            .Child(
                new StackPanel()
                    .Spacing(16)
                    .Children(
                        // Top row: Rank and Anki state
                        new Grid()
                            .ColumnDefinitions("Auto,*,Auto")
                            .Children(
                                // Rank badge
                                new Border()
                                    .Background(Theme.Brushes.Primary.Default)
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
                                                    .Foreground(Theme.Brushes.OnPrimary.Default),
                                                new TextBlock()
                                                    .Text(rankText)
                                                    .FontSize(12)
                                                    .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                                                    .Foreground(Theme.Brushes.OnPrimary.Default)
                                            )
                                    )
                                    .Grid(column: 0),
                                new TextBlock()
                                    .Text(ankiState)
                                    .FontSize(14)
                                    .HorizontalAlignment(HorizontalAlignment.Right)
                                    .Foreground(Theme.Brushes.OnBackground.Medium)
                                    .Grid(column: 2)
                            ),
                        
                        // Main Pali word
                        new TextBlock()
                            .Text(currentWord)
                            .FontSize(48)
                            .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .TextAlignment(TextAlignment.Center)
                            .Foreground(Theme.Brushes.OnBackground.Default)
                            .Margin(0, 16),
                        
                        // Usage example
                        new StackPanel()
                            .Spacing(8)
                            .Children(
                                new TextBlock()
                                    .Text(usageExample)
                                    .FontSize(16)
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .TextAlignment(TextAlignment.Center)
                                    .Foreground(Theme.Brushes.OnBackground.Default)
                                    .TextWrapping(TextWrapping.Wrap),
                                new TextBlock()
                                    .Text(suttaReference)
                                    .FontSize(12)
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .TextAlignment(TextAlignment.Center)
                                    .Foreground(Theme.Brushes.OnBackground.Medium)
                            )
                    )
            );
    }

    public static Border CreateProgressBar(Func<string> dailyGoalText, Func<double> dailyProgress)
    {
        return new Border()
            .Background(Theme.Brushes.Surface.Default)
            .Padding(20, 12)
            .Child(
                new StackPanel()
                    .Spacing(8)
                    .Children(
                        new Grid()
                            .ColumnDefinitions("*,Auto")
                            .Children(
                                new TextBlock()
                                    .Text("Daily goal")
                                    .FontSize(14)
                                    .Foreground(Theme.Brushes.OnBackground.Medium)
                                    .Grid(column: 0),
                                new TextBlock()
                                    .Text(dailyGoalText)
                                    .FontSize(14)
                                    .Foreground(Theme.Brushes.OnBackground.Medium)
                                    .Grid(column: 1)
                            ),
                        new ProgressBar()
                            .Value(dailyProgress)
                            .Maximum(100)
                            .Height(6)
                            .CornerRadius(3)
                    )
            );
    }

    public static ToggleButton CreateToggleButton(string iconGlyph, string text, Color backgroundColor)
    {
        return new ToggleButton()
            .Content(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .Spacing(8)
                .Children(
                    new FontIcon().Glyph(iconGlyph).FontSize(14),
                    new TextBlock().Text(text)
                )
            )
            .Padding(12, 6)
            .Background(new SolidColorBrush(backgroundColor));
    }

    public static ProgressRing CreateLoadingIndicator(Func<bool> isLoading)
    {
        return new ProgressRing()
            .IsActive(isLoading)
            .Visibility(isLoading, loading => loading ? Visibility.Visible : Visibility.Collapsed)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .VerticalAlignment(VerticalAlignment.Center);
    }

    public static TextBlock CreateErrorMessage(Func<string> errorMessage)
    {
        return new TextBlock()
            .Text(errorMessage)
            .Visibility(errorMessage, error => !string.IsNullOrEmpty(error) ? Visibility.Visible : Visibility.Collapsed)
            .Foreground(Theme.Brushes.OnBackground.Medium)
            .TextAlignment(TextAlignment.Center)
            .HorizontalAlignment(HorizontalAlignment.Center);
    }
}
using Microsoft.UI;

namespace PaliPractice.Presentation;

public sealed partial class ConjugationPracticePage : Page
{
    public ConjugationPracticePage()
    {
        this.DataContext<ConjugationPracticeViewModel>((page, vm) => page
            .NavigationCacheMode(NavigationCacheMode.Required)
            .Background(Theme.Brushes.Background.Default)
            .Content(new Grid()
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
                .RowDefinitions("Auto,*,Auto,Auto")
                .Children(
                    // Title bar with back button
                    new Grid()
                        .ColumnDefinitions("Auto,*,Auto")
                        .Background(Theme.Brushes.Surface.Default)
                        .Padding(16, 8)
                        .Children(
                            new Button()
                                .Content(new FontIcon().Glyph("\uE72B")) // Back arrow
                                .Command(() => vm.GoBackCommand)
                                .Background(Theme.Brushes.Surface.Default)
                                .Grid(column: 0),
                            new TextBlock()
                                .Text("Conjugation Practice")
                                .FontSize(20)
                                .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .VerticalAlignment(VerticalAlignment.Center)
                                .Grid(column: 1),
                            new Button()
                                .Content(new FontIcon().Glyph("\uE734")) // Star icon
                                .Background(Theme.Brushes.Surface.Default)
                                .Grid(column: 2)
                        ),
                    
                    // Main scrollable content - just the word card
                    new ScrollViewer()
                        .Grid(row: 1)
                        .Content(
                            new Grid()
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .VerticalAlignment(VerticalAlignment.Center)
                                .Children(
                                    new StackPanel()
                                        .Padding(20)
                                        .Spacing(24)
                                        .MaxWidth(600) // Constrained width for better readability on wide screens
                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                        .Children(
                                    // Loading indicator
                                    new ProgressRing()
                                        .IsActive(() => vm.IsLoading)
                                        .Visibility(() => vm.IsLoading, loading => loading ? Visibility.Visible : Visibility.Collapsed)
                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                        .VerticalAlignment(VerticalAlignment.Center),
                                    
                                    // Error message
                                    new TextBlock()
                                        .Text(() => vm.ErrorMessage)
                                        .Visibility(() => vm.ErrorMessage, error => !string.IsNullOrEmpty(error) ? Visibility.Visible : Visibility.Collapsed)
                                        .Foreground(Theme.Brushes.OnBackground.Medium)
                                        .TextAlignment(TextAlignment.Center)
                                        .HorizontalAlignment(HorizontalAlignment.Center),
                                    
                                    // Word card
                                    new Border()
                                        .Visibility(() => vm.IsLoading, loading => !loading ? Visibility.Visible : Visibility.Collapsed)
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
                                                                                .Text("V")
                                                                                .FontSize(12)
                                                                                .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                                                                                .Foreground(Theme.Brushes.OnPrimary.Default),
                                                                            new TextBlock()
                                                                                .Text(() => vm.RankText)
                                                                                .FontSize(12)
                                                                                .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                                                                                .Foreground(Theme.Brushes.OnPrimary.Default)
                                                                        )
                                                                )
                                                                .Grid(column: 0),
                                                            new TextBlock()
                                                                .Text(() => vm.AnkiState)
                                                                .FontSize(14)
                                                                .HorizontalAlignment(HorizontalAlignment.Right)
                                                                .Foreground(Theme.Brushes.OnBackground.Medium)
                                                                .Grid(column: 2)
                                                        ),
                                                    
                                                    // Main Pali word
                                                    new TextBlock()
                                                        .Text(() => vm.CurrentWord)
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
                                                                .Text(() => vm.UsageExample)
                                                                .FontSize(16)
                                                                .HorizontalAlignment(HorizontalAlignment.Center)
                                                                .TextAlignment(TextAlignment.Center)
                                                                .Foreground(Theme.Brushes.OnBackground.Default)
                                                                .TextWrapping(TextWrapping.Wrap),
                                                            new TextBlock()
                                                                .Text(() => vm.SuttaReference)
                                                                .FontSize(12)
                                                                .HorizontalAlignment(HorizontalAlignment.Center)
                                                                .TextAlignment(TextAlignment.Center)
                                                                .Foreground(Theme.Brushes.OnBackground.Medium)
                                                        )
                                                )
                                        )
                                    )
                                )
                        ),
                    
                    // Button groups - pinned to bottom
                    new StackPanel()
                        .Grid(row: 2)
                        .Visibility(() => vm.IsLoading, loading => !loading ? Visibility.Visible : Visibility.Collapsed)
                        .Padding(20)
                        .Spacing(16)
                        .Children(
                            // Number selection (Singular/Plural) - same as nouns
                            new StackPanel()
                                .Orientation(Orientation.Horizontal)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .Spacing(8)
                                .Children(
                                    new ToggleButton()
                                        .Content(new StackPanel()
                                            .Orientation(Orientation.Horizontal)
                                            .Spacing(8)
                                            .Children(
                                                new FontIcon().Glyph("\uE77B"), // Person icon
                                                new TextBlock().Text("Singular")
                                            )
                                        )
                                        .IsChecked(() => vm.IsSingularSelected)
                                        .Command(() => vm.SelectSingularCommand)
                                        .Padding(16, 8)
                                        .Background(new SolidColorBrush(Color.FromArgb(255, 230, 230, 255))), // Light purple
                                    new ToggleButton()
                                        .Content(new StackPanel()
                                            .Orientation(Orientation.Horizontal)
                                            .Spacing(8)
                                            .Children(
                                                new FontIcon().Glyph("\uE716"), // People icon
                                                new TextBlock().Text("Plural")
                                            )
                                        )
                                        .IsChecked(() => vm.IsPluralSelected)
                                        .Command(() => vm.SelectPluralCommand)
                                        .Padding(16, 8)
                                        .Background(new SolidColorBrush(Color.FromArgb(255, 230, 230, 255))) // Light purple
                                ),
                            
                            // Person selection (1st/2nd/3rd)
                            new StackPanel()
                                .Orientation(Orientation.Horizontal)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .Spacing(8)
                                .Children(
                                    new ToggleButton()
                                        .Content(new StackPanel()
                                            .Orientation(Orientation.Horizontal)
                                            .Spacing(8)
                                            .Children(
                                                new FontIcon().Glyph("\uE77B"), // Person icon
                                                new TextBlock().Text("1st")
                                            )
                                        )
                                        .IsChecked(() => vm.IsFirstPersonSelected)
                                        .Command(() => vm.SelectFirstPersonCommand)
                                        .Padding(16, 8)
                                        .Background(new SolidColorBrush(Color.FromArgb(255, 255, 230, 230))), // Light pink
                                    new ToggleButton()
                                        .Content(new StackPanel()
                                            .Orientation(Orientation.Horizontal)
                                            .Spacing(8)
                                            .Children(
                                                new FontIcon().Glyph("\uE716"), // People icon
                                                new TextBlock().Text("2nd")
                                            )
                                        )
                                        .IsChecked(() => vm.IsSecondPersonSelected)
                                        .Command(() => vm.SelectSecondPersonCommand)
                                        .Padding(16, 8)
                                        .Background(new SolidColorBrush(Color.FromArgb(255, 255, 230, 230))), // Light pink
                                    new ToggleButton()
                                        .Content(new StackPanel()
                                            .Orientation(Orientation.Horizontal)
                                            .Spacing(8)
                                            .Children(
                                                new FontIcon().Glyph("\uE125"), // Three people icon
                                                new TextBlock().Text("3rd")
                                            )
                                        )
                                        .IsChecked(() => vm.IsThirdPersonSelected)
                                        .Command(() => vm.SelectThirdPersonCommand)
                                        .Padding(16, 8)
                                        .Background(new SolidColorBrush(Color.FromArgb(255, 255, 230, 230))) // Light pink
                                ),
                            
                            // Voice selection (Normal/Reflexive)
                            new StackPanel()
                                .Orientation(Orientation.Horizontal)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .Spacing(8)
                                .Children(
                                    new ToggleButton()
                                        .Content(new StackPanel()
                                            .Orientation(Orientation.Horizontal)
                                            .Spacing(8)
                                            .Children(
                                                new FontIcon().Glyph("\uE8E6"), // Play icon
                                                new TextBlock().Text("Normal")
                                            )
                                        )
                                        .IsChecked(() => vm.IsNormalSelected)
                                        .Command(() => vm.SelectNormalCommand)
                                        .Padding(16, 8)
                                        .Background(new SolidColorBrush(Color.FromArgb(255, 255, 245, 230))), // Light yellow
                                    new ToggleButton()
                                        .Content(new StackPanel()
                                            .Orientation(Orientation.Horizontal)
                                            .Spacing(8)
                                            .Children(
                                                new FontIcon().Glyph("\uE71C"), // Sync icon
                                                new TextBlock().Text("Reflexive")
                                            )
                                        )
                                        .IsChecked(() => vm.IsReflexiveSelected)
                                        .Command(() => vm.SelectReflexiveCommand)
                                        .Padding(16, 8)
                                        .Background(new SolidColorBrush(Color.FromArgb(255, 255, 245, 230))) // Light yellow
                                ),
                            
                            // Tense/Mood selection (2 rows: 2+3 buttons)
                            new StackPanel()
                                .Spacing(8)
                                .Children(
                                    // First row: Present, Imperative
                                    new StackPanel()
                                        .Orientation(Orientation.Horizontal)
                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                        .Spacing(8)
                                        .Children(
                                            new ToggleButton()
                                                .Content(new StackPanel()
                                                    .Orientation(Orientation.Horizontal)
                                                    .Spacing(6)
                                                    .Children(
                                                        new FontIcon().Glyph("\uE121").FontSize(14), // Clock icon
                                                        new TextBlock().Text("Present")
                                                    )
                                                )
                                                .IsChecked(() => vm.IsPresentSelected)
                                                .Command(() => vm.SelectPresentCommand)
                                                .Padding(12, 6)
                                                .Background(new SolidColorBrush(Color.FromArgb(255, 230, 255, 230))), // Light green
                                            new ToggleButton()
                                                .Content(new StackPanel()
                                                    .Orientation(Orientation.Horizontal)
                                                    .Spacing(6)
                                                    .Children(
                                                        new FontIcon().Glyph("\uE7BA").FontSize(14), // Exclamation icon
                                                        new TextBlock().Text("Imperative")
                                                    )
                                                )
                                                .IsChecked(() => vm.IsImperativeSelected)
                                                .Command(() => vm.SelectImperativeCommand)
                                                .Padding(12, 6)
                                                .Background(new SolidColorBrush(Color.FromArgb(255, 230, 255, 230))) // Light green
                                        ),
                                    // Second row: Aorist, Optative, Future
                                    new StackPanel()
                                        .Orientation(Orientation.Horizontal)
                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                        .Spacing(8)
                                        .Children(
                                            new ToggleButton()
                                                .Content(new StackPanel()
                                                    .Orientation(Orientation.Horizontal)
                                                    .Spacing(6)
                                                    .Children(
                                                        new FontIcon().Glyph("\uE81C").FontSize(14), // History icon
                                                        new TextBlock().Text("Aorist")
                                                    )
                                                )
                                                .IsChecked(() => vm.IsAoristSelected)
                                                .Command(() => vm.SelectAoristCommand)
                                                .Padding(12, 6)
                                                .Background(new SolidColorBrush(Color.FromArgb(255, 230, 255, 230))), // Light green
                                            new ToggleButton()
                                                .Content(new StackPanel()
                                                    .Orientation(Orientation.Horizontal)
                                                    .Spacing(6)
                                                    .Children(
                                                        new FontIcon().Glyph("\uE734").FontSize(14), // Star icon
                                                        new TextBlock().Text("Optative")
                                                    )
                                                )
                                                .IsChecked(() => vm.IsOptativeSelected)
                                                .Command(() => vm.SelectOptativeCommand)
                                                .Padding(12, 6)
                                                .Background(new SolidColorBrush(Color.FromArgb(255, 230, 255, 230))), // Light green
                                            new ToggleButton()
                                                .Content(new StackPanel()
                                                    .Orientation(Orientation.Horizontal)
                                                    .Spacing(6)
                                                    .Children(
                                                        new FontIcon().Glyph("\uE72C").FontSize(14), // Forward icon
                                                        new TextBlock().Text("Future")
                                                    )
                                                )
                                                .IsChecked(() => vm.IsFutureSelected)
                                                .Command(() => vm.SelectFutureCommand)
                                                .Padding(12, 6)
                                                .Background(new SolidColorBrush(Color.FromArgb(255, 230, 255, 230))) // Light green
                                        )
                                )
                        ),
                    
                    // Daily goal progress bar at bottom
                    new Border()
                        .Grid(row: 3)
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
                                                .Text(() => vm.DailyGoalText)
                                                .FontSize(14)
                                                .Foreground(Theme.Brushes.OnBackground.Medium)
                                                .Grid(column: 1)
                                        ),
                                    new ProgressBar()
                                        .Value(() => vm.DailyProgress)
                                        .Maximum(100)
                                        .Height(6)
                                        .CornerRadius(3)
                                )
                        )
                )
            )
        );
    }
}
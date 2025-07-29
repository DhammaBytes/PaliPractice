namespace PaliPractice.Presentation;

public sealed partial class DeclensionPracticePage : Page
{
    public DeclensionPracticePage()
    {
        this.DataContext<DeclensionPracticeViewModel>((page, vm) => page
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
                                .Text("Declension Practice")
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
                                                                                .Text("N")
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
                            // Number selection (Singular/Plural)
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
                            
                            // Gender selection
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
                                                new FontIcon().Glyph("\uE70D"), // Down triangle
                                                new TextBlock().Text("Masc")
                                            )
                                        )
                                        .IsChecked(() => vm.IsMasculineSelected)
                                        .Command(() => vm.SelectMasculineCommand)
                                        .Padding(16, 8)
                                        .Background(new SolidColorBrush(Color.FromArgb(255, 255, 230, 230))), // Light pink
                                    new ToggleButton()
                                        .Content(new StackPanel()
                                            .Orientation(Orientation.Horizontal)
                                            .Spacing(8)
                                            .Children(
                                                new FontIcon().Glyph("\uE11B"), // Circle icon
                                                new TextBlock().Text("Neutr")
                                            )
                                        )
                                        .IsChecked(() => vm.IsNeuterSelected)
                                        .Command(() => vm.SelectNeuterCommand)
                                        .Padding(16, 8)
                                        .Background(new SolidColorBrush(Color.FromArgb(255, 255, 230, 230))), // Light pink
                                    new ToggleButton()
                                        .Content(new StackPanel()
                                            .Orientation(Orientation.Horizontal)
                                            .Spacing(8)
                                            .Children(
                                                new FontIcon().Glyph("\uE70E"), // Up triangle
                                                new TextBlock().Text("Fem")
                                            )
                                        )
                                        .IsChecked(() => vm.IsFeminineSelected)
                                        .Command(() => vm.SelectFeminineCommand)
                                        .Padding(16, 8)
                                        .Background(new SolidColorBrush(Color.FromArgb(255, 255, 230, 230))) // Light pink
                                ),
                            
                            // Case selection (8 cases in 2 rows)
                            new StackPanel()
                                .Spacing(8)
                                .Children(
                                    // First row of cases
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
                                                        new FontIcon().Glyph("\uE7C3").FontSize(14), // Home icon
                                                        new TextBlock().Text("Nom")
                                                    )
                                                )
                                                .IsChecked(() => vm.IsNominativeSelected)
                                                .Command(() => vm.SelectNominativeCommand)
                                                .Padding(12, 6)
                                                .Background(new SolidColorBrush(Color.FromArgb(255, 230, 255, 230))), // Light green
                                            new ToggleButton()
                                                .Content(new StackPanel()
                                                    .Orientation(Orientation.Horizontal)
                                                    .Spacing(6)
                                                    .Children(
                                                        new FontIcon().Glyph("\uE896").FontSize(14), // Target icon
                                                        new TextBlock().Text("Acc")
                                                    )
                                                )
                                                .IsChecked(() => vm.IsAccusativeSelected)
                                                .Command(() => vm.SelectAccusativeCommand)
                                                .Padding(12, 6)
                                                .Background(new SolidColorBrush(Color.FromArgb(255, 230, 255, 230))), // Light green
                                            new ToggleButton()
                                                .Content(new StackPanel()
                                                    .Orientation(Orientation.Horizontal)
                                                    .Spacing(6)
                                                    .Children(
                                                        new FontIcon().Glyph("\uE90F").FontSize(14), // Tool icon
                                                        new TextBlock().Text("Instr")
                                                    )
                                                )
                                                .IsChecked(() => vm.IsInstrumentalSelected)
                                                .Command(() => vm.SelectInstrumentalCommand)
                                                .Padding(12, 6)
                                                .Background(new SolidColorBrush(Color.FromArgb(255, 230, 255, 230))), // Light green
                                            new ToggleButton()
                                                .Content(new StackPanel()
                                                    .Orientation(Orientation.Horizontal)
                                                    .Spacing(6)
                                                    .Children(
                                                        new FontIcon().Glyph("\uE8CA").FontSize(14), // Gift icon
                                                        new TextBlock().Text("Dat")
                                                    )
                                                )
                                                .IsChecked(() => vm.IsDativeSelected)
                                                .Command(() => vm.SelectDativeCommand)
                                                .Padding(12, 6)
                                                .Background(new SolidColorBrush(Color.FromArgb(255, 230, 255, 230))) // Light green
                                        ),
                                    // Second row of cases
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
                                                        new FontIcon().Glyph("\uE72A").FontSize(14), // Back arrow
                                                        new TextBlock().Text("Abl")
                                                    )
                                                )
                                                .IsChecked(() => vm.IsAblativeSelected)
                                                .Command(() => vm.SelectAblativeCommand)
                                                .Padding(12, 6)
                                                .Background(new SolidColorBrush(Color.FromArgb(255, 230, 255, 230))), // Light green
                                            new ToggleButton()
                                                .Content(new StackPanel()
                                                    .Orientation(Orientation.Horizontal)
                                                    .Spacing(6)
                                                    .Children(
                                                        new FontIcon().Glyph("\uE8F4").FontSize(14), // List icon
                                                        new TextBlock().Text("Gen")
                                                    )
                                                )
                                                .IsChecked(() => vm.IsGenitiveSelected)
                                                .Command(() => vm.SelectGenitiveCommand)
                                                .Padding(12, 6)
                                                .Background(new SolidColorBrush(Color.FromArgb(255, 230, 255, 230))), // Light green
                                            new ToggleButton()
                                                .Content(new StackPanel()
                                                    .Orientation(Orientation.Horizontal)
                                                    .Spacing(6)
                                                    .Children(
                                                        new FontIcon().Glyph("\uE707").FontSize(14), // Pin icon
                                                        new TextBlock().Text("Loc")
                                                    )
                                                )
                                                .IsChecked(() => vm.IsLocativeSelected)
                                                .Command(() => vm.SelectLocativeCommand)
                                                .Padding(12, 6)
                                                .Background(new SolidColorBrush(Color.FromArgb(255, 230, 255, 230))), // Light green
                                            new ToggleButton()
                                                .Content(new StackPanel()
                                                    .Orientation(Orientation.Horizontal)
                                                    .Spacing(6)
                                                    .Children(
                                                        new FontIcon().Glyph("\uE134").FontSize(14), // Message icon
                                                        new TextBlock().Text("Voc")
                                                    )
                                                )
                                                .IsChecked(() => vm.IsVocativeSelected)
                                                .Command(() => vm.SelectVocativeCommand)
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
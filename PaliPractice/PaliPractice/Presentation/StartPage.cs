namespace PaliPractice.Presentation;

public sealed partial class StartPage : Page
{
    public StartPage()
    {
        this.DataContext<StartViewModel>((page, vm) => page
            .NavigationCacheMode(NavigationCacheMode.Required)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(new Grid()
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
                .RowDefinitions("Auto,*")
                .Children(
                    new NavigationBar().Content(() => vm.Title),
                    
                    // Main content area
                    new Grid()
                        .Grid(row: 1)
                        .Margin(20)
                        .Children(
                            new StackPanel()
                                .VerticalAlignment(VerticalAlignment.Center)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .Spacing(32)
                                .MaxWidth(400)
                                .Children(
                                    // App title
                                    new TextBlock()
                                        .Text("Pali Practice")
                                        .FontSize(36)
                                        .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                        .TextAlignment(TextAlignment.Center)
                                        .Foreground(ThemeResource.Get<Brush>("PrimaryBrush")),
                                    
                                    // Subtitle
                                    new TextBlock()
                                        .Text("Choose your practice mode")
                                        .FontSize(18)
                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                        .TextAlignment(TextAlignment.Center)
                                        .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                                        .Margin(0, 0, 0, 16),
                                    
                                    // Practice buttons
                                    new StackPanel()
                                        .Spacing(16)
                                        .Children(
                                            // Declension Practice Button
                                            new Button()
                                                .Content(new StackPanel()
                                                    .Orientation(Orientation.Horizontal)
                                                    .Spacing(12)
                                                    .Children(
                                                        new FontIcon()
                                                            .Glyph("\uE8D4") // Book icon
                                                            .FontSize(24),
                                                        new StackPanel()
                                                            .Children(
                                                                new TextBlock()
                                                                    .Text("Declension Practice")
                                                                    .FontSize(18)
                                                                    .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold),
                                                                new TextBlock()
                                                                    .Text("Practice noun cases and forms")
                                                                    .FontSize(14)
                                                                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                                                            )
                                                    )
                                                )
                                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                                .HorizontalContentAlignment(HorizontalAlignment.Left)
                                                .Padding(20, 16)
                                                .Command(() => vm.GoToDeclensionCommand)
                                                .Background(ThemeResource.Get<Brush>("PrimaryBrush")),
                                            
                                            // Conjugation Practice Button
                                            new Button()
                                                .Content(new StackPanel()
                                                    .Orientation(Orientation.Horizontal)
                                                    .Spacing(12)
                                                    .Children(
                                                        new FontIcon()
                                                            .Glyph("\uE8F4") // List icon
                                                            .FontSize(24),
                                                        new StackPanel()
                                                            .Children(
                                                                new TextBlock()
                                                                    .Text("Conjugation Practice")
                                                                    .FontSize(18)
                                                                    .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold),
                                                                new TextBlock()
                                                                    .Text("Practice verb tenses and forms")
                                                                    .FontSize(14)
                                                                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                                                            )
                                                    )
                                                )
                                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                                .HorizontalContentAlignment(HorizontalAlignment.Left)
                                                .Padding(20, 16)
                                                .Command(() => vm.GoToConjugationCommand)
                                                .Background(ThemeResource.Get<Brush>("PrimaryBrush")),
                                            
                                            // Settings Button (for future use)
                                            new Button()
                                                .Content(new StackPanel()
                                                    .Orientation(Orientation.Horizontal)
                                                    .Spacing(12)
                                                    .Children(
                                                        new FontIcon()
                                                            .Glyph("\uE713") // Settings icon
                                                            .FontSize(24),
                                                        new TextBlock()
                                                            .Text("Settings")
                                                            .FontSize(18)
                                                            .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                                                    )
                                                )
                                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                                .HorizontalContentAlignment(HorizontalAlignment.Left)
                                                .Padding(20, 16)
                                                .IsEnabled(false) // Disabled for now
                                                .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
                                        )
                                )
                        )
                )
            )
        );
    }
}
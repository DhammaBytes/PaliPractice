namespace PaliPractice.Presentation.Main;

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
                                    // App icon
                                    new Image()
                                        .Source("ms-appx:///Assets/Svg/icon.svg")
                                        .Width(120)
                                        .Height(120)
                                        .Stretch(Stretch.Uniform)
                                        .HorizontalAlignment(HorizontalAlignment.Center),

                                    // App title
                                    new TextBlock()
                                        .Text("Pali Practice")
                                        .FontSize(36)
                                        .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                        .TextAlignment(TextAlignment.Center)
                                        .Foreground(ThemeResource.Get<Brush>("PrimaryBrush")),
                                    
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
                                                                    .Text("Nouns and cases")
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
                                                                    .Text("Verbs and tenses")
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
                                            
                                            // Settings Button
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
                                                .Command(() => vm.GoToSettingsCommand)
                                                .Background(ThemeResource.Get<Brush>("SurfaceBrush")),

                                            // Help and About buttons (50/50 split)
                                            new Grid()
                                                .ColumnDefinitions("*,12,*")
                                                .Children(
                                                    // Help Button
                                                    new Button()
                                                        .Grid(column: 0)
                                                        .Content(new StackPanel()
                                                            .Orientation(Orientation.Horizontal)
                                                            .Spacing(8)
                                                            .Children(
                                                                new FontIcon()
                                                                    .Glyph("\uE897") // Help icon
                                                                    .FontSize(20),
                                                                new TextBlock()
                                                                    .Text("Help")
                                                                    .FontSize(16)
                                                                    .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                                                            )
                                                        )
                                                        .HorizontalAlignment(HorizontalAlignment.Stretch)
                                                        .HorizontalContentAlignment(HorizontalAlignment.Center)
                                                        .Padding(16, 12)
                                                        .Command(() => vm.GoToHelpCommand)
                                                        .Background(ThemeResource.Get<Brush>("SurfaceBrush")),

                                                    // About Button
                                                    new Button()
                                                        .Grid(column: 2)
                                                        .Content(new StackPanel()
                                                            .Orientation(Orientation.Horizontal)
                                                            .Spacing(8)
                                                            .Children(
                                                                new FontIcon()
                                                                    .Glyph("\uE946") // Info icon
                                                                    .FontSize(20),
                                                                new TextBlock()
                                                                    .Text("About")
                                                                    .FontSize(16)
                                                                    .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                                                            )
                                                        )
                                                        .HorizontalAlignment(HorizontalAlignment.Stretch)
                                                        .HorizontalContentAlignment(HorizontalAlignment.Center)
                                                        .Padding(16, 12)
                                                        .Command(() => vm.GoToAboutCommand)
                                                        .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
                                                )
                                        )
                                )
                        )
                )
            )
        );
    }
}

using PaliPractice.Presentation.Main.ViewModels;

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
                                        .Text("Pāli Practice")
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
                                            new SquircleButton()
                                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                                .HorizontalContentAlignment(HorizontalAlignment.Left)
                                                .Fill(ThemeResource.Get<Brush>("PrimaryBrush"))
                                                .RadiusMode(SquircleRadiusMode.ButtonSmall)
                                                .Padding(20, 16)
                                                .Command(() => vm.GoToDeclensionCommand)
                                                .Child(new StackPanel()
                                                    .Orientation(Orientation.Horizontal)
                                                    .Spacing(12)
                                                    .Children(
                                                        new FontIcon()
                                                            .Glyph("\uE8D4")
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
                                                    )),

                                            // Conjugation Practice Button
                                            new SquircleButton()
                                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                                .HorizontalContentAlignment(HorizontalAlignment.Left)
                                                .Fill(ThemeResource.Get<Brush>("PrimaryBrush"))
                                                .RadiusMode(SquircleRadiusMode.ButtonSmall)
                                                .Padding(20, 16)
                                                .Command(() => vm.GoToConjugationCommand)
                                                .Child(new StackPanel()
                                                    .Orientation(Orientation.Horizontal)
                                                    .Spacing(12)
                                                    .Children(
                                                        new FontIcon()
                                                            .Glyph("\uE8F4")
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
                                                    )),

                                            // Settings Button
                                            new SquircleButton()
                                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                                .HorizontalContentAlignment(HorizontalAlignment.Left)
                                                .Fill(ThemeResource.Get<Brush>("SurfaceBrush"))
                                                .RadiusMode(SquircleRadiusMode.ButtonSmall)
                                                .Padding(20, 16)
                                                .Command(() => vm.GoToSettingsCommand)
                                                .Child(new StackPanel()
                                                    .Orientation(Orientation.Horizontal)
                                                    .Spacing(12)
                                                    .Children(
                                                        new FontIcon()
                                                            .Glyph("\uE713")
                                                            .FontSize(24),
                                                        new TextBlock()
                                                            .Text("Settings")
                                                            .FontSize(18)
                                                            .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                                                    )),

                                            // Help and About buttons (50/50 split)
                                            new Grid()
                                                .ColumnDefinitions("*,12,*")
                                                .Children(
                                                    // Help Button
                                                    new SquircleButton()
                                                        .Grid(column: 0)
                                                        .HorizontalAlignment(HorizontalAlignment.Stretch)
                                                        .Fill(ThemeResource.Get<Brush>("SurfaceBrush"))
                                                        .RadiusMode(SquircleRadiusMode.ButtonSmall)
                                                        .Padding(16, 12)
                                                        .Command(() => vm.GoToHelpCommand)
                                                        .Child(new StackPanel()
                                                            .Orientation(Orientation.Horizontal)
                                                            .Spacing(8)
                                                            .Children(
                                                                new FontIcon()
                                                                    .Glyph("\uE897")
                                                                    .FontSize(20),
                                                                new TextBlock()
                                                                    .Text("Help")
                                                                    .FontSize(16)
                                                                    .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                                                            )),

                                                    // About Button
                                                    new SquircleButton()
                                                        .Grid(column: 2)
                                                        .HorizontalAlignment(HorizontalAlignment.Stretch)
                                                        .Fill(ThemeResource.Get<Brush>("SurfaceBrush"))
                                                        .RadiusMode(SquircleRadiusMode.ButtonSmall)
                                                        .Padding(16, 12)
                                                        .Command(() => vm.GoToAboutCommand)
                                                        .Child(new StackPanel()
                                                            .Orientation(Orientation.Horizontal)
                                                            .Spacing(8)
                                                            .Children(
                                                                new FontIcon()
                                                                    .Glyph("\uE946")
                                                                    .FontSize(20),
                                                                new TextBlock()
                                                                    .Text("About")
                                                                    .FontSize(16)
                                                                    .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                                                            ))
                                                )
                                        )
                                )
                        )
                )
            )
        );
    }
}

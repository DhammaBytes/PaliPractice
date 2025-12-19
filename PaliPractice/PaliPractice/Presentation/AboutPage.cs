using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Controls;

namespace PaliPractice.Presentation;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        this.DataContext<AboutViewModel>((page, _) => page
            .NavigationCacheMode(NavigationCacheMode.Required)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(new Grid()
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
                .RowDefinitions("Auto,*")
                .Children(
                    // Row 0: Title bar
                    AppTitleBar.Build<AboutViewModel>("About", vm => vm.GoBackCommand),

                    // Row 1: Content
                    new ScrollViewer()
                        .Grid(row: 1)
                        .Content(
                            new StackPanel()
                                .Padding(20)
                                .Spacing(24)
                                .MaxWidth(600)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .VerticalAlignment(VerticalAlignment.Center)
                                .Children(
                                    // App icon placeholder
                                    new FontIcon()
                                        .Glyph("\uE82D")
                                        .FontSize(80)
                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                        .Foreground(ThemeResource.Get<Brush>("PrimaryBrush")),

                                    // App name
                                    new TextBlock()
                                        .Text<AboutViewModel>(vm => vm.AppName)
                                        .FontSize(32)
                                        .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                        .Foreground(ThemeResource.Get<Brush>("PrimaryBrush")),

                                    // Version
                                    new TextBlock()
                                        .Text<AboutViewModel>(vm => vm.Version)
                                        .FontSize(16)
                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                        .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush")),

                                    // Description
                                    new TextBlock()
                                        .Text<AboutViewModel>(vm => vm.Description)
                                        .FontSize(16)
                                        .TextWrapping(TextWrapping.Wrap)
                                        .TextAlignment(TextAlignment.Center)
                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                        .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush")),

                                    // Data source
                                    new Border()
                                        .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
                                        .CornerRadius(8)
                                        .Padding(16)
                                        .Child(
                                            new TextBlock()
                                                .Text<AboutViewModel>(vm => vm.DataSource)
                                                .FontSize(14)
                                                .TextWrapping(TextWrapping.Wrap)
                                                .TextAlignment(TextAlignment.Center)
                                                .HorizontalAlignment(HorizontalAlignment.Center)
                                                .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
                                        ),

                                    // Copyright
                                    new TextBlock()
                                        .Text("© 2024")
                                        .FontSize(14)
                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                        .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                                )
                        )
                )
            )
        );
    }
}

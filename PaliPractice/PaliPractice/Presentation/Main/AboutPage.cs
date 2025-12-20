using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;

namespace PaliPractice.Presentation.Main;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        this.DataContext<AboutViewModel>((page, _) => page
            .NavigationCacheMode<AboutPage>(NavigationCacheMode.Required)
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
                                    // App icon
                                    new Image()
                                        .Source("ms-appx:///Assets/Svg/icon.svg")
                                        .Width(100)
                                        .Height(100)
                                        .Stretch(Stretch.Uniform)
                                        .HorizontalAlignment(HorizontalAlignment.Center),

                                    // App name with version
                                    new TextBlock()
                                        .Text<AboutViewModel>(vm => vm.AppName)
                                        .FontSize(32)
                                        .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                        .Foreground(ThemeResource.Get<Brush>("PrimaryBrush")),

                                    // Description
                                    new TextBlock()
                                        .Text<AboutViewModel>(vm => vm.Description)
                                        .FontSize(16)
                                        .TextWrapping(TextWrapping.Wrap)
                                        .TextAlignment(TextAlignment.Center)
                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                        .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush")),

                                    // License section
                                    new Border()
                                        .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
                                        .CornerRadius(8)
                                        .Padding(16)
                                        .Child(
                                            new StackPanel()
                                                .Spacing(8)
                                                .Children(
                                                    new TextBlock()
                                                        .Text<AboutViewModel>(vm => vm.LicenseTitle)
                                                        .FontSize(16)
                                                        .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                                        .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush")),
                                                    new TextBlock()
                                                        .Text<AboutViewModel>(vm => vm.LicenseText)
                                                        .FontSize(14)
                                                        .TextWrapping(TextWrapping.Wrap)
                                                        .TextAlignment(TextAlignment.Center)
                                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                                        .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
                                                )
                                        ),

                                    // Blessing
                                    new TextBlock()
                                        .Text<AboutViewModel>(vm => vm.Blessing)
                                        .FontSize(16)
                                        .FontStyle(Windows.UI.Text.FontStyle.Italic)
                                        .TextWrapping(TextWrapping.Wrap)
                                        .TextAlignment(TextAlignment.Center)
                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                        .Foreground(ThemeResource.Get<Brush>("PrimaryBrush"))
                                )
                        )
                )
            )
        );
    }
}

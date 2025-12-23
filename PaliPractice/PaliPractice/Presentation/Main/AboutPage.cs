using System.Linq.Expressions;
using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Main.ViewModels;
using static PaliPractice.Presentation.Common.TextHelpers;

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
                                    PaliText()
                                        .Text<AboutViewModel>(vm => vm.AppName)
                                        .FontSize(32)
                                        .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                        .Foreground(ThemeResource.Get<Brush>("PrimaryBrush")),

                                    // Description section (title-less)
                                    BuildSection<AboutViewModel>(null, vm => vm.Description),

                                    // App Icon section
                                    BuildSection<AboutViewModel>(vm => vm.IconTitle, vm => vm.IconDescription),

                                    // License section
                                    BuildSection<AboutViewModel>(vm => vm.LicenseTitle, vm => vm.LicenseText),

                                    // Fonts section
                                    BuildSection<AboutViewModel>(vm => vm.FontsTitle, vm => vm.FontsText),

                                    // Blessing
                                    PaliText()
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

    /// <summary>
    /// Builds a section with optional title and content text.
    /// </summary>
    static Border BuildSection<TVM>(
        Expression<Func<TVM, string>>? titlePath,
        Expression<Func<TVM, string>> contentPath)
    {
        var children = new List<UIElement>();

        if (titlePath is not null)
        {
            children.Add(
                RegularText()
                    .Text(titlePath)
                    .FontSize(16)
                    .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
            );
        }

        children.Add(
            RegularText()
                .Text(contentPath)
                .FontSize(14)
                .TextWrapping(TextWrapping.Wrap)
                .TextAlignment(TextAlignment.Center)
                .HorizontalAlignment(HorizontalAlignment.Center)
                .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
        );

        return new Border()
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .CornerRadius(8)
            .Padding(16)
            .Child(
                new StackPanel()
                    .Spacing(8)
                    .Children(children.ToArray())
            );
    }
}

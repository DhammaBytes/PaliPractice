using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Main.ViewModels;
using static PaliPractice.Presentation.Common.TextHelpers;
using static PaliPractice.Presentation.Common.RichTextHelper;

namespace PaliPractice.Presentation.Main;

public sealed partial class AboutPage : Page
{
    // Styling constants
    const double BodyFontSize = 15;
    const double TitleFontSize = 18;
    const double SectionPadding = 24;
    const double ContentPadding = 20;

    public AboutPage()
    {
        this.DataContext<AboutViewModel>((page, vm) => page
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
                                .MaxWidth(LayoutConstants.ContentMaxWidth + ContentPadding * 2)
                                .Padding(ContentPadding)
                                .Spacing(24)
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

                                    // Description section (title-less, with clickable links)
                                    BuildRichSection(null, AboutViewModel.Description),

                                    // Contact button
                                    new Button()
                                        .Content(
                                            new StackPanel()
                                                .Orientation(Orientation.Horizontal)
                                                .Spacing(8)
                                                .Children(
                                                    new FontIcon()
                                                        .Glyph("\uE715") // Message
                                                        .FontSize(16),
                                                    RegularText()
                                                        .Text("Contact DhammaBytes")
                                                        .FontSize(14)
                                                ))
                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                        .Command(() => vm.ContactUsCommand),

                                    // App Icon section (with clickable links)
                                    BuildRichSection(AboutViewModel.IconTitle, AboutViewModel.IconDescription),

                                    // License section (with clickable links)
                                    BuildRichSection(AboutViewModel.LicenseTitle, AboutViewModel.LicenseText),

                                    // Fonts section (plain text)
                                    BuildRichSection(AboutViewModel.FontsTitle, AboutViewModel.FontsText),

                                    // Libraries section
                                    BuildRichSection(AboutViewModel.LibrariesTitle, AboutViewModel.LibrariesText),

                                    // Blessing
                                    new StackPanel()
                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                        .Children(
                                            PaliText()
                                                .Text(AboutViewModel.BlessingPali)
                                                .FontSize(TitleFontSize)
                                                .FontStyle(Windows.UI.Text.FontStyle.Italic)
                                                .HorizontalAlignment(HorizontalAlignment.Center)
                                                .Foreground(ThemeResource.Get<Brush>("PrimaryBrush")),
                                            RegularText()
                                                .Text(AboutViewModel.BlessingEnglish)
                                                .FontSize(TitleFontSize - 1)
                                                .HorizontalAlignment(HorizontalAlignment.Center)
                                                .Foreground(ThemeResource.Get<Brush>("PrimaryBrush"))
                                        )
                                )
                        )
                )
            )
        );
    }

    /// <summary>
    /// Builds a section with optional title and rich content with clickable links.
    /// Titles are centered, content is left-aligned.
    /// </summary>
    static Border BuildRichSection(string? title, string content)
    {
        var children = new List<UIElement>();

        if (title is not null)
        {
            children.Add(
                RegularText()
                    .Text(title)
                    .FontSize(TitleFontSize)
                    .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
            );
        }

        children.Add(CreateRichContent(content, fontSize: BodyFontSize));

        return new Border()
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .CornerRadius(8)
            .Padding(SectionPadding)
            .Child(
                new StackPanel()
                    .Spacing(12)
                    .Children(children.ToArray())
            );
    }
}

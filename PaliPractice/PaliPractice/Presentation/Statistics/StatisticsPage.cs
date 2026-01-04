using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Statistics.ViewModels;
using static PaliPractice.Presentation.Common.TextHelpers;

namespace PaliPractice.Presentation.Statistics;

public sealed partial class StatisticsPage : Page
{
    public StatisticsPage()
    {
        this.DataContext<StatisticsViewModel>((page, vm) => page
            .NavigationCacheMode(NavigationCacheMode.Required)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(new Grid()
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
                .RowDefinitions("Auto,*")
                .Children(
                    // Row 0: Title bar
                    AppTitleBar.Build<StatisticsViewModel>("Statistics", v => v.GoBackCommand),

                    // Row 1: Content
                    new Grid()
                        .Grid(row: 1)
                        .Children(
                            new StackPanel()
                                .VerticalAlignment(VerticalAlignment.Center)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .Spacing(16)
                                .Children(
                                    new FontIcon()
                                        .Glyph("\uE9D9")
                                        .FontSize(48)
                                        .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                                        .HorizontalAlignment(HorizontalAlignment.Center),
                                    RegularText()
                                        .Text("Coming soon")
                                        .FontSize(18)
                                        .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                )
                        )
                )
            )
        );
    }
}

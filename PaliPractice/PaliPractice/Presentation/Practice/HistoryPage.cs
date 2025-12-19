using PaliPractice.Presentation.Common;

namespace PaliPractice.Presentation.Practice;

public sealed partial class HistoryPage : Page
{
    public HistoryPage()
    {
        this.DataContext<HistoryViewModel>((page, vm) => page
            .NavigationCacheMode(NavigationCacheMode.Disabled) // Don't cache - different data each time
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(new Grid()
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
                .RowDefinitions("Auto,*")
                .Children(
                    // Row 0: Title bar
                    AppTitleBar.Build<HistoryViewModel>("History", v => v.GoBackCommand),

                    // Row 1: Content
                    new ListView()
                        .MaxWidth(LayoutConstants.ContentMaxWidth)
                        .Grid(row: 1)
                        .ItemsSource(() => vm.Records)
                        .ItemTemplate<HistoryRecord>(record =>
                            new Grid()
                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                .ColumnDefinitions("*,Auto,60")
                                .Padding(16, 12)
                                .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
                                .Children(
                                    // Column 0: Form text
                                    new TextBlock()
                                        .Text(() => record.Form)
                                        .FontSize(16)
                                        .VerticalAlignment(VerticalAlignment.Center)
                                        .Grid(column: 0),

                                    // Column 1: Arrow icon (up/down based on progress)
                                    new FontIcon()
                                        .Glyph(() => record.IsImproved, improved => improved ? "\uE74A" : "\uE74B") // Up or Down arrow
                                        .FontSize(16)
                                        .Margin(12, 0)
                                        .VerticalAlignment(VerticalAlignment.Center)
                                        .Foreground(() => record.IsImproved, improved =>
                                            improved
                                                ? (Brush)Application.Current.Resources["SystemFillColorSuccessBrush"]
                                                : (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"])
                                        .Grid(column: 1),

                                    // Column 2: Progress bar
                                    new ProgressBar()
                                        .Width(60)
                                        .Minimum(0)
                                        .Maximum(100)
                                        .Value(() => record.NewValue)
                                        .VerticalAlignment(VerticalAlignment.Center)
                                        .Grid(column: 2)
                                )
                        )
                )
            )
        );
    }
}

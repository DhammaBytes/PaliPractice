using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Practice.ViewModels;
using PaliPractice.Services.UserData.Entities;
using PaliPractice.Themes;
using static PaliPractice.Presentation.Common.Text.TextHelpers;

namespace PaliPractice.Presentation.Practice;

public sealed partial class HistoryPage : Page
{
    readonly StackPanel _sectionsContainer = new StackPanel()
        .HorizontalAlignment(HorizontalAlignment.Stretch)
        .MaxWidth(LayoutConstants.ContentMaxWidth);

    readonly TextBlock _emptyState;

    public HistoryPage()
    {
        _emptyState = RegularText()
            .Text("No practice history yet")
            .FontSize(16)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
            .HorizontalAlignment(HorizontalAlignment.Center)
            .VerticalAlignment(VerticalAlignment.Center)
            .Visibility(Visibility.Collapsed);

        this.DataContext<HistoryViewModel>((page, vm) => page
            .NavigationCacheMode(NavigationCacheMode.Disabled) // Don't cache - different data each time
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(PageFadeIn.Wrap(page,
                new Grid()
                    .SafeArea(SafeArea.InsetMask.Top) // Only top safe area - content extends to physical bottom
                    .RowDefinitions("Auto,*")
                    .Children(
                        // Row 0: Title bar
                        AppTitleBar.Build<HistoryViewModel>("History", v => v.GoBackCommand),

                        // Row 1: Content - scrollable sections
                        new ScrollViewer()
                            .Grid(row: 1)
                            .Content(_sectionsContainer),

                        // Empty state
                        _emptyState.Grid(row: 1)
                    )
            ))
        );

        // Build sections when DataContext is set
        DataContextChanged += OnDataContextChanged;
    }

    void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs e)
    {
        if (e.NewValue is not HistoryViewModel vm) return;

        _sectionsContainer.Children.Clear();

        if (!vm.HasHistory)
        {
            _emptyState.Visibility = Visibility.Visible;
            return;
        }

        _emptyState.Visibility = Visibility.Collapsed;

        foreach (var section in vm.Sections)
        {
            _sectionsContainer.Children.Add(BuildSection(section));
        }
    }

    /// <summary>
    /// Builds a section with header and history items (styled like settings sections).
    /// </summary>
    static StackPanel BuildSection(HistorySection section)
    {
        var sectionPanel = new StackPanel()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Spacing(1)
            .Margin(0, 0, 0, 16);

        // Header (same style as settings sections)
        sectionPanel.Children.Add(
            RegularText()
                .Text(section.Header)
                .FontSize(14)
                .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                .Margin(16, 8, 16, 4)
        );

        // Items container with surface background
        var itemsPanel = new StackPanel()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Spacing(1)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"));

        foreach (var record in section.Records)
        {
            itemsPanel.Children.Add(BuildHistoryItem(record));
        }

        sectionPanel.Children.Add(
            new Border()
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
                .Child(itemsPanel)
        );

        return sectionPanel;
    }

    /// <summary>
    /// Builds a single history item row.
    /// </summary>
    static Grid BuildHistoryItem(IPracticeHistory record)
    {
        var fillBorder = BuildProgressFill(record.NewLevelPercent);

        return new Grid()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .ColumnDefinitions("*,Auto,60")
            .Padding(16, 12)
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .Children(
                // Column 0: Form text (Pāli word)
                PaliText()
                    .Text(record.FormText)
                    .FontSize(16)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Grid(column: 0),

                // Column 1: Arrow icon (up/down based on progress)
                new BitmapIcon()
                    .UriSource(new Uri(NavigationIcons.ArrowBack))
                    .ShowAsMonochrome(true)
                    .Width(13)
                    .Height(13)
                    .Margin(12, 0)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Foreground(record.IsImproved
                        ? ThemeResource.Get<Brush>("AccentGreenBrush")
                        : ThemeResource.Get<Brush>("AccentRedBrush"))
                    .RenderTransformOrigin(new Windows.Foundation.Point(0.5, 0.5))
                    .RenderTransform(new RotateTransform { Angle = record.IsImproved ? 90 : -90 })
                    .Grid(column: 1),

                // Column 2: Progress bar (0-100 scale)
                BuildProgressTrack(fillBorder)
                    .Grid(column: 2)
            );
    }

    /// <summary>
    /// Builds the progress bar fill border with width set based on progress.
    /// </summary>
    static Border BuildProgressFill(double progress)
    {
        const double barWidth = 60;
        const double barHeight = 6;
        var cornerRadius = new CornerRadius(barHeight / 2);
        var fillWidth = Math.Max(barHeight, barWidth * (progress / 100.0));

        return new Border()
            .Background(ThemeResource.Get<Brush>("AccentBrush"))
            .Width(fillWidth)
            .Height(barHeight)
            .CornerRadius(cornerRadius)
            .HorizontalAlignment(HorizontalAlignment.Left)
            .MinWidth(barHeight);
    }

    /// <summary>
    /// Builds the progress bar track with the fill inside.
    /// </summary>
    static Border BuildProgressTrack(Border fillBorder)
    {
        const double barWidth = 60;
        const double barHeight = 6;
        var cornerRadius = new CornerRadius(barHeight / 2);

        return new Border()
            .Background(ThemeResource.Get<Brush>("BackgroundVariantBrush"))
            .Width(barWidth)
            .Height(barHeight)
            .CornerRadius(cornerRadius)
            .VerticalAlignment(VerticalAlignment.Center)
            .Child(fillBorder);
    }
}

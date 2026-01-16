using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Statistics.ViewModels;
using PaliPractice.Services.UserData.Entities;
using PaliPractice.Services.UserData.Statistics;
using Microsoft.UI.Xaml.Media;
using static PaliPractice.Presentation.Common.TextHelpers;

namespace PaliPractice.Presentation.Statistics;

public sealed partial class StatisticsPage : Page
{
    // Dynamic content containers
    readonly Grid _nounDistributionBar = new();
    readonly StackPanel _nounStrongestPanel = new();
    readonly StackPanel _nounWeakestPanel = new();
    readonly Grid _verbDistributionBar = new();
    readonly StackPanel _verbStrongestPanel = new();
    readonly StackPanel _verbWeakestPanel = new();

    public StatisticsPage()
    {
        InitializeDistributionBar(_nounDistributionBar);
        InitializeDistributionBar(_verbDistributionBar);
        _nounStrongestPanel.Spacing(6);
        _nounWeakestPanel.Spacing(6);
        _verbStrongestPanel.Spacing(6);
        _verbWeakestPanel.Spacing(6);

        this.DataContext<StatisticsViewModel>((page, vm) => page
            .NavigationCacheMode(NavigationCacheMode.Disabled)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(new Grid()
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
                .RowDefinitions("Auto,*")
                .Children(
                    // Row 0: Title bar
                    AppTitleBar.Build<StatisticsViewModel>("Statistics", v => v.GoBackCommand),

                    // Row 1: Content
                    new ScrollViewer()
                        .Grid(row: 1)
                        .VerticalScrollBarVisibility(ScrollBarVisibility.Auto)
                        .HorizontalContentAlignment(HorizontalAlignment.Center)
                        .Content(
                            new StackPanel()
                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                .MaxWidth(LayoutConstants.ContentMaxWidth)
                                .Padding(16)
                                .Spacing(24)
                                .Children(
                                    // === General Section ===
                                    BuildGeneralSection(vm),

                                    // === Nouns Section ===
                                    BuildPracticeTypeSection("Nouns", "\uE8F1", vm, isNouns: true),

                                    // === Verbs Section ===
                                    BuildPracticeTypeSection("Verbs", "\uE943", vm, isNouns: false)
                                )
                        )
                )
            )
        );

        Loaded += OnLoaded;
    }

    void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not StatisticsViewModel vm) return;

        // Subscribe to property changes to update dynamic content when data loads
        vm.PropertyChanged += OnViewModelPropertyChanged;

        // If already loaded, populate now
        if (!vm.IsLoading)
            PopulateDynamicContent(vm);
    }

    void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StatisticsViewModel.IsLoading) &&
            DataContext is StatisticsViewModel vm && !vm.IsLoading)
        {
            PopulateDynamicContent(vm);
        }
    }

    void PopulateDynamicContent(StatisticsViewModel vm)
    {
        PopulateDistributionBar(_nounDistributionBar, vm.NounStats.Distribution);
        PopulateCombos(_nounStrongestPanel, _nounWeakestPanel, vm.NounStats);
        PopulateDistributionBar(_verbDistributionBar, vm.VerbStats.Distribution);
        PopulateCombos(_verbStrongestPanel, _verbWeakestPanel, vm.VerbStats);
    }

    static void InitializeDistributionBar(Grid bar)
    {
        bar.Height(16);
        bar.HorizontalAlignment(HorizontalAlignment.Stretch);
        bar.CornerRadius(8);
        bar.RowDefinitions("*");
        bar.Background(ThemeResource.Get<Brush>("NeutralGrayBrush"));
    }

    FrameworkElement BuildGeneralSection(StatisticsViewModel vm)
    {
        var calendarRepeater = new ItemsRepeater()
            .ItemTemplate(CreateCalendarItemTemplate())
            .Layout(new UniformGridLayout()
                .Orientation(Orientation.Horizontal)
                .ItemsStretch(UniformGridLayoutItemsStretch.None)
                .MinColumnSpacing(4)
                .MinRowSpacing(4)
                .MinItemWidth(20)
                .MinItemHeight(20))
            .ItemsSource(x => x.Binding(() => vm.CalendarData));

        return new StackPanel()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Spacing(16)
            .Children(
                BuildSectionHeader("Overview", "\uE9D9"),
                new SquircleBorder()
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Fill(ThemeResource.Get<Brush>("SurfaceBrush"))
                    .Child(
                        new StackPanel()
                            .Padding(16)
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .Spacing(20)
                            .Children(
                                // Streak row
                                new Grid()
                                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                                    .ColumnDefinitions("*,*")
                                    .Children(
                                        BuildStreakCard("\uE8E1", "Practice Streak",
                                            tb => tb.Text(() => vm.General.CurrentPracticeStreak, v => $"{v} {(v == 1 ? "day" : "days")}"), 0),
                                        BuildStreakCard("\uE735", "Total",
                                            tb => tb.Text(() => vm.General.TotalPracticeDays, v => $"{v} {(v == 1 ? "day" : "days")}"), 1)
                                    ),
                                // Today's progress
                                new StackPanel()
                                    .Spacing(8)
                                    .Children(
                                        RegularText().Text("Today").FontSize(13)
                                            .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush")),
                                        new Grid()
                                            .ColumnDefinitions("*,*")
                                            .Children(
                                                BuildTodayItem("Nouns",
                                                    tb => tb.Text(() => vm.General.TodayDeclensions, v => $"{v}"),
                                                    cb => cb.IsChecked(() => vm.General.NounGoalMet), 0),
                                                BuildTodayItem("Verbs",
                                                    tb => tb.Text(() => vm.General.TodayConjugations, v => $"{v}"),
                                                    cb => cb.IsChecked(() => vm.General.VerbGoalMet), 1)
                                            )
                                    ),
                                // Calendar heatmap
                                new StackPanel()
                                    .Spacing(8)
                                    .Children(
                                        new Grid()
                                            .ColumnDefinitions("*,Auto")
                                            .Children(
                                                RegularText().Text("Last 30 days").FontSize(12)
                                                    .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush")),
                                                BuildCalendarLegend().Grid(column: 1)
                                            ),
                                        calendarRepeater
                                    )
                            )
                    )
            );
    }

    FrameworkElement BuildPracticeTypeSection(string title, string icon, StatisticsViewModel vm, bool isNouns)
    {
        var distributionBar = isNouns ? _nounDistributionBar : _verbDistributionBar;
        var strongestPanel = isNouns ? _nounStrongestPanel : _verbStrongestPanel;
        var weakestPanel = isNouns ? _nounWeakestPanel : _verbWeakestPanel;

        return new StackPanel()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Spacing(16)
            .Children(
                BuildSectionHeader(title, icon),
                new SquircleBorder()
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Fill(ThemeResource.Get<Brush>("SurfaceBrush"))
                    .Child(
                        new StackPanel()
                            .Padding(16)
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .Spacing(20)
                            .Children(
                                // Stats row
                                new Grid()
                                    .ColumnDefinitions("*,*,*")
                                    .Children(
                                        BuildStatItem("All-time",
                                            isNouns
                                                ? (tb => tb.Text(() => vm.NounStats.TotalPracticed, v => $"{v}"))
                                                : (tb => tb.Text(() => vm.VerbStats.TotalPracticed, v => $"{v}")), 0),
                                        BuildStatItem("Last 7 days",
                                            isNouns
                                                ? (tb => tb.Text(() => vm.NounStats.PeriodStats.Last7Days, v => $"{v}"))
                                                : (tb => tb.Text(() => vm.VerbStats.PeriodStats.Last7Days, v => $"{v}")), 1),
                                        BuildStatItem("To repeat",
                                            isNouns
                                                ? (tb => tb.Text(() => vm.NounStats.DueForReview, v => $"{v}"))
                                                : (tb => tb.Text(() => vm.VerbStats.DueForReview, v => $"{v}")), 2)
                                    ),
                                // SRS distribution
                                new StackPanel()
                                    .Spacing(12)
                                    .Children(
                                        RegularText().Text("Mastery Distribution").FontSize(13)
                                            .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush")),
                                        distributionBar,
                                        BuildDistributionLegend()
                                    ),
                                // Combos section
                                new Grid()
                                    .ColumnDefinitions("*,*")
                                    .ColumnSpacing(16)
                                    .Children(
                                        new StackPanel()
                                            .Grid(column: 0)
                                            .Spacing(8)
                                            .Children(
                                                RegularText().Text("Strongest").FontSize(13)
                                                    .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                                                    .Foreground(new SolidColorBrush(SuccessColor)),
                                                strongestPanel
                                            ),
                                        new StackPanel()
                                            .Grid(column: 1)
                                            .Spacing(8)
                                            .Children(
                                                RegularText().Text("Needs Work").FontSize(13)
                                                    .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                                                    .Foreground(new SolidColorBrush(ErrorColor)),
                                                weakestPanel
                                            )
                                    )
                            )
                    )
            );
    }

    // === Distribution Bar ===

    static void PopulateDistributionBar(Grid container, SrsDistributionDto distribution)
    {
        container.Children.Clear();
        container.ColumnDefinitions.Clear();

        var total = distribution.Struggling + distribution.Learning + distribution.Strong + distribution.Mastered;
        if (total == 0) return;

        var segments = new List<(int count, Color color)>();
        if (distribution.Struggling > 0) segments.Add((distribution.Struggling, ErrorColor));
        if (distribution.Learning > 0) segments.Add((distribution.Learning, TertiaryColor));
        if (distribution.Strong > 0) segments.Add((distribution.Strong, SuccessColor));
        if (distribution.Mastered > 0) segments.Add((distribution.Mastered, PrimaryColor));

        for (int i = 0; i < segments.Count; i++)
        {
            container.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(segments[i].count, GridUnitType.Star)
            });

            var isFirst = i == 0;
            var isLast = i == segments.Count - 1;

            var segment = new Border()
                .Background(new SolidColorBrush(segments[i].color))
                .CornerRadius(new CornerRadius(
                    isFirst ? 8 : 0, isLast ? 8 : 0,
                    isLast ? 8 : 0, isFirst ? 8 : 0))
                .VerticalAlignment(VerticalAlignment.Stretch)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .Grid(column: i);

            container.Children.Add(segment);
        }
    }

    // === Combo Lists ===

    static void PopulateCombos(StackPanel strongestPanel, StackPanel weakestPanel, PracticeTypeStatsDto stats)
    {
        strongestPanel.Children.Clear();
        weakestPanel.Children.Clear();

        // Lists are already padded to 5 entries by repository
        foreach (var combo in stats.StrongestCombos)
            strongestPanel.Children.Add(BuildComboRow(combo, SuccessColor));

        foreach (var combo in stats.WeakestCombos)
            weakestPanel.Children.Add(BuildComboRow(combo, ErrorColor));
    }

    static FrameworkElement BuildComboRow(ComboStatDto combo, Color accentColor)
    {
        // Placeholder entry - just show the dash
        if (combo.IsPlaceholder)
        {
            return RegularText()
                .Text(combo.DisplayName)
                .FontSize(11)
                .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"));
        }

        var progressFill = new Border()
            .HorizontalAlignment(HorizontalAlignment.Left)
            .Background(new SolidColorBrush(accentColor))
            .CornerRadius(3);

        var progressContainer = new Grid()
            .Height(6)
            .CornerRadius(3)
            .Background(ThemeResource.Get<Brush>("BackgroundVariantBrush"))
            .Children(progressFill);

        progressContainer.SizeChanged += (s, e) =>
        {
            if (s is Grid g && g.ActualWidth > 0)
                progressFill.Width = g.ActualWidth * combo.MasteryPercent / 100.0;
        };

        return new StackPanel()
            .Spacing(2)
            .Children(
                new Grid()
                    .ColumnDefinitions("*,Auto")
                    .Children(
                        RegularText()
                            .Text(combo.DisplayName)
                            .FontSize(11)
                            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
                            .TextTrimming(TextTrimming.CharacterEllipsis),
                        RegularText()
                            .Grid(column: 1)
                            .Text($"{combo.MasteryPercent}%")
                            .FontSize(11)
                            .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
                    ),
                progressContainer
            );
    }

    // === UI Builders ===

    static FrameworkElement BuildSectionHeader(string title, string icon)
    {
        return new StackPanel()
            .Orientation(Orientation.Horizontal)
            .Spacing(8)
            .Children(
                new FontIcon()
                    .Glyph(icon)
                    .FontSize(20)
                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush")),
                RegularText()
                    .Text(title)
                    .FontSize(18)
                    .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
            );
    }

    static FrameworkElement BuildStreakCard(string icon, string label, Action<TextBlock> bindValue, int column)
    {
        var valueText = RegularText()
            .FontSize(24)
            .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"));
        bindValue(valueText);

        return new StackPanel()
            .Grid(column: column)
            .HorizontalAlignment(HorizontalAlignment.Left)
            .Spacing(4)
            .Children(
                new StackPanel()
                    .Orientation(Orientation.Horizontal)
                    .Spacing(6)
                    .Children(
                        new FontIcon().Glyph(icon).FontSize(16)
                            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush")),
                        RegularText().Text(label).FontSize(12)
                            .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
                    ),
                valueText
            );
    }

    static FrameworkElement BuildTodayItem(string label, Action<TextBlock> bindValue, Action<CheckBox> bindGoalMet, int column)
    {
        var valueText = RegularText()
            .FontSize(20)
            .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"));
        bindValue(valueText);

        var checkbox = new CheckBox().IsHitTestVisible(false).MinWidth(0).Padding(0);
        bindGoalMet(checkbox);

        return new StackPanel()
            .Grid(column: column)
            .Orientation(Orientation.Horizontal)
            .HorizontalAlignment(HorizontalAlignment.Left)
            .Spacing(8)
            .Children(
                new StackPanel().Children(
                    RegularText().Text(label).FontSize(12)
                        .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush")),
                    valueText
                ),
                checkbox
            );
    }

    static FrameworkElement BuildStatItem(Action<TextBlock> bindValue, int column)
    {
        var valueText = RegularText()
            .FontSize(20)
            .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"));
        bindValue(valueText);
        return valueText.Grid(column: column).HorizontalAlignment(HorizontalAlignment.Center);
    }

    static FrameworkElement BuildStatItem(string label, Action<TextBlock> bindValue, int column)
    {
        var valueText = RegularText()
            .FontSize(20)
            .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"));
        bindValue(valueText);

        return new StackPanel()
            .Grid(column: column)
            .HorizontalAlignment(HorizontalAlignment.Left)
            .Children(
                RegularText().Text(label).FontSize(12)
                    .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush")),
                valueText
            );
    }

    // === Calendar ===

    static FrameworkElement BuildCalendarLegend()
    {
        return new StackPanel()
            .Orientation(Orientation.Horizontal)
            .Spacing(4)
            .VerticalAlignment(VerticalAlignment.Center)
            .Children(
                RegularText().Text("Less").FontSize(10)
                    .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush")),
                CreateLegendCell(0), CreateLegendCell(1), CreateLegendCell(2), CreateLegendCell(3),
                RegularText().Text("More").FontSize(10)
                    .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
            );
    }

    static Border CreateLegendCell(int intensity)
    {
        return new Border()
            .Width(12).Height(12).CornerRadius(2)
            .Background(GetIntensityBrush(intensity));
    }

    static Brush GetIntensityBrush(int intensity)
    {
        return intensity switch
        {
            0 => Application.Current.Resources["NeutralGrayBrush"] as Brush
                 ?? new SolidColorBrush(Color.FromArgb(255, 224, 224, 224)),
            1 => new SolidColorBrush(Color.FromArgb(80, 179, 92, 0)),
            2 => new SolidColorBrush(Color.FromArgb(140, 179, 92, 0)),
            3 => new SolidColorBrush(Color.FromArgb(220, 179, 92, 0)),
            _ => new SolidColorBrush(Color.FromArgb(255, 179, 92, 0))
        };
    }

    static DataTemplate CreateCalendarItemTemplate()
    {
        return new DataTemplate(() =>
        {
            var border = new Border().Width(20).Height(20).CornerRadius(3);

            border.SetBinding(BackgroundProperty, new Binding
            {
                Path = new PropertyPath("Intensity"),
                Converter = new IntensityToBrushConverter()
            });

            border.DataContextChanged += (s, e) =>
            {
                if (s is Border b && b.DataContext is CalendarDayDto day)
                {
                    var date = DailyProgress.FromDateKey(day.Date);
                    ToolTipService.SetToolTip(b, $"{date:ddd, MMM d}\nNouns: {day.DeclensionsCount}\nVerbs: {day.ConjugationsCount}");
                }
            };

            return border;
        });
    }

    static FrameworkElement BuildDistributionLegend()
    {
        return new Grid()
            .ColumnDefinitions("*,*,*,*")
            .Children(
                BuildLegendItem("Struggling", ErrorColor, 0),
                BuildLegendItem("Learning", TertiaryColor, 1),
                BuildLegendItem("Strong", SuccessColor, 2),
                BuildLegendItem("Mastered", PrimaryColor, 3)
            );
    }

    static FrameworkElement BuildLegendItem(string label, Color color, int column)
    {
        return new StackPanel()
            .Grid(column: column)
            .Orientation(Orientation.Horizontal)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Spacing(4)
            .Children(
                new Ellipse().Width(8).Height(8).Fill(new SolidColorBrush(color)),
                RegularText().Text(label).FontSize(10)
                    .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
            );
    }

    // === Colors ===

    static readonly Color ErrorColor = Color.FromArgb(255, 179, 38, 30);
    static readonly Color SuccessColor = Color.FromArgb(255, 46, 125, 50);
    static readonly Color TertiaryColor = Color.FromArgb(255, 0, 97, 164);
    static readonly Color PrimaryColor = Color.FromArgb(255, 179, 92, 0);
}

/// <summary>
/// Converts intensity level (0-3) to brush for calendar cells.
/// </summary>
public class IntensityToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int intensity)
        {
            return intensity switch
            {
                0 => Application.Current.Resources["NeutralGrayBrush"] as Brush
                     ?? new SolidColorBrush(Color.FromArgb(255, 224, 224, 224)),
                1 => new SolidColorBrush(Color.FromArgb(80, 179, 92, 0)),
                2 => new SolidColorBrush(Color.FromArgb(140, 179, 92, 0)),
                3 => new SolidColorBrush(Color.FromArgb(220, 179, 92, 0)),
                _ => new SolidColorBrush(Color.FromArgb(255, 179, 92, 0))
            };
        }
        return new SolidColorBrush(Color.FromArgb(255, 224, 224, 224));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

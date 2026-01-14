using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Grammar.ViewModels;
using PaliPractice.Themes;
using static PaliPractice.Presentation.Common.TextHelpers;

namespace PaliPractice.Presentation.Grammar;

/// <summary>
/// Page displaying complete inflection table for a lemma.
/// Shows all declensions (nouns) or conjugations (verbs) with frozen headers.
/// </summary>
public sealed partial class InflectionTablePage : Page
{
    readonly Grid? _tableContainer;
    readonly TextBlock? _titleTextBlock;
    readonly TextBlock? _headerTextBlock;
    readonly TextBlock? _hintTextBlock;

    public InflectionTablePage()
    {
        // Create placeholder for the table that will be built after data is available
        _tableContainer = new Grid();
        _titleTextBlock = PaliText()
            .FontSize(22)
            .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .VerticalAlignment(VerticalAlignment.Center);
        _headerTextBlock = RegularText()
            .FontSize(16)
            .TextWrapping(TextWrapping.Wrap);
        _hintTextBlock = RegularText()
            .FontSize(12)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
            .Visibility(Visibility.Collapsed); // Hidden by default, shown if table has non-corpus forms

        this.DataContext<InflectionTableViewModel>((page, vm) => page
            .NavigationCacheMode<InflectionTablePage>(NavigationCacheMode.Disabled)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(new Grid()
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
                .RowDefinitions("Auto,*")
                .Children(
                    // Row 0: Title bar with back button (full width)
                    BuildTitleBar(vm)
                        .Grid(row: 0),

                    // Row 1: Centered content container (header info + table)
                    new Grid()
                        .HorizontalAlignment(HorizontalAlignment.Center)
                        .MaxWidth(800) // Max width for readability on large screens
                        .RowDefinitions("Auto,*")
                        .Children(
                            // Row 0: Header info panel (left-aligned within centered container)
                            BuildHeaderInfo()
                                .Grid(row: 0),

                            // Row 1: Table container (populated in DataContextChanged)
                            _tableContainer
                                .Grid(row: 1)
                        )
                        .Grid(row: 1)
                )
            )
        );

        // Use DataContextChanged instead of Loaded to ensure ViewModel is ready
        DataContextChanged += OnDataContextChanged;
    }

    void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (args.NewValue is InflectionTableViewModel vm && _tableContainer != null)
        {
            // Update title with lemma name
            _titleTextBlock?.Text = vm.LemmaName;

            // Update formatted header: <pattern> <type> (like <example>)
            // Skip "(like ...)" if the example is the same as the current lemma
            if (_headerTextBlock != null)
            {
                _headerTextBlock.Inlines.Clear();
                _headerTextBlock.Inlines.Add(new Run
                {
                    Text = vm.PatternName,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                });
                _headerTextBlock.Inlines.Add(new Run { Text = $" {vm.TypeName}" });

                if (!string.Equals(vm.LikeExample, vm.LemmaName, StringComparison.OrdinalIgnoreCase))
                {
                    _headerTextBlock.Inlines.Add(new Run { Text = " (like " });
                    _headerTextBlock.Inlines.Add(new Run
                    {
                        Text = vm.LikeExample,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        FontFamily = new FontFamily(FontPaths.LibertinusSans)
                    });
                    _headerTextBlock.Inlines.Add(new Run { Text = ")" });
                }
            }

            // Build the frozen header table with actual data
            if (vm.RowHeaders.Count > 0 && vm.ColumnHeaders.Count > 0)
            {
                var result = FrozenHeaderTable.Build(
                    vm.ColumnHeaders,
                    vm.RowHeaders,
                    vm.Cells);

                _tableContainer.Children.Clear();
                _tableContainer.Children.Add(result.Table);

                // Show hint if there are non-corpus forms
                if (_hintTextBlock != null && result.HasNonCorpusForms)
                {
                    _hintTextBlock.Text = "Forms not found in the Pāli corpus are grayed out";
                    _hintTextBlock.Visibility = Visibility.Visible;
                }
                else
                {
                    _hintTextBlock?.Visibility = Visibility.Collapsed;
                }
            }
        }
    }

    Grid BuildTitleBar(InflectionTableViewModel vm)
    {
        var backButton = new SquircleButton()
            .Fill(ThemeResource.Get<Brush>("SecondaryButtonBrush"))
            .Stroke(ThemeResource.Get<Brush>("OutlineBrush"))
            .StrokeThickness(LayoutConstants.Sizes.ButtonStrokeThickness)
            .RadiusMode(SquircleRadiusMode.NearPill)
            .Padding(12, 8)
            .WithPillShadow();
        backButton.SetBinding(ButtonBase.CommandProperty, new Binding { Path = new PropertyPath("GoBackCommand") });
        backButton.Child(new StackPanel()
            .Orientation(Orientation.Horizontal)
            .Spacing(6)
            .Children(
                new FontIcon()
                    .Glyph("\uE72B")
                    .FontSize(16)
                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush")),
                RegularText()
                    .Text("Back")
                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                    .VerticalAlignment(VerticalAlignment.Center)
            ));

        // Transparent background - bar blends with page background
        return new Grid()
            .Padding(16, 8)
            .Children(
                _titleTextBlock!,
                new Grid()
                    .ColumnDefinitions("Auto,*")
                    .Children(backButton.Grid(column: 0))
            );
    }

    Border BuildHeaderInfo()
    {
        return new Border()
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .Padding(16, 12)
            .Margin(0, 0, 0, 1)
            .Child(new StackPanel()
                .Spacing(8)
                .Children(
                    _headerTextBlock!,
                    _hintTextBlock!
                ));
    }
}

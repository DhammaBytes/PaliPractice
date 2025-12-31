using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Practice.ViewModels;
using static PaliPractice.Presentation.Common.TextHelpers;

namespace PaliPractice.Presentation.Practice;

/// <summary>
/// Page displaying complete inflection table for a lemma.
/// Shows all declensions (nouns) or conjugations (verbs) with frozen headers.
/// </summary>
public sealed partial class InflectionTablePage : Page
{
    Grid? _tableContainer;

    public InflectionTablePage()
    {
        // Create placeholder for the table that will be built after data is available
        _tableContainer = new Grid();

        this.DataContext<InflectionTableViewModel>((page, vm) => page
            .NavigationCacheMode(NavigationCacheMode.Disabled)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(new Grid()
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
                .RowDefinitions("Auto,*")
                .Children(
                    // Row 0: Title bar with back button (full width)
                    AppTitleBar.Build<InflectionTableViewModel>(
                        "Inflection Table",
                        v => v.GoBackCommand)
                        .Grid(row: 0),

                    // Row 1: Centered content container (header info + table)
                    new Grid()
                        .HorizontalAlignment(HorizontalAlignment.Center)
                        .MaxWidth(800) // Max width for readability on large screens
                        .RowDefinitions("Auto,*")
                        .Children(
                            // Row 0: Header info panel (left-aligned within centered container)
                            BuildHeaderInfo(vm)
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
            // Build the frozen header table with actual data
            if (vm.RowHeaders.Count > 0 && vm.ColumnHeaders.Count > 0)
            {
                var table = FrozenHeaderTable.Build(
                    vm.ColumnHeaders,
                    vm.RowHeaders,
                    vm.Cells);

                _tableContainer.Children.Clear();
                _tableContainer.Children.Add(table);
            }
        }
    }

    static Border BuildHeaderInfo(InflectionTableViewModel vm)
    {
        // Create TextBlocks for binding
        var lemmaText = PaliText()
            .FontSize(24)
            .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
            .Margin(0, 0, 0, 4);

        var patternText = RegularText()
            .FontSize(14)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"));

        var headerText = RegularText()
            .FontSize(16)
            .TextWrapping(TextWrapping.Wrap);

        // Apply bindings using the ViewModel expressions
        lemmaText.Text(() => vm.Lemma.BaseForm);
        patternText.Text(() => vm.PatternText);
        headerText.Text(() => vm.HeaderText);

        return new Border()
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .Padding(16, 12)
            .Margin(0, 0, 0, 1)
            .Child(
                new StackPanel()
                    .Spacing(4)
                    .Children(
                        lemmaText,
                        patternText,
                        headerText
                    )
            );
    }
}

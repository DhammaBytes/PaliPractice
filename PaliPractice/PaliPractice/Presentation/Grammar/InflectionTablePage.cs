using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Grammar.ViewModels;
using PaliPractice.Themes;
using static PaliPractice.Presentation.Common.Text.TextHelpers;
using static PaliPractice.Presentation.Common.ShadowHelper;

namespace PaliPractice.Presentation.Grammar;

/// <summary>
/// Page displaying complete inflection table for a lemma.
/// Shows all declensions (nouns) or conjugations (verbs) with frozen headers.
/// </summary>
public sealed partial class InflectionTablePage : Page
{
    readonly Grid _tableCardContainer = new();
    readonly TextBlock? _titleTextBlock;
    readonly TextBlock? _headerTextBlock;
    readonly TextBlock? _hintTextBlock;

    public InflectionTablePage()
    {
        _titleTextBlock = PaliText()
            .FontSize(22)
            .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .VerticalAlignment(VerticalAlignment.Center)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"));
        _headerTextBlock = RegularText()
            .FontSize(16)
            .TextWrapping(TextWrapping.Wrap)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"));
        _hintTextBlock = RegularText()
            .FontSize(12)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceSecondaryBrush"))
            .Visibility(Visibility.Collapsed); // Hidden by default, shown if table has non-corpus forms

        this.DataContext<InflectionTableViewModel>((page, vm) => page
            .NavigationCacheMode<InflectionTablePage>(NavigationCacheMode.Disabled)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(PageFadeIn.Wrap(page,
                new Grid()
                    .SafeArea(SafeArea.InsetMask.VisibleBounds)
                    .RowDefinitions("Auto,*")
                    .Children(
                        // Row 0: Title bar with back button
                        AppTitleBar.BuildWithCenterElement<InflectionTableViewModel>(
                                _titleTextBlock!, vm => vm.GoBackCommand)
                            .Grid(row: 0),

                        // Row 1: Content area with header and table card
                        new ScrollViewer()
                            .Content(
                                new StackPanel()
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .MaxWidth(800)
                                    .Padding(16)
                                    .Spacing(16)
                                    .Children(
                                        // Header info (pattern + hint) - on background
                                        BuildHeaderInfo(),

                                        // Table card with shadow
                                        _tableCardContainer
                                    )
                            )
                            .Grid(row: 1)
                    )
            ))
        );

        // Use DataContextChanged instead of Loaded to ensure ViewModel is ready
        DataContextChanged += OnDataContextChanged;
    }

    void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (args.NewValue is not InflectionTableViewModel vm) return;

        // Update title with lemma name
        if (_titleTextBlock != null)
            _titleTextBlock.Text = vm.LemmaName;

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
                    FontFamily = PaliFont
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

            // Show hint if there are non-corpus forms
            if (_hintTextBlock != null && result.HasNonCorpusForms)
            {
                _hintTextBlock.Text = "Forms not found in the Pāli corpus are grayed out.";
                _hintTextBlock.Visibility = Visibility.Visible;
            }
            else if (_hintTextBlock != null)
            {
                _hintTextBlock.Visibility = Visibility.Collapsed;
            }

            // Wrap table in a SquircleBorder card with shadow
            var tableCard = new SquircleBorder()
                .Fill(ThemeResource.Get<Brush>("SurfaceBrush"))
                .Child(
                    new Border()
                        .Padding(16)
                        .Child(result.Table)
                );

            _tableCardContainer.Children.Clear();
            _tableCardContainer.Children.Add(CardShadow(tableCard));
        }
    }

    StackPanel BuildHeaderInfo()
    {
        return new StackPanel()
            .Spacing(4)
            .Children(
                _headerTextBlock!,
                _hintTextBlock!
            );
    }
}

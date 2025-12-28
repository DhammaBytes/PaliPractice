using Microsoft.UI.Xaml.Controls;
using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Settings.Controls;
using PaliPractice.Presentation.Settings.ViewModels;

namespace PaliPractice.Presentation.Settings;

public sealed partial class LemmaRangeSettingsPage : Page
{
    static void OnNumberBoxLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: LemmaRangeSettingsViewModel viewModel })
            viewModel.ValidateRangeCommand.Execute(null);
    }

    public LemmaRangeSettingsPage()
    {
        LemmaRangeSettingsPageMarkup.DataContext<LemmaRangeSettingsViewModel>(this, (page, vm) => page
            .NavigationCacheMode<LemmaRangeSettingsPage>(NavigationCacheMode.Disabled)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(new Grid()
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
                .RowDefinitions("Auto,*")
                .Children(
                    // Row 0: Title bar with dynamic title
                    BuildTitleBar(vm),

                    // Row 1: Content
                    new ScrollViewer()
                        .Grid(row: 1)
                        .HorizontalScrollBarVisibility(ScrollBarVisibility.Disabled)
                        .HorizontalScrollMode(ScrollMode.Disabled)
                        .Content(
                            new StackPanel()
                                .MaxWidth(LayoutConstants.ContentMaxWidth)
                                .Padding(16, 0)
                                .Spacing(16)
                                .Children(
                                    // Practice range section with RadioButtons
                                    BuildRangeSection(vm),

                                    // Custom range section (visible only when Custom is selected)
                                    BuildCustomRangeSection(vm)
                                )
                        )
                )
            )
        );
    }

    static Grid BuildTitleBar(LemmaRangeSettingsViewModel vm)
    {
        return AppTitleBar.Build<LemmaRangeSettingsViewModel>("Practice Range", v => v.GoBackCommand);
    }

    static StackPanel BuildRangeSection(LemmaRangeSettingsViewModel vm)
    {
        var radioButtons = new RadioButtons()
            .MaxColumns(1)
            .Margin(0, 8, 0, 0);

        // Add radio button options: 0=Top100, 1=Top300, 2=Top500, 3=All, 4=Custom
        radioButtons.Items.Add(BuildRadioOption("Top 100", "Most common words"));
        radioButtons.Items.Add(BuildRadioOption("Top 300", "Common words"));
        radioButtons.Items.Add(BuildRadioOption("Top 500", "Extended vocabulary"));
        radioButtons.Items.Add(BuildAllWordsOption(vm));
        radioButtons.Items.Add(BuildRadioOption("Custom range", "Specify your own range"));

        // Bind selected index
        radioButtons.SelectedIndex(x => x.Binding(() => vm.SelectedPreset).TwoWay());

        return new StackPanel()
            .Spacing(4)
            .Children(
                TextHelpers.RegularText()
                    .Text("Practice Range")
                    .FontSize(14)
                    .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                    .Foreground(ThemeResource.Get<Brush>("PrimaryBrush"))
                    .Margin(0, 8, 0, 4),
                new Border()
                    .CornerRadius(8)
                    .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
                    .Padding(16, 8)
                    .Child(radioButtons)
            );
    }

    static StackPanel BuildAllWordsOption(LemmaRangeSettingsViewModel vm)
    {
        var subtitleText = new TextBlock()
            .FontSize(12)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"));

        // Bind to TotalLemmaCount with format
        subtitleText.Text(x => x.Binding(() => vm.TotalLemmaCount)
            .Convert(count => $"{count} available"));

        return new StackPanel()
            .Spacing(2)
            .Children(
                TextHelpers.RegularText()
                    .Text("All words")
                    .FontSize(16),
                subtitleText
            );
    }

    static StackPanel BuildRadioOption(string title, string subtitle)
    {
        return new StackPanel()
            .Spacing(2)
            .Children(
                TextHelpers.RegularText()
                    .Text(title)
                    .FontSize(16),
                TextHelpers.RegularText()
                    .Text(subtitle)
                    .FontSize(12)
                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
            );
    }

    static StackPanel BuildCustomRangeSection(LemmaRangeSettingsViewModel vm)
    {
        // NumberBox for LemmaMin - prevent empty values and set valid range
        // Maximum is set high; validation is handled in the ViewModel on LostFocus
        var minNumberBox = new NumberBox()
            .Minimum(1)
            .Maximum(9999)
            .SmallChange(10)
            .LargeChange(50)
            .SpinButtonPlacementMode(NumberBoxSpinButtonPlacementMode.Compact)
            .ValidationMode(NumberBoxValidationMode.InvalidInputOverwritten)
            .MinWidth(120);
        minNumberBox.Value(x => x.Binding(() => vm.LemmaMin).TwoWay());
        minNumberBox.LostFocus += OnNumberBoxLostFocus;

        // NumberBox for LemmaMax - prevent empty values and set valid range
        // Maximum is set high; validation is handled in the ViewModel on LostFocus
        var maxNumberBox = new NumberBox()
            .Minimum(2)
            .Maximum(9999)
            .SmallChange(10)
            .LargeChange(50)
            .SpinButtonPlacementMode(NumberBoxSpinButtonPlacementMode.Compact)
            .ValidationMode(NumberBoxValidationMode.InvalidInputOverwritten)
            .MinWidth(120);
        maxNumberBox.Value(x => x.Binding(() => vm.LemmaMax).TwoWay());
        maxNumberBox.LostFocus += OnNumberBoxLostFocus;

        var section = new StackPanel()
            .Spacing(4);

        // Visibility bound to IsCustomRange
        section.Visibility(x => x.Binding(() => vm.IsCustomRange)
            .Convert(isCustom => isCustom ? Visibility.Visible : Visibility.Collapsed));

        section.Children(
            TextHelpers.RegularText()
                .Text("Custom Range")
                .FontSize(14)
                .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                .Foreground(ThemeResource.Get<Brush>("PrimaryBrush"))
                .Margin(0, 8, 0, 4),
            new Border()
                .CornerRadius(8)
                .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
                .Padding(16, 12)
                .Child(
                    new StackPanel()
                        .Spacing(16)
                        .Children(
                            new Grid()
                                .ColumnDefinitions("*,Auto")
                                .Children(
                                    TextHelpers.RegularText()
                                        .Text("From (most common)")
                                        .FontSize(16)
                                        .VerticalAlignment(VerticalAlignment.Center)
                                        .Grid(column: 0),
                                    minNumberBox.Grid(column: 1)
                                ),
                            new Grid()
                                .ColumnDefinitions("*,Auto")
                                .Children(
                                    TextHelpers.RegularText()
                                        .Text("To (less common)")
                                        .FontSize(16)
                                        .VerticalAlignment(VerticalAlignment.Center)
                                        .Grid(column: 0),
                                    maxNumberBox.Grid(column: 1)
                                )
                        )
                )
        );

        return section;
    }
}

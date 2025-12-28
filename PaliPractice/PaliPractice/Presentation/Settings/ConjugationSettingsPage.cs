using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Settings.Controls;
using PaliPractice.Presentation.Settings.ViewModels;

namespace PaliPractice.Presentation.Settings;

public sealed partial class ConjugationSettingsPage : Page
{
    public ConjugationSettingsPage()
    {
        Loaded += OnLoaded;

        ConjugationSettingsPageMarkup.DataContext<ConjugationSettingsViewModel>(this, (page, vm) => page
            .NavigationCacheMode<ConjugationSettingsPage>(NavigationCacheMode.Required)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(new Grid()
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
                .RowDefinitions("Auto,*")
                .Children(
                    // Row 0: Title bar
                    AppTitleBar.Build<ConjugationSettingsViewModel>("Conjugation Settings", v => v.GoBackCommand),

                    // Row 1: Content
                    new ScrollViewer()
                        .Grid(row: 1)
                        .HorizontalScrollBarVisibility(ScrollBarVisibility.Disabled)
                        .HorizontalScrollMode(ScrollMode.Disabled)
                        .Content(
                            new StackPanel()
                                .MaxWidth(LayoutConstants.ContentMaxWidth)
                                .Children(
                                    // General section
                                    SettingsSection.Build("General",
                                        SettingsRow.BuildNavigation<ConjugationSettingsViewModel>(
                                            "Practice range",
                                            v => v.GoToLemmaRangeCommand,
                                            tb => tb.Text(() => vm.RangeText)),
                                        SettingsRow.BuildDropdown(
                                            "Daily practice goal",
                                            ConjugationSettingsViewModel.DailyGoalOptions,
                                            cb => cb.SetBinding(
                                                ComboBox.SelectedItemProperty,
                                                Bind.TwoWayPath<ConjugationSettingsViewModel, int>(v => v.DailyGoal)))
                                    ),

                                    // Practice filters section
                                    BuildPracticeFiltersSection(vm),

                                    // Tense section
                                    SettingsSection.Build("Tense",
                                        SettingsRow.BuildToggle<ConjugationSettingsViewModel>(
                                            "Present", v => v.Present, v => v.CanDisablePresent),
                                        SettingsRow.BuildToggle<ConjugationSettingsViewModel>(
                                            "Imperative", v => v.Imperative, v => v.CanDisableImperative),
                                        SettingsRow.BuildToggle<ConjugationSettingsViewModel>(
                                            "Optative", v => v.Optative, v => v.CanDisableOptative),
                                        SettingsRow.BuildToggle<ConjugationSettingsViewModel>(
                                            "Future", v => v.Future, v => v.CanDisableFuture)
                                    )
                                )
                        )
                )
            )
        );
    }

    static StackPanel BuildPracticeFiltersSection(ConjugationSettingsViewModel vm)
    {
        return new StackPanel()
            .Spacing(4)
            .Children(
                // Section header
                TextHelpers.RegularText()
                    .Text("Practice filters")
                    .FontSize(14)
                    .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                    .Foreground(ThemeResource.Get<Brush>("PrimaryBrush"))
                    .Margin(16, 16, 16, 8),

                // Endings checkboxes row
                BuildEndingsCheckboxRow(),

                // Person checkboxes row
                BuildPersonCheckboxRow(),

                // Number dropdown
                SettingsRow.BuildDropdown(
                    "Number",
                    ConjugationSettingsViewModel.NumberOptions,
                    cb => cb.SetBinding(
                        ComboBox.SelectedItemProperty,
                        Bind.TwoWayPath<ConjugationSettingsViewModel, string>(v => v.NumberSetting)))
            );
    }

    static Grid BuildEndingsCheckboxRow()
    {
        // Create rows of checkboxes (wrapping grid for 4 checkboxes)
        var checkboxContainer = new StackPanel()
            .Spacing(4);

        var row = new StackPanel()
            .Orientation(Orientation.Horizontal)
            .Spacing(4);

        // ati checkbox
        var cbAti = new CheckBox()
            .VerticalContentAlignment(VerticalAlignment.Center)
            .Content(
                TextHelpers.PaliText()
                    .Text("ati")
                    .FontSize(18)
                    .Margin(4, 0, 0, 0)
            )
            .MinWidth(60)
            .Padding(4, 0);
        cbAti.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<ConjugationSettingsViewModel, bool>(v => v.PatternAti));
        cbAti.SetBinding(IsEnabledProperty, Bind.Path<ConjugationSettingsViewModel, bool>(v => v.CanDisablePatternAti));

        // eti checkbox
        var cbEti = new CheckBox()
            .VerticalContentAlignment(VerticalAlignment.Center)
            .Content(
                TextHelpers.PaliText()
                    .Text("eti")
                    .FontSize(18)
                    .Margin(4, 0, 0, 0)
            )
            .MinWidth(60)
            .Padding(4, 0);
        cbEti.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<ConjugationSettingsViewModel, bool>(v => v.PatternEti));
        cbEti.SetBinding(IsEnabledProperty, Bind.Path<ConjugationSettingsViewModel, bool>(v => v.CanDisablePatternEti));

        // oti checkbox
        var cbOti = new CheckBox()
            .VerticalContentAlignment(VerticalAlignment.Center)
            .Content(
                TextHelpers.PaliText()
                    .Text("oti")
                    .FontSize(18)
                    .Margin(4, 0, 0, 0)
            )
            .MinWidth(60)
            .Padding(4, 0);
        cbOti.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<ConjugationSettingsViewModel, bool>(v => v.PatternOti));
        cbOti.SetBinding(IsEnabledProperty, Bind.Path<ConjugationSettingsViewModel, bool>(v => v.CanDisablePatternOti));

        // āti checkbox
        var cbAtiLong = new CheckBox()
            .VerticalContentAlignment(VerticalAlignment.Center)
            .Content(
                TextHelpers.PaliText()
                    .Text("āti")
                    .FontSize(18)
                    .Margin(4, 0, 0, 0)
            )
            .MinWidth(60)
            .Padding(4, 0);
        cbAtiLong.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<ConjugationSettingsViewModel, bool>(v => v.PatternAtiLong));
        cbAtiLong.SetBinding(IsEnabledProperty, Bind.Path<ConjugationSettingsViewModel, bool>(v => v.CanDisablePatternAtiLong));

        // Traditional order: a, ā, e, o
        row.Children.Add(cbAti);
        row.Children.Add(cbAtiLong);
        row.Children.Add(cbEti);
        row.Children.Add(cbOti);
        checkboxContainer.Children.Add(row);

        return new Grid()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .ColumnDefinitions("Auto,*")
            .Padding(16, 12)
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .Children(
                TextHelpers.RegularText()
                    .Text("Endings")
                    .FontSize(16)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Grid(column: 0),
                checkboxContainer
                    .HorizontalAlignment(HorizontalAlignment.Right)
                    .Grid(column: 1)
            );
    }

    static Grid BuildPersonCheckboxRow()
    {
        var checkboxContainer = new StackPanel()
            .Orientation(Orientation.Horizontal)
            .Spacing(8)
            .HorizontalAlignment(HorizontalAlignment.Right);

        // 1st person checkbox
        var cb1 = new CheckBox()
            .VerticalContentAlignment(VerticalAlignment.Center)
            .Content(
                TextHelpers.RegularText()
                    .Text("1st")
                    .FontSize(16)
                    .Margin(4, 0, 0, 0)
            )
            .MinWidth(60)
            .Padding(4, 0);
        cb1.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<ConjugationSettingsViewModel, bool>(v => v.FirstPerson));
        cb1.SetBinding(IsEnabledProperty, Bind.Path<ConjugationSettingsViewModel, bool>(v => v.CanDisableFirstPerson));

        // 2nd person checkbox
        var cb2 = new CheckBox()
            .VerticalContentAlignment(VerticalAlignment.Center)
            .Content(
                TextHelpers.RegularText()
                    .Text("2nd")
                    .FontSize(16)
                    .Margin(4, 0, 0, 0)
            )
            .MinWidth(60)
            .Padding(4, 0);
        cb2.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<ConjugationSettingsViewModel, bool>(v => v.SecondPerson));
        cb2.SetBinding(IsEnabledProperty, Bind.Path<ConjugationSettingsViewModel, bool>(v => v.CanDisableSecondPerson));

        // 3rd person checkbox
        var cb3 = new CheckBox()
            .VerticalContentAlignment(VerticalAlignment.Center)
            .Content(
                TextHelpers.RegularText()
                    .Text("3rd")
                    .FontSize(16)
                    .Margin(4, 0, 0, 0)
            )
            .MinWidth(60)
            .Padding(4, 0);
        cb3.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<ConjugationSettingsViewModel, bool>(v => v.ThirdPerson));
        cb3.SetBinding(IsEnabledProperty, Bind.Path<ConjugationSettingsViewModel, bool>(v => v.CanDisableThirdPerson));

        checkboxContainer.Children.Add(cb1);
        checkboxContainer.Children.Add(cb2);
        checkboxContainer.Children.Add(cb3);

        return new Grid()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .ColumnDefinitions("Auto,*")
            .Padding(16, 12)
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .Children(
                TextHelpers.RegularText()
                    .Text("Person")
                    .FontSize(16)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Grid(column: 0),
                checkboxContainer
                    .Grid(column: 1)
            );
    }

    void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ConjugationSettingsViewModel vm)
            vm.RefreshRange();
    }
}

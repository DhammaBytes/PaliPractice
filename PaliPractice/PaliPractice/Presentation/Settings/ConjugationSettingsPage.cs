using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Settings.Controls;
using PaliPractice.Presentation.Settings.ViewModels;
using PaliPractice.Services.UserData;
using PaliPractice.Themes;

namespace PaliPractice.Presentation.Settings;

public sealed partial class ConjugationSettingsPage : Page
{
    ConjugationSettingsViewModel? _viewModel;

    static void OnDailyGoalLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ConjugationSettingsViewModel viewModel })
            viewModel.ValidateDailyGoalCommand.Execute(null);
    }

    public ConjugationSettingsPage()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;

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
                                        SettingsRow.BuildNumberBox(
                                            "Daily practice goal",
                                            minimum: SettingsHelpers.DailyGoalMin,
                                            maximum: SettingsHelpers.DailyGoalMax,
                                            bindValue: nb =>
                                            {
                                                nb.Value(x => x.Binding(() => vm.DailyGoal).TwoWay());
                                                nb.LostFocus += OnDailyGoalLostFocus;
                                            })
                                    ),

                                    // Practice filters section
                                    BuildPracticeFiltersSection(vm),

                                    // Tense section
                                    SettingsSection.Build("Tense",
                                        SettingsRow.BuildToggleWithIconAndHint<ConjugationSettingsViewModel>(
                                            BadgeIcons.GetIconPath(Tense.Present),
                                            "Present", "present/habitual actions, general truths",
                                            v => v.Present, v => v.CanDisablePresent),
                                        SettingsRow.BuildToggleWithIconAndHint<ConjugationSettingsViewModel>(
                                            BadgeIcons.GetIconPath(Tense.Imperative),
                                            "Imperative", "commands, requests, encouragements",
                                            v => v.Imperative, v => v.CanDisableImperative),
                                        SettingsRow.BuildToggleWithIconAndHint<ConjugationSettingsViewModel>(
                                            BadgeIcons.GetIconPath(Tense.Optative),
                                            "Optative", "wishes, 'may/might', 'should/ought to'",
                                            v => v.Optative, v => v.CanDisableOptative),
                                        SettingsRow.BuildToggleWithIconAndHint<ConjugationSettingsViewModel>(
                                            BadgeIcons.GetIconPath(Tense.Future),
                                            "Future", "actions yet to happen, predictions",
                                            v => v.Future, v => v.CanDisableFuture)
                                    ),

                                    // Tense section footer
                                    TextHelpers.RegularText()
                                        .Text("Aorist (past narrative tense) is not included: its forms vary a lot across verbs and often need verb-by-verb learning. A dedicated aorist trainer may be added later.")
                                        .FontSize(13)
                                        .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
                                        .TextWrapping(TextWrapping.Wrap)
                                        .Margin(16, 4, 16, 16)
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
                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                    .Margin(16, 16, 16, 8),

                // Endings checkboxes row
                BuildEndingsCheckboxRow(),

                // Person checkboxes row
                BuildPersonCheckboxRow(),

                // Number dropdown
                SettingsRow.BuildDropdown(
                    "Number",
                    SettingsHelpers.NumberOptions,
                    cb => cb.SetBinding(
                        ComboBox.SelectedIndexProperty,
                        Bind.TwoWayPath<ConjugationSettingsViewModel, int>(v => v.NumberIndex))),

                // Voice dropdown
                SettingsRow.BuildDropdownWithHint(
                    "Voice",
                    "Active (parassapada) is common, while Reflexive (attanopada) mostly appears in poetic verses",
                    ConjugationSettingsViewModel.VoiceOptions,
                    cb => cb.SetBinding(
                        ComboBox.SelectedIndexProperty,
                        Bind.TwoWayPath<ConjugationSettingsViewModel, int>(v => v.VoiceIndex)))
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

    void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.CitationFormWarningRequested -= OnCitationFormWarning;
            _viewModel = null;
        }
    }

    void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        // Unsubscribe from old ViewModel
        if (_viewModel is not null)
        {
            _viewModel.CitationFormWarningRequested -= OnCitationFormWarning;
            _viewModel = null;
        }

        // Subscribe to new ViewModel
        if (args.NewValue is ConjugationSettingsViewModel vm)
        {
            _viewModel = vm;
            _viewModel.CitationFormWarningRequested += OnCitationFormWarning;
        }
    }

    async void OnCitationFormWarning(object? sender, EventArgs e)
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = "Settings adjusted",
                Content = "Present tense 3rd person singular is used as the citation form in questions because Pāli doesn't commonly use infinitives. " +
                          "To ensure you have forms to practice, 1st and 2nd person have been re-enabled.",
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };

            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConjugationSettings] Failed to show dialog: {ex.Message}");
        }
    }
}

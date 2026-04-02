using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Common.Text;
using PaliPractice.Presentation.Settings.Controls;
using PaliPractice.Presentation.Settings.ViewModels;
using PaliPractice.Localization;
using PaliPractice.Services.UserData;
using PaliPractice.Themes;
using PaliPractice.Themes.Icons;

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
            .Content(PageFadeIn.Wrap(page, new Grid()
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
                .RowDefinitions("Auto,*")
                .Children(
                    // Row 0: Title bar
                    AppTitleBar.Build<ConjugationSettingsViewModel>(AppText.Get("Settings.Conjugation.Title"), v => v.GoBackCommand),

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
                                    SettingsSection.Build(AppText.Get("Settings.Section.General"),
                                        SettingsRow.BuildNavigation<ConjugationSettingsViewModel>(
                                            AppText.Get("Settings.Row.PracticeRange"),
                                            v => v.GoToLemmaRangeCommand,
                                            tb => tb.Text(() => vm.RangeText)),
                                        SettingsRow.BuildNumberBox(
                                            AppText.Get("Settings.Row.DailyPracticeGoal"),
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

                                    // Tenses section
                                    SettingsSection.Build(AppText.Get("Settings.Section.Tenses"),
                                        SettingsRow.BuildToggleWithIconAndHint<ConjugationSettingsViewModel>(
                                            BadgeIcons.GetIconPath(Tense.Present),
                                            GrammarText.GetTense(Tense.Present), GrammarText.GetTenseSettingsHint(Tense.Present),
                                            v => v.Present, v => v.CanDisablePresent),
                                        SettingsRow.BuildToggleWithIconAndHint<ConjugationSettingsViewModel>(
                                            BadgeIcons.GetIconPath(Tense.Imperative),
                                            GrammarText.GetTense(Tense.Imperative), GrammarText.GetTenseSettingsHint(Tense.Imperative),
                                            v => v.Imperative, v => v.CanDisableImperative),
                                        SettingsRow.BuildToggleWithIconAndHint<ConjugationSettingsViewModel>(
                                            BadgeIcons.GetIconPath(Tense.Optative),
                                            GrammarText.GetTense(Tense.Optative), GrammarText.GetTenseSettingsHint(Tense.Optative),
                                            v => v.Optative, v => v.CanDisableOptative),
                                        SettingsRow.BuildToggleWithIconAndHint<ConjugationSettingsViewModel>(
                                            BadgeIcons.GetIconPath(Tense.Future),
                                            GrammarText.GetTense(Tense.Future), GrammarText.GetTenseSettingsHint(Tense.Future),
                                            v => v.Future, v => v.CanDisableFuture)
                                    ),

                                    // Tense section footer
                                    TextHelpers.RegularText()
                                        .Text(AppText.Get("Settings.Tenses.AoristFooter"))
                                        .FontSize(13)
                                        .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
                                        .TextWrapping(TextWrapping.Wrap)
                                        .Margin(16, 4, 16, 16)
                                )
                        )
                ))
            )
        );
    }

    static StackPanel BuildPracticeFiltersSection(ConjugationSettingsViewModel vm)
    {
        return new StackPanel()
            .Spacing(1)
            .Margin(0, 0, 0, 16)
            .Children(
                // Section header
                TextHelpers.RegularText()
                    .Text(AppText.Get("Settings.Section.PracticeFilters"))
                    .FontSize(14)
                    .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                    .Margin(16, 16, 16, 4),

                // Items container with 1px separator lines
                new Border()
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
                    .Child(
                        new StackPanel()
                            .Spacing(1)
                            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
                            .Children(
                                BuildEndingsCheckboxRow(),
                                BuildPersonCheckboxRow(),
                                SettingsRow.BuildDropdown(
                                    AppText.Get("Settings.Row.Number"),
                                    SettingsHelpers.NumberOptions,
                                    cb => cb.SetBinding(
                                        ComboBox.SelectedIndexProperty,
                                        Bind.TwoWayPath<ConjugationSettingsViewModel, int>(v => v.NumberIndex))),
                                SettingsRow.BuildDropdownWithHint(
                                    AppText.Get("Settings.Row.Voice"),
                                    AppText.Get("Settings.Row.VoiceHint"),
                                    ConjugationSettingsViewModel.VoiceOptions,
                                    cb => cb.SetBinding(
                                        ComboBox.SelectedIndexProperty,
                                        Bind.TwoWayPath<ConjugationSettingsViewModel, int>(v => v.VoiceIndex)))
                            )
                    )
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
                    .Text(AppText.Get("Settings.Row.Endings"))
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
                    .Text(GrammarText.GetPersonShort(Person.First))
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
                    .Text(GrammarText.GetPersonShort(Person.Second))
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
                    .Text(GrammarText.GetPersonShort(Person.Third))
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
                    .Text(AppText.Get("Settings.Row.Person"))
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
                Title = AppText.Get("Settings.CitationFormWarning.Title"),
                Content = AppText.Get("Settings.CitationFormWarning.Content"),
                CloseButtonText = AppText.Get("Common.Ok"),
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

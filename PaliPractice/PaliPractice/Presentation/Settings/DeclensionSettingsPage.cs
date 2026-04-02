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

public sealed partial class DeclensionSettingsPage : Page
{
    static void OnDailyGoalLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DeclensionSettingsViewModel viewModel })
            viewModel.ValidateDailyGoalCommand.Execute(null);
    }

    public DeclensionSettingsPage()
    {
        Loaded += OnLoaded;

        DeclensionSettingsPageMarkup.DataContext<DeclensionSettingsViewModel>(this, (page, vm) => page
            .NavigationCacheMode<DeclensionSettingsPage>(NavigationCacheMode.Required)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(PageFadeIn.Wrap(page, new Grid()
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
                .RowDefinitions("Auto,*")
                .Children(
                    // Row 0: Title bar
                    AppTitleBar.Build<DeclensionSettingsViewModel>(AppText.Get("Settings.Declension.Title"), v => v.GoBackCommand),

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
                                        SettingsRow.BuildNavigation<DeclensionSettingsViewModel>(
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

                                    // Cases section
                                    SettingsSection.Build(AppText.Get("Settings.Section.Cases"),
                                        SettingsRow.BuildToggleWithIconAndHint<DeclensionSettingsViewModel>(
                                            BadgeIcons.GetIconPath(Case.Nominative),
                                            GrammarText.GetCase(Case.Nominative), GrammarText.GetCaseSettingsHint(Case.Nominative),
                                            v => v.Nominative, v => v.CanDisableNominative),
                                        SettingsRow.BuildToggleWithIconAndHint<DeclensionSettingsViewModel>(
                                            BadgeIcons.GetIconPath(Case.Accusative),
                                            GrammarText.GetCase(Case.Accusative), GrammarText.GetCaseSettingsHint(Case.Accusative),
                                            v => v.Accusative, v => v.CanDisableAccusative),
                                        SettingsRow.BuildToggleWithIconAndHint<DeclensionSettingsViewModel>(
                                            BadgeIcons.GetIconPath(Case.Instrumental),
                                            GrammarText.GetCase(Case.Instrumental), GrammarText.GetCaseSettingsHint(Case.Instrumental),
                                            v => v.Instrumental, v => v.CanDisableInstrumental),
                                        SettingsRow.BuildToggleWithIconAndHint<DeclensionSettingsViewModel>(
                                            BadgeIcons.GetIconPath(Case.Dative),
                                            GrammarText.GetCase(Case.Dative), GrammarText.GetCaseSettingsHint(Case.Dative),
                                            v => v.Dative, v => v.CanDisableDative),
                                        SettingsRow.BuildToggleWithIconAndHint<DeclensionSettingsViewModel>(
                                            BadgeIcons.GetIconPath(Case.Ablative),
                                            GrammarText.GetCase(Case.Ablative), GrammarText.GetCaseSettingsHint(Case.Ablative),
                                            v => v.Ablative, v => v.CanDisableAblative),
                                        SettingsRow.BuildToggleWithIconAndHint<DeclensionSettingsViewModel>(
                                            BadgeIcons.GetIconPath(Case.Genitive),
                                            GrammarText.GetCase(Case.Genitive), GrammarText.GetCaseSettingsHint(Case.Genitive),
                                            v => v.Genitive, v => v.CanDisableGenitive),
                                        SettingsRow.BuildToggleWithIconAndHint<DeclensionSettingsViewModel>(
                                            BadgeIcons.GetIconPath(Case.Locative),
                                            GrammarText.GetCase(Case.Locative), GrammarText.GetCaseSettingsHint(Case.Locative),
                                            v => v.Locative, v => v.CanDisableLocative),
                                        SettingsRow.BuildToggleWithIconAndHint<DeclensionSettingsViewModel>(
                                            BadgeIcons.GetIconPath(Case.Vocative),
                                            GrammarText.GetCase(Case.Vocative), GrammarText.GetCaseSettingsHint(Case.Vocative),
                                            v => v.Vocative, v => v.CanDisableVocative)
                                    )
                                )
                        )
                ))
            )
        );
    }

    static StackPanel BuildPracticeFiltersSection(DeclensionSettingsViewModel vm)
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
                                GenderPatternSection.Build(
                                    GrammarText.GetGender(Gender.Masculine),
                                    [
                                        ("a", cb => cb.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<DeclensionSettingsViewModel, bool>(v => v.PatternMascA)),
                                              cb => cb.SetBinding(IsEnabledProperty, Bind.Path<DeclensionSettingsViewModel, bool>(v => v.CanDisablePatternMascA))),
                                        ("i", cb => cb.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<DeclensionSettingsViewModel, bool>(v => v.PatternMascI)),
                                              cb => cb.SetBinding(IsEnabledProperty, Bind.Path<DeclensionSettingsViewModel, bool>(v => v.CanDisablePatternMascI))),
                                        ("ī", cb => cb.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<DeclensionSettingsViewModel, bool>(v => v.PatternMascILong)),
                                              cb => cb.SetBinding(IsEnabledProperty, Bind.Path<DeclensionSettingsViewModel, bool>(v => v.CanDisablePatternMascILong))),
                                        ("u", cb => cb.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<DeclensionSettingsViewModel, bool>(v => v.PatternMascU)),
                                              cb => cb.SetBinding(IsEnabledProperty, Bind.Path<DeclensionSettingsViewModel, bool>(v => v.CanDisablePatternMascU))),
                                        ("ū", cb => cb.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<DeclensionSettingsViewModel, bool>(v => v.PatternMascULong)),
                                              cb => cb.SetBinding(IsEnabledProperty, Bind.Path<DeclensionSettingsViewModel, bool>(v => v.CanDisablePatternMascULong))),
                                        ("as", cb => cb.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<DeclensionSettingsViewModel, bool>(v => v.PatternMascAs)),
                                              cb => cb.SetBinding(IsEnabledProperty, Bind.Path<DeclensionSettingsViewModel, bool>(v => v.CanDisablePatternMascAs))),
                                        ("ar", cb => cb.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<DeclensionSettingsViewModel, bool>(v => v.PatternMascAr)),
                                              cb => cb.SetBinding(IsEnabledProperty, Bind.Path<DeclensionSettingsViewModel, bool>(v => v.CanDisablePatternMascAr))),
                                        ("ant", cb => cb.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<DeclensionSettingsViewModel, bool>(v => v.PatternMascAnt)),
                                               cb => cb.SetBinding(IsEnabledProperty, Bind.Path<DeclensionSettingsViewModel, bool>(v => v.CanDisablePatternMascAnt)))
                                    ]),
                                GenderPatternSection.Build(
                                    GrammarText.GetGender(Gender.Feminine),
                                    [
                                        ("ā", cb => cb.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<DeclensionSettingsViewModel, bool>(v => v.PatternFemALong)),
                                              cb => cb.SetBinding(IsEnabledProperty, Bind.Path<DeclensionSettingsViewModel, bool>(v => v.CanDisablePatternFemA))),
                                        ("i", cb => cb.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<DeclensionSettingsViewModel, bool>(v => v.PatternFemI)),
                                              cb => cb.SetBinding(IsEnabledProperty, Bind.Path<DeclensionSettingsViewModel, bool>(v => v.CanDisablePatternFemI))),
                                        ("ī", cb => cb.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<DeclensionSettingsViewModel, bool>(v => v.PatternFemILong)),
                                              cb => cb.SetBinding(IsEnabledProperty, Bind.Path<DeclensionSettingsViewModel, bool>(v => v.CanDisablePatternFemILong))),
                                        ("u", cb => cb.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<DeclensionSettingsViewModel, bool>(v => v.PatternFemU)),
                                              cb => cb.SetBinding(IsEnabledProperty, Bind.Path<DeclensionSettingsViewModel, bool>(v => v.CanDisablePatternFemU))),
                                        ("ar", cb => cb.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<DeclensionSettingsViewModel, bool>(v => v.PatternFemAr)),
                                              cb => cb.SetBinding(IsEnabledProperty, Bind.Path<DeclensionSettingsViewModel, bool>(v => v.CanDisablePatternFemAr)))
                                    ]),
                                GenderPatternSection.Build(
                                    GrammarText.GetGender(Gender.Neuter),
                                    [
                                        ("a", cb => cb.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<DeclensionSettingsViewModel, bool>(v => v.PatternNtA)),
                                              cb => cb.SetBinding(IsEnabledProperty, Bind.Path<DeclensionSettingsViewModel, bool>(v => v.CanDisablePatternNtA))),
                                        ("i", cb => cb.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<DeclensionSettingsViewModel, bool>(v => v.PatternNtI)),
                                              cb => cb.SetBinding(IsEnabledProperty, Bind.Path<DeclensionSettingsViewModel, bool>(v => v.CanDisablePatternNtI))),
                                        ("u", cb => cb.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<DeclensionSettingsViewModel, bool>(v => v.PatternNtU)),
                                              cb => cb.SetBinding(IsEnabledProperty, Bind.Path<DeclensionSettingsViewModel, bool>(v => v.CanDisablePatternNtU)))
                                    ]),
                                SettingsRow.BuildDropdown(
                                    AppText.Get("Settings.Row.Number"),
                                    SettingsHelpers.NumberOptions,
                                    cb => cb.SetBinding(
                                        ComboBox.SelectedIndexProperty,
                                        Bind.TwoWayPath<DeclensionSettingsViewModel, int>(v => v.NumberIndex)))
                            )
                    )
            );
    }

    void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is DeclensionSettingsViewModel vm)
            vm.RefreshRange();
    }
}

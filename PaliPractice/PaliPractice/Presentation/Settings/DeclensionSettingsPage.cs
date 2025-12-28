using Microsoft.UI.Xaml.Controls;
using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Settings.Controls;
using PaliPractice.Presentation.Settings.ViewModels;
using PaliPractice.Services.UserData;

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
            .Content(new Grid()
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
                .RowDefinitions("Auto,*")
                .Children(
                    // Row 0: Title bar
                    AppTitleBar.Build<DeclensionSettingsViewModel>("Declension Settings", v => v.GoBackCommand),

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
                                        SettingsRow.BuildNavigation<DeclensionSettingsViewModel>(
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

                                    // Case section
                                    SettingsSection.Build("Case",
                                        SettingsRow.BuildToggle<DeclensionSettingsViewModel>(
                                            "Nominative", v => v.Nominative, v => v.CanDisableNominative),
                                        SettingsRow.BuildToggle<DeclensionSettingsViewModel>(
                                            "Accusative", v => v.Accusative, v => v.CanDisableAccusative),
                                        SettingsRow.BuildToggle<DeclensionSettingsViewModel>(
                                            "Instrumental", v => v.Instrumental, v => v.CanDisableInstrumental),
                                        SettingsRow.BuildToggle<DeclensionSettingsViewModel>(
                                            "Dative", v => v.Dative, v => v.CanDisableDative),
                                        SettingsRow.BuildToggle<DeclensionSettingsViewModel>(
                                            "Ablative", v => v.Ablative, v => v.CanDisableAblative),
                                        SettingsRow.BuildToggle<DeclensionSettingsViewModel>(
                                            "Genitive", v => v.Genitive, v => v.CanDisableGenitive),
                                        SettingsRow.BuildToggle<DeclensionSettingsViewModel>(
                                            "Locative", v => v.Locative, v => v.CanDisableLocative),
                                        SettingsRow.BuildToggle<DeclensionSettingsViewModel>(
                                            "Vocative", v => v.Vocative, v => v.CanDisableVocative)
                                    )
                                )
                        )
                )
            )
        );
    }

    static StackPanel BuildPracticeFiltersSection(DeclensionSettingsViewModel vm)
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

                // Masculine patterns
                GenderPatternSection.Build(
                    "Masculine",
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
                    ]
                ),

                // Feminine patterns (now second)
                GenderPatternSection.Build(
                    "Feminine",
                    [
                        ("ā", cb => cb.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<DeclensionSettingsViewModel, bool>(v => v.PatternFemA)),
                              cb => cb.SetBinding(IsEnabledProperty, Bind.Path<DeclensionSettingsViewModel, bool>(v => v.CanDisablePatternFemA))),
                        ("i", cb => cb.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<DeclensionSettingsViewModel, bool>(v => v.PatternFemI)),
                              cb => cb.SetBinding(IsEnabledProperty, Bind.Path<DeclensionSettingsViewModel, bool>(v => v.CanDisablePatternFemI))),
                        ("ī", cb => cb.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<DeclensionSettingsViewModel, bool>(v => v.PatternFemILong)),
                              cb => cb.SetBinding(IsEnabledProperty, Bind.Path<DeclensionSettingsViewModel, bool>(v => v.CanDisablePatternFemILong))),
                        ("u", cb => cb.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<DeclensionSettingsViewModel, bool>(v => v.PatternFemU)),
                              cb => cb.SetBinding(IsEnabledProperty, Bind.Path<DeclensionSettingsViewModel, bool>(v => v.CanDisablePatternFemU))),
                        ("ar", cb => cb.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<DeclensionSettingsViewModel, bool>(v => v.PatternFemAr)),
                              cb => cb.SetBinding(IsEnabledProperty, Bind.Path<DeclensionSettingsViewModel, bool>(v => v.CanDisablePatternFemAr)))
                    ]
                ),

                // Neuter patterns
                GenderPatternSection.Build(
                    "Neuter",
                    [
                        ("a", cb => cb.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<DeclensionSettingsViewModel, bool>(v => v.PatternNtA)),
                              cb => cb.SetBinding(IsEnabledProperty, Bind.Path<DeclensionSettingsViewModel, bool>(v => v.CanDisablePatternNtA))),
                        ("i", cb => cb.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<DeclensionSettingsViewModel, bool>(v => v.PatternNtI)),
                              cb => cb.SetBinding(IsEnabledProperty, Bind.Path<DeclensionSettingsViewModel, bool>(v => v.CanDisablePatternNtI))),
                        ("u", cb => cb.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath<DeclensionSettingsViewModel, bool>(v => v.PatternNtU)),
                              cb => cb.SetBinding(IsEnabledProperty, Bind.Path<DeclensionSettingsViewModel, bool>(v => v.CanDisablePatternNtU)))
                    ]
                ),

                // Number dropdown (at end of Practice filters section)
                SettingsRow.BuildDropdown(
                    "Number",
                    SettingsHelpers.NumberOptions,
                    cb => cb.SetBinding(
                        ComboBox.SelectedIndexProperty,
                        Bind.TwoWayPath<DeclensionSettingsViewModel, int>(v => v.NumberIndex)))
            );
    }

    void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is DeclensionSettingsViewModel vm)
            vm.RefreshRange();
    }
}

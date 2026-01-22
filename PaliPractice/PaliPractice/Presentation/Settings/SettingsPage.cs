using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Settings.Controls;
using PaliPractice.Presentation.Settings.ViewModels;
using PaliPractice.Themes;
using PaliPractice.Themes.Icons;
using static PaliPractice.Presentation.Common.Text.TextHelpers;

namespace PaliPractice.Presentation.Settings;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        this.DataContext<SettingsViewModel>((page, vm) => page
            .NavigationCacheMode<SettingsPage>(NavigationCacheMode.Required)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(PageFadeIn.Wrap(page,
                new Grid()
                    .SafeArea(SafeArea.InsetMask.VisibleBounds)
                    .RowDefinitions("Auto,*")
                    .Children(
                        // Row 0: Title bar
                        AppTitleBar.Build<SettingsViewModel>("Settings", v => v.GoBackCommand),

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
                                            SettingsRow.BuildDropdownWithBitmapIcon(
                                                "Theme",
                                                SettingsIcons.Appearance,
                                                SettingsViewModel.ThemeOptions,
                                                cb => cb.SetBinding(
                                                    ComboBox.SelectedIndexProperty,
                                                    Bind.TwoWayPath<SettingsViewModel, int>(v => v.ThemeIndex)))
                                        ),

                                        // Practice settings sections
                                        SettingsSection.Build("Practice",
                                            SettingsRow.BuildNavigationWithIcon<SettingsViewModel>(
                                                "Noun declensions",
                                                SettingsIcons.Noun,
                                                v => v.GoToDeclensionSettingsCommand),
                                            SettingsRow.BuildNavigationWithIcon<SettingsViewModel>(
                                                "Verb conjugations",
                                                SettingsIcons.Verb,
                                                v => v.GoToConjugationSettingsCommand)
                                        ),

                                        // About & Support section
                                        SettingsSection.Build("About & Support",
                                            SettingsRow.BuildNavigationWithIcon<SettingsViewModel>(
                                                "About Pāli Practice",
                                                SettingsIcons.About,
                                                v => v.GoToAboutCommand),
                                            SettingsRow.BuildActionWithIcon<SettingsViewModel>(
                                                "Contact us",
                                                SettingsIcons.Contact,
                                                v => v.ContactUsCommand),
                                            SettingsRow.BuildActionWithIcon<SettingsViewModel>(
                                                "Write a review",
                                                SettingsIcons.Rate,
                                                v => v.RateAppCommand)
                                                .Visibility(Visibility.Collapsed)
                                                .Visibility(() => vm.IsStoreReviewAvailable)
                                        ),

                                        // Review explanation (visible until user opens store page)
                                        RegularText()
                                            .Text("If you have a minute, please leave a review for Pāli Practice. Even one sentence is valuable feedback and it helps other Pāli learners discover the app on the store.")
                                            .FontSize(13)
                                            .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
                                            .TextWrapping(TextWrapping.Wrap)
                                            .Margin(16, -12, 16, 16)
                                            .Visibility(Visibility.Collapsed)
                                            .Visibility(() => vm.ShouldShowReviewExplanation)
                                    )
                            )
                    )
            ))
        );
    }
}

using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Settings.Controls;
using PaliPractice.Presentation.Settings.ViewModels;
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
                                            SettingsRow.BuildDropdownWithIcon(
                                                "Theme",
                                                "\uE790", // ColorSolid
                                                SettingsViewModel.ThemeOptions,
                                                cb => cb.SetBinding(
                                                    ComboBox.SelectedIndexProperty,
                                                    Bind.TwoWayPath<SettingsViewModel, int>(v => v.ThemeIndex)))
                                        ),

                                        // Practice settings sections
                                        SettingsSection.Build("Practice",
                                            SettingsRow.BuildNavigation<SettingsViewModel>(
                                                "Noun declensions",
                                                "\uE8AB", // Shuffle
                                                v => v.GoToDeclensionSettingsCommand),
                                            SettingsRow.BuildNavigation<SettingsViewModel>(
                                                "Verb conjugations",
                                                "\uE823", // Clock
                                                v => v.GoToConjugationSettingsCommand)
                                        ),

                                        // About & Support section
                                        SettingsSection.Build("About & Support",
                                            SettingsRow.BuildNavigation<SettingsViewModel>(
                                                "About Pāli Practice",
                                                "\uE946", // Info
                                                v => v.GoToAboutCommand),
                                            SettingsRow.BuildAction<SettingsViewModel>(
                                                "Contact us",
                                                "\uE715", // Message
                                                v => v.ContactUsCommand),
                                            SettingsRow.BuildAction<SettingsViewModel>(
                                                "Write a review",
                                                "\uE734", // FavoriteStar outline
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

using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Settings.Controls;
using PaliPractice.Presentation.Settings.ViewModels;

namespace PaliPractice.Presentation.Settings;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        this.DataContext<SettingsViewModel>((page, vm) => page
            .NavigationCacheMode<SettingsPage>(NavigationCacheMode.Required)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(new Grid()
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
                                        SettingsRow.BuildPlaceholder("Appearance", "\uE790") // ColorSolid
                                    ),

                                    // Practice settings sections
                                    SettingsSection.Build("Practice",
                                        SettingsRow.BuildNavigation<SettingsViewModel>(
                                            "Declensions",
                                            "\uE8AB", // Shuffle
                                            v => v.GoToDeclensionSettingsCommand),
                                        SettingsRow.BuildNavigation<SettingsViewModel>(
                                            "Conjugations",
                                            "\uE823", // Clock
                                            v => v.GoToConjugationSettingsCommand)
                                    ),

                                    // Feedback section
                                    SettingsSection.Build("Feedback",
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

                                    // Review explanation (only visible on supported platforms)
                                    new TextBlock()
                                        .Text("If you have a minute, please leave a review for Pāli Practice. Even one sentence is valuable feedback and it helps other Pāli learners discover the app on the store.")
                                        .FontSize(12)
                                        .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                                        .TextWrapping(TextWrapping.Wrap)
                                        .Margin(16, -12, 16, 16)
                                        .Visibility(Visibility.Collapsed)
                                        .Visibility(() => vm.IsStoreReviewAvailable)
                                )
                        )
                )
            )
        );
    }
}

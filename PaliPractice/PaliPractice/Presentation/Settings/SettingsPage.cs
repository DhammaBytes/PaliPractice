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
                                            "\uE8C8", // Repair/transformation
                                            v => v.GoToDeclensionSettingsCommand),
                                        SettingsRow.BuildNavigation<SettingsViewModel>(
                                            "Conjugations",
                                            "\uE823", // Clock
                                            v => v.GoToConjugationSettingsCommand)
                                    ),

                                    // Feedback section (only visible on supported platforms)
                                    new StackPanel()
                                        .Visibility(() => vm.IsStoreReviewAvailable)
                                        .Children(
                                            SettingsSection.Build("Feedback",
                                                SettingsRow.BuildAction<SettingsViewModel>(
                                                    "Write a review",
                                                    "\uE735", // FavoriteStar
                                                    v => v.RateAppCommand)
                                            ),
                                            new TextBlock()
                                                .Text("If you have a minute, please leave a review for Pāli Practice. Even one sentence is valuable feedback and it helps other Pāli learners discover the app on the store.")
                                                .FontSize(12)
                                                .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                                                .TextWrapping(TextWrapping.Wrap)
                                                .Margin(16, -12, 16, 16)
                                        )
                                )
                        )
                )
            )
        );
    }
}

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
                                        SettingsRow.BuildPlaceholder("Appearance")
                                    ),

                                    // Practice settings sections
                                    SettingsSection.Build("Practice",
                                        SettingsRow.BuildNavigation<SettingsViewModel>(
                                            "Conjugations",
                                            v => v.GoToConjugationSettingsCommand),
                                        SettingsRow.BuildNavigation<SettingsViewModel>(
                                            "Declensions",
                                            v => v.GoToDeclensionSettingsCommand)
                                    )
                                )
                        )
                )
            )
        );
    }
}

using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Settings.Controls;

namespace PaliPractice.Presentation.Settings;

public sealed partial class DeclensionSettingsPage : Page
{
    public DeclensionSettingsPage()
    {
        DeclensionSettingsPageMarkup.DataContext<ViewModels.DeclensionSettingsViewModel>(this, (page, vm) => page
            .NavigationCacheMode<DeclensionSettingsPage>(NavigationCacheMode.Required)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(new Grid()
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
                .RowDefinitions("Auto,*")
                .Children(
                    // Row 0: Title bar
                    AppTitleBar.Build<ViewModels.DeclensionSettingsViewModel>("Declension Settings", v => v.GoBackCommand),

                    // Row 1: Content
                    new ScrollViewer()
                        .Grid(row: 1)
                        .HorizontalScrollBarVisibility(ScrollBarVisibility.Disabled)
                        .HorizontalScrollMode(ScrollMode.Disabled)
                        .Content(
                            new StackPanel()
                                .MaxWidth(LayoutConstants.ContentMaxWidth)
                                .Children(
                                    // Gender section
                                    SettingsSection.Build("Gender",
                                        SettingsRow.BuildToggle<ViewModels.DeclensionSettingsViewModel>(
                                            "Masculine", v => v.Masculine),
                                        SettingsRow.BuildToggle<ViewModels.DeclensionSettingsViewModel>(
                                            "Feminine", v => v.Feminine),
                                        SettingsRow.BuildToggle<ViewModels.DeclensionSettingsViewModel>(
                                            "Neuter", v => v.Neuter)
                                    ),

                                    // Number section
                                    SettingsSection.Build("Number",
                                        SettingsRow.BuildToggle<ViewModels.DeclensionSettingsViewModel>(
                                            "Singular", v => v.Singular),
                                        SettingsRow.BuildToggle<ViewModels.DeclensionSettingsViewModel>(
                                            "Plural", v => v.Plural)
                                    ),

                                    // Case section
                                    SettingsSection.Build("Case",
                                        SettingsRow.BuildToggle<ViewModels.DeclensionSettingsViewModel>(
                                            "Nominative", v => v.Nominative),
                                        SettingsRow.BuildToggle<ViewModels.DeclensionSettingsViewModel>(
                                            "Accusative", v => v.Accusative),
                                        SettingsRow.BuildToggle<ViewModels.DeclensionSettingsViewModel>(
                                            "Instrumental", v => v.Instrumental),
                                        SettingsRow.BuildToggle<ViewModels.DeclensionSettingsViewModel>(
                                            "Dative", v => v.Dative),
                                        SettingsRow.BuildToggle<ViewModels.DeclensionSettingsViewModel>(
                                            "Ablative", v => v.Ablative),
                                        SettingsRow.BuildToggle<ViewModels.DeclensionSettingsViewModel>(
                                            "Genitive", v => v.Genitive),
                                        SettingsRow.BuildToggle<ViewModels.DeclensionSettingsViewModel>(
                                            "Locative", v => v.Locative),
                                        SettingsRow.BuildToggle<ViewModels.DeclensionSettingsViewModel>(
                                            "Vocative", v => v.Vocative)
                                    )
                                )
                        )
                )
            )
        );
    }
}

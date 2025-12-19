using PaliPractice.Presentation.Controls;

namespace PaliPractice.Presentation;

public sealed partial class DeclensionSettingsPage : Page
{
    public DeclensionSettingsPage()
    {
        this.DataContext<DeclensionSettingsViewModel>((page, vm) => page
            .NavigationCacheMode(NavigationCacheMode.Required)
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
                                .MaxWidth(450)
                                .Children(
                                    // Gender section
                                    SettingsSection.Build("Gender",
                                        SettingsRow.BuildToggle<DeclensionSettingsViewModel>(
                                            "Masculine", v => v.Masculine),
                                        SettingsRow.BuildToggle<DeclensionSettingsViewModel>(
                                            "Feminine", v => v.Feminine),
                                        SettingsRow.BuildToggle<DeclensionSettingsViewModel>(
                                            "Neuter", v => v.Neuter)
                                    ),

                                    // Number section
                                    SettingsSection.Build("Number",
                                        SettingsRow.BuildToggle<DeclensionSettingsViewModel>(
                                            "Singular", v => v.Singular),
                                        SettingsRow.BuildToggle<DeclensionSettingsViewModel>(
                                            "Plural", v => v.Plural)
                                    ),

                                    // Case section
                                    SettingsSection.Build("Case",
                                        SettingsRow.BuildToggle<DeclensionSettingsViewModel>(
                                            "Nominative", v => v.Nominative),
                                        SettingsRow.BuildToggle<DeclensionSettingsViewModel>(
                                            "Accusative", v => v.Accusative),
                                        SettingsRow.BuildToggle<DeclensionSettingsViewModel>(
                                            "Instrumental", v => v.Instrumental),
                                        SettingsRow.BuildToggle<DeclensionSettingsViewModel>(
                                            "Dative", v => v.Dative),
                                        SettingsRow.BuildToggle<DeclensionSettingsViewModel>(
                                            "Ablative", v => v.Ablative),
                                        SettingsRow.BuildToggle<DeclensionSettingsViewModel>(
                                            "Genitive", v => v.Genitive),
                                        SettingsRow.BuildToggle<DeclensionSettingsViewModel>(
                                            "Locative", v => v.Locative),
                                        SettingsRow.BuildToggle<DeclensionSettingsViewModel>(
                                            "Vocative", v => v.Vocative)
                                    )
                                )
                        )
                )
            )
        );
    }
}

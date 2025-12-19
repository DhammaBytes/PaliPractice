using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Settings.Controls;

namespace PaliPractice.Presentation.Settings;

public sealed partial class ConjugationSettingsPage : Page
{
    public ConjugationSettingsPage()
    {
        ConjugationSettingsPageMarkup.DataContext<ConjugationSettingsViewModel>(this, (page, vm) => page
            .NavigationCacheMode<ConjugationSettingsPage>(NavigationCacheMode.Required)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(new Grid()
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
                .RowDefinitions("Auto,*")
                .Children(
                    // Row 0: Title bar
                    AppTitleBar.Build<ConjugationSettingsViewModel>("Conjugation Settings", v => v.GoBackCommand),

                    // Row 1: Content
                    new ScrollViewer()
                        .Grid(row: 1)
                        .HorizontalScrollBarVisibility(ScrollBarVisibility.Disabled)
                        .HorizontalScrollMode(ScrollMode.Disabled)
                        .Content(
                            new StackPanel()
                                .MaxWidth(450)
                                .Children(
                                    // Person section
                                    SettingsSection.Build("Person",
                                        SettingsRow.BuildToggle<ConjugationSettingsViewModel>(
                                            "1st Person", v => v.FirstPerson),
                                        SettingsRow.BuildToggle<ConjugationSettingsViewModel>(
                                            "2nd Person", v => v.SecondPerson),
                                        SettingsRow.BuildToggle<ConjugationSettingsViewModel>(
                                            "3rd Person", v => v.ThirdPerson)
                                    ),

                                    // Number section
                                    SettingsSection.Build("Number",
                                        SettingsRow.BuildToggle<ConjugationSettingsViewModel>(
                                            "Singular", v => v.Singular),
                                        SettingsRow.BuildToggle<ConjugationSettingsViewModel>(
                                            "Plural", v => v.Plural)
                                    ),

                                    // Tense section
                                    SettingsSection.Build("Tense",
                                        SettingsRow.BuildToggle<ConjugationSettingsViewModel>(
                                            "Present", v => v.Present),
                                        SettingsRow.BuildToggle<ConjugationSettingsViewModel>(
                                            "Imperfect", v => v.Imperfect),
                                        SettingsRow.BuildToggle<ConjugationSettingsViewModel>(
                                            "Aorist", v => v.Aorist),
                                        SettingsRow.BuildToggle<ConjugationSettingsViewModel>(
                                            "Future", v => v.Future),
                                        SettingsRow.BuildToggle<ConjugationSettingsViewModel>(
                                            "Optative", v => v.Optative),
                                        SettingsRow.BuildToggle<ConjugationSettingsViewModel>(
                                            "Imperative", v => v.Imperative),
                                        SettingsRow.BuildToggle<ConjugationSettingsViewModel>(
                                            "Conditional", v => v.Conditional)
                                    )
                                )
                        )
                )
            )
        );
    }
}

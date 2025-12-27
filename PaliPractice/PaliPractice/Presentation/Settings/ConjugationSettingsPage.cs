using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Settings.Controls;

namespace PaliPractice.Presentation.Settings;

public sealed partial class ConjugationSettingsPage : Page
{
    public ConjugationSettingsPage()
    {
        ConjugationSettingsPageMarkup.DataContext<ViewModels.ConjugationSettingsViewModel>(this, (page, vm) => page
            .NavigationCacheMode<ConjugationSettingsPage>(NavigationCacheMode.Required)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(new Grid()
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
                .RowDefinitions("Auto,*")
                .Children(
                    // Row 0: Title bar
                    AppTitleBar.Build<ViewModels.ConjugationSettingsViewModel>("Conjugation Settings", v => v.GoBackCommand),

                    // Row 1: Content
                    new ScrollViewer()
                        .Grid(row: 1)
                        .HorizontalScrollBarVisibility(ScrollBarVisibility.Disabled)
                        .HorizontalScrollMode(ScrollMode.Disabled)
                        .Content(
                            new StackPanel()
                                .MaxWidth(LayoutConstants.ContentMaxWidth)
                                .Children(
                                    // Person section
                                    SettingsSection.Build("Person",
                                        SettingsRow.BuildToggle<ViewModels.ConjugationSettingsViewModel>(
                                            "1st Person", v => v.FirstPerson),
                                        SettingsRow.BuildToggle<ViewModels.ConjugationSettingsViewModel>(
                                            "2nd Person", v => v.SecondPerson),
                                        SettingsRow.BuildToggle<ViewModels.ConjugationSettingsViewModel>(
                                            "3rd Person", v => v.ThirdPerson)
                                    ),

                                    // Number section
                                    SettingsSection.Build("Number",
                                        SettingsRow.BuildToggle<ViewModels.ConjugationSettingsViewModel>(
                                            "Singular", v => v.Singular),
                                        SettingsRow.BuildToggle<ViewModels.ConjugationSettingsViewModel>(
                                            "Plural", v => v.Plural)
                                    ),

                                    // Tense section (aligned with current Tense enum)
                                    SettingsSection.Build("Tense",
                                        SettingsRow.BuildToggle<ViewModels.ConjugationSettingsViewModel>(
                                            "Present", v => v.Present),
                                        SettingsRow.BuildToggle<ViewModels.ConjugationSettingsViewModel>(
                                            "Imperative", v => v.Imperative),
                                        SettingsRow.BuildToggle<ViewModels.ConjugationSettingsViewModel>(
                                            "Optative", v => v.Optative),
                                        SettingsRow.BuildToggle<ViewModels.ConjugationSettingsViewModel>(
                                            "Future", v => v.Future)
                                    )
                                )
                        )
                )
            )
        );
    }
}

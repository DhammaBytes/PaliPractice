using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Settings.Controls;
using PaliPractice.Presentation.Settings.ViewModels;
using static PaliPractice.Presentation.Common.TextHelpers;

namespace PaliPractice.Presentation.Settings;

public sealed partial class AppearanceSettingsPage : Page
{
    public AppearanceSettingsPage()
    {
        AppearanceSettingsPageMarkup.DataContext<AppearanceSettingsViewModel>(this, (page, vm) => page
            .NavigationCacheMode<AppearanceSettingsPage>(NavigationCacheMode.Required)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(new Grid()
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
                .RowDefinitions("Auto,*")
                .Children(
                    // Row 0: Title bar
                    AppTitleBar.Build<AppearanceSettingsViewModel>("Appearance", v => v.GoBackCommand),

                    // Row 1: Content
                    new ScrollViewer()
                        .Grid(row: 1)
                        .HorizontalScrollBarVisibility(ScrollBarVisibility.Disabled)
                        .HorizontalScrollMode(ScrollMode.Disabled)
                        .Content(
                            new StackPanel()
                                .MaxWidth(LayoutConstants.ContentMaxWidth)
                                .Spacing(16)
                                .Children(
                                    // Theme section
                                    BuildThemeSection(vm)
                                )
                        )
                )
            )
        );
    }

    static StackPanel BuildThemeSection(AppearanceSettingsViewModel vm)
    {
        var radioButtons = new RadioButtons()
            .MaxColumns(1)
            .Margin(0, 8, 0, 0);

        // Add radio button options: 0=System, 1=Light, 2=Dark
        radioButtons.Items.Add(BuildRadioOption("System", "Follow your device's appearance settings"));
        radioButtons.Items.Add(BuildRadioOption("Light", "Always use light theme"));
        radioButtons.Items.Add(BuildRadioOption("Dark", "Always use dark theme"));

        // Bind selected index
        radioButtons.SelectedIndex(x => x.Binding(() => vm.ThemeIndex).TwoWay());

        return new StackPanel()
            .Spacing(4)
            .Children(
                RegularText()
                    .Text("Theme")
                    .FontSize(14)
                    .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                    .Margin(16, 8, 16, 4),
                new Border()
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
                    .Padding(16, 8)
                    .Child(radioButtons)
            );
    }

    static StackPanel BuildRadioOption(string title, string subtitle)
    {
        return new StackPanel()
            .Spacing(2)
            .Children(
                RegularText()
                    .Text(title)
                    .FontSize(16),
                RegularText()
                    .Text(subtitle)
                    .FontSize(13)
                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
            );
    }
}

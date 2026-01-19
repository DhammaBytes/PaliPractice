using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Main.ViewModels;
using PaliPractice.Themes;
using static PaliPractice.Presentation.Common.Text.TextHelpers;
using static PaliPractice.Presentation.Common.ShadowHelper;

namespace PaliPractice.Presentation.Main;

public sealed partial class StartPage : Page
{
    public StartPage()
    {
        this.DataContext<StartViewModel>((page, vm) => page
            .NavigationCacheMode(NavigationCacheMode.Required)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(new Grid()
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
                .RowDefinitions("Auto,*")
                .Children(
                    // Main content area
                    new Grid()
                        .Grid(row: 1)
                        .Margin(20)
                        .Children(
                            new StackPanel()
                                .VerticalAlignment(VerticalAlignment.Center)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .Margin(0, 0, 0, 48) // Shift center up by 24pt
                                .Spacing(32)
                                .MaxWidth(400)
                                .Children(
                                    // App title (no icon, larger font)
                                    PaliText()
                                        .Text("Pāli Practice")
                                        .FontSize(48)
                                        .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                        .TextAlignment(TextAlignment.Center)
                                        .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush")),

                                    // Practice buttons
                                    new StackPanel()
                                        .Spacing(16)
                                        .Children(
                                            // Declension Practice Button
                                            // Command must be set at call site for source generator to see the binding
                                            StartPrimaryButtonShadow(
                                                BuildPracticeButton(MenuIcons.Nouns, "Nouns & Cases", "Declension Practice")
                                                    .Command(() => vm.GoToDeclensionCommand)),

                                            // Conjugation Practice Button
                                            StartPrimaryButtonShadow(
                                                BuildPracticeButton(MenuIcons.Verbs, "Verbs & Tenses", "Conjugation Practice")
                                                    .Command(() => vm.GoToConjugationCommand)),

                                            // Settings Button
                                            StartSecondaryButtonShadow(
                                                BuildSecondaryButton(MenuIcons.Settings, "Settings", iconHeight: 30)
                                                    .Command(() => vm.GoToSettingsCommand)),

                                            // Stats and Help row (side by side)
                                            new Grid()
                                                .ColumnDefinitions("*,16,*")
                                                .Children(
                                                    // Stats Button
                                                    StartSecondaryButtonShadow(
                                                        BuildSecondaryButton(MenuIcons.Stats, "Stats", iconHeight: 27, centerContent: true, iconVerticalOffset: -2)
                                                            .Command(() => vm.GoToStatisticsCommand))
                                                        .Grid(column: 0),

                                                    // Help Button
                                                    StartSecondaryButtonShadow(
                                                        BuildSecondaryButton(MenuIcons.Help, "Help", iconHeight: 27, centerContent: true)
                                                            .Command(() => vm.GoToHelpCommand))
                                                        .Grid(column: 2)
                                                )
                                )
                        )
                )
            )
        ));
    }

    /// <summary>
    /// Builds a primary practice button (Nouns & Cases, Verbs & Tenses).
    /// Uses NavigationButtonVariantBrush fill. Shadow is applied at call site via StartNavShadow wrapper.
    /// Note: Command must be set at call site for source generator to see the binding.
    /// </summary>
    static SquircleButton BuildPracticeButton(string iconPath, string title, string subtitle)
    {
        return new SquircleButton()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .HorizontalContentAlignment(HorizontalAlignment.Left)
            .RadiusMode(SquircleRadiusMode.ButtonLarge)
            .Fill(ThemeResource.Get<Brush>("NavigationButtonVariantBrush"))
            .Stroke(ThemeResource.Get<Brush>("NavigationButtonVariantOutlineBrush"))
            .StrokeThickness(LayoutConstants.Sizes.StartPageStrokeThickness)
            .Padding(20, 20) // 4pt extra vertical padding
            .Child(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .Spacing(20) // 20pt between icon and text
                .Children(
                    new BitmapIcon()
                        .UriSource(new Uri(iconPath))
                        .ShowAsMonochrome(true)
                        .Height(30) // 25% larger than 24
                        .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush")),
                    new StackPanel()
                        .Spacing(0)
                        .Children(
                            RegularText()
                                .Text(title)
                                .FontSize(22)
                                .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                                .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush")),
                            RegularText()
                                .Text(subtitle)
                                .FontSize(16)
                                .Opacity(0.6)
                                .FontWeight(Microsoft.UI.Text.FontWeights.Medium)
                                .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
                                .Margin(0, -4, 0, 0) // 4pt tighter spacing
                        )
                ));
    }

    /// <summary>
    /// Builds a secondary button (Settings, Stats, Help).
    /// Uses BackgroundBrush fill with OutlineBrush stroke. Shadow is applied at call site via StartNavigationButtonShadow wrapper.
    /// Note: Command must be set at call site for source generator to see the binding.
    /// </summary>
    static SquircleButton BuildSecondaryButton(string iconPath, string label, double iconHeight = 28, bool centerContent = false, double iconVerticalOffset = 0)
    {
        return new SquircleButton()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .HorizontalContentAlignment(centerContent ? HorizontalAlignment.Center : HorizontalAlignment.Left)
            .RadiusMode(SquircleRadiusMode.ButtonLarge)
            .Fill(ThemeResource.Get<Brush>("NavigationButtonBrush"))
            .Stroke(ThemeResource.Get<Brush>("NavigationButtonOutlineBrush"))
            .StrokeThickness(LayoutConstants.Sizes.StartPageStrokeThickness)
            .Padding(centerContent ? 14 : 20, 16) // 14pt for short buttons, 20pt for long
            .Child(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .Spacing(centerContent ? 8 : 20) // 8pt for short buttons, 20pt for long
                .Children(
                    new BitmapIcon()
                        .UriSource(new Uri(iconPath))
                        .ShowAsMonochrome(true)
                        .Height(iconHeight)
                        .Margin(0, iconVerticalOffset, 0, -iconVerticalOffset)
                        .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush")),
                    RegularText()
                        .Text(label)
                        .FontSize(22)
                        .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                        .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                ));
    }
}

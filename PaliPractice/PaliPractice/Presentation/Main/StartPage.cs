using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Main.ViewModels;
using PaliPractice.Themes;
using static PaliPractice.Presentation.Common.TextHelpers;
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
                                            StartNavShadow(
                                                BuildPracticeButton(MenuIcons.Nouns, "Nouns & Cases", "Declension Practice")
                                                    .Command(() => vm.GoToDeclensionCommand)),

                                            // Conjugation Practice Button
                                            StartNavShadow(
                                                BuildPracticeButton(MenuIcons.Verbs, "Verbs & Tenses", "Conjugation Practice")
                                                    .Command(() => vm.GoToConjugationCommand)),

                                            // Settings Button
                                            StartNavigationButtonShadow(
                                                BuildSecondaryButton(MenuIcons.Settings, "Settings", iconHeight: 28)
                                                    .Command(() => vm.GoToSettingsCommand)),

                                            // Stats and Help row (side by side)
                                            new Grid()
                                                .ColumnDefinitions("*,16,*")
                                                .Children(
                                                    // Stats Button
                                                    StartNavigationButtonShadow(
                                                        BuildSecondaryButton(MenuIcons.Stats, "Stats", iconHeight: 24, centerContent: true)
                                                            .Command(() => vm.GoToStatisticsCommand))
                                                        .Grid(column: 0),

                                                    // Help Button
                                                    StartNavigationButtonShadow(
                                                        BuildSecondaryButton(MenuIcons.Help, "Help", iconHeight: 25, centerContent: true)
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
            .StrokeThickness(3) // Thicker to compensate for 10% opacity stroke
            .Padding(20, 16)
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
    static SquircleButton BuildSecondaryButton(string iconPath, string label, double iconHeight = 28, bool centerContent = false)
    {
        return new SquircleButton()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .HorizontalContentAlignment(centerContent ? HorizontalAlignment.Center : HorizontalAlignment.Left)
            .RadiusMode(SquircleRadiusMode.ButtonLarge)
            .Fill(ThemeResource.Get<Brush>("NavigationButtonBrush"))
            .Stroke(ThemeResource.Get<Brush>("NavigationButtonOutlineBrush"))
            .StrokeThickness(LayoutConstants.Sizes.ButtonStrokeThickness)
            .Padding(centerContent ? 14 : 20, 16) // 14pt for short buttons, 20pt for long
            .Child(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .Spacing(centerContent ? 10 : 20) // 10pt for short buttons, 20pt for long
                .Children(
                    new BitmapIcon()
                        .UriSource(new Uri(iconPath))
                        .ShowAsMonochrome(true)
                        .Height(iconHeight)
                        .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush")),
                    RegularText()
                        .Text(label)
                        .FontSize(22)
                        .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                        .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                ));
    }
}

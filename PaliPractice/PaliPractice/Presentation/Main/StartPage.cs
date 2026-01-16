using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Main.ViewModels;
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
                                                BuildPracticeButton("\uE8D4", "Nouns & Cases", "Declension Practice")
                                                    .Command(() => vm.GoToDeclensionCommand)),

                                            // Conjugation Practice Button
                                            StartNavShadow(
                                                BuildPracticeButton("\uE8F4", "Verbs & Tenses", "Conjugation Practice")
                                                    .Command(() => vm.GoToConjugationCommand)),

                                            // Settings Button
                                            StartNavigationButtonShadow(
                                                BuildSecondaryButton("\uE713", "Settings")
                                                    .Command(() => vm.GoToSettingsCommand)),

                                            // Stats and Help row (side by side)
                                            new Grid()
                                                .ColumnDefinitions("*,16,*")
                                                .Children(
                                                    // Stats Button
                                                    StartNavigationButtonShadow(
                                                        BuildSecondaryButton("\uE9D9", "Stats", centerContent: true)
                                                            .Command(() => vm.GoToStatisticsCommand))
                                                        .Grid(column: 0),

                                                    // Help Button
                                                    StartNavigationButtonShadow(
                                                        BuildSecondaryButton("\uE897", "Help", centerContent: true)
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
    static SquircleButton BuildPracticeButton(string glyph, string title, string subtitle)
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
                .Spacing(12)
                .Children(
                    new FontIcon()
                        .Glyph(glyph)
                        .FontSize(24)
                        .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush")),
                    new StackPanel()
                        .Spacing(1) // Tighter spacing between title and subtitle (40% smaller)
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
                        )
                ));
    }

    /// <summary>
    /// Builds a secondary button (Settings, Stats, Help).
    /// Uses BackgroundBrush fill with OutlineBrush stroke. Shadow is applied at call site via StartNavigationButtonShadow wrapper.
    /// Note: Command must be set at call site for source generator to see the binding.
    /// </summary>
    static SquircleButton BuildSecondaryButton(string glyph, string label, bool centerContent = false)
    {
        return new SquircleButton()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .HorizontalContentAlignment(centerContent ? HorizontalAlignment.Center : HorizontalAlignment.Left)
            .RadiusMode(SquircleRadiusMode.ButtonLarge)
            .Fill(ThemeResource.Get<Brush>("NavigationButtonBrush"))
            .Stroke(ThemeResource.Get<Brush>("NavigationButtonOutlineBrush"))
            .StrokeThickness(LayoutConstants.Sizes.ButtonStrokeThickness)
            .Padding(centerContent ? 16 : 20, 16)
            .Child(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .Spacing(centerContent ? 6 : 12) // Tighter spacing for centered buttons
                .Children(
                    new FontIcon()
                        .Glyph(glyph)
                        .FontSize(24)
                        .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush")),
                    RegularText()
                        .Text(label)
                        .FontSize(22)
                        .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                        .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                ));
    }
}

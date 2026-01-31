using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Main.ViewModels;
using PaliPractice.Themes;
using PaliPractice.Themes.Icons;
using static PaliPractice.Presentation.Common.Text.TextHelpers;
using static PaliPractice.Presentation.Common.ShadowHelper;

namespace PaliPractice.Presentation.Main;

public sealed partial class StartPage : Page
{
    readonly BitmapIcon _lotusTop;
    readonly BitmapIcon _lotusBottom;
    StackPanel _contentStack = null!;

    public StartPage()
    {
        _lotusTop = new BitmapIcon()
            .UriSource(new Uri(MenuIcons.LotusTop))
            .ShowAsMonochrome(true)
            .Foreground(ThemeResource.Get<Brush>("NavigationButtonOutlineBrush"))
            .Width(180)
            .HorizontalAlignment(HorizontalAlignment.Center);

        _lotusBottom = new BitmapIcon()
            .UriSource(new Uri(MenuIcons.LotusBottom))
            .ShowAsMonochrome(true)
            .Foreground(ThemeResource.Get<Brush>("NavigationButtonOutlineBrush"))
            .Width(288)
            .Margin(IsDesktop ? new Thickness(0, 0, 12, 12) : new Thickness(0, 0, 8, 20))
            .HorizontalAlignment(HorizontalAlignment.Right)
            .VerticalAlignment(VerticalAlignment.Bottom);

        this.DataContext<StartViewModel>((page, vm) => page
            .NavigationCacheMode(NavigationCacheMode.Required)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(PageFadeIn.Wrap(page,
                new Grid()
                    .Children(
                        // Bottom lotus decoration (outside SafeArea, screen-edge aligned, lowest Z-order)
                        _lotusBottom,

                        // SafeArea content
                        new Grid()
                            .SafeArea(SafeArea.InsetMask.VisibleBounds)
                            .RowDefinitions("Auto,*")
                            .Children(
                                // Main content area
                                new Grid()
                                    .Grid(row: 1)
                                    .Margin(20)
                                    .Children(
                                        (_contentStack = new StackPanel()
                                            .VerticalAlignment(VerticalAlignment.Center)
                                            .HorizontalAlignment(HorizontalAlignment.Center)
                                            .Spacing(32)
                                            .MaxWidth(400))
                                            .Children(
                                                // Title group: lotus + title
                                                new StackPanel()
                                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                                    .Spacing(24)
                                                    .Children(
                                                        _lotusTop,
                                                        PaliText()
                                                            .Text("Pāli Practice")
                                                            .FontSize(48)
                                                            .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                                                            .HorizontalAlignment(HorizontalAlignment.Center)
                                                            .TextAlignment(TextAlignment.Center)
                                                            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                                                    ),

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
                    )
            )));

        AttachResponsiveHandler();
    }

    void AttachResponsiveHandler()
    {
        var window = App.MainWindow;
        if (window is null) return;

        Update();
        window.SizeChanged += (_, _) => Update();
        return;

        void Update()
        {
            var heightClass = LayoutConstants.GetCurrentHeightClass();
            var width = window.Bounds.Width;

            // Top lotus: hidden on Short/Minimum
            _lotusTop.Visibility = heightClass is HeightClass.Short or HeightClass.Minimum
                ? Visibility.Collapsed
                : Visibility.Visible;

            // Push content stack higher on tall screens via bottom margin
            var bottomShift = heightClass switch
            {
                HeightClass.Tall => 100,
                HeightClass.Medium => 80,
                HeightClass.Short => 48,
                _ => 24
            };
            _contentStack.Margin(0, 0, 0, bottomShift);

            // Bottom lotus: 70% of screen width, max 280px
            _lotusBottom.Width = Math.Min(width * 0.7, 280);
        }
    }

    static bool IsDesktop => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux();

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

using System.Linq.Expressions;
using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Practice.Common;
using PaliPractice.Presentation.Practice.ViewModels;
using PaliPractice.Themes;
using static PaliPractice.Presentation.Common.Text.TextHelpers;
using static PaliPractice.Presentation.Common.ShadowHelper;
using ExampleCarouselViewModel = PaliPractice.Presentation.Practice.ViewModels.Common.ExampleCarouselViewModel;
using PracticeIcons = PaliPractice.Themes.Icons.PracticeIcons;

namespace PaliPractice.Presentation.Practice;

#region Configuration Records

/// <summary>
/// Configuration for building a practice page layout.
/// </summary>
public record PracticePageConfig<TVM>(
    string Title,
    Expression<Func<TVM, FlashCardViewModel>> FlashCardPath,
    Expression<Func<TVM, string>> AnswerStemPath,
    Expression<Func<TVM, string>> AnswerEndingPath,
    Expression<Func<TVM, string>> AlternativeFormsPath,
    Expression<Func<TVM, bool>> IsRevealedPath,
    Expression<Func<TVM, ExampleCarouselViewModel>> CarouselPath,
    Expression<Func<TVM, ICommand>> GoBackCommandPath,
    Expression<Func<TVM, ICommand>> GoToHistoryCommandPath,
    Expression<Func<TVM, ICommand>> GoToInflectionTableCommandPath,
    Expression<Func<TVM, ICommand>> RevealCommandPath,
    Expression<Func<TVM, ICommand>> HardCommandPath,
    Expression<Func<TVM, ICommand>> EasyCommandPath,
    Expression<Func<TVM, string>> DailyGoalTextPath,
    Expression<Func<TVM, double>> DailyProgressPath
);

/// <summary>
/// Badge set with elements for responsive sizing.
/// </summary>
public record BadgeSet(
    StackPanel Panel,
    SquircleBorder[] Borders,
    TextBlock[] TextBlocks,
    BufferedBitmapIcon[] Icons,
    StackPanel[] ContentPanels
);

#endregion

/// <summary>
/// Unified builder for practice pages.
/// Consolidates all UI construction for DeclensionPracticePage and ConjugationPracticePage.
/// </summary>
public static class PracticePageBuilder
{
    #region Page Layout

    /// <summary>
    /// Builds the complete practice page layout.
    /// </summary>
    public static Grid BuildPage<TVM>(
        PracticePageConfig<TVM> config,
        BadgeSet badges,
        UIElement? hintElement,
        ResponsiveElements elements,
        HeightClass heightClass)
    {
        var fonts = LayoutConstants.PracticeFontSizes.Get(heightClass);

        // Build word display (Viewbox shrinks long words automatically)
        var (wordViewbox, wordTextBlock) = BuildCardWord(config.FlashCardPath, fonts, heightClass);
        elements.WordViewbox = wordViewbox;
        elements.WordTextBlock = wordTextBlock;
        elements.BadgesPanel = badges.Panel;
        elements.BadgeBorders.AddRange(badges.Borders);
        elements.BadgeTextBlocks.AddRange(badges.TextBlocks);
        elements.BadgeIcons.AddRange(badges.Icons);
        elements.BadgeContentPanels.AddRange(badges.ContentPanels);

        // Build answer section (Viewbox shrinks long words automatically)
        var (answerViewbox, secondaryViewbox, placeholder, answerContainer, answerTextBlock, secondaryTextBlock, answerSpacer, answerContentPanel) = BuildAnswerSection(
            config.AnswerStemPath,
            config.AnswerEndingPath,
            config.AlternativeFormsPath,
            config.IsRevealedPath,
            fonts,
            heightClass);
        elements.AnswerViewbox = answerViewbox;
        elements.AnswerSecondaryViewbox = secondaryViewbox;
        elements.AnswerPlaceholder = placeholder;
        elements.AnswerTextBlock = answerTextBlock;
        elements.AnswerSecondaryTextBlock = secondaryTextBlock;
        elements.AnswerContainer = answerContainer;
        elements.AnswerSpacer = answerSpacer;
        elements.AnswerContentPanel = answerContentPanel;

        // Build translation carousel
        var (translationText, translationInnerBorder, translationContainer, translationContentPanel) = BuildTranslationCarousel(
            config.CarouselPath, config.IsRevealedPath, fonts, heightClass);
        elements.TranslationTextBlock = translationText;
        elements.TranslationInnerBorder = translationInnerBorder;
        elements.TranslationContainer = translationContainer;
        elements.TranslationContentPanel = translationContentPanel;

        // Build example section
        var (exampleText, referenceText, exampleContainer) = BuildExampleSection(config.CarouselPath, fonts);
        elements.SuttaExampleTextBlock = exampleText;
        elements.SuttaReferenceTextBlock = referenceText;

        // Build navigation
        var (buttonElements, navContainer) = BuildNavigation(
            config.RevealCommandPath,
            config.HardCommandPath,
            config.EasyCommandPath,
            config.IsRevealedPath,
            fonts,
            heightClass);
        elements.ButtonElements.AddRange(buttonElements);


#if DEBUG
        // Debug text for size bracket (in header middle) - hidden in screenshot mode
        TextBlock? debugText = null;
        if (!ScreenshotMode.IsEnabled)
        {
            debugText = RegularText()
                .Text("Size: --")
                .FontSize(fonts.Debug)
                .Foreground(ThemeResource.Get<Brush>("OnSurfaceSecondaryBrush"));
            elements.DebugTextBlock = debugText;
        }
#else
        TextBlock? debugText = null;
#endif

        
        // Assemble card children
        var cardChildren = new List<UIElement>
        {
            BuildCardHeader(config.FlashCardPath, debugText, fonts),
            wordViewbox,
            badges.Panel
        };

        if (hintElement is not null)
            cardChildren.Add(hintElement);

        cardChildren.Add(answerContainer);

        // Build card border (asymmetric padding: smaller top/bottom)
        // Shadow is applied via CardShadow wrapper when adding to grid
        var cardPadSides = LayoutConstants.Gaps.CardHorizontalPadding(heightClass);
        var cardPadTop = LayoutConstants.Gaps.CardPaddingTop(heightClass);
        var cardPadBottom = LayoutConstants.Gaps.CardPaddingBottom(heightClass);
        var cardStackPanel = new StackPanel()
            .Padding(new Thickness(cardPadSides, cardPadTop, cardPadSides, cardPadBottom))
            .Spacing(LayoutConstants.Gaps.CardContentSpacing(heightClass))
            .Children(cardChildren.ToArray());
        var cardBorder = new SquircleBorder()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Fill(ThemeResource.Get<Brush>("SurfaceBrush"))
            .Child(cardStackPanel);
        elements.CardBorder = cardBorder;
        elements.CardStackPanel = cardStackPanel;

        // Set up dynamic widths based on card size
        cardBorder.SizeChanged += (_, e) =>
        {
            // Get current height class for responsive calculations
            var currentHeightClass = LayoutConstants.GetCurrentHeightClass();
            var currentFonts = LayoutConstants.PracticeFontSizes.Get(currentHeightClass);

            // Translation width is card width minus arrow button space (50px each side)
            const double arrowHitboxWidth = 50.0;
            var availableWidthForTranslation = e.NewSize.Width - (2 * arrowHitboxWidth);
            var textWidth = availableWidthForTranslation - (LayoutConstants.Gaps.TranslationPaddingH(currentHeightClass) * 2);

            elements.AnswerPlaceholder?.Width = e.NewSize.Width * LayoutConstants.AnswerPlaceholderWidthRatio;

            // Translation border stretches to fill Star column (space between arrows)
            // Example container should match translation width (card width minus arrow space)
            exampleContainer.MaxWidth = availableWidthForTranslation;

            // Update text balancing width in the carousel ViewModel
            if (translationText.DataContext is ExampleCarouselViewModel carousel)
            {
                carousel.UpdateAvailableWidth(textWidth, currentFonts.Translation, translationText.FontFamily);
            }

            // Check if badges need abbreviation based on available width
            var cardPadSides = LayoutConstants.Gaps.CardHorizontalPadding(currentHeightClass);
            var availableBadgeWidth = e.NewSize.Width - (2 * cardPadSides);

            if (cardBorder.DataContext is PracticeViewModelBase vm)
            {
                var hasVoice = vm is ConjugationPracticeViewModel { IsReflexive: true };
                vm.UseAbbreviatedLabels = BadgeWidthMeasurer.ShouldAbbreviate(
                    availableBadgeWidth, vm.PracticeTypePublic, hasVoice, currentHeightClass);
            }
        };

        // Compute content width from window (clamped to max)
        var contentPadding = LayoutConstants.Gaps.ContentSpacing(heightClass);
        var windowWidth = App.MainWindow?.Bounds.Width ?? LayoutConstants.ContentMaxWidth;
        var contentWidth = Math.Min(windowWidth, LayoutConstants.ContentMaxWidth);

        // Wrap example in ScrollViewer for overflow handling
        var exampleScrollViewer = new ScrollViewer()
            .VerticalScrollBarVisibility(ScrollBarVisibility.Hidden)
            .HorizontalScrollBarVisibility(ScrollBarVisibility.Disabled)
            .VerticalAlignment(VerticalAlignment.Top)
            .Content(exampleContainer);

        // Fade overlay to hint at scrollable content (theme-aware gradient)
        var fadeOverlay = new FadeOverlay();

        // Detect overflow and show/hide fade overlay
        exampleScrollViewer.SizeChanged += (_, _) => UpdateFadeOverlay();
        exampleContainer.SizeChanged += (_, _) => UpdateFadeOverlay();
        exampleScrollViewer.ViewChanged += (_, _) => UpdateFadeOverlay();

        // Container that fills available space with ScrollViewer and fade overlay
        var exampleArea = new Grid()
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Children(exampleScrollViewer, fadeOverlay);
        elements.ExampleArea = exampleArea;

        // Build content area with explicit width and uniform padding
        var contentArea = new Grid()
            .RowDefinitions("Auto,Auto,*")
            .Width(contentWidth)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Padding(contentPadding, contentPadding, contentPadding, contentPadding)
            .Children(
                CardShadow(cardBorder).Grid(row: 0),
                translationContainer.Margin(0, contentPadding, 0, 0).Grid(row: 1),
                exampleArea.Margin(0, contentPadding, 0, 0).Grid(row: 2)
            );
        elements.ContentArea = contentArea;

        // Build title bar with "All Forms" button and daily goal bar
        var titleBar = AppTitleBar.BuildWithCenterButton<TVM>(
            config.GoBackCommandPath,
            config.GoToInflectionTableCommandPath,
            config.GoToHistoryCommandPath);
        var dailyGoalBar = BuildDailyGoalBar(config.DailyGoalTextPath, config.DailyProgressPath, heightClass, fonts);
        elements.TitleBar = titleBar;
        elements.DailyGoalBar = dailyGoalBar;

        // Main page grid
        return new Grid()
            .SafeArea(SafeArea.InsetMask.VisibleBounds)
            .RowDefinitions("Auto,*,Auto,Auto")
            .Children(
                titleBar.Grid(row: 0),
                contentArea.Grid(row: 1),
                navContainer.Grid(row: 2),
                dailyGoalBar.Grid(row: 3)
            );

        void UpdateFadeOverlay()
        {
            var hasOverflow = exampleScrollViewer.ScrollableHeight > 0;
            var isAtBottom = exampleScrollViewer.VerticalOffset >= exampleScrollViewer.ScrollableHeight - 4;
            fadeOverlay.Opacity = hasOverflow && !isAtBottom ? 1 : 0;
        }
    }

    #endregion

    #region Card Components

    /// <summary>
    /// Builds the header row: [√Root] [Debug] [Level: N]
    /// </summary>
    static Grid BuildCardHeader<TVM>(
        Expression<Func<TVM, FlashCardViewModel>> cardPath,
        TextBlock? debugText,
        LayoutConstants.PracticeFontSizes fonts)
    {
        return new Grid()
            .ColumnDefinitions("Auto,*,Auto")
            .Children(
                // Left: √Root in gray text (1px up to align with Level due to font difference)
                new StackPanel()
                    .Orientation(Orientation.Horizontal)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Margin(0, -1, 0, 0)
                    .Scope(cardPath)
                    .Children(
                        PaliText()
                            .TextWithin<FlashCardViewModel>(c => c.Root)
                            .FontSize(fonts.PaliRoot)
                            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
                            .Opacity(0.6)
                    )
                    .Grid(column: 0),
                (debugText ?? new TextBlock())
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Grid(column: 1),
                // Right: Level: N
                new StackPanel()
                    .Orientation(Orientation.Horizontal)
                    .HorizontalAlignment(HorizontalAlignment.Right)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Scope(cardPath)
                    .Children(
                        RegularText()
                            .Text("Level: ")
                            .FontSize(fonts.Level)
                            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
                            .Opacity(0.6),
                        RegularText()
                            .TextWithin<FlashCardViewModel>(c => c.LevelText)
                            .FontSize(fonts.Level)
                            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
                            .Opacity(0.6)
                    )
                    .Grid(column: 2)
            );
    }

    /// <summary>
    /// Builds the main word display with Viewbox for shrinking long words.
    /// Returns both the Viewbox and TextBlock for responsive font sizing.
    /// </summary>
    static (Viewbox viewbox, TextBlock textBlock) BuildCardWord<TVM>(
        Expression<Func<TVM, FlashCardViewModel>> cardPath,
        LayoutConstants.PracticeFontSizes fonts,
        HeightClass heightClass)
    {
        var textBlock = PaliText()
            .Scope(cardPath)
            .TextWithin<FlashCardViewModel>(c => c.Question)
            .FontSize(fonts.Word)
            .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .TextAlignment(TextAlignment.Center)
            .TextWrapping(TextWrapping.NoWrap)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"));

        var viewbox = new Viewbox()
            .Stretch(Stretch.Uniform)
            .StretchDirection(StretchDirection.DownOnly)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .MaxHeight(fonts.Word * 1.5) // Limit height to prevent over-shrinking
            .Margin(0, LayoutConstants.Gaps.WordMarginTop(heightClass), 0, LayoutConstants.Gaps.WordMarginBottom(heightClass))
            .Child(textBlock);

        return (viewbox, textBlock);
    }

    /// <summary>
    /// Builds the answer section with spacer, content, and placeholder.
    /// Returns Viewbox wrappers that shrink long words automatically,
    /// plus TextBlocks for responsive font sizing.
    /// </summary>
    static (Viewbox answerViewbox, Viewbox secondaryViewbox, Border placeholder, Grid answerContainer, TextBlock answerTextBlock, TextBlock secondaryTextBlock, StackPanel answerSpacer, StackPanel answerContent) BuildAnswerSection<TVM>(
        Expression<Func<TVM, string>> answerStemPath,
        Expression<Func<TVM, string>> answerEndingPath,
        Expression<Func<TVM, string>> alternativeFormsPath,
        Expression<Func<TVM, bool>> isRevealedPath,
        LayoutConstants.PracticeFontSizes fonts,
        HeightClass heightClass)
    {
        // Choose answer display style: toggle between these two lines to test
        // var answerTextBlock = CreateBoldEndingAnswer(answerStemPath, answerEndingPath);
        var answerTextBlock = CreateColorEndingAnswer(answerStemPath, answerEndingPath, fonts);

        // Wrap answer in Viewbox to shrink long words
        var answerViewbox = new Viewbox()
            .Stretch(Stretch.Uniform)
            .StretchDirection(StretchDirection.DownOnly)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .MaxHeight(fonts.Answer * 1.5)
            .Child(answerTextBlock);

        var alternativeFormsTextBlock = PaliText()
            .FontSize(fonts.AnswerSecondary)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .TextAlignment(TextAlignment.Center)
            .TextWrapping(TextWrapping.NoWrap)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
            .Text<TVM>(alternativeFormsPath)
            .StringToVisibility<TextBlock, TVM>(alternativeFormsPath);

        // Wrap alternatives in Viewbox to shrink long forms
        var alternativesViewbox = new Viewbox()
            .Stretch(Stretch.Uniform)
            .StretchDirection(StretchDirection.DownOnly)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .MaxHeight(fonts.AnswerSecondary * 1.5)
            .StringToVisibility<Viewbox, TVM>(alternativeFormsPath)
            .Child(alternativeFormsTextBlock);

        var answerSpacer = new StackPanel()
            .Spacing(LayoutConstants.Gaps.AnswerLineSpacing(heightClass))
            .Opacity(0)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Children(
                PaliText()
                    .FontSize(fonts.Answer)
                    .Text("X"),
                PaliText()
                    .FontSize(fonts.AnswerSecondary)
                    .FontWeight(Microsoft.UI.Text.FontWeights.Medium)
                    .Text("X")
            );

        // Invisible spacer that appears when there's only one answer line (no alternatives).
        // Since the StackPanel is centered, this pushes the answer up by half the spacer height.
        var singleLineSpacer = new Border()
            .Height(8) // 8px spacer → 4px upward shift when centered
            .StringToVisibility<Border, TVM>(alternativeFormsPath, invert: true);

        var answerContent = new StackPanel()
            .Spacing(LayoutConstants.Gaps.AnswerLineSpacing(heightClass))
            .HorizontalAlignment(HorizontalAlignment.Center)
            .VerticalAlignment(VerticalAlignment.Center)
            .BoolToVisibility<StackPanel, TVM>(isRevealedPath)
            .Children(answerViewbox, alternativesViewbox, singleLineSpacer);

        // Placeholder uses relative width via binding or we track width changes
        var answerPlaceholder = new Border()
            .Height(LayoutConstants.Sizes.PlaceholderHeight)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .VerticalAlignment(VerticalAlignment.Center)
            .BorderBrush(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
            .Opacity(0.75)
            .BorderThickness(0, 0, 0, LayoutConstants.Sizes.AnswerLineThickness)
            .BoolToVisibility<Border, TVM>(isRevealedPath, invert: true);

        var answerContainer = new Grid()
            .Margin(0, LayoutConstants.Gaps.AnswerMarginTop(heightClass), 0, 0)
            .Children(answerSpacer, answerContent, answerPlaceholder);

        return (answerViewbox, alternativesViewbox, answerPlaceholder, answerContainer, answerTextBlock, alternativeFormsTextBlock, answerSpacer, answerContent);
    }

    static TextBlock CreateColorEndingAnswer<TVM>(
        Expression<Func<TVM, string>> answerStemPath,
        Expression<Func<TVM, string>> answerEndingPath,
        LayoutConstants.PracticeFontSizes fonts)
    {
        var stemRun = new Run()
            .FontFamily(PaliFont)
            .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"));
        stemRun.SetBinding(Run.TextProperty, Bind.Path(answerStemPath));

        var endingRun = new Run()
            .FontFamily(PaliFont)
            .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
            .Foreground(ThemeResource.Get<Brush>("AccentEndingBrush"));
        endingRun.SetBinding(Run.TextProperty, Bind.Path(answerEndingPath));

        var textBlock = PaliText()
            .FontSize(fonts.Answer)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .TextAlignment(TextAlignment.Center);
        textBlock.Inlines.Add(stemRun);
        textBlock.Inlines.Add(endingRun);

        return textBlock;
    }

    #endregion

    #region Badges

    /// <summary>
    /// Builds a grammar badge with tintable PNG icon and text.
    /// Uses BufferedBitmapIcon for seamless icon transitions without blinking.
    /// </summary>
    public static (BufferedBitmapIcon icon, TextBlock text, SquircleBorder badge, StackPanel contentPanel) BuildBadge<TVM>(
        HeightClass heightClass,
        Expression<Func<TVM, string?>> iconPath,
        Expression<Func<TVM, string>> labelPath,
        Expression<Func<TVM, Color>> colorPath)
    {
        var fonts = LayoutConstants.PracticeFontSizes.Get(heightClass);

        // Double-buffered icon prevents blinking during card transitions
        var icon = new BufferedBitmapIcon
        {
            IconHeight = fonts.Badge,
            IconForeground = Application.Current.Resources["OnSurfaceBrush"] as Brush,
            Width = fonts.Badge,  // Fixed width (all icons are square)
            VerticalAlignment = VerticalAlignment.Center
        };
        icon.SetBinding(BufferedBitmapIcon.SourceProperty, Bind.Path(iconPath));

        var text = RegularText()
            .FontSize(fonts.Badge)
            .FontWeight(Microsoft.UI.Text.FontWeights.Medium)
            .VerticalAlignment(VerticalAlignment.Center)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
            .Text<TVM>(labelPath);

        var badgeContentPanel = new StackPanel()
            .Orientation(Orientation.Horizontal)
            .Spacing(LayoutConstants.Gaps.BadgeIconTextSpacing(heightClass))
            .Padding(LayoutConstants.Gaps.BadgePadding(heightClass))
            .VerticalAlignment(VerticalAlignment.Center)
            .Children(icon, text);

        var badge = new SquircleBorder()
            .RadiusMode(SquircleRadiusMode.Pill)
            .FillColor<TVM>(colorPath)
            .Child(badgeContentPanel);

        return (icon, text, badge, badgeContentPanel);
    }

    /// <summary>
    /// Builds a BadgeSet from individual badges.
    /// </summary>
    public static BadgeSet CreateBadgeSet(
        HeightClass heightClass,
        params (BufferedBitmapIcon icon, TextBlock text, SquircleBorder badge, StackPanel contentPanel)[] badges)
    {
        var panel = new StackPanel()
            .Orientation(Orientation.Horizontal)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Spacing(LayoutConstants.Gaps.BadgeRowSpacing(heightClass))
            .Children(badges.Select(b => b.badge).ToArray());

        return new BadgeSet(
            panel,
            badges.Select(b => b.badge).ToArray(),
            badges.Select(b => b.text).ToArray(),
            badges.Select(b => b.icon).ToArray(),
            badges.Select(b => b.contentPanel).ToArray()
        );
    }

    #endregion

    #region Translation Carousel

    /// <summary>
    /// Builds the translation display with navigation arrows at content edges.
    /// Returns elements for responsive updates.
    /// </summary>
    static (TextBlock translationText, Border translationInnerBorder, Grid container, StackPanel contentPanel) BuildTranslationCarousel<TVM>(
        Expression<Func<TVM, ExampleCarouselViewModel>> carouselPath,
        Expression<Func<TVM, bool>> isRevealedPath,
        LayoutConstants.PracticeFontSizes fonts,
        HeightClass heightClass)
    {
        var translationTextBlock = RegularText()
            .TextWithin<ExampleCarouselViewModel>(c => c.CurrentMeaning)
            .FontSize(fonts.Translation)
            .FontWeight(Microsoft.UI.Text.FontWeights.Medium)
            .TextWrapping(TextWrapping.Wrap)
            .TextAlignment(TextAlignment.Center)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
            .Opacity(0.9);

        // Shadow reference for measuring single-line height (used to position arrows)
        // This has the same structure as real content but with single-line text
        var singleLineReference = new StackPanel()
            .Spacing(LayoutConstants.Gaps.TranslationContentSpacing(heightClass))
            .Opacity(0)
            .IsHitTestVisible(false)
            .Children(
                RegularText()
                    .Text("X") // Single character to get one-line height
                    .FontSize(fonts.Translation)
                    .TextAlignment(TextAlignment.Center),
                RegularText()
                    .Text("1 / 1")
                    .FontSize(fonts.TranslationPagination)
                    .TextAlignment(TextAlignment.Center)
            );

        // Content panel for translation text and pagination (tracked for responsive updates)
        var translationContentPanel = new StackPanel()
            .Spacing(LayoutConstants.Gaps.TranslationContentSpacing(heightClass))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Center)
            .OpacityWithin<StackPanel, ExampleCarouselViewModel>(c => c.IsRevealed)
            .Children(
                translationTextBlock,
                RegularText()
                    .TextWithin<ExampleCarouselViewModel>(c => c.PaginationText)
                    .FontSize(fonts.TranslationPagination)
                    .TextAlignment(TextAlignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
                    .Opacity(0.6)
            );

        // Inner border with padding - tracked for responsive updates
        var translationInnerBorder = new Border()
            .Padding(LayoutConstants.Gaps.TranslationPaddingH(heightClass), LayoutConstants.Gaps.TranslationPaddingV(heightClass))
            .Child(
                new Grid()
                    .Scope(carouselPath)
                    .Children(
                        // Shadow reference - same structure, single line, invisible
                        // Used to measure height for arrow positioning
                        singleLineReference,

                        // Placeholder dots - centered decoration, doesn't affect layout
                        // Uses drawn Ellipses instead of text "…" for true vertical centering
                        new StackPanel()
                            .Orientation(Orientation.Horizontal)
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .VerticalAlignment(VerticalAlignment.Center)
                            .Spacing(6)
                            .Opacity(0.75)
                            .VisibilityWithin<StackPanel, ExampleCarouselViewModel>(c => c.IsRevealed, invert: true)
                            .Children(
                                new Ellipse().Width(6).Height(6).Fill(ThemeResource.Get<Brush>("OnSurfaceVariantBrush")),
                                new Ellipse().Width(6).Height(6).Fill(ThemeResource.Get<Brush>("OnSurfaceVariantBrush")),
                                new Ellipse().Width(6).Height(6).Fill(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
                            ),

                        // Translation content - always participates in layout (uses Opacity)
                        translationContentPanel
                    )
            );

        // Translation border - width set dynamically based on card width
        // Shadow is applied via CardShadow wrapper when adding to container
        var translationBorder = new SquircleBorder()
            .Fill(ThemeResource.Get<Brush>("SurfaceBrush"))
            .Child(translationInnerBorder);

        // Arrow buttons - 36px visible circle with 50px hit area for easier tapping
        // Using Border as hit area wrapper to avoid Button's hover/press visual states
        var visualSize = 36.0;
        var hitSize = 50.0;

        var prevVisual = new Border()
            .Width(visualSize)
            .Height(visualSize)
            .CornerRadius(new CornerRadius(visualSize / 2))
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .HorizontalAlignment(HorizontalAlignment.Left)
            .VerticalAlignment(VerticalAlignment.Top)
            .Child(
                new BitmapIcon()
                    .UriSource(new Uri(PracticeIcons.ChevronLeft))
                    .ShowAsMonochrome(true)
                    .Height(13)
                    .Margin(-2, 0, 0, 0)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
            );

        var prevButton = new Border()
            .Width(hitSize)
            .Height(hitSize)
            .Background(new SolidColorBrush(Colors.Transparent))
            .VerticalAlignment(VerticalAlignment.Top)
            .HorizontalAlignment(HorizontalAlignment.Left)
            .Scope(carouselPath)
            .VisibilityWithin<Border, ExampleCarouselViewModel>(c => c.HasMultipleTranslations)
            .OpacityWithin<Border, ExampleCarouselViewModel>(c => c.IsRevealed)
            .Child(prevVisual);

        prevButton.Tapped += (s, e) =>
        {
            if (s is FrameworkElement { DataContext: ExampleCarouselViewModel { IsRevealed: true } vm })
                vm.PreviousCommand.Execute(null);
        };

        var nextVisual = new Border()
            .Width(visualSize)
            .Height(visualSize)
            .CornerRadius(new CornerRadius(visualSize / 2))
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .HorizontalAlignment(HorizontalAlignment.Right)
            .VerticalAlignment(VerticalAlignment.Top)
            .Child(
                new BitmapIcon()
                    .UriSource(new Uri(PracticeIcons.ChevronRight))
                    .ShowAsMonochrome(true)
                    .Height(13)
                    .Margin(2, 0, 0, 0)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
            );

        var nextButton = new Border()
            .Width(hitSize)
            .Height(hitSize)
            .Background(new SolidColorBrush(Colors.Transparent))
            .VerticalAlignment(VerticalAlignment.Top)
            .HorizontalAlignment(HorizontalAlignment.Right)
            .Opacity(0) // Start hidden to prevent blink on load
            .Scope(carouselPath)
            .VisibilityWithin<Border, ExampleCarouselViewModel>(c => c.HasMultipleTranslations)
            .OpacityWithin<Border, ExampleCarouselViewModel>(c => c.IsRevealed)
            .Child(nextVisual);

        nextButton.Tapped += (s, e) =>
        {
            if (s is FrameworkElement { DataContext: ExampleCarouselViewModel { IsRevealed: true } vm })
                vm.NextCommand.Execute(null);
        };

        // Grid layout: arrows at edges, translation fills space between
        // Fixed 50px columns reserve arrow space even when arrows are hidden
        var container = new Grid()
            .ColumnDefinitions("50,*,50")
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Children(
                prevButton.Grid(column: 0),
                CardShadow(translationBorder).Grid(column: 1).HorizontalAlignment(HorizontalAlignment.Stretch),
                nextButton.Grid(column: 2)
            );

        return (translationTextBlock, translationInnerBorder, container, translationContentPanel);
    }

    #endregion

    #region Example Section

    /// <summary>
    /// Builds the example sentence and reference display.
    /// </summary>
    static (TextBlock exampleText, TextBlock referenceText, StackPanel container) BuildExampleSection<TVM>(
        Expression<Func<TVM, ExampleCarouselViewModel>> carouselPath,
        LayoutConstants.PracticeFontSizes fonts)
    {
        var exampleTextBlock = PaliText()
            .HtmlTextWithin<ExampleCarouselViewModel>(c => c.CurrentExample)
            .FontSize(fonts.SuttaExample)
            .TextWrapping(TextWrapping.Wrap)
            .TextAlignment(TextAlignment.Center)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"));

        // Reference uses Viewbox to shrink font instead of wrapping
        var referenceTextBlock = PaliText()
            .TextWithin<ExampleCarouselViewModel>(c => c.CurrentReference)
            .FontSize(fonts.SuttaReference)
            .TextWrapping(TextWrapping.NoWrap)
            .TextAlignment(TextAlignment.Center)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"));

        var referenceViewbox = new Viewbox()
            .Stretch(Stretch.Uniform)
            .StretchDirection(StretchDirection.DownOnly) // Only shrink, never grow
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Child(referenceTextBlock);

        var container = new StackPanel()
            .Spacing(LayoutConstants.Gaps.ExampleLineSpacing)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Scope(carouselPath)
            .Children(exampleTextBlock, referenceViewbox);

        return (exampleTextBlock, referenceTextBlock, container);
    }

    #endregion

    #region Navigation

    /// <summary>
    /// Builds navigation with reveal button that swaps to Easy/Hard after reveal.
    /// </summary>
    static (List<(BitmapIcon, TextBlock)> buttonElements, Grid container) BuildNavigation<TVM>(
        Expression<Func<TVM, ICommand>> revealCommand,
        Expression<Func<TVM, ICommand>> hardCommand,
        Expression<Func<TVM, ICommand>> easyCommand,
        Expression<Func<TVM, bool>> isRevealedPath,
        LayoutConstants.PracticeFontSizes fonts,
        HeightClass heightClass)
    {
        var (hardIcon, hardText, hardButton) = BuildActionButton<TVM>("Hard", PracticeIcons.Hard, 14, hardCommand, "HardButtonBrush", "HardButtonOutlineBrush", "OnHardButtonBrush", fonts, heightClass);
        var (easyIcon, easyText, easyButton) = BuildActionButton<TVM>("Easy", PracticeIcons.Easy, 13, easyCommand, "EasyButtonBrush", "EasyButtonOutlineBrush", "OnEasyButtonBrush", fonts, heightClass);

        var contentPadding = LayoutConstants.Gaps.ContentSpacing(heightClass);
        var container = new Grid()
            .MaxWidth(LayoutConstants.ContentMaxWidth)
            .Padding(contentPadding, 0, contentPadding, 0) // No top/bottom padding (gaps handled by adjacent elements)
            .Children(
                // Reveal button - visible when NOT revealed
                StartPrimaryButtonShadow(BuildRevealButton(revealCommand, fonts, heightClass))
                    .BoolToVisibility<ShadowContainer, TVM>(isRevealedPath, invert: true),

                // Hard/Easy buttons - visible when revealed
                new Grid()
                    .ColumnDefinitions("*,*")
                    .ColumnSpacing(contentPadding)
                    .BoolToVisibility<Grid, TVM>(isRevealedPath)
                    .Children(
                        ButtonShadow(hardButton).Grid(column: 0),
                        ButtonShadow(easyButton).Grid(column: 1)
                    )
            );

        return (new List<(BitmapIcon, TextBlock)> { (hardIcon, hardText), (easyIcon, easyText) }, container);
    }

    static SquircleButton BuildRevealButton<TVM>(
        Expression<Func<TVM, ICommand>> commandPath,
        LayoutConstants.PracticeFontSizes fonts,
        HeightClass heightClass)
    {
        var button = new SquircleButton()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Fill(ThemeResource.Get<Brush>("NavigationButtonVariantBrush"))
            .Stroke(ThemeResource.Get<Brush>("NavigationButtonVariantOutlineBrush"))
            .StrokeThickness(LayoutConstants.Sizes.PracticeButtonStrokeThickness)
            .Padding(LayoutConstants.Gaps.ActionButtonPaddingH(heightClass), LayoutConstants.Gaps.ActionButtonPaddingV(heightClass));

        // Defer command binding until Loaded to avoid ResourceResolver scope crash
        // (Uno Platform race condition when CanExecute triggers visual state change before button is in tree)
        button.Loaded += (s, _) =>
        {
            if (s is SquircleButton btn && btn.Command is null)
                btn.SetBinding(ButtonBase.CommandProperty, Bind.Path(commandPath));
        };

        return button
            .Child(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .HorizontalAlignment(HorizontalAlignment.Center)
                .Spacing(LayoutConstants.Gaps.ButtonIconTextSpacing(heightClass) + 1)
                .Children(
                    new BitmapIcon()
                        .UriSource(new Uri(PracticeIcons.Reveal))
                        .ShowAsMonochrome(true)
                        .Height(16) // Figma @1x: 24x16
                        .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush")),
                    RegularText()
                        .Text("Reveal Answer")
                        .FontSize(fonts.RevealButton)
                        .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                        .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
                ));
    }

    static (BitmapIcon icon, TextBlock text, SquircleButton button) BuildActionButton<TVM>(
        string label,
        string iconPath,
        double iconHeight,
        Expression<Func<TVM, ICommand>> commandPath,
        string fillBrushKey,
        string strokeBrushKey,
        string textBrushKey,
        LayoutConstants.PracticeFontSizes fonts,
        HeightClass heightClass)
    {
        var iconElement = new BitmapIcon()
            .UriSource(new Uri(iconPath))
            .ShowAsMonochrome(true)
            .Height(iconHeight)
            .Foreground(ThemeResource.Get<Brush>(textBrushKey));

        var textElement = RegularText()
            .Text(label)
            .FontSize(fonts.Button)
            .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
            .Foreground(ThemeResource.Get<Brush>(textBrushKey));

        var button = new SquircleButton()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Fill(ThemeResource.Get<Brush>(fillBrushKey))
            .Stroke(ThemeResource.Get<Brush>(strokeBrushKey))
            .StrokeThickness(LayoutConstants.Sizes.PracticeButtonStrokeThickness)
            .Padding(LayoutConstants.Gaps.ActionButtonPaddingH(heightClass), LayoutConstants.Gaps.ActionButtonPaddingV(heightClass));

        // Defer command binding until Loaded to avoid ResourceResolver scope crash
        // (Uno Platform race condition when CanExecute triggers visual state change before button is in tree)
        button.Loaded += (s, _) =>
        {
            if (s is SquircleButton btn && btn.Command is null)
                btn.SetBinding(ButtonBase.CommandProperty, Bind.Path(commandPath));
        };

        button.Child(new StackPanel()
            .Orientation(Orientation.Horizontal)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Spacing(LayoutConstants.Gaps.ButtonIconTextSpacing(heightClass) + 2)
            .Children(iconElement, textElement));

        return (iconElement, textElement, button);
    }

    #endregion

    #region Daily Goal Bar

    /// <summary>
    /// Builds the daily goal bar with progress indicator.
    /// </summary>
    static Border BuildDailyGoalBar<TVM>(
        Expression<Func<TVM, string>> dailyGoalText,
        Expression<Func<TVM, double>> dailyProgress,
        HeightClass heightClass,
        LayoutConstants.PracticeFontSizes fonts)
    {
        var contentPadding = LayoutConstants.Gaps.ContentSpacing(heightClass);
        var navToDailyGoalMargin = LayoutConstants.Gaps.NavToDailyGoalMargin(heightClass);
        var topPadding = contentPadding / 2;
        var bottomPadding = LayoutConstants.Gaps.DailyGoalBottomPadding(heightClass);

        // Custom progress bar using rounded Border (simpler than SquircleBorder, updates reliably)
        var progressHeight = LayoutConstants.Sizes.ProgressBarHeight;
        var cornerRadius = new CornerRadius(progressHeight / 2);

        var fillBorder = new Border()
            .Background(ThemeResource.Get<Brush>("AccentBrush"))
            .Height(progressHeight)
            .CornerRadius(cornerRadius)
            .HorizontalAlignment(HorizontalAlignment.Left)
            .MinWidth(progressHeight); // Ensure minimum width equals height for proper pill shape

        var trackBorder = new Border()
            .Background(ThemeResource.Get<Brush>("BackgroundVariantBrush"))
            .Height(progressHeight)
            .CornerRadius(cornerRadius)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Child(fillBorder);

        // Helper to update fill width based on progress percentage
        void UpdateFillWidth(double progress, double trackWidth)
        {
            if (trackWidth <= 0) return;
            var fillWidth = Math.Max(progressHeight, trackWidth * (progress / 100.0));
            fillBorder.Width = fillWidth;
        }

        // Update fill width when track size changes
        trackBorder.SizeChanged += (s, e) =>
        {
            if (trackBorder.DataContext is double progress)
                UpdateFillWidth(progress, e.NewSize.Width);
        };

        // Bind progress value to DataContext
        trackBorder.SetBinding(FrameworkElement.DataContextProperty, Bind.Path(dailyProgress));

        // Use RegisterPropertyChangedCallback to detect when bound progress changes
        // This fires reliably when the binding updates, unlike DataContextChanged
        trackBorder.RegisterPropertyChangedCallback(FrameworkElement.DataContextProperty, (sender, _) =>
        {
            if (sender is Border border && border.DataContext is double progress)
                UpdateFillWidth(progress, border.ActualWidth);
        });

        return new Border()
            // Transparent background - bar blends with page background
            .Margin(0, navToDailyGoalMargin, 0, 0) // Top margin for gap from nav buttons
            .Padding(contentPadding, topPadding, contentPadding, bottomPadding)
            .Child(
                new StackPanel().Spacing(LayoutConstants.Gaps.DailyGoalSpacing).Children(
                    new Grid().ColumnDefinitions("*,Auto").Children(
                        RegularText()
                            .Text("Daily goal")
                            .FontSize(fonts.DailyGoal)
                            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
                            .Grid(column: 0),
                        RegularText()
                            .Text(dailyGoalText)
                            .FontSize(fonts.DailyGoal)
                            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
                            .Grid(column: 1)
                    ),
                    trackBorder
                )
            );
    }

    #endregion

    #region Responsive

    /// <summary>
    /// Applies responsive values to all tracked elements using centralized font config.
    /// </summary>
    public static void ApplyResponsiveValues(ResponsiveElements elements, HeightClass heightClass)
    {
        var fonts = LayoutConstants.PracticeFontSizes.Get(heightClass);

        // Content width and uniform padding
        var contentPadding = LayoutConstants.Gaps.ContentSpacing(heightClass);
        if (elements.ContentArea is not null)
        {
            var windowWidth = App.MainWindow?.Bounds.Width ?? LayoutConstants.ContentMaxWidth;
            var contentWidth = Math.Min(windowWidth, LayoutConstants.ContentMaxWidth);

            elements.ContentArea.Width = contentWidth;
            elements.ContentArea.Padding(new Thickness(contentPadding));
        }

        // Card padding (asymmetric: smaller top/bottom) and content spacing
        if (elements.CardStackPanel is not null)
        {
            var cardPadSides = LayoutConstants.Gaps.CardHorizontalPadding(heightClass);
            var cardPadTop = LayoutConstants.Gaps.CardPaddingTop(heightClass);
            var cardPadBottom = LayoutConstants.Gaps.CardPaddingBottom(heightClass);
            elements.CardStackPanel.Padding(new Thickness(cardPadSides, cardPadTop, cardPadSides, cardPadBottom));
            elements.CardStackPanel.Spacing = LayoutConstants.Gaps.CardContentSpacing(heightClass);
        }

        // Badge spacing
        elements.BadgesPanel?.Spacing = LayoutConstants.Gaps.BadgeRowSpacing(heightClass);

        // Badge content panel spacing and padding
        var badgePadding = LayoutConstants.Gaps.BadgePadding(heightClass);
        var badgeIconTextSpacing = LayoutConstants.Gaps.BadgeIconTextSpacing(heightClass);
        foreach (var panel in elements.BadgeContentPanels)
        {
            panel.Spacing = badgeIconTextSpacing;
            panel.Padding(badgePadding);
        }

        // Word Viewbox max height and margins
        if (elements.WordViewbox is not null)
        {
            elements.WordViewbox.MaxHeight = fonts.Word * 1.5;
            elements.WordViewbox.Margin(0, LayoutConstants.Gaps.WordMarginTop(heightClass), 0, LayoutConstants.Gaps.WordMarginBottom(heightClass));
        }

        // Answer Viewbox max heights and container margin
        elements.AnswerViewbox?.MaxHeight = fonts.Answer * 1.5;
        elements.AnswerSecondaryViewbox?.MaxHeight = fonts.AnswerSecondary * 1.5;
        elements.AnswerContainer?.Margin(0, LayoutConstants.Gaps.AnswerMarginTop(heightClass), 0, 0);

        // Answer line spacing (between primary and secondary answer lines)
        var answerLineSpacing = LayoutConstants.Gaps.AnswerLineSpacing(heightClass);
        if (elements.AnswerSpacer is not null)
            elements.AnswerSpacer.Spacing = answerLineSpacing;
        if (elements.AnswerContentPanel is not null)
            elements.AnswerContentPanel.Spacing = answerLineSpacing;

        // Word, Answer, AnswerSecondary font sizes
        if (elements.WordTextBlock is not null)
            elements.WordTextBlock.FontSize = fonts.Word;
        if (elements.AnswerTextBlock is not null)
            elements.AnswerTextBlock.FontSize = fonts.Answer;
        if (elements.AnswerSecondaryTextBlock is not null)
            elements.AnswerSecondaryTextBlock.FontSize = fonts.AnswerSecondary;

        // Translation border padding
        elements.TranslationInnerBorder?.Padding(
            LayoutConstants.Gaps.TranslationPaddingH(heightClass),
            LayoutConstants.Gaps.TranslationPaddingV(heightClass));

        // Translation container top margin (space between card and translation)
        elements.TranslationContainer?.Margin(0, contentPadding, 0, 0);

        // Translation content panel spacing (space between text and pagination)
        if (elements.TranslationContentPanel is not null)
            elements.TranslationContentPanel.Spacing = LayoutConstants.Gaps.TranslationContentSpacing(heightClass);

        // Example area top margin (space between translation and sutta)
        elements.ExampleArea?.Margin(0, contentPadding, 0, 0);

        // Daily goal bar margin and padding
        if (elements.DailyGoalBar is Border dailyGoalBorder)
        {
            var navToDailyGoalMargin = LayoutConstants.Gaps.NavToDailyGoalMargin(heightClass);
            var topPadding = contentPadding / 2;
            var bottomPadding = LayoutConstants.Gaps.DailyGoalBottomPadding(heightClass);
            dailyGoalBorder.Margin(0, navToDailyGoalMargin, 0, 0);
            dailyGoalBorder.Padding(contentPadding, topPadding, contentPadding, bottomPadding);
        }

        foreach (var textBlock in elements.BadgeTextBlocks)
            textBlock.FontSize = fonts.Badge;

        foreach (var icon in elements.BadgeIcons)
        {
            icon.IconHeight = fonts.Badge;
            icon.Width = fonts.Badge;  // All icons are square
        }

        // Badge hint
        elements.BadgeHintTextBlock?.FontSize = fonts.BadgeHint;

        // Translation
        elements.TranslationTextBlock?.FontSize = fonts.Translation;

        // Sutta example and reference
        elements.SuttaExampleTextBlock?.FontSize = fonts.SuttaExample;

        elements.SuttaReferenceTextBlock?.FontSize = fonts.SuttaReference;

        // Buttons (icon height is fixed at 18, only text scales)
        foreach (var (icon, text) in elements.ButtonElements)
        {
            text.FontSize = fonts.Button;
        }

        // Debug text
        if (elements.DebugTextBlock is not null)
        {
            #if DEBUG
            var windowHeight = App.MainWindow?.Bounds.Height ?? 0;
            elements.DebugTextBlock.Text = $"{heightClass} ({windowHeight:F0}pt)";
            #endif
        }
    }

    #endregion
}

/// <summary>
/// Holds references to elements for responsive sizing.
/// </summary>
public class ResponsiveElements
{
    public FrameworkElement? TitleBar { get; set; }
    public FrameworkElement? DailyGoalBar { get; set; }
    public Grid? ContentArea { get; set; }
    public SquircleBorder? CardBorder { get; set; }
    public StackPanel? CardStackPanel { get; set; }
    public StackPanel? BadgesPanel { get; set; }
    public Viewbox? WordViewbox { get; set; }
    public Viewbox? AnswerViewbox { get; set; }
    public Viewbox? AnswerSecondaryViewbox { get; set; }
    public Border? AnswerPlaceholder { get; set; }
    public Grid? AnswerContainer { get; set; }
    public StackPanel? AnswerSpacer { get; set; }
    public StackPanel? AnswerContentPanel { get; set; }
    public TextBlock? WordTextBlock { get; set; }
    public TextBlock? AnswerTextBlock { get; set; }
    public TextBlock? AnswerSecondaryTextBlock { get; set; }
    public TextBlock? BadgeHintTextBlock { get; set; }
    public TextBlock? TranslationTextBlock { get; set; }
    public Border? TranslationInnerBorder { get; set; }
    public Grid? TranslationContainer { get; set; }
    public StackPanel? TranslationContentPanel { get; set; }
    public Grid? ExampleArea { get; set; }
    public TextBlock? SuttaExampleTextBlock { get; set; }
    public TextBlock? SuttaReferenceTextBlock { get; set; }
    public TextBlock? DebugTextBlock { get; set; }
    public List<SquircleBorder> BadgeBorders { get; } = [];
    public List<TextBlock> BadgeTextBlocks { get; } = [];
    public List<BufferedBitmapIcon> BadgeIcons { get; } = [];
    public List<StackPanel> BadgeContentPanels { get; } = [];
    public List<(BitmapIcon Icon, TextBlock Text)> ButtonElements { get; } = [];
}

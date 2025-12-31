using System.Linq.Expressions;
using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Practice.ViewModels;
using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Themes;
using static PaliPractice.Presentation.Common.TextHelpers;
using ExampleCarouselViewModel = PaliPractice.Presentation.Practice.ViewModels.Common.ExampleCarouselViewModel;

namespace PaliPractice.Presentation.Practice;

#region Configuration Records

/// <summary>
/// Configuration for building a practice page layout.
/// </summary>
public record PracticePageConfig<TVM>(
    string Title,
    string RankPrefix,
    Expression<Func<TVM, FlashCardViewModel>> FlashCardPath,
    Expression<Func<TVM, string>> AnswerStemPath,
    Expression<Func<TVM, string>> AnswerEndingPath,
    Expression<Func<TVM, string>> AlternativeFormsPath,
    Expression<Func<TVM, bool>> IsRevealedPath,
    Expression<Func<TVM, ExampleCarouselViewModel>> CarouselPath,
    Expression<Func<TVM, ICommand>> GoBackCommandPath,
    Expression<Func<TVM, ICommand>> GoToHistoryCommandPath,
    Expression<Func<TVM, ICommand>> GoToInflectionTableCommandPath,
    Expression<Func<TVM, string>> CurrentLemmaTextPath,
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
    FontIcon[] Icons
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
        var wordViewbox = BuildCardWord(config.FlashCardPath, fonts);
        elements.WordViewbox = wordViewbox;
        elements.BadgesPanel = badges.Panel;
        elements.BadgeBorders.AddRange(badges.Borders);
        elements.BadgeTextBlocks.AddRange(badges.TextBlocks);
        elements.BadgeIcons.AddRange(badges.Icons);

        // Build answer section (Viewbox shrinks long words automatically)
        var (answerViewbox, secondaryViewbox, placeholder, answerContainer) = BuildAnswerSection(
            config.AnswerStemPath,
            config.AnswerEndingPath,
            config.AlternativeFormsPath,
            config.IsRevealedPath,
            fonts);
        elements.AnswerViewbox = answerViewbox;
        elements.AnswerSecondaryViewbox = secondaryViewbox;
        elements.AnswerPlaceholder = placeholder;

        // Build translation carousel
        var (translationText, translationBorder, translationContainer) = BuildTranslationCarousel(
            config.CarouselPath, config.IsRevealedPath, fonts);
        elements.TranslationTextBlock = translationText;

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

        // Debug text for size bracket (in header middle)
        var debugText = new TextBlock()
            .Text("Size: --")
            .FontSize(LayoutConstants.FixedFonts.DebugText)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
            .Opacity(0.6);
        elements.DebugTextBlock = debugText;

        // Assemble card children
        var cardChildren = new List<UIElement>
        {
            BuildCardHeader(config.FlashCardPath, config.RankPrefix, debugText),
            wordViewbox,
            badges.Panel
        };

        if (hintElement is not null)
            cardChildren.Add(hintElement);

        cardChildren.Add(answerContainer);

        // Build card border
        var cardBorder = new SquircleBorder()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Fill(ThemeResource.Get<Brush>("SurfaceBrush"))
            .RadiusMode(SquircleRadiusMode.CardSmall)
            .Child(
                new StackPanel()
                    .Padding(LayoutConstants.Gaps.Card(heightClass))
                    .Spacing(LayoutConstants.Gaps.CardInternal)
                    .Children(cardChildren.ToArray())
            );
        elements.CardBorder = cardBorder;

        // Set up dynamic widths based on card size
        cardBorder.SizeChanged += (s, e) =>
        {
            var translationWidth = e.NewSize.Width * LayoutConstants.TranslationWidthRatio;

            elements.AnswerPlaceholder?.Width = e.NewSize.Width * LayoutConstants.AnswerPlaceholderWidthRatio;

            translationBorder.Width = translationWidth;
            exampleContainer.MaxWidth = translationWidth;
        };

        // Compute content width from window (clamped to max)
        var contentPadding = LayoutConstants.Gaps.Primary(heightClass);
        var windowWidth = App.MainWindow?.Bounds.Width ?? LayoutConstants.ContentMaxWidth;
        var contentWidth = Math.Min(windowWidth, LayoutConstants.ContentMaxWidth);

        // Wrap example in ScrollViewer for overflow handling
        var exampleScrollViewer = new ScrollViewer()
            .VerticalScrollBarVisibility(ScrollBarVisibility.Hidden)
            .HorizontalScrollBarVisibility(ScrollBarVisibility.Disabled)
            .VerticalAlignment(VerticalAlignment.Top)
            .Content(exampleContainer);

        // Fade overlay to hint at scrollable content (gradient from transparent to background)
        var bgBrush = Application.Current.Resources["BackgroundBrush"] as SolidColorBrush;
        var bgColor = bgBrush?.Color ?? Colors.White;

        var fadeOverlay = new Border()
            .Height(16)
            .VerticalAlignment(VerticalAlignment.Bottom)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .IsHitTestVisible(false)
            .Opacity(0) // Start hidden
            .Background(new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0.5, 0),
                EndPoint = new Windows.Foundation.Point(0.5, 1),
                GradientStops =
                {
                    new GradientStop { Color = Colors.Transparent, Offset = 0 },
                    new GradientStop { Color = bgColor, Offset = 1 }
                }
            });

        // Detect overflow and show/hide fade overlay
        exampleScrollViewer.SizeChanged += (s, e) => UpdateFadeOverlay();
        exampleContainer.SizeChanged += (s, e) => UpdateFadeOverlay();
        exampleScrollViewer.ViewChanged += (s, e) => UpdateFadeOverlay();

        // Container that fills available space with ScrollViewer and fade overlay
        var exampleArea = new Grid()
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Children(exampleScrollViewer, fadeOverlay);

        // Build content area with explicit width and uniform padding
        var contentArea = new Grid()
            .RowDefinitions("Auto,Auto,*")
            .Width(contentWidth)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Padding(contentPadding, contentPadding, contentPadding, contentPadding)
            .Children(
                cardBorder.Grid(row: 0),
                translationContainer.Margin(0, contentPadding, 0, 0).Grid(row: 1),
                exampleArea.Margin(0, contentPadding, 0, 0).Grid(row: 2)
            );
        elements.ContentArea = contentArea;

        // Build title bar with clickable lemma button and daily goal bar
        var titleBar = AppTitleBar.BuildWithCenterButton<TVM>(
            config.GoBackCommandPath,
            tb => tb.Text(config.CurrentLemmaTextPath),
            config.GoToInflectionTableCommandPath,
            config.GoToHistoryCommandPath);
        var dailyGoalBar = BuildDailyGoalBar(config.DailyGoalTextPath, config.DailyProgressPath, heightClass);
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
    /// Builds the header row: [Rank Badge] [Debug] [Anki State]
    /// </summary>
    static Grid BuildCardHeader<TVM>(
        Expression<Func<TVM, FlashCardViewModel>> cardPath,
        string rankPrefix,
        TextBlock? debugText)
    {
        return new Grid()
            .ColumnDefinitions("Auto,*,Auto")
            .Children(
                new Border()
                    .Background(ThemeResource.Get<Brush>("PrimaryBrush"))
                    .CornerRadius(LayoutConstants.Sizes.RankBadgeCornerRadius)
                    .Padding(LayoutConstants.Gaps.RankBadgeHorizontal, LayoutConstants.Gaps.RankBadgeVertical)
                    .Child(
                        new StackPanel()
                            .Orientation(Orientation.Horizontal)
                            .Spacing(LayoutConstants.Gaps.RankBadge)
                            .Children(
                                RegularText()
                                    .Text(rankPrefix)
                                    .FontSize(LayoutConstants.FixedFonts.RankText)
                                    .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                                    .Foreground(ThemeResource.Get<Brush>("OnPrimaryBrush")),
                                RegularText()
                                    .Scope(cardPath)
                                    .TextWithin<FlashCardViewModel>(c => c.RankText)
                                    .FontSize(LayoutConstants.FixedFonts.RankText)
                                    .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                                    .Foreground(ThemeResource.Get<Brush>("OnPrimaryBrush"))
                            )
                    )
                    .Grid(column: 0),
                (debugText ?? new TextBlock())
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Grid(column: 1),
                RegularText()
                    .Scope(cardPath)
                    .TextWithin<FlashCardViewModel>(c => c.MasteryText)
                    .FontSize(LayoutConstants.FixedFonts.AnkiState)
                    .HorizontalAlignment(HorizontalAlignment.Right)
                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                    .Grid(column: 2)
            );
    }

    /// <summary>
    /// Builds the main word display with Viewbox for shrinking long words.
    /// </summary>
    static Viewbox BuildCardWord<TVM>(
        Expression<Func<TVM, FlashCardViewModel>> cardPath,
        LayoutConstants.PracticeFontSizes fonts)
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

        return new Viewbox()
            .Stretch(Stretch.Uniform)
            .StretchDirection(StretchDirection.DownOnly)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .MaxHeight(fonts.Word * 1.5) // Limit height to prevent over-shrinking
            .Margin(0, LayoutConstants.Gaps.WordTop, 0, LayoutConstants.Gaps.WordBottom)
            .Child(textBlock);
    }

    /// <summary>
    /// Builds the answer section with spacer, content, and placeholder.
    /// Returns Viewbox wrappers that shrink long words automatically.
    /// </summary>
    static (Viewbox answerViewbox, Viewbox secondaryViewbox, Border placeholder, Grid answerContainer) BuildAnswerSection<TVM>(
        Expression<Func<TVM, string>> answerStemPath,
        Expression<Func<TVM, string>> answerEndingPath,
        Expression<Func<TVM, string>> alternativeFormsPath,
        Expression<Func<TVM, bool>> isRevealedPath,
        LayoutConstants.PracticeFontSizes fonts)
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
            .FontWeight(Microsoft.UI.Text.FontWeights.Medium)
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
            .Spacing(LayoutConstants.Gaps.AnswerLines)
            .Opacity(0)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Children(
                new TextBlock()
                    .FontSize(fonts.Answer)
                    .Text("X"),
                new TextBlock()
                    .FontSize(fonts.AnswerSecondary)
                    .FontWeight(Microsoft.UI.Text.FontWeights.Medium)
                    .Text("X")
            );

        var answerContent = new StackPanel()
            .Spacing(LayoutConstants.Gaps.AnswerLines)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .VerticalAlignment(VerticalAlignment.Center)
            .BoolToVisibility<StackPanel, TVM>(isRevealedPath)
            .Children(answerViewbox, alternativesViewbox);

        // Placeholder uses relative width via binding or we track width changes
        var answerPlaceholder = new Border()
            .Height(LayoutConstants.Sizes.PlaceholderHeight)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .VerticalAlignment(VerticalAlignment.Center)
            .BorderBrush(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
            .BorderThickness(0, 0, 0, LayoutConstants.Sizes.PlaceholderBorderThickness)
            .Opacity(0.5)
            .BoolToVisibility<Border, TVM>(isRevealedPath, invert: true);

        var answerContainer = new Grid()
            .Margin(0, LayoutConstants.Gaps.AnswerTop, 0, 0)
            .Children(answerSpacer, answerContent, answerPlaceholder);

        return (answerViewbox, alternativesViewbox, answerPlaceholder, answerContainer);
    }

    static TextBlock CreateColorEndingAnswer<TVM>(
        Expression<Func<TVM, string>> answerStemPath,
        Expression<Func<TVM, string>> answerEndingPath,
        LayoutConstants.PracticeFontSizes fonts)
    {
        var paliFont = new FontFamily(FontPaths.LibertinusSans);

        var stemRun = new Run()
            .FontFamily(paliFont)
            .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"));
        stemRun.SetBinding(Run.TextProperty, Bind.Path(answerStemPath));

        var endingRun = new Run()
            .FontFamily(paliFont)
            .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
            // Try different colors: Colors.Blue, Colors.DarkBlue, or custom RGB
            .Foreground(new SolidColorBrush(Color.FromArgb(255, 11,83,148)));
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
    /// Builds a grammar badge with icon and text.
    /// </summary>
    public static (FontIcon icon, TextBlock text, SquircleBorder badge) BuildBadge<TVM>(
        HeightClass heightClass,
        Expression<Func<TVM, string?>> glyphPath,
        Expression<Func<TVM, string>> labelPath,
        Expression<Func<TVM, Color>> colorPath)
    {
        var fonts = LayoutConstants.PracticeFontSizes.Get(heightClass);

        var icon = new FontIcon()
            .FontSize(fonts.Badge)
            .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
            .VerticalAlignment(VerticalAlignment.Center)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
            .GlyphWithVisibility<TVM>(glyphPath);

        var text = RegularText()
            .FontSize(fonts.Badge)
            .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
            .VerticalAlignment(VerticalAlignment.Center)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
            .Text<TVM>(labelPath);

        var badge = new SquircleBorder()
            .RadiusMode(SquircleRadiusMode.Pill)
            .FillColor<TVM>(colorPath)
            .Child(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .Spacing(LayoutConstants.Gaps.BadgeInternal)
                .Padding(LayoutConstants.Gaps.Badge(heightClass))
                .VerticalAlignment(VerticalAlignment.Center)
                .Children(icon, text));

        return (icon, text, badge);
    }

    /// <summary>
    /// Builds a BadgeSet from individual badges.
    /// </summary>
    public static BadgeSet CreateBadgeSet(
        HeightClass heightClass,
        params (FontIcon icon, TextBlock text, SquircleBorder badge)[] badges)
    {
        var panel = new StackPanel()
            .Orientation(Orientation.Horizontal)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Spacing(LayoutConstants.Gaps.BadgeSpacing(heightClass))
            .Children(badges.Select(b => b.badge).ToArray());

        return new BadgeSet(
            panel,
            badges.Select(b => b.badge).ToArray(),
            badges.Select(b => b.text).ToArray(),
            badges.Select(b => b.icon).ToArray()
        );
    }

    #endregion

    #region Translation Carousel

    /// <summary>
    /// Builds the translation display with navigation arrows.
    /// </summary>
    static (TextBlock translationText, SquircleBorder translationBorder, StackPanel container) BuildTranslationCarousel<TVM>(
        Expression<Func<TVM, ExampleCarouselViewModel>> carouselPath,
        Expression<Func<TVM, bool>> isRevealedPath,
        LayoutConstants.PracticeFontSizes fonts)
    {
        var translationTextBlock = RegularText()
            .TextWithin<ExampleCarouselViewModel>(c => c.CurrentMeaning)
            .FontSize(fonts.Translation)
            .TextWrapping(TextWrapping.Wrap)
            .TextAlignment(TextAlignment.Center)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"));

        // Shadow reference for measuring single-line height (used to position arrows)
        // This has the same structure as real content but with single-line text
        var singleLineReference = new StackPanel()
            .Spacing(LayoutConstants.Gaps.TranslationContent)
            .Opacity(0)
            .IsHitTestVisible(false)
            .Children(
                RegularText()
                    .Text("X") // Single character to get one-line height
                    .FontSize(fonts.Translation)
                    .TextAlignment(TextAlignment.Center),
                RegularText()
                    .Text("1 / 1")
                    .FontSize(LayoutConstants.FixedFonts.TranslationPagination)
                    .TextAlignment(TextAlignment.Center)
            );

        // Translation border - width set dynamically based on card width
        var translationBorder = new SquircleBorder()
            .Fill(ThemeResource.Get<Brush>("SurfaceBrush"))
            .RadiusMode(SquircleRadiusMode.ButtonSmall)
            .Child(
                new Border()
                    .Padding(LayoutConstants.Gaps.TranslationHorizontal, LayoutConstants.Gaps.TranslationVertical)
                    .Child(
                        new Grid()
                            .Scope(carouselPath)
                            .Children(
                                // Shadow reference - same structure, single line, invisible
                                // Used to measure height for arrow positioning
                                singleLineReference,

                                // Placeholder "…" - centered decoration, doesn't affect layout
                                // Uses Visibility since it overlays in Grid (translation determines size)
                                RegularText()
                                    .Text("…")
                                    .FontSize(LayoutConstants.FixedFonts.TranslationPlaceholder)
                                    .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .VerticalAlignment(VerticalAlignment.Center)
                                    .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
                                    .VisibilityWithin<TextBlock, ExampleCarouselViewModel>(c => c.IsRevealed, invert: true),

                                // Translation content - always participates in layout (uses Opacity)
                                new StackPanel()
                                    .Spacing(LayoutConstants.Gaps.TranslationContent)
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .VerticalAlignment(VerticalAlignment.Center)
                                    .OpacityWithin<StackPanel, ExampleCarouselViewModel>(c => c.IsRevealed)
                                    .Children(
                                        translationTextBlock,
                                        RegularText()
                                            .TextWithin<ExampleCarouselViewModel>(c => c.PaginationText)
                                            .FontSize(LayoutConstants.FixedFonts.TranslationPagination)
                                            .TextAlignment(TextAlignment.Center)
                                            .HorizontalAlignment(HorizontalAlignment.Center)
                                            .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
                                    )
                            )
                    )
            );

        // Arrow containers - height set to match single-line block, arrows centered within
        var prevArrowContainer = new Border()
            .VerticalAlignment(VerticalAlignment.Top)
            .Child(
                new Button()
                    .Background(new SolidColorBrush(Colors.Transparent))
                    .Padding(LayoutConstants.Gaps.TranslationArrowHorizontal, LayoutConstants.Gaps.TranslationArrowVertical)
                    .MinWidth(0)
                    .MinHeight(0)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Scope(carouselPath)
                    .CommandWithin<Button, ExampleCarouselViewModel>(c => c.PreviousCommand)
                    .VisibilityWithin<Button, ExampleCarouselViewModel>(c => c.HasMultipleTranslations)
                    .OpacityWithin<Button, ExampleCarouselViewModel>(c => c.IsRevealed)
                    .Content(new FontIcon()
                        .Glyph("\uE76B") // ChevronLeft
                        .FontSize(LayoutConstants.FixedFonts.TranslationArrowIcon)
                        .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush")))
            );

        var nextArrowContainer = new Border()
            .VerticalAlignment(VerticalAlignment.Top)
            .Child(
                new Button()
                    .Background(new SolidColorBrush(Colors.Transparent))
                    .Padding(LayoutConstants.Gaps.TranslationArrowHorizontal, LayoutConstants.Gaps.TranslationArrowVertical)
                    .MinWidth(0)
                    .MinHeight(0)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Scope(carouselPath)
                    .CommandWithin<Button, ExampleCarouselViewModel>(c => c.NextCommand)
                    .VisibilityWithin<Button, ExampleCarouselViewModel>(c => c.HasMultipleTranslations)
                    .OpacityWithin<Button, ExampleCarouselViewModel>(c => c.IsRevealed)
                    .Content(new FontIcon()
                        .Glyph("\uE76C") // ChevronRight
                        .FontSize(LayoutConstants.FixedFonts.TranslationArrowIcon)
                        .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush")))
            );

        // Update arrow container height when reference is measured
        // Height = reference content + border padding (top + bottom)
        singleLineReference.SizeChanged += (s, e) =>
        {
            var singleLineBlockHeight = e.NewSize.Height + (LayoutConstants.Gaps.TranslationVertical * 2);
            prevArrowContainer.Height = singleLineBlockHeight;
            nextArrowContainer.Height = singleLineBlockHeight;
        };

        var container = new StackPanel()
            .Orientation(Orientation.Horizontal)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Children(
                prevArrowContainer,
                translationBorder,
                nextArrowContainer
            );

        return (translationTextBlock, translationBorder, container);
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
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"));

        // Reference uses Viewbox to shrink font instead of wrapping
        var referenceTextBlock = PaliText()
            .TextWithin<ExampleCarouselViewModel>(c => c.CurrentReference)
            .FontSize(fonts.SuttaReference)
            .TextWrapping(TextWrapping.NoWrap)
            .TextAlignment(TextAlignment.Center)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"));

        var referenceViewbox = new Viewbox()
            .Stretch(Stretch.Uniform)
            .StretchDirection(StretchDirection.DownOnly) // Only shrink, never grow
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Child(referenceTextBlock);

        var container = new StackPanel()
            .Spacing(LayoutConstants.Gaps.ExampleSection)
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
    static (List<(FontIcon, TextBlock)> buttonElements, Grid container) BuildNavigation<TVM>(
        Expression<Func<TVM, ICommand>> revealCommand,
        Expression<Func<TVM, ICommand>> hardCommand,
        Expression<Func<TVM, ICommand>> easyCommand,
        Expression<Func<TVM, bool>> isRevealedPath,
        LayoutConstants.PracticeFontSizes fonts,
        HeightClass heightClass)
    {
        var (hardIcon, hardText, hardButton) = BuildActionButton<TVM>("Hard", "\uE711", hardCommand, "SurfaceBrush", fonts);
        var (easyIcon, easyText, easyButton) = BuildActionButton<TVM>("Easy", "\uE73E", easyCommand, "SurfaceBrush", fonts);

        var contentPadding = LayoutConstants.Gaps.Primary(heightClass);
        var container = new Grid()
            .MaxWidth(LayoutConstants.ContentMaxWidth)
            .Padding(contentPadding, 0, contentPadding, 0) // No top/bottom padding (gaps handled by adjacent elements)
            .Children(
                // Reveal button - visible when NOT revealed
                BuildRevealButton(revealCommand)
                    .BoolToVisibility<SquircleButton, TVM>(isRevealedPath, invert: true),

                // Hard/Easy buttons - visible when revealed
                new Grid()
                    .ColumnDefinitions("*,*")
                    .ColumnSpacing(contentPadding)
                    .BoolToVisibility<Grid, TVM>(isRevealedPath)
                    .Children(
                        hardButton
                            .Grid(column: 0)
                            .HorizontalAlignment(HorizontalAlignment.Stretch),
                        easyButton
                            .Grid(column: 1)
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                    )
            );

        return ([(hardIcon, hardText), (easyIcon, easyText)], container);
    }

    static SquircleButton BuildRevealButton<TVM>(Expression<Func<TVM, ICommand>> commandPath)
    {
        var button = new SquircleButton()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Fill(ThemeResource.Get<Brush>("PrimaryBrush"))
            .RadiusMode(SquircleRadiusMode.ButtonSmall)
            .Padding(LayoutConstants.Gaps.ActionButtonHorizontal, LayoutConstants.Gaps.ActionButtonVertical);
        button.SetBinding(ButtonBase.CommandProperty, Bind.Path(commandPath));
        return button
            .Child(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .HorizontalAlignment(HorizontalAlignment.Center)
                .Spacing(LayoutConstants.Gaps.ButtonContent)
                .Children(
                    new FontIcon()
                        .Glyph("\uE7B3") // Eye icon
                        .FontSize(LayoutConstants.FixedFonts.RevealButton)
                        .Foreground(ThemeResource.Get<Brush>("OnPrimaryBrush")),
                    RegularText()
                        .Text("Reveal Answer")
                        .FontSize(LayoutConstants.FixedFonts.RevealButton)
                        .Foreground(ThemeResource.Get<Brush>("OnPrimaryBrush"))
                ));
    }

    static (FontIcon icon, TextBlock text, SquircleButton button) BuildActionButton<TVM>(
        string label,
        string glyph,
        Expression<Func<TVM, ICommand>> commandPath,
        string brushKey,
        LayoutConstants.PracticeFontSizes fonts)
    {
        var iconElement = new FontIcon()
            .Glyph(glyph)
            .FontSize(fonts.Button)
            .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold);

        var textElement = RegularText()
            .Text(label)
            .FontSize(fonts.Button)
            .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold);

        var button = new SquircleButton()
            .Fill(ThemeResource.Get<Brush>(brushKey))
            .RadiusMode(SquircleRadiusMode.ButtonSmall)
            .Padding(LayoutConstants.Gaps.ActionButtonHorizontal, LayoutConstants.Gaps.ActionButtonVertical);
        button.SetBinding(ButtonBase.CommandProperty, Bind.Path(commandPath));
        button.Child(new StackPanel()
            .Orientation(Orientation.Horizontal)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Spacing(LayoutConstants.Gaps.ButtonContent)
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
        HeightClass heightClass)
    {
        var contentPadding = LayoutConstants.Gaps.Primary(heightClass);
        var verticalPadding = contentPadding / 2;
        return new Border()
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .Margin(0, contentPadding, 0, 0) // Top margin for gap from nav buttons
            .Padding(contentPadding, verticalPadding, contentPadding, verticalPadding) // Centered vertically
            .Child(
                new StackPanel().Spacing(LayoutConstants.Gaps.DailyGoal).Children(
                    new Grid().ColumnDefinitions("*,Auto").Children(
                        RegularText()
                            .Text("Daily goal")
                            .FontSize(LayoutConstants.FixedFonts.DailyGoalText)
                            .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                            .Grid(column: 0),
                        RegularText()
                            .Text(dailyGoalText)
                            .FontSize(LayoutConstants.FixedFonts.DailyGoalText)
                            .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                            .Grid(column: 1)
                    ),
                    new ProgressBar()
                        .Maximum(100)
                        .Height(LayoutConstants.Sizes.ProgressBarHeight)
                        .CornerRadius(LayoutConstants.Sizes.ProgressBarCornerRadius)
                        .Value(dailyProgress)
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
        var contentPadding = LayoutConstants.Gaps.Primary(heightClass);
        if (elements.ContentArea is not null)
        {
            var windowWidth = App.MainWindow?.Bounds.Width ?? LayoutConstants.ContentMaxWidth;
            var contentWidth = Math.Min(windowWidth, LayoutConstants.ContentMaxWidth);

            elements.ContentArea.Width = contentWidth;
            elements.ContentArea.Padding(new Thickness(contentPadding));
        }

        // Card padding
        if (elements.CardBorder is not null)
        {
            var cardPad = LayoutConstants.Gaps.Card(heightClass);
            elements.CardBorder.Padding(new Thickness(cardPad));
        }

        // Badge spacing
        elements.BadgesPanel?.Spacing = LayoutConstants.Gaps.BadgeSpacing(heightClass);

        // Word Viewbox max height (controls shrinking threshold)
        elements.WordViewbox?.MaxHeight = fonts.Word * 1.5;

        // Answer Viewbox max heights
        elements.AnswerViewbox?.MaxHeight = fonts.Answer * 1.5;
        elements.AnswerSecondaryViewbox?.MaxHeight = fonts.AnswerSecondary * 1.5;

        // Badge fonts and padding
        var badgePadding = LayoutConstants.Gaps.Badge(heightClass);
        foreach (var border in elements.BadgeBorders)
            border.Padding(badgePadding);

        foreach (var textBlock in elements.BadgeTextBlocks)
            textBlock.FontSize = fonts.Badge;

        foreach (var icon in elements.BadgeIcons)
            icon.FontSize = fonts.Badge;

        // Badge hint
        elements.BadgeHintTextBlock?.FontSize = fonts.BadgeHint;

        // Translation
        elements.TranslationTextBlock?.FontSize = fonts.Translation;

        // Sutta example and reference
        elements.SuttaExampleTextBlock?.FontSize = fonts.SuttaExample;

        elements.SuttaReferenceTextBlock?.FontSize = fonts.SuttaReference;

        // Buttons
        foreach (var (icon, text) in elements.ButtonElements)
        {
            icon.FontSize = fonts.Button;
            text.FontSize = fonts.Button;
        }

        // Debug text
        if (elements.DebugTextBlock is not null)
        {
            var windowHeight = App.MainWindow?.Bounds.Height ?? 0;
            elements.DebugTextBlock.Text = $"{heightClass} ({windowHeight:F0}pt)";
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
    public StackPanel? BadgesPanel { get; set; }
    public Viewbox? WordViewbox { get; set; }
    public Viewbox? AnswerViewbox { get; set; }
    public Viewbox? AnswerSecondaryViewbox { get; set; }
    public Border? AnswerPlaceholder { get; set; }
    public TextBlock? BadgeHintTextBlock { get; set; }
    public TextBlock? TranslationTextBlock { get; set; }
    public TextBlock? SuttaExampleTextBlock { get; set; }
    public TextBlock? SuttaReferenceTextBlock { get; set; }
    public TextBlock? DebugTextBlock { get; set; }
    public List<SquircleBorder> BadgeBorders { get; } = [];
    public List<TextBlock> BadgeTextBlocks { get; } = [];
    public List<FontIcon> BadgeIcons { get; } = [];
    public List<(FontIcon Icon, TextBlock Text)> ButtonElements { get; } = [];
}

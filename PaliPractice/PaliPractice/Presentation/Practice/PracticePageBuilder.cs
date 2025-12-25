using System.Linq.Expressions;
using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Practice.ViewModels;
using PaliPractice.Themes;
using static PaliPractice.Presentation.Common.TextHelpers;
using ExampleCarouselViewModel = PaliPractice.Presentation.Practice.ViewModels.Common.ExampleCarouselViewModel;
using WordCardViewModel = PaliPractice.Presentation.Practice.ViewModels.Common.WordCardViewModel;

namespace PaliPractice.Presentation.Practice;

#region Configuration Records

/// <summary>
/// Configuration for building a practice page layout.
/// </summary>
public record PracticePageConfig<TVM>(
    string Title,
    string RankPrefix,
    Expression<Func<TVM, WordCardViewModel>> WordCardPath,
    Expression<Func<TVM, string>> AnswerStemPath,
    Expression<Func<TVM, string>> AnswerEndingPath,
    Expression<Func<TVM, string>> AlternativeFormsPath,
    Expression<Func<TVM, bool>> IsRevealedPath,
    Expression<Func<TVM, ExampleCarouselViewModel>> CarouselPath,
    Expression<Func<TVM, ICommand>> GoBackCommandPath,
    Expression<Func<TVM, ICommand>> GoToHistoryCommandPath,
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
        ResponsiveElements elements)
    {
        // Build word display
        var wordTextBlock = BuildCardWord(config.WordCardPath);
        elements.WordTextBlock = wordTextBlock;
        elements.BadgesPanel = badges.Panel;
        elements.BadgeBorders.AddRange(badges.Borders);
        elements.BadgeTextBlocks.AddRange(badges.TextBlocks);
        elements.BadgeIcons.AddRange(badges.Icons);

        // Build answer section
        var (answerTextBlock, secondaryTextBlock, placeholder, answerContainer) = BuildAnswerSection(
            config.AnswerStemPath,
            config.AnswerEndingPath,
            config.AlternativeFormsPath,
            config.IsRevealedPath);
        elements.AnswerTextBlock = answerTextBlock;
        elements.AnswerSecondaryTextBlock = secondaryTextBlock;
        elements.AnswerPlaceholder = placeholder;

        // Build translation carousel
        var (translationText, translationBorder, translationContainer) = BuildTranslationCarousel(
            config.CarouselPath, config.IsRevealedPath);
        elements.TranslationTextBlock = translationText;

        // Build example section
        var (exampleText, referenceText, exampleContainer) = BuildExampleSection(config.CarouselPath);
        elements.SuttaExampleTextBlock = exampleText;
        elements.SuttaReferenceTextBlock = referenceText;

        // Build navigation
        var (buttonElements, navContainer) = BuildNavigation(
            config.RevealCommandPath,
            config.HardCommandPath,
            config.EasyCommandPath,
            config.IsRevealedPath);
        elements.ButtonElements.AddRange(buttonElements);

        // Assemble card children
        var cardChildren = new List<UIElement>
        {
            BuildCardHeader(config.WordCardPath, config.RankPrefix),
            wordTextBlock,
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
                    .Padding(LayoutConstants.Paddings.CardPaddingTall)
                    .Spacing(LayoutConstants.Spacing.CardInternal)
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

        // Debug text for size bracket
        var debugText = new TextBlock()
            .Text("Size: --")
            .FontSize(LayoutConstants.FixedFonts.DebugText)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Opacity(0.6);
        elements.DebugTextBlock = debugText;

        // Build content area
        var contentArea = new Grid()
            .RowDefinitions($"{LayoutConstants.Margins.TopSpacing},Auto,Auto,Auto,Auto,*")
            .MaxWidth(LayoutConstants.ContentMaxWidth)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Padding(LayoutConstants.Spacing.ContentPaddingTall, 0)
            .Children(
                new Border().Grid(row: 0), // Fixed top spacing
                cardBorder.Grid(row: 1),
                translationContainer.Margin(0, LayoutConstants.Margins.TranslationContainerTop, 0, 0).Grid(row: 2),
                exampleContainer.Margin(0, LayoutConstants.Margins.ExampleContainerTop, 0, 0).Grid(row: 3),
                debugText.Margin(0, LayoutConstants.Margins.DebugTextTop, 0, 0).Grid(row: 4),
                new Border().Grid(row: 5) // Dynamic bottom spacing
            );
        elements.ContentArea = contentArea;

        // Build title bar and daily goal bar (capture references for measurement)
        var titleBar = AppTitleBar.BuildWithHistory(config.Title, config.GoBackCommandPath, config.GoToHistoryCommandPath);
        var dailyGoalBar = BuildDailyGoalBar(config.DailyGoalTextPath, config.DailyProgressPath);
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
    }

    #endregion

    #region Card Components

    /// <summary>
    /// Builds the header row: [Rank Badge] ... [Anki State]
    /// </summary>
    static Grid BuildCardHeader<TVM>(
        Expression<Func<TVM, WordCardViewModel>> cardPath,
        string rankPrefix)
    {
        return new Grid()
            .ColumnDefinitions("Auto,*,Auto")
            .Children(
                new Border()
                    .Background(ThemeResource.Get<Brush>("PrimaryBrush"))
                    .CornerRadius(LayoutConstants.Sizes.RankBadgeCornerRadius)
                    .Padding(LayoutConstants.Paddings.RankBadgeHorizontal, LayoutConstants.Paddings.RankBadgeVertical)
                    .Child(
                        new StackPanel()
                            .Orientation(Orientation.Horizontal)
                            .Spacing(LayoutConstants.Spacing.RankBadge)
                            .Children(
                                RegularText()
                                    .Text(rankPrefix)
                                    .FontSize(LayoutConstants.FixedFonts.RankText)
                                    .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                                    .Foreground(ThemeResource.Get<Brush>("OnPrimaryBrush")),
                                RegularText()
                                    .Scope(cardPath)
                                    .TextWithin<WordCardViewModel>(c => c.RankText)
                                    .FontSize(LayoutConstants.FixedFonts.RankText)
                                    .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                                    .Foreground(ThemeResource.Get<Brush>("OnPrimaryBrush"))
                            )
                    )
                    .Grid(column: 0),
                RegularText()
                    .Scope(cardPath)
                    .TextWithin<WordCardViewModel>(c => c.AnkiState)
                    .FontSize(LayoutConstants.FixedFonts.AnkiState)
                    .HorizontalAlignment(HorizontalAlignment.Right)
                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                    .Grid(column: 2)
            );
    }

    /// <summary>
    /// Builds the main word TextBlock.
    /// </summary>
    static TextBlock BuildCardWord<TVM>(Expression<Func<TVM, WordCardViewModel>> cardPath)
    {
        return PaliText()
            .Scope(cardPath)
            .TextWithin<WordCardViewModel>(c => c.CurrentWord)
            .FontSize(LayoutConstants.Fonts.WordSizeTall)
            .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .TextAlignment(TextAlignment.Center)
            .TextWrapping(TextWrapping.Wrap)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
            .Margin(0, LayoutConstants.Margins.WordTop, 0, LayoutConstants.Margins.WordBottom);
    }

    /// <summary>
    /// Builds the answer section with spacer, content, and placeholder.
    /// </summary>
    static (TextBlock answerTextBlock, TextBlock secondaryTextBlock, Border placeholder, Grid answerContainer) BuildAnswerSection<TVM>(
        Expression<Func<TVM, string>> answerStemPath,
        Expression<Func<TVM, string>> answerEndingPath,
        Expression<Func<TVM, string>> alternativeFormsPath,
        Expression<Func<TVM, bool>> isRevealedPath)
    {
        // Choose answer display style: toggle between these two lines to test
        // var answerTextBlock = CreateBoldEndingAnswer(answerStemPath, answerEndingPath);
        var answerTextBlock = CreateColorEndingAnswer(answerStemPath, answerEndingPath);

        var alternativeFormsTextBlock = PaliText()
            .FontSize(LayoutConstants.PracticeFontSizes.Tall.AnswerSecondary)
            .FontWeight(Microsoft.UI.Text.FontWeights.Medium)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .TextAlignment(TextAlignment.Center)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
            .Text<TVM>(alternativeFormsPath)
            .StringToVisibility<TextBlock, TVM>(alternativeFormsPath);

        var answerSpacer = new StackPanel()
            .Spacing(LayoutConstants.Spacing.AnswerSpacer)
            .Opacity(0)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Children(
                new TextBlock()
                    .FontSize(LayoutConstants.Fonts.AnswerSizeTall)
                    .Text("X"),
                new TextBlock()
                    .FontSize(LayoutConstants.PracticeFontSizes.Tall.AnswerSecondary)
                    .FontWeight(Microsoft.UI.Text.FontWeights.Medium)
                    .Text("X")
            );

        var answerContent = new StackPanel()
            .Spacing(LayoutConstants.Spacing.AnswerContent)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .VerticalAlignment(VerticalAlignment.Center)
            .BoolToVisibility<StackPanel, TVM>(isRevealedPath)
            .Children(answerTextBlock, alternativeFormsTextBlock);

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
            .Margin(0, LayoutConstants.Margins.AnswerContainerTop, 0, 0)
            .Children(answerSpacer, answerContent, answerPlaceholder);

        return (answerTextBlock, alternativeFormsTextBlock, answerPlaceholder, answerContainer);
    }

    static TextBlock CreateColorEndingAnswer<TVM>(
        Expression<Func<TVM, string>> answerStemPath,
        Expression<Func<TVM, string>> answerEndingPath)
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
            .FontSize(LayoutConstants.Fonts.AnswerSizeTall)
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
        Expression<Func<TVM, string?>> glyphPath,
        Expression<Func<TVM, string>> labelPath,
        Expression<Func<TVM, Brush>> brushPath)
    {
        var icon = new FontIcon()
            .FontSize(LayoutConstants.Fonts.BadgeSizeTall)
            .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
            .VerticalAlignment(VerticalAlignment.Center)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
            .GlyphWithVisibility<TVM>(glyphPath);

        var text = RegularText()
            .FontSize(LayoutConstants.Fonts.BadgeSizeTall)
            .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
            .VerticalAlignment(VerticalAlignment.Center)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
            .Text<TVM>(labelPath);

        var badge = new SquircleBorder()
            .RadiusMode(SquircleRadiusMode.Pill)
            .Fill<TVM>(brushPath)
            .Child(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .Spacing(LayoutConstants.Spacing.BadgeInternal)
                .Padding(LayoutConstants.Paddings.BadgeHorizontalTall, LayoutConstants.Paddings.BadgeVerticalTall)
                .VerticalAlignment(VerticalAlignment.Center)
                .Children(icon, text));

        return (icon, text, badge);
    }

    /// <summary>
    /// Builds a BadgeSet from individual badges.
    /// </summary>
    public static BadgeSet CreateBadgeSet(params (FontIcon icon, TextBlock text, SquircleBorder badge)[] badges)
    {
        var panel = new StackPanel()
            .Orientation(Orientation.Horizontal)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Spacing(LayoutConstants.Spacing.BadgeSpacingTall)
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
        Expression<Func<TVM, bool>> isRevealedPath)
    {
        var translationTextBlock = RegularText()
            .TextWithin<ExampleCarouselViewModel>(c => c.CurrentMeaning)
            .FontSize(LayoutConstants.PracticeFontSizes.Tall.Translation)
            .TextWrapping(TextWrapping.Wrap)
            .TextAlignment(TextAlignment.Center)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"));

        // Shadow reference for measuring single-line height (used to position arrows)
        // This has the same structure as real content but with single-line text
        var singleLineReference = new StackPanel()
            .Spacing(LayoutConstants.Spacing.TranslationContent)
            .Opacity(0)
            .IsHitTestVisible(false)
            .Children(
                RegularText()
                    .Text("M") // Single character to get one-line height
                    .FontSize(LayoutConstants.PracticeFontSizes.Tall.Translation)
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
                    .Padding(LayoutConstants.Paddings.TranslationBorderHorizontal, LayoutConstants.Paddings.TranslationBorderVertical)
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
                                    .Spacing(LayoutConstants.Spacing.TranslationContent)
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
                    .Padding(LayoutConstants.Paddings.TranslationArrowButtonHorizontal, LayoutConstants.Paddings.TranslationArrowButtonVertical)
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
                    .Padding(LayoutConstants.Paddings.TranslationArrowButtonHorizontal, LayoutConstants.Paddings.TranslationArrowButtonVertical)
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
            var singleLineBlockHeight = e.NewSize.Height + (LayoutConstants.Paddings.TranslationBorderVertical * 2);
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
        Expression<Func<TVM, ExampleCarouselViewModel>> carouselPath)
    {
        var exampleTextBlock = PaliText()
            .HtmlTextWithin<ExampleCarouselViewModel>(c => c.CurrentExample)
            .FontSize(LayoutConstants.PracticeFontSizes.Tall.SuttaExample)
            .TextWrapping(TextWrapping.Wrap)
            .TextAlignment(TextAlignment.Center)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"));

        var referenceTextBlock = PaliText()
            .TextWithin<ExampleCarouselViewModel>(c => c.CurrentReference)
            .FontSize(LayoutConstants.PracticeFontSizes.Tall.SuttaReference)
            .TextWrapping(TextWrapping.Wrap)
            .TextAlignment(TextAlignment.Center)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"));

        var container = new StackPanel()
            .Spacing(LayoutConstants.Spacing.ExampleSection)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Scope(carouselPath)
            .Children(exampleTextBlock, referenceTextBlock);

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
        Expression<Func<TVM, bool>> isRevealedPath)
    {
        var (hardIcon, hardText, hardButton) = BuildActionButton<TVM>("Hard", "\uE711", hardCommand, "SurfaceBrush");
        var (easyIcon, easyText, easyButton) = BuildActionButton<TVM>("Easy", "\uE73E", easyCommand, "SurfaceBrush");

        var container = new Grid()
            .MaxWidth(LayoutConstants.ContentMaxWidth)
            .Padding(LayoutConstants.Paddings.NavigationContainerHorizontal, LayoutConstants.Paddings.NavigationContainerVertical)
            .Children(
                // Reveal button - visible when NOT revealed
                BuildRevealButton(revealCommand)
                    .BoolToVisibility<SquircleButton, TVM>(isRevealedPath, invert: true),

                // Hard/Easy buttons - visible when revealed
                new Grid()
                    .ColumnDefinitions("*,*")
                    .ColumnSpacing(LayoutConstants.Spacing.ButtonColumns)
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
            .Padding(LayoutConstants.Paddings.ActionButtonHorizontal, LayoutConstants.Paddings.ActionButtonVertical);
        button.SetBinding(ButtonBase.CommandProperty, Bind.Path(commandPath));
        return button
            .Child(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .HorizontalAlignment(HorizontalAlignment.Center)
                .Spacing(LayoutConstants.Spacing.ButtonContent)
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
        string brushKey)
    {
        var iconElement = new FontIcon()
            .Glyph(glyph)
            .FontSize(LayoutConstants.PracticeFontSizes.Tall.Button)
            .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold);

        var textElement = RegularText()
            .Text(label)
            .FontSize(LayoutConstants.PracticeFontSizes.Tall.Button)
            .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold);

        var button = new SquircleButton()
            .Fill(ThemeResource.Get<Brush>(brushKey))
            .RadiusMode(SquircleRadiusMode.ButtonSmall)
            .Padding(LayoutConstants.Paddings.ActionButtonHorizontal, LayoutConstants.Paddings.ActionButtonVertical);
        button.SetBinding(ButtonBase.CommandProperty, Bind.Path(commandPath));
        button.Child(new StackPanel()
            .Orientation(Orientation.Horizontal)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Spacing(LayoutConstants.Spacing.ButtonContent)
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
        Expression<Func<TVM, double>> dailyProgress)
    {
        return new Border()
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .Padding(LayoutConstants.Paddings.DailyGoalHorizontal, LayoutConstants.Paddings.DailyGoalVertical)
            .Child(
                new StackPanel().Spacing(LayoutConstants.Spacing.DailyGoal).Children(
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
    public static void ApplyResponsiveValues(ResponsiveElements elements, HeightResponsiveHelper.HeightClass heightClass)
    {
        var fonts = HeightResponsiveHelper.GetFontSizes(heightClass);

        // Content padding
        if (elements.ContentArea is not null)
        {
            var padding = HeightResponsiveHelper.GetContentPadding(heightClass);
            elements.ContentArea.Padding(new Thickness(padding, 0, padding, 0));
        }

        // Card padding
        if (elements.CardBorder is not null)
        {
            var cardPad = HeightResponsiveHelper.GetCardPadding(heightClass);
            elements.CardBorder.Padding(new Thickness(cardPad));
        }

        // Badge spacing
        elements.BadgesPanel?.Spacing = HeightResponsiveHelper.GetBadgeSpacing(heightClass);

        // Word font
        elements.WordTextBlock?.FontSize = fonts.Word;

        // Answer fonts
        elements.AnswerTextBlock?.FontSize = fonts.Answer;
        elements.AnswerSecondaryTextBlock?.FontSize = fonts.AnswerSecondary;

        // Badge fonts
        var badgePadding = HeightResponsiveHelper.GetBadgePadding(heightClass);
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
            var availableHeight = HeightResponsiveHelper.MeasureAvailableHeight(elements);
            elements.DebugTextBlock.Text = $"{heightClass} ({availableHeight:F0}pt)";
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
    public TextBlock? WordTextBlock { get; set; }
    public TextBlock? AnswerTextBlock { get; set; }
    public TextBlock? AnswerSecondaryTextBlock { get; set; }
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

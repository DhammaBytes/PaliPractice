using System.Linq.Expressions;
using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Practice.ViewModels;
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
    Expression<Func<TVM, string>> AnswerPath,
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
        var (answerTextBlock, answerContainer) = BuildAnswerSection(
            config.AnswerPath,
            config.AlternativeFormsPath,
            config.IsRevealedPath);
        elements.AnswerTextBlock = answerTextBlock;

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
            .Padding(LayoutConstants.Paddings.CardPaddingTall)
            .Child(
                new StackPanel()
                    .Spacing(12)
                    .Children(cardChildren.ToArray())
            );
        elements.CardBorder = cardBorder;

        // Build content area
        var contentArea = new Grid()
            .RowDefinitions("16,Auto,Auto,Auto,*")
            .MaxWidth(LayoutConstants.ContentMaxWidth)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Padding(LayoutConstants.Spacing.ContentPaddingTall, 0)
            .Children(
                new Border().Grid(row: 0), // Fixed top spacing
                cardBorder.Grid(row: 1),
                BuildTranslationCarousel(config.CarouselPath, config.IsRevealedPath).Margin(0, 12, 0, 0).Grid(row: 2),
                BuildExampleSection(config.CarouselPath).Margin(0, 8, 0, 0).Grid(row: 3),
                new Border().Grid(row: 4) // Dynamic bottom spacing
            );
        elements.ContentArea = contentArea;

        // Main page grid
        return new Grid()
            .SafeArea(SafeArea.InsetMask.VisibleBounds)
            .RowDefinitions("Auto,*,Auto,Auto")
            .Children(
                AppTitleBar.BuildWithHistory(config.Title, config.GoBackCommandPath, config.GoToHistoryCommandPath)
                    .Grid(row: 0),
                contentArea.Grid(row: 1),
                BuildNavigation(
                    config.RevealCommandPath,
                    config.HardCommandPath,
                    config.EasyCommandPath,
                    config.IsRevealedPath)
                    .Grid(row: 2),
                BuildDailyGoalBar(config.DailyGoalTextPath, config.DailyProgressPath)
                    .Grid(row: 3)
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
                    .CornerRadius(12)
                    .Padding(8, 4)
                    .Child(
                        new StackPanel()
                            .Orientation(Orientation.Horizontal)
                            .Spacing(4)
                            .Children(
                                RegularText()
                                    .Text(rankPrefix)
                                    .FontSize(12)
                                    .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                                    .Foreground(ThemeResource.Get<Brush>("OnPrimaryBrush")),
                                RegularText()
                                    .Scope(cardPath)
                                    .TextWithin<WordCardViewModel>(c => c.RankText)
                                    .FontSize(12)
                                    .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                                    .Foreground(ThemeResource.Get<Brush>("OnPrimaryBrush"))
                            )
                    )
                    .Grid(column: 0),
                RegularText()
                    .Scope(cardPath)
                    .TextWithin<WordCardViewModel>(c => c.AnkiState)
                    .FontSize(14)
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
            .FontSize(46)
            .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .TextAlignment(TextAlignment.Center)
            .TextWrapping(TextWrapping.Wrap)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
            .Margin(0, 16, 0, 8);
    }

    /// <summary>
    /// Builds the answer section with spacer, content, and placeholder.
    /// </summary>
    static (TextBlock answerTextBlock, Grid answerContainer) BuildAnswerSection<TVM>(
        Expression<Func<TVM, string>> answerPath,
        Expression<Func<TVM, string>> alternativeFormsPath,
        Expression<Func<TVM, bool>> isRevealedPath)
    {
        var answerTextBlock = PaliText()
            .FontSize(LayoutConstants.Fonts.AnswerSizeTall)
            .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .TextAlignment(TextAlignment.Center)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
            .Text<TVM>(answerPath);

        var alternativeFormsTextBlock = PaliText()
            .FontSize(20)
            .FontWeight(Microsoft.UI.Text.FontWeights.Medium)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .TextAlignment(TextAlignment.Center)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
            .Text<TVM>(alternativeFormsPath)
            .StringToVisibility<TextBlock, TVM>(alternativeFormsPath);

        var answerSpacer = new StackPanel()
            .Spacing(4)
            .Opacity(0)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Children(
                new TextBlock()
                    .FontSize(LayoutConstants.Fonts.AnswerSizeTall)
                    .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                    .Text("X"),
                new TextBlock()
                    .FontSize(20)
                    .FontWeight(Microsoft.UI.Text.FontWeights.Medium)
                    .Text("X")
            );

        var answerContent = new StackPanel()
            .Spacing(4)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .VerticalAlignment(VerticalAlignment.Center)
            .BoolToVisibility<StackPanel, TVM>(isRevealedPath)
            .Children(answerTextBlock, alternativeFormsTextBlock);

        var answerPlaceholder = new Border()
            .Height(2)
            .Margin(40, 0, 40, 0)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Center)
            .BorderBrush(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
            .BorderThickness(0, 0, 0, 2)
            .Opacity(0.5)
            .BoolToVisibility<Border, TVM>(isRevealedPath, invert: true);

        var answerContainer = new Grid()
            .Margin(0, 8, 0, 0)
            .Children(answerSpacer, answerContent, answerPlaceholder);

        return (answerTextBlock, answerContainer);
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
            .FontWeight(Microsoft.UI.Text.FontWeights.Medium)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
            .GlyphWithVisibility<TVM>(glyphPath);

        var text = RegularText()
            .FontSize(LayoutConstants.Fonts.BadgeSizeTall)
            .FontWeight(Microsoft.UI.Text.FontWeights.Medium)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
            .Text<TVM>(labelPath);

        var badge = new SquircleBorder()
            .RadiusMode(SquircleRadiusMode.Pill)
            .Padding(LayoutConstants.Paddings.BadgeHorizontalTall, LayoutConstants.Paddings.BadgeVerticalTall)
            .Fill<TVM>(brushPath)
            .Child(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .Spacing(6)
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
    static StackPanel BuildTranslationCarousel<TVM>(
        Expression<Func<TVM, ExampleCarouselViewModel>> carouselPath,
        Expression<Func<TVM, bool>> isRevealedPath)
    {
        return new StackPanel()
            .Orientation(Orientation.Horizontal)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Spacing(8)
            .Children(
                // Previous button
                new Button()
                    .Background(new SolidColorBrush(Colors.Transparent))
                    .Padding(8, 6)
                    .MinWidth(0)
                    .MinHeight(0)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Scope(carouselPath)
                    .CommandWithin<Button, ExampleCarouselViewModel>(c => c.PreviousCommand)
                    .VisibilityWithin<Button, ExampleCarouselViewModel>(c => c.HasMultipleTranslations)
                    .OpacityWithin<Button, ExampleCarouselViewModel>(c => c.IsRevealed)
                    .Content(new FontIcon()
                        .Glyph("\uE76B") // ChevronLeft
                        .FontSize(14)
                        .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))),

                // Translation in squircle
                new SquircleBorder()
                    .Fill(ThemeResource.Get<Brush>("SurfaceBrush"))
                    .RadiusMode(SquircleRadiusMode.ButtonSmall)
                    .Child(
                        new Border()
                            .Padding(24, 16)
                            .Child(
                                new Grid()
                                    .Scope(carouselPath)
                                    .Children(
                                        // Placeholder "…" shown when not revealed
                                        RegularText()
                                            .Text("…")
                                            .FontSize(24)
                                            .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                                            .HorizontalAlignment(HorizontalAlignment.Center)
                                            .VerticalAlignment(VerticalAlignment.Center)
                                            .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
                                            .VisibilityWithin<TextBlock, ExampleCarouselViewModel>(c => c.IsRevealed, invert: true),

                                        // Translation content
                                        new StackPanel()
                                            .Spacing(8)
                                            .HorizontalAlignment(HorizontalAlignment.Center)
                                            .VerticalAlignment(VerticalAlignment.Center)
                                            .VisibilityWithin<StackPanel, ExampleCarouselViewModel>(c => c.IsRevealed)
                                            .Children(
                                                RegularText()
                                                    .TextWithin<ExampleCarouselViewModel>(c => c.CurrentMeaning)
                                                    .FontSize(16)
                                                    .FontStyle(Windows.UI.Text.FontStyle.Italic)
                                                    .TextWrapping(TextWrapping.Wrap)
                                                    .TextAlignment(TextAlignment.Center)
                                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                                    .MaxWidth(280)
                                                    .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush")),
                                                RegularText()
                                                    .TextWithin<ExampleCarouselViewModel>(c => c.PaginationText)
                                                    .FontSize(11)
                                                    .TextAlignment(TextAlignment.Center)
                                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                                    .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
                                                    .VisibilityWithin<TextBlock, ExampleCarouselViewModel>(c => c.HasMultipleTranslations)
                                            )
                                    )
                            )
                    ),

                // Next button
                new Button()
                    .Background(new SolidColorBrush(Colors.Transparent))
                    .Padding(8, 6)
                    .MinWidth(0)
                    .MinHeight(0)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Scope(carouselPath)
                    .CommandWithin<Button, ExampleCarouselViewModel>(c => c.NextCommand)
                    .VisibilityWithin<Button, ExampleCarouselViewModel>(c => c.HasMultipleTranslations)
                    .OpacityWithin<Button, ExampleCarouselViewModel>(c => c.IsRevealed)
                    .Content(new FontIcon()
                        .Glyph("\uE76C") // ChevronRight
                        .FontSize(14)
                        .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush")))
            );
    }

    #endregion

    #region Example Section

    /// <summary>
    /// Builds the example sentence and reference display.
    /// </summary>
    static StackPanel BuildExampleSection<TVM>(Expression<Func<TVM, ExampleCarouselViewModel>> carouselPath)
    {
        return new StackPanel()
            .Spacing(4)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .MaxWidth(LayoutConstants.ReferenceMaxWidth)
            .Scope(carouselPath)
            .Children(
                PaliText()
                    .HtmlTextWithin<ExampleCarouselViewModel>(c => c.CurrentExample)
                    .FontSize(14)
                    .TextWrapping(TextWrapping.Wrap)
                    .TextAlignment(TextAlignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush")),
                PaliText()
                    .TextWithin<ExampleCarouselViewModel>(c => c.CurrentReference)
                    .FontSize(12)
                    .TextWrapping(TextWrapping.Wrap)
                    .TextAlignment(TextAlignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
            );
    }

    #endregion

    #region Navigation

    /// <summary>
    /// Builds navigation with reveal button that swaps to Easy/Hard after reveal.
    /// </summary>
    static UIElement BuildNavigation<TVM>(
        Expression<Func<TVM, ICommand>> revealCommand,
        Expression<Func<TVM, ICommand>> hardCommand,
        Expression<Func<TVM, ICommand>> easyCommand,
        Expression<Func<TVM, bool>> isRevealedPath)
    {
        return new Grid()
            .MaxWidth(LayoutConstants.ContentMaxWidth)
            .Padding(20, 16)
            .Children(
                // Reveal button - visible when NOT revealed
                BuildRevealButton(revealCommand)
                    .BoolToVisibility<SquircleButton, TVM>(isRevealedPath, invert: true),

                // Hard/Easy buttons - visible when revealed
                new Grid()
                    .ColumnDefinitions("*,*")
                    .ColumnSpacing(16)
                    .BoolToVisibility<Grid, TVM>(isRevealedPath)
                    .Children(
                        BuildActionButton("Hard", "\uE711", hardCommand, "SurfaceBrush")
                            .Grid(column: 0)
                            .HorizontalAlignment(HorizontalAlignment.Stretch),
                        BuildActionButton("Easy", "\uE73E", easyCommand, "SurfaceBrush")
                            .Grid(column: 1)
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                    )
            );
    }

    static SquircleButton BuildRevealButton<TVM>(Expression<Func<TVM, ICommand>> commandPath)
    {
        var button = new SquircleButton()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Fill(ThemeResource.Get<Brush>("PrimaryBrush"))
            .RadiusMode(SquircleRadiusMode.ButtonSmall)
            .Padding(16, 12);
        button.SetBinding(ButtonBase.CommandProperty, Bind.Path(commandPath));
        return button
            .Child(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .HorizontalAlignment(HorizontalAlignment.Center)
                .Spacing(8)
                .Children(
                    new FontIcon()
                        .Glyph("\uE7B3") // Eye icon
                        .FontSize(16)
                        .Foreground(ThemeResource.Get<Brush>("OnPrimaryBrush")),
                    RegularText()
                        .Text("Reveal Answer")
                        .FontSize(16)
                        .Foreground(ThemeResource.Get<Brush>("OnPrimaryBrush"))
                ));
    }

    static SquircleButton BuildActionButton<TVM>(
        string text,
        string glyph,
        Expression<Func<TVM, ICommand>> commandPath,
        string brushKey)
    {
        var button = new SquircleButton()
            .Fill(ThemeResource.Get<Brush>(brushKey))
            .RadiusMode(SquircleRadiusMode.ButtonSmall)
            .Padding(16, 12);
        button.SetBinding(ButtonBase.CommandProperty, Bind.Path(commandPath));
        return button
            .Child(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .HorizontalAlignment(HorizontalAlignment.Center)
                .Spacing(8)
                .Children(
                    new FontIcon().Glyph(glyph).FontSize(16),
                    RegularText().Text(text).FontSize(16)
                ));
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
            .Padding(20, 12)
            .Child(
                new StackPanel().Spacing(8).Children(
                    new Grid().ColumnDefinitions("*,Auto").Children(
                        RegularText()
                            .Text("Daily goal")
                            .FontSize(14)
                            .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                            .Grid(column: 0),
                        RegularText()
                            .Text(dailyGoalText)
                            .FontSize(14)
                            .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                            .Grid(column: 1)
                    ),
                    new ProgressBar()
                        .Maximum(100)
                        .Height(6)
                        .CornerRadius(3)
                        .Value(dailyProgress)
                )
            );
    }

    #endregion

    #region Responsive

    /// <summary>
    /// Applies responsive values to all tracked elements.
    /// </summary>
    public static void ApplyResponsiveValues(ResponsiveElements elements, HeightResponsiveHelper.HeightClass heightClass)
    {
        if (elements.ContentArea is not null)
        {
            var padding = HeightResponsiveHelper.GetContentPadding(heightClass);
            elements.ContentArea.Padding(new Thickness(padding, 0, padding, 0));
        }

        if (elements.BadgesPanel is not null)
        {
            elements.BadgesPanel.Spacing = HeightResponsiveHelper.GetBadgeSpacing(heightClass);
        }

        if (elements.WordTextBlock is not null)
        {
            elements.WordTextBlock.FontSize = HeightResponsiveHelper.GetWordFontSize(heightClass);
        }

        if (elements.AnswerTextBlock is not null)
        {
            elements.AnswerTextBlock.FontSize = HeightResponsiveHelper.GetAnswerFontSize(heightClass);
        }

        if (elements.CardBorder is not null)
        {
            var cardPad = HeightResponsiveHelper.GetCardPadding(heightClass);
            elements.CardBorder.Padding(new Thickness(cardPad));
        }

        var badgePadding = HeightResponsiveHelper.GetBadgePadding(heightClass);
        var badgeFontSize = HeightResponsiveHelper.GetBadgeFontSize(heightClass);

        foreach (var border in elements.BadgeBorders)
        {
            border.Padding(badgePadding);
        }

        foreach (var textBlock in elements.BadgeTextBlocks)
        {
            textBlock.FontSize = badgeFontSize;
        }

        foreach (var icon in elements.BadgeIcons)
        {
            icon.FontSize = badgeFontSize;
        }
    }

    #endregion
}

/// <summary>
/// Holds references to elements for responsive sizing.
/// </summary>
public class ResponsiveElements
{
    public Grid? ContentArea { get; set; }
    public SquircleBorder? CardBorder { get; set; }
    public StackPanel? BadgesPanel { get; set; }
    public TextBlock? WordTextBlock { get; set; }
    public TextBlock? AnswerTextBlock { get; set; }
    public List<SquircleBorder> BadgeBorders { get; } = [];
    public List<TextBlock> BadgeTextBlocks { get; } = [];
    public List<FontIcon> BadgeIcons { get; } = [];
}

using System.Linq.Expressions;
using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Practice.Controls;
using PaliPractice.Themes;
using ExampleCarouselViewModel = PaliPractice.Presentation.Practice.Controls.ViewModels.ExampleCarouselViewModel;
using WordCardViewModel = PaliPractice.Presentation.Practice.Controls.ViewModels.WordCardViewModel;

namespace PaliPractice.Presentation.Practice;

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

/// <summary>
/// Shared layout helpers for practice pages.
/// Extracts common code between DeclensionPracticePage and ConjugationPracticePage.
/// </summary>
public static class PracticeLayoutHelper
{
    /// <summary>
    /// Builds the complete practice page layout.
    /// </summary>
    /// <param name="config">Page configuration with all binding paths</param>
    /// <param name="badges">Badge set built by caller (ViewModel-specific)</param>
    /// <param name="hintElement">Optional hint element (e.g., case hint for declension)</param>
    /// <param name="elements">Output: element references for responsive sizing</param>
    public static Grid BuildPageLayout<TVM>(
        PracticePageConfig<TVM> config,
        BadgeSet badges,
        UIElement? hintElement,
        ResponsiveElements elements)
    {
        // Word TextBlock
        var wordTextBlock = CardWord.Build(config.WordCardPath);
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

        // Build card children list
        var cardChildren = new List<UIElement>
        {
            CardHeader.Build(config.WordCardPath, config.RankPrefix),
            wordTextBlock,
            badges.Panel
        };

        if (hintElement is not null)
        {
            cardChildren.Add(hintElement);
        }

        cardChildren.Add(answerContainer);

        // Build card border with squircle corners (CardSmall = 30% smaller radius)
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
        // Layout: fixed top spacing, card, translation, example, dynamic bottom spacing
        var contentArea = new Grid()
            .RowDefinitions("16,Auto,Auto,Auto,*")
            .MaxWidth(LayoutConstants.ContentMaxWidth)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Padding(LayoutConstants.Spacing.ContentPaddingTall, 0)
            .Children(
                new Border().Grid(row: 0), // Fixed top spacing
                cardBorder.Grid(row: 1),
                TranslationDisplay.Build(config.CarouselPath, config.IsRevealedPath).Margin(0, 12, 0, 0).Grid(row: 2),
                ExampleSection.Build(config.CarouselPath).Margin(0, 8, 0, 0).Grid(row: 3),
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
                PracticeNavigation.BuildWithReveal(
                    config.RevealCommandPath,
                    config.HardCommandPath,
                    config.EasyCommandPath,
                    config.IsRevealedPath)
                    .Grid(row: 2),
                DailyGoalBar.Build(config.DailyGoalTextPath, config.DailyProgressPath)
                    .Grid(row: 3)
            );
    }

    /// <summary>
    /// Builds a grammar badge with icon and text.
    /// Uses medium font weight and improved sizing for better visibility.
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

        var text = new TextBlock()
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

    /// <summary>
    /// Builds the answer section with spacer, content, and placeholder.
    /// </summary>
    public static (TextBlock answerTextBlock, Grid answerContainer) BuildAnswerSection<TVM>(
        Expression<Func<TVM, string>> answerPath,
        Expression<Func<TVM, string>> alternativeFormsPath,
        Expression<Func<TVM, bool>> isRevealedPath)
    {
        var answerTextBlock = new TextBlock()
            .FontSize(LayoutConstants.Fonts.AnswerSizeTall)
            .FontFamily(FontPaths.LibertinusSans)
            .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .TextAlignment(TextAlignment.Center)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
            .Text<TVM>(answerPath);

        var alternativeFormsTextBlock = new TextBlock()
            .FontSize(20)
            .FontFamily(FontPaths.LibertinusSans)
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

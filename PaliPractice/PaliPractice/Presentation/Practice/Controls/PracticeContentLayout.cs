using PaliPractice.Presentation.Common;

namespace PaliPractice.Presentation.Practice.Controls;

/// <summary>
/// Shared layout container for practice pages. Provides centered, constrained content
/// with height-responsive sizing.
/// </summary>
public static class PracticeContentLayout
{
    /// <summary>
    /// Creates a centered container that constrains content to max width and height.
    /// </summary>
    /// <param name="content">The content to wrap</param>
    /// <returns>A Grid container with max constraints and centering</returns>
    public static Grid BuildContainer(UIElement content)
    {
        return new Grid()
            .HorizontalAlignment(HorizontalAlignment.Center)
            .VerticalAlignment(VerticalAlignment.Center)
            .MaxWidth(LayoutConstants.ContentMaxWidth)
            .MaxHeight(LayoutConstants.ContentMaxHeight)
            .Children(content);
    }

    /// <summary>
    /// Holds references to elements that need responsive sizing.
    /// </summary>
    public class ResponsiveElements
    {
        public Grid? ContentGrid { get; set; }
        public StackPanel? BadgesPanel { get; set; }
        public TextBlock? WordTextBlock { get; set; }
        public TextBlock? AnswerTextBlock { get; set; }
        public Border? AnswerBorder { get; set; }
        public Border? CardBorder { get; set; }
        public List<Border> BadgeBorders { get; } = new();
        public List<TextBlock> BadgeTextBlocks { get; } = new();
        public List<FontIcon> BadgeIcons { get; } = new();
    }

    /// <summary>
    /// Attaches height-responsive behavior to the practice content.
    /// Call this after the content Grid is attached to the visual tree.
    /// </summary>
    /// <param name="containerGrid">The Grid containing the practice content (row 1 of the page)</param>
    /// <param name="elements">References to elements that need responsive sizing</param>
    public static void AttachHeightResponsive(Grid containerGrid, ResponsiveElements elements)
    {
        HeightResponsiveHelper.AttachResponsiveHandler(containerGrid, heightClass =>
        {
            ApplyResponsiveValues(elements, heightClass);
        });
    }

    /// <summary>
    /// Applies responsive values to elements based on height class.
    /// </summary>
    static void ApplyResponsiveValues(ResponsiveElements elements, HeightResponsiveHelper.HeightClass heightClass)
    {
        // Content padding
        if (elements.ContentGrid is not null)
        {
            var padding = HeightResponsiveHelper.GetContentPadding(heightClass);
            elements.ContentGrid.Padding(new Thickness(padding));
        }

        // Badges panel spacing
        if (elements.BadgesPanel is not null)
        {
            elements.BadgesPanel.Spacing = HeightResponsiveHelper.GetBadgeSpacing(heightClass);
        }

        // Main word font size
        if (elements.WordTextBlock is not null)
        {
            elements.WordTextBlock.FontSize = HeightResponsiveHelper.GetWordFontSize(heightClass);
        }

        // Answer font size
        if (elements.AnswerTextBlock is not null)
        {
            elements.AnswerTextBlock.FontSize = HeightResponsiveHelper.GetAnswerFontSize(heightClass);
        }

        // Answer border padding
        if (elements.AnswerBorder is not null)
        {
            var answerPad = HeightResponsiveHelper.GetAnswerPadding(heightClass);
            elements.AnswerBorder.Padding(new Thickness(answerPad, answerPad - 4, answerPad, answerPad - 4));
        }

        // Card padding
        if (elements.CardBorder is not null)
        {
            var cardPad = HeightResponsiveHelper.GetCardPadding(heightClass);
            elements.CardBorder.Padding(new Thickness(cardPad));
        }

        // Badge styling
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

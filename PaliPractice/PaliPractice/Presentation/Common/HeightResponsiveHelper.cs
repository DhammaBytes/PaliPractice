using PaliPractice.Presentation.Practice;

namespace PaliPractice.Presentation.Common;

/// <summary>
/// Helper for height-responsive layouts. Measures available height between
/// title bar and daily goal bar using screen coordinates.
/// </summary>
public static class HeightResponsiveHelper
{
    /// <summary>
    /// Height classification based on available vertical space.
    /// </summary>
    public enum HeightClass
    {
        /// <summary>800pt+ - Normal/comfortable layout</summary>
        Tall,
        /// <summary>720-800pt - Slightly reduced spacing</summary>
        Medium,
        /// <summary>600-720pt - Further reduced spacing</summary>
        Short,
        /// <summary>&lt;600pt - Scale down fonts, padding, badges</summary>
        Minimum
    }

    /// <summary>
    /// Determines the height class based on available height.
    /// </summary>
    public static HeightClass GetHeightClass(double height) => height switch
    {
        >= LayoutConstants.HeightTall => HeightClass.Tall,
        >= LayoutConstants.HeightMedium => HeightClass.Medium,
        >= LayoutConstants.HeightShort => HeightClass.Short,
        _ => HeightClass.Minimum
    };

    /// <summary>
    /// Attaches responsive handler that measures available height between title bar and daily goal bar.
    /// </summary>
    public static void AttachResponsiveHandler(
        ResponsiveElements elements,
        Action<HeightClass> onHeightClassChanged)
    {
        if (elements.DailyGoalBar is null)
            return;

        HeightClass? lastClass = null;

        // Attach to daily goal bar since it's laid out last
        elements.DailyGoalBar.SizeChanged += HandleSizeChanged;

        void HandleSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var availableHeight = MeasureAvailableHeight(elements);
            if (availableHeight <= 0) return;

            var newClass = GetHeightClass(availableHeight);
            if (newClass != lastClass)
            {
                lastClass = newClass;
                onHeightClassChanged(newClass);
            }
        }
    }

    /// <summary>
    /// Measures available height between title bar bottom and daily goal bar top.
    /// </summary>
    public static double MeasureAvailableHeight(ResponsiveElements elements)
    {
        if (elements.TitleBar is null || elements.DailyGoalBar is null)
            return 0;

        try
        {
            // Get title bar's bottom Y in screen coordinates
            var titleBarTransform = elements.TitleBar.TransformToVisual(null);
            var titleBarBottom = titleBarTransform.TransformPoint(
                new Windows.Foundation.Point(0, elements.TitleBar.ActualHeight));

            // Get daily goal bar's top Y in screen coordinates
            var dailyGoalTransform = elements.DailyGoalBar.TransformToVisual(null);
            var dailyGoalTop = dailyGoalTransform.TransformPoint(
                new Windows.Foundation.Point(0, 0));

            return dailyGoalTop.Y - titleBarBottom.Y;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Gets the content padding for the given height class.
    /// </summary>
    public static double GetContentPadding(HeightClass heightClass) => heightClass switch
    {
        HeightClass.Tall => LayoutConstants.Spacing.ContentPaddingTall,
        HeightClass.Medium => LayoutConstants.Spacing.ContentPaddingMedium,
        HeightClass.Short => LayoutConstants.Spacing.ContentPaddingShort,
        _ => LayoutConstants.Spacing.ContentPaddingMinimum
    };

    /// <summary>
    /// Gets the section spacing for the given height class.
    /// </summary>
    public static double GetSectionSpacing(HeightClass heightClass) => heightClass switch
    {
        HeightClass.Tall => LayoutConstants.Spacing.SectionSpacingTall,
        HeightClass.Medium => LayoutConstants.Spacing.SectionSpacingMedium,
        HeightClass.Short => LayoutConstants.Spacing.SectionSpacingShort,
        _ => LayoutConstants.Spacing.SectionSpacingMinimum
    };

    /// <summary>
    /// Gets the badge spacing for the given height class.
    /// </summary>
    public static double GetBadgeSpacing(HeightClass heightClass) => heightClass switch
    {
        HeightClass.Tall => LayoutConstants.Spacing.BadgeSpacingTall,
        HeightClass.Medium => LayoutConstants.Spacing.BadgeSpacingMedium,
        HeightClass.Short => LayoutConstants.Spacing.BadgeSpacingShort,
        _ => LayoutConstants.Spacing.BadgeSpacingMinimum
    };

    /// <summary>
    /// Gets the complete font size configuration for the given height class.
    /// </summary>
    public static LayoutConstants.PracticeFontSizes GetFontSizes(HeightClass heightClass) => heightClass switch
    {
        HeightClass.Tall => LayoutConstants.PracticeFontSizes.Tall,
        HeightClass.Medium => LayoutConstants.PracticeFontSizes.Medium,
        HeightClass.Short => LayoutConstants.PracticeFontSizes.Short,
        _ => LayoutConstants.PracticeFontSizes.Minimum
    };

    /// <summary>
    /// Gets the badge padding for the given height class.
    /// </summary>
    public static Thickness GetBadgePadding(HeightClass heightClass) => heightClass switch
    {
        HeightClass.Tall => new Thickness(
            LayoutConstants.Paddings.BadgeHorizontalTall,
            LayoutConstants.Paddings.BadgeVerticalTall,
            LayoutConstants.Paddings.BadgeHorizontalTall,
            LayoutConstants.Paddings.BadgeVerticalTall),
        HeightClass.Medium => new Thickness(
            LayoutConstants.Paddings.BadgeHorizontalMedium,
            LayoutConstants.Paddings.BadgeVerticalMedium,
            LayoutConstants.Paddings.BadgeHorizontalMedium,
            LayoutConstants.Paddings.BadgeVerticalMedium),
        HeightClass.Short => new Thickness(
            LayoutConstants.Paddings.BadgeHorizontalShort,
            LayoutConstants.Paddings.BadgeVerticalShort,
            LayoutConstants.Paddings.BadgeHorizontalShort,
            LayoutConstants.Paddings.BadgeVerticalShort),
        _ => new Thickness(
            LayoutConstants.Paddings.BadgeHorizontalMinimum,
            LayoutConstants.Paddings.BadgeVerticalMinimum,
            LayoutConstants.Paddings.BadgeHorizontalMinimum,
            LayoutConstants.Paddings.BadgeVerticalMinimum)
    };

    /// <summary>
    /// Gets the answer border padding for the given height class.
    /// </summary>
    public static double GetAnswerPadding(HeightClass heightClass) => heightClass switch
    {
        HeightClass.Tall or HeightClass.Medium => LayoutConstants.Paddings.AnswerPaddingTall,
        HeightClass.Short => LayoutConstants.Paddings.AnswerPaddingShort,
        _ => LayoutConstants.Paddings.AnswerPaddingMinimum
    };

    /// <summary>
    /// Gets the card padding for the given height class.
    /// </summary>
    public static double GetCardPadding(HeightClass heightClass) => heightClass switch
    {
        HeightClass.Tall or HeightClass.Medium => LayoutConstants.Paddings.CardPaddingTall,
        HeightClass.Short => LayoutConstants.Paddings.CardPaddingShort,
        _ => LayoutConstants.Paddings.CardPaddingMinimum
    };
}

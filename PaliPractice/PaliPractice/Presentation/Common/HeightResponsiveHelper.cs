namespace PaliPractice.Presentation.Common;

/// <summary>
/// Helper for height-responsive layouts. Uses SizeChanged events to detect
/// height class and apply appropriate sizing values.
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
    /// Attaches a SizeChanged handler that calls the callback when height class changes.
    /// </summary>
    /// <param name="element">The element to monitor for size changes</param>
    /// <param name="onHeightClassChanged">Callback invoked when height class changes</param>
    /// <param name="invokeImmediately">Whether to invoke the callback immediately with current size</param>
    public static void AttachResponsiveHandler(
        FrameworkElement element,
        Action<HeightClass> onHeightClassChanged,
        bool invokeImmediately = true)
    {
        HeightClass? lastClass = null;

        void HandleSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var newClass = GetHeightClass(e.NewSize.Height);
            if (newClass != lastClass)
            {
                lastClass = newClass;
                onHeightClassChanged(newClass);
            }
        }

        element.SizeChanged += HandleSizeChanged;

        if (invokeImmediately && element.ActualHeight > 0)
        {
            var currentClass = GetHeightClass(element.ActualHeight);
            lastClass = currentClass;
            onHeightClassChanged(currentClass);
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
    /// Gets the main word font size for the given height class.
    /// </summary>
    public static double GetWordFontSize(HeightClass heightClass) => heightClass switch
    {
        HeightClass.Tall => LayoutConstants.Fonts.WordSizeTall,
        HeightClass.Medium => LayoutConstants.Fonts.WordSizeMedium,
        HeightClass.Short => LayoutConstants.Fonts.WordSizeShort,
        _ => LayoutConstants.Fonts.WordSizeMinimum
    };

    /// <summary>
    /// Gets the answer font size for the given height class.
    /// </summary>
    public static double GetAnswerFontSize(HeightClass heightClass) => heightClass switch
    {
        HeightClass.Tall => LayoutConstants.Fonts.AnswerSizeTall,
        HeightClass.Medium => LayoutConstants.Fonts.AnswerSizeMedium,
        HeightClass.Short => LayoutConstants.Fonts.AnswerSizeShort,
        _ => LayoutConstants.Fonts.AnswerSizeMinimum
    };

    /// <summary>
    /// Gets the badge font size for the given height class.
    /// </summary>
    public static double GetBadgeFontSize(HeightClass heightClass) => heightClass switch
    {
        HeightClass.Tall or HeightClass.Medium => LayoutConstants.Fonts.BadgeSizeTall,
        _ => LayoutConstants.Fonts.BadgeSizeMinimum
    };

    /// <summary>
    /// Gets the badge padding for the given height class.
    /// </summary>
    public static Thickness GetBadgePadding(HeightClass heightClass) => heightClass switch
    {
        HeightClass.Tall or HeightClass.Medium => new Thickness(
            LayoutConstants.Paddings.BadgeHorizontalTall,
            LayoutConstants.Paddings.BadgeVerticalTall,
            LayoutConstants.Paddings.BadgeHorizontalTall,
            LayoutConstants.Paddings.BadgeVerticalTall),
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

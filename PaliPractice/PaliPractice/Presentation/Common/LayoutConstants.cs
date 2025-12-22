namespace PaliPractice.Presentation.Common;

public static class LayoutConstants
{
    // Width constraints
    public const double ContentMaxWidth = 450;

    // Height constraints
    public const double ContentMaxHeight = 900;

    // Height breakpoints (for responsive sizing)
    // Tall: 800pt+ - Normal/comfortable layout
    // Medium: 720-800pt - Slightly reduced spacing
    // Short: 600-720pt - Further reduced spacing
    // Minimum: <600pt - Scale down fonts, padding, badges
    public const double HeightTall = 800;
    public const double HeightMedium = 720;
    public const double HeightShort = 600;

    /// <summary>
    /// Spacing values for each height breakpoint.
    /// </summary>
    public static class Spacing
    {
        // Content area padding
        public const double ContentPaddingTall = 24;
        public const double ContentPaddingMedium = 20;
        public const double ContentPaddingShort = 16;
        public const double ContentPaddingMinimum = 12;

        // Gap between main sections
        public const double SectionSpacingTall = 20;
        public const double SectionSpacingMedium = 16;
        public const double SectionSpacingShort = 12;
        public const double SectionSpacingMinimum = 8;

        // Badge spacing
        public const double BadgeSpacingTall = 12;
        public const double BadgeSpacingMedium = 10;
        public const double BadgeSpacingShort = 8;
        public const double BadgeSpacingMinimum = 6;
    }

    /// <summary>
    /// Font sizes for each height breakpoint.
    /// </summary>
    public static class Fonts
    {
        // Main word (lemma)
        public const double WordSizeTall = 48;
        public const double WordSizeMedium = 44;
        public const double WordSizeShort = 40;
        public const double WordSizeMinimum = 36;

        // Answer display
        public const double AnswerSizeTall = 32;
        public const double AnswerSizeMedium = 28;
        public const double AnswerSizeShort = 26;
        public const double AnswerSizeMinimum = 24;

        // Badge text (improved for better visibility)
        public const double BadgeSizeTall = 18;
        public const double BadgeSizeMedium = 16;
        public const double BadgeSizeShort = 14;
        public const double BadgeSizeMinimum = 12;
    }

    /// <summary>
    /// Padding values for each height breakpoint.
    /// </summary>
    public static class Paddings
    {
        // Badge internal padding (horizontal, vertical) - improved for better appearance
        public const double BadgeHorizontalTall = 16;
        public const double BadgeHorizontalMedium = 14;
        public const double BadgeHorizontalShort = 12;
        public const double BadgeHorizontalMinimum = 10;
        public const double BadgeVerticalTall = 8;
        public const double BadgeVerticalMedium = 7;
        public const double BadgeVerticalShort = 6;
        public const double BadgeVerticalMinimum = 5;

        // Answer border padding
        public const double AnswerPaddingTall = 16;
        public const double AnswerPaddingShort = 14;
        public const double AnswerPaddingMinimum = 12;

        // WordCard internal padding
        public const double CardPaddingTall = 24;
        public const double CardPaddingShort = 20;
        public const double CardPaddingMinimum = 16;
    }
}

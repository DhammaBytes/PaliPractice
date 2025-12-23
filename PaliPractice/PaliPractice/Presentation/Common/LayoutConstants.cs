namespace PaliPractice.Presentation.Common;

public static class LayoutConstants
{
    // Width constraints
    public const double ContentMaxWidth = 450;
    public const double ReferenceMaxWidth = ContentMaxWidth * 0.8;

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
    /// Centralized font size configuration for practice pages.
    /// All sizes are precisely defined for each screen bracket.
    /// </summary>
    public readonly record struct PracticeFontSizes(
        double Word,            // Main word (lemma)
        double Answer,          // Primary answer
        double AnswerSecondary, // Alternative forms
        double Badge,           // Badge text and icon
        double BadgeHint,       // Declension case hint (1pt less than badge)
        double Translation,     // Translation text
        double SuttaExample,    // Sutta example sentence
        double SuttaReference,  // Sutta reference
        double Button           // Action button text
    )
    {
        /// <summary>800pt+ - Comfortable layout</summary>
        public static readonly PracticeFontSizes Tall = new(
            Word: 44,
            Answer: 30,
            AnswerSecondary: 22,
            Badge: 18,
            BadgeHint: 17,
            Translation: 18,
            SuttaExample: 17,
            SuttaReference: 15,
            Button: 18
        );

        /// <summary>720-800pt - Slightly reduced</summary>
        public static readonly PracticeFontSizes Medium = new(
            Word: 42,
            Answer: 28,
            AnswerSecondary: 21,
            Badge: 17,
            BadgeHint: 16,
            Translation: 17,
            SuttaExample: 16,
            SuttaReference: 14,
            Button: 17
        );

        /// <summary>600-720pt - Further reduced</summary>
        public static readonly PracticeFontSizes Short = new(
            Word: 40,
            Answer: 26,
            AnswerSecondary: 20,
            Badge: 16,
            BadgeHint: 15,
            Translation: 16,
            SuttaExample: 15,
            SuttaReference: 13,
            Button: 16
        );

        /// <summary>&lt;600pt - Minimum sizes</summary>
        public static readonly PracticeFontSizes Minimum = new(
            Word: 36,
            Answer: 24,
            AnswerSecondary: 18,
            Badge: 15,
            BadgeHint: 14,
            Translation: 15,
            SuttaExample: 14,
            SuttaReference: 12,
            Button: 15
        );
    }

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
    /// Legacy font sizes - use PracticeFontSizes for new code.
    /// </summary>
    public static class Fonts
    {
        // Used for initial layout before responsive handler kicks in
        public const double WordSizeTall = 44;
        public const double AnswerSizeTall = 30;
        public const double BadgeSizeTall = 18;
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

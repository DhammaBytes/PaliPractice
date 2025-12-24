namespace PaliPractice.Presentation.Common;

public static class LayoutConstants
{
    #region Width/Height Constraints

    public const double ContentMaxWidth = 450;
    public const double ContentMaxHeight = 900;

    // Translation block width as percentage of card width
    public const double TranslationWidthRatio = 0.76;
    public const double AnswerPlaceholderWidthRatio = 0.5;

    #endregion

    #region Height Breakpoints

    // Tall: 800pt+ - Normal/comfortable layout
    // Medium: 720-800pt - Slightly reduced spacing
    // Short: 600-720pt - Further reduced spacing
    // Minimum: <600pt - Scale down fonts, padding, badges
    public const double HeightTall = 800;
    public const double HeightMedium = 720;
    public const double HeightShort = 600;

    #endregion

    #region Font Sizes

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
    /// Fixed font sizes (not responsive).
    /// </summary>
    public static class FixedFonts
    {
        public const double RankText = 12;
        public const double AnkiState = 14;
        public const double DebugText = 10;
        public const double TranslationPagination = 11;
        public const double TranslationPlaceholder = 24;
        public const double TranslationArrowIcon = 14;
        public const double DailyGoalText = 14;
        public const double RevealButton = 16;
    }

    #endregion

    #region Spacing

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

        // Card internal spacing
        public const double CardInternal = 12;
        public const double BadgeInternal = 6;

        // Translation block
        public const double TranslationContent = 8;

        // Answer section
        public const double AnswerSpacer = 4;
        public const double AnswerContent = 4;

        // Example section
        public const double ExampleSection = 4;

        // Navigation
        public const double ButtonColumns = 16;
        public const double ButtonContent = 8;

        // Daily goal bar
        public const double DailyGoal = 8;

        // Header
        public const double RankBadge = 4;
    }

    #endregion

    #region Paddings

    /// <summary>
    /// Padding values for each height breakpoint.
    /// </summary>
    public static class Paddings
    {
        // Badge internal padding (horizontal, vertical)
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

        // Translation block
        public const double TranslationBorderHorizontal = 24;
        public const double TranslationBorderVertical = 16;
        public const double TranslationArrowButtonHorizontal = 8;
        public const double TranslationArrowButtonVertical = 6;

        // Rank badge
        public const double RankBadgeHorizontal = 8;
        public const double RankBadgeVertical = 4;

        // Navigation buttons
        public const double NavigationContainerHorizontal = 20;
        public const double NavigationContainerVertical = 16;
        public const double ActionButtonHorizontal = 16;
        public const double ActionButtonVertical = 12;

        // Daily goal bar
        public const double DailyGoalHorizontal = 20;
        public const double DailyGoalVertical = 12;
    }

    #endregion

    #region Margins

    /// <summary>
    /// Margin values for layout elements.
    /// </summary>
    public static class Margins
    {
        public const double TopSpacing = 16;
        public const double TranslationContainerTop = 12;
        public const double ExampleContainerTop = 8;
        public const double DebugTextTop = 12;
        public const double AnswerContainerTop = 8;
        public const double WordTop = 16;
        public const double WordBottom = 8;
    }

    #endregion

    #region Sizes

    /// <summary>
    /// Fixed sizes for UI elements.
    /// </summary>
    public static class Sizes
    {
        public const double RankBadgeCornerRadius = 12;
        public const double PlaceholderHeight = 2;
        public const double PlaceholderBorderThickness = 2;
        public const double ProgressBarHeight = 6;
        public const double ProgressBarCornerRadius = 3;
    }

    #endregion
}

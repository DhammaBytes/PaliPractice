namespace PaliPractice.Presentation.Common;

/// <summary>
/// Height classification based on available vertical space.
/// </summary>
public enum HeightClass
{
    Tall,
    Medium,
    Short,
    Minimum
}

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
    
    // lower limits
    const double HeightTall = 800;
    const double HeightMedium = 700; // iPhone 8+ (414×736), iPad Mini (744×1133), taller old Androids
    const double HeightShort = 600; // iPhone 8 (375×667, old Androids (360×640), old desktops (1024×768, 1280×720)
    // anything smaller: iPhone 5s (320×568)

    /// <summary>
    /// Determines the height class based on window height.
    /// Uses window height directly for instant availability.
    /// </summary>
    static HeightClass GetHeightClass(double windowHeight) => windowHeight switch
    {
        >= HeightTall => HeightClass.Tall,
        >= HeightMedium => HeightClass.Medium,
        >= HeightShort => HeightClass.Short,
        _ => HeightClass.Minimum
    };

    /// <summary>
    /// Gets the current HeightClass from the window.
    /// </summary>
    public static HeightClass GetCurrentHeightClass()
    {
        var window = App.MainWindow;
        return GetHeightClass(window?.Bounds.Height ?? HeightMedium);
    }

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
        double BadgeHint,       // Declension case hint
        double Translation,     // Translation text
        double SuttaExample,    // Sutta example sentence
        double SuttaReference,  // Sutta reference
        double Button           // Action button text
    )
    {
        static readonly PracticeFontSizes Tall = new(
            Word: 35,
            Answer: 29,
            AnswerSecondary: 20,
            Badge: 15,
            BadgeHint: 15,
            Translation: 18,
            SuttaExample: 17,
            SuttaReference: 15,
            Button: 18
        );

        static readonly PracticeFontSizes Medium = new(
            Word: 34,
            Answer: 28,
            AnswerSecondary: 18,
            Badge: 15,
            BadgeHint: 15,
            Translation: 17,
            SuttaExample: 16,
            SuttaReference: 14,
            Button: 17
        );

        static readonly PracticeFontSizes Short = new(
            Word: 32,
            Answer: 26,
            AnswerSecondary: 17,
            Badge: 15,
            BadgeHint: 15,
            Translation: 16,
            SuttaExample: 15,
            SuttaReference: 13,
            Button: 16
        );

        static readonly PracticeFontSizes Minimum = new(
            Word: 29,
            Answer: 24,
            AnswerSecondary: 16,
            Badge: 15,
            BadgeHint: 15,
            Translation: 15,
            SuttaExample: 14,
            SuttaReference: 12,
            Button: 15
        );

        public static PracticeFontSizes Get(HeightClass heightClass) => heightClass switch
        {
            HeightClass.Tall => Tall,
            HeightClass.Medium => Medium,
            HeightClass.Short => Short,
            _ => Minimum
        };
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

    #region Gaps

    /// <summary>
    /// Unified gap values for padding, margins, and spacing throughout the layout.
    /// </summary>
    public static class Gaps
    {
        #region Responsive gaps

        /// <summary>
        /// Primary gap - used for content padding, section margins, button columns.
        /// Controls: content area sides, card-to-translation, translation-to-example, nav-to-dailygoal, easy/hard spacing.
        /// </summary>
        public static double Primary(HeightClass h) => h switch
        {
            HeightClass.Tall or HeightClass.Medium => 16,
            HeightClass.Short => 12,
            _ => 10
        };

        /// <summary>
        /// Card internal padding.
        /// </summary>
        public static double Card(HeightClass h) => h switch
        {
            HeightClass.Tall or HeightClass.Medium => 24,
            HeightClass.Short => 20,
            _ => 16
        };

        /// <summary>
        /// Badge internal padding (horizontal, vertical).
        /// </summary>
        public static Thickness Badge(HeightClass h) => new(6, 3, 6, 3);

        /// <summary>
        /// Spacing between badges.
        /// </summary>
        public static double BadgeSpacing(HeightClass h) => h switch
        {
            HeightClass.Tall or HeightClass.Medium => 8,
            _ => 6
        };

        #endregion

        #region Fixed gaps

        // Card internal
        public const double CardInternal = 12;

        // Word section
        public const double WordTop = 16;
        public const double WordBottom = 8;

        // Answer section
        public const double AnswerTop = 8;
        public const double AnswerLines = 4;

        // Badge
        public const double BadgeInternal = 6;

        // Translation block
        public const double TranslationHorizontal = 24;
        public const double TranslationVertical = 16;
        public const double TranslationContent = 8;
        public const double TranslationArrowHorizontal = 8;
        public const double TranslationArrowVertical = 6;

        // Example section
        public const double ExampleSection = 4;

        // Buttons
        public const double ButtonContent = 8;
        public const double ActionButtonHorizontal = 16;
        public const double ActionButtonVertical = 12;

        // Daily goal
        public const double DailyGoal = 8;

        #endregion
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

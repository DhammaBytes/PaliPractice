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
    const double HeightMedium = 720;
    const double HeightShort = 600;

    /// <summary>
    /// Determines the height class based on window height.
    /// Uses window height directly for instant availability (no layout wait).
    /// </summary>
    public static HeightClass GetHeightClass(double windowHeight) => windowHeight switch
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
            Translation: 19,
            SuttaExample: 18,
            SuttaReference: 17,
            Button: 18
        );

        static readonly PracticeFontSizes Medium = new(
            Word: 34,
            Answer: 28,
            AnswerSecondary: 18,
            Badge: 15,
            BadgeHint: 15,
            Translation: 18,
            SuttaExample: 17,
            SuttaReference: 16,
            Button: 17
        );

        static readonly PracticeFontSizes Short = new(
            Word: 32,
            Answer: 26,
            AnswerSecondary: 17,
            Badge: 15,
            BadgeHint: 15,
            Translation: 17,
            SuttaExample: 16,
            SuttaReference: 15,
            Button: 16
        );

        static readonly PracticeFontSizes Minimum = new(
            Word: 29,
            Answer: 24,
            AnswerSecondary: 16,
            Badge: 15,
            BadgeHint: 15,
            Translation: 16,
            SuttaExample: 15,
            SuttaReference: 14,
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

    #region Spacing

    /// <summary>
    /// Spacing values for StackPanel.Spacing and Grid row/column spacing.
    /// </summary>
    public static class Spacing
    {
        public static double BadgeSpacing(HeightClass h) => 8; // Currently uniform

        // Fixed spacing (not responsive)
        public const double CardInternal = 12;
        public const double BadgeInternal = 6;
        public const double TranslationContent = 8;
        public const double AnswerSpacer = 4;
        public const double AnswerContent = 4;
        public const double ExampleSection = 4;
        public const double ButtonColumns = 16;
        public const double ButtonContent = 8;
        public const double DailyGoal = 8;
        public const double RankBadge = 4;
    }

    #endregion

    #region Paddings

    /// <summary>
    /// Padding values for element internal spacing.
    /// </summary>
    public static class Paddings
    {
        public static double ContentHorizontal(HeightClass h) => h switch
        {
            HeightClass.Tall => 16,
            HeightClass.Medium => 16,
            _ => 12
        };

        public static Thickness BadgePadding(HeightClass h) => h switch
        {
            _ => new Thickness(6, 3, 6, 3)
        };

        public static double AnswerPadding(HeightClass h) => h switch
        {
            HeightClass.Tall or HeightClass.Medium => 16,
            HeightClass.Short => 14,
            _ => 12
        };

        public static double CardPadding(HeightClass h) => h switch
        {
            HeightClass.Tall or HeightClass.Medium => 24,
            HeightClass.Short => 20,
            _ => 16
        };

        // Fixed paddings (not responsive)
        public const double TranslationBorderHorizontal = 24;
        public const double TranslationBorderVertical = 16;
        public const double TranslationArrowButtonHorizontal = 8;
        public const double TranslationArrowButtonVertical = 6;
        public const double RankBadgeHorizontal = 8;
        public const double RankBadgeVertical = 4;
        public const double NavigationContainerHorizontal = 20;
        public const double NavigationContainerVertical = 16;
        public const double ActionButtonHorizontal = 16;
        public const double ActionButtonVertical = 12;
        public const double DailyGoalHorizontal = 20;
        public const double DailyGoalVertical = 12;
    }

    #endregion

    #region Margins

    /// <summary>
    /// Margin values for layout elements (fixed, not responsive).
    /// </summary>
    public static class Margins
    {
        public const double CardTop = 16;
        public const double TranslationTop = 16;
        
        public const double ExampleContainerTop = 8;
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

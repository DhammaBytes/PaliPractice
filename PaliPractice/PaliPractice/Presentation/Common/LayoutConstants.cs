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
    // public const double ContentMaxHeight = 900;

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
        // Card content
        double Word,            // Main word (lemma)
        double Answer,          // Primary answer
        double AnswerSecondary, // Alternative forms
        double Badge,           // Badge text and icon
        double BadgeHint,       // Declension case hint
        // Card header
        double PaliRoot,        // √Root display
        double Level,           // Level indicator
        // Translation block
        double Translation,     // Translation text
        double TranslationPagination, // Page indicator (1/3)
        double TranslationDots, // "…" placeholder
        // Example section
        double SuttaExample,    // Sutta example sentence
        double SuttaReference,  // Sutta reference
        // Buttons and footer
        double Button,          // Action button text (Hard/Easy)
        double RevealButton,    // Reveal button text
        double DailyGoal,       // Daily goal text
        // Debug
        double Debug            // Size bracket debug text
    )
    {
        static readonly PracticeFontSizes Tall = new(
            Word: 32,
            Answer: 30,
            AnswerSecondary: 22,
            Badge: 16,
            BadgeHint: 16,
            PaliRoot: 15,
            Level: 14,
            Translation: 18,
            TranslationPagination: 12,
            TranslationDots: 26,
            SuttaExample: 17,
            SuttaReference: 15,
            Button: 18,
            RevealButton: 17,
            DailyGoal: 14,
            Debug: 10
        );

        static readonly PracticeFontSizes Medium = new(
            Word: 34,
            Answer: 28,
            AnswerSecondary: 18,
            Badge: 15,
            BadgeHint: 15,
            PaliRoot: 15,
            Level: 14,
            Translation: 17,
            TranslationPagination: 11,
            TranslationDots: 24,
            SuttaExample: 16,
            SuttaReference: 14,
            Button: 17,
            RevealButton: 16,
            DailyGoal: 14,
            Debug: 10
        );

        static readonly PracticeFontSizes Short = new(
            Word: 32,
            Answer: 26,
            AnswerSecondary: 17,
            Badge: 15,
            BadgeHint: 15,
            PaliRoot: 14,
            Level: 13,
            Translation: 16,
            TranslationPagination: 11,
            TranslationDots: 22,
            SuttaExample: 15,
            SuttaReference: 13,
            Button: 16,
            RevealButton: 15,
            DailyGoal: 13,
            Debug: 10
        );

        static readonly PracticeFontSizes Minimum = new(
            Word: 29,
            Answer: 24,
            AnswerSecondary: 16,
            Badge: 15,
            BadgeHint: 15,
            PaliRoot: 13,
            Level: 12,
            Translation: 15,
            TranslationPagination: 10,
            TranslationDots: 20,
            SuttaExample: 14,
            SuttaReference: 12,
            Button: 15,
            RevealButton: 14,
            DailyGoal: 12,
            Debug: 10
        );

        public static PracticeFontSizes Get(HeightClass heightClass) => heightClass switch
        {
            HeightClass.Tall => Tall,
            HeightClass.Medium => Medium,
            HeightClass.Short => Short,
            _ => Minimum
        };
    }

    #endregion

    #region Gaps

    /// <summary>
    /// Gap values for padding, margins, and spacing throughout the layout.
    /// Naming convention: suffix indicates UI property (Padding/Margin/Spacing).
    /// </summary>
    public static class Gaps
    {
        // === Responsive (height-dependent) ===

        /// <summary>
        /// Main content spacing - used for content area margins, section spacing, button column gaps.
        /// </summary>
        public static double ContentSpacing(HeightClass h) => h switch
        {
            HeightClass.Tall or HeightClass.Medium => 16,
            HeightClass.Short => 12,
            _ => 10
        };

        // === Card ===

        /// <summary>Internal padding of the practice card.</summary>
        public static double CardPadding(HeightClass h) => h switch
        {
            HeightClass.Tall or HeightClass.Medium => 24,
            HeightClass.Short => 20,
            _ => 16
        };

        /// <summary>Spacing between card child elements (word, answer, badges).</summary>
        public const double CardContentSpacing = 12;

        // === Badge ===

        /// <summary>Spacing between badge icon and text inside a badge.</summary>
        public const double BadgeIconTextSpacing = 6;

        /// <summary>Padding inside the badge border.</summary>
        public static readonly Thickness BadgePadding = new(6, 3, 6, 3);

        /// <summary>Spacing between badges in the badge row.</summary>
        public static double BadgeRowSpacing(HeightClass h) => h switch
        {
            HeightClass.Tall or HeightClass.Medium => 8,
            _ => 6
        };

        // === Word Section ===

        /// <summary>Margin above the main word.</summary>
        public const double WordMarginTop = 16;

        /// <summary>Margin below the main word.</summary>
        public const double WordMarginBottom = 8;

        // === Answer Section ===

        /// <summary>Margin above the answer container.</summary>
        public const double AnswerMarginTop = 8;

        /// <summary>Spacing between answer lines.</summary>
        public const double AnswerLineSpacing = 4;

        // === Translation Block ===

        /// <summary>Horizontal padding inside translation block.</summary>
        public const double TranslationPaddingH = 24;

        /// <summary>Vertical padding inside translation block.</summary>
        public const double TranslationPaddingV = 16;

        /// <summary>Spacing between translation text and pagination.</summary>
        public const double TranslationContentSpacing = 8;

        // === Example Section ===

        /// <summary>Spacing between example sentence and reference text.</summary>
        public const double ExampleLineSpacing = 4;

        // === Buttons ===

        /// <summary>Spacing between button icon and text.</summary>
        public const double ButtonIconTextSpacing = 8;

        /// <summary>Horizontal padding for action buttons (reveal, hard, easy).</summary>
        public const double ActionButtonPaddingH = 16;

        /// <summary>Vertical padding for action buttons.</summary>
        public const double ActionButtonPaddingV = 12;

        // === Daily Goal ===

        /// <summary>Spacing between daily goal text and progress bar.</summary>
        public const double DailyGoalSpacing = 8;
    }

    #endregion

    #region Sizes

    /// <summary>
    /// Fixed sizes for UI elements.
    /// </summary>
    public static class Sizes
    {
        /// <summary>Height of the underline shown before answer is revealed.</summary>
        public const double PlaceholderHeight = 2;

        /// <summary>Border thickness for the answer placeholder underline.</summary>
        public const double PlaceholderBorderThickness = 2;

        /// <summary>Height of the daily goal progress bar.</summary>
        public const double ProgressBarHeight = 6;

        /// <summary>Corner radius for the daily goal progress bar.</summary>
        public const double ProgressBarCornerRadius = 3;
    }

    #endregion
}

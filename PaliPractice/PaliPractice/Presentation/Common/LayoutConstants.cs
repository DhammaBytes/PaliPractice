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

/// <summary>
/// Layout constants for the app. All values use expression-bodied properties
/// to support hot reload during development.
/// </summary>
public static class LayoutConstants
{
    #region Width/Height Constraints

    public static double ContentMaxWidth => 450;

    // Translation block width as percentage of card width
    public static double TranslationWidthRatio => 0.76;
    public static double AnswerPlaceholderWidthRatio => 0.5;

    #endregion

    #region Height Breakpoints

    // Lower limits for height classes
    static double HeightTall => 800;
    static double HeightMedium => 700; // iPhone 8+ (414×736), iPad Mini (744×1133), taller old Androids
    static double HeightShort => 600; // iPhone 8 (375×667), old Androids (360×640), old desktops (1024×768, 1280×720)
    // anything smaller: iPhone 5s (320×568)

    /// <summary>
    /// Determines the height class based on window height.
    /// Uses window height directly for instant availability.
    /// </summary>
    static HeightClass GetHeightClass(double windowHeight) => windowHeight switch
    {
        >= 800 => HeightClass.Tall,
        >= 700 => HeightClass.Medium,
        >= 600 => HeightClass.Short,
        _ => HeightClass.Minimum
    };

    /// <summary>
    /// Gets the current HeightClass from the window.
    /// </summary>
    public static HeightClass GetCurrentHeightClass()
    {
        var window = App.MainWindow;
        return GetHeightClass(window?.Bounds.Height ?? 700);
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
        // Fixed font sizes per user specification (same across all height classes)
        static PracticeFontSizes Tall => new(
            Word: 33,
            Answer: 30,
            AnswerSecondary: 22,
            Badge: 16,
            BadgeHint: 16,
            PaliRoot: 18,
            Level: 16,
            Translation: 18,
            TranslationPagination: 13,
            TranslationDots: 26,
            SuttaExample: 18,
            SuttaReference: 16,
            Button: 20,
            RevealButton: 20,
            DailyGoal: 15,
            Debug: 10
        );

        static PracticeFontSizes Medium => new(
            Word: 33,
            Answer: 30,
            AnswerSecondary: 22,
            Badge: 16,
            BadgeHint: 16,
            PaliRoot: 18,
            Level: 16,
            Translation: 18,
            TranslationPagination: 13,
            TranslationDots: 26,
            SuttaExample: 18,
            SuttaReference: 16,
            Button: 20,
            RevealButton: 20,
            DailyGoal: 15,
            Debug: 10
        );

        static PracticeFontSizes Short => new(
            Word: 33,
            Answer: 30,
            AnswerSecondary: 22,
            Badge: 16,
            BadgeHint: 16,
            PaliRoot: 18,
            Level: 16,
            Translation: 18,
            TranslationPagination: 13,
            TranslationDots: 26,
            SuttaExample: 18,
            SuttaReference: 16,
            Button: 20,
            RevealButton: 20,
            DailyGoal: 15,
            Debug: 10
        );

        static PracticeFontSizes Minimum => new(
            Word: 33,
            Answer: 30,
            AnswerSecondary: 22,
            Badge: 16,
            BadgeHint: 16,
            PaliRoot: 18,
            Level: 16,
            Translation: 18,
            TranslationPagination: 13,
            TranslationDots: 26,
            SuttaExample: 18,
            SuttaReference: 16,
            Button: 20,
            RevealButton: 20,
            DailyGoal: 15,
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

        /// <summary>Internal padding of the practice card (sides).</summary>
        public static double CardPadding(HeightClass h) => h switch
        {
            HeightClass.Tall or HeightClass.Medium => 24,
            HeightClass.Short => 20,
            _ => 16
        };

        /// <summary>Top padding of the practice card (25% smaller than sides).</summary>
        public static double CardPaddingTop(HeightClass h) => h switch
        {
            HeightClass.Tall or HeightClass.Medium => 18, // 24 * 0.75
            HeightClass.Short => 15, // 20 * 0.75
            _ => 12 // 16 * 0.75
        };

        /// <summary>Bottom padding of the practice card (10% smaller than sides).</summary>
        public static double CardPaddingBottom(HeightClass h) => h switch
        {
            HeightClass.Tall or HeightClass.Medium => 22, // 24 * 0.9
            HeightClass.Short => 18, // 20 * 0.9
            _ => 14 // 16 * 0.9
        };

        /// <summary>Spacing between card child elements (word, answer, badges).</summary>
        public static double CardContentSpacing => 10;

        // === Badge ===

        /// <summary>Spacing between badge icon and text inside a badge.</summary>
        public static double BadgeIconTextSpacing => 6;

        /// <summary>Padding inside the badge border.</summary>
        public static Thickness BadgePadding => new(6, 3, 6, 3);

        /// <summary>Spacing between badges in the badge row.</summary>
        public static double BadgeRowSpacing(HeightClass h) => h switch
        {
            HeightClass.Tall or HeightClass.Medium => 8,
            _ => 6
        };

        // === Word Section ===

        /// <summary>Margin above the main word.</summary>
        public static double WordMarginTop => 12;

        /// <summary>Margin below the main word.</summary>
        public static double WordMarginBottom => 12;

        // === Answer Section ===

        /// <summary>Margin above the answer container.</summary>
        public static double AnswerMarginTop => 6;

        /// <summary>Spacing between answer lines.</summary>
        public static double AnswerLineSpacing => 4;

        // === Translation Block ===

        /// <summary>Horizontal padding inside translation block.</summary>
        public static double TranslationPaddingH => 24;

        /// <summary>Vertical padding inside translation block.</summary>
        public static double TranslationPaddingV => 16;

        /// <summary>Spacing between translation text and pagination.</summary>
        public static double TranslationContentSpacing => 8;

        // === Example Section ===

        /// <summary>Spacing between example sentence and reference text.</summary>
        public static double ExampleLineSpacing => 4;

        // === Buttons ===

        /// <summary>Spacing between button icon and text.</summary>
        public static double ButtonIconTextSpacing => 8;

        /// <summary>Horizontal padding for action buttons (reveal, hard, easy).</summary>
        public static double ActionButtonPaddingH => 16;

        /// <summary>Vertical padding for action buttons.</summary>
        public static double ActionButtonPaddingV => 10;

        // === Daily Goal ===

        /// <summary>Spacing between daily goal text and progress bar.</summary>
        public static double DailyGoalSpacing => 6;

        /// <summary>Top margin above daily goal bar (spacing from nav buttons).</summary>
        public static double NavToDailyGoalMargin(HeightClass h) => h switch
        {
            HeightClass.Tall or HeightClass.Medium => 13, // 16 * 0.8
            HeightClass.Short => 10, // 12 * 0.8
            _ => 8 // 10 * 0.8
        };
    }

    #endregion

    #region Sizes

    /// <summary>
    /// Fixed sizes for UI elements.
    /// </summary>
    public static class Sizes
    {
        /// <summary>Height of the underline shown before answer is revealed.</summary>
        public static double PlaceholderHeight => 2;

        /// <summary>Border thickness for the answer placeholder underline.</summary>
        public static double PlaceholderBorderThickness => 2;

        /// <summary>Height of the daily goal progress bar.</summary>
        public static double ProgressBarHeight => 8;

        /// <summary>Corner radius for the daily goal progress bar.</summary>
        public static double ProgressBarCornerRadius => 8;

        /// <summary>Default stroke thickness for squircle buttons with borders.</summary>
        public static double ButtonStrokeThickness => 1.5;
    }

    #endregion
}

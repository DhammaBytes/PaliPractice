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

    public static int ContentMaxWidth => 450;

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
            Word: 30,
            Answer: 27,
            AnswerSecondary: 20,
            
            Badge: 15,
            BadgeHint: 15,
            
            PaliRoot: 17,
            Level: 15,
            
            Translation: 17,
            TranslationPagination: 13,
            TranslationDots: 26,
            
            SuttaExample: 17,
            SuttaReference: 15,
            
            Button: 20,
            RevealButton: 20,
            DailyGoal: 15,
            Debug: 10
        );

        static PracticeFontSizes Short => new(
            Word: 28,
            Answer: 25,
            AnswerSecondary: 18,
           
            Badge: 14,
            BadgeHint: 14,
            
            PaliRoot: 13,
            Level: 11,
            
            Translation: 16,
            TranslationPagination: 12,
            TranslationDots: 26,
            
            SuttaExample: 16,
            SuttaReference: 14,
            
            Button: 20,
            RevealButton: 20,
            DailyGoal: 14,
            Debug: 10
        );

        static PracticeFontSizes Minimum => new(
            Word: 26,
            Answer: 23,
            AnswerSecondary: 16,
            
            Badge: 13,
            BadgeHint: 13,
            
            PaliRoot: 12,
            Level: 10,
            
            Translation: 15,
            TranslationPagination: 11,
            TranslationDots: 26,
            
            SuttaExample: 15,
            SuttaReference: 13,
            
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
            HeightClass.Tall => 16,
            HeightClass.Medium => 12,
            HeightClass.Short => 10,
            _ => 8
        };

        // === Card ===

        /// <summary>Internal padding of the practice card (sides).</summary>
        public static double CardHorizontalPadding(HeightClass h) => h switch
        {
            HeightClass.Tall => 24,
            _ => 20,
        };

        /// <summary>Top padding of the practice card.</summary>
        public static double CardPaddingTop(HeightClass h) => h switch
        {
            HeightClass.Tall => 18,
            HeightClass.Medium => 16,
            _ => 12
        };

        /// <summary>Bottom padding of the practice card.</summary>
        public static double CardPaddingBottom(HeightClass h) => h switch
        {
            HeightClass.Tall => 22,
            HeightClass.Medium => 18,
            HeightClass.Short => 14,
            _ => 10
        };

        /// <summary>Spacing between card child elements (word, answer, badges).</summary>
        public static double CardContentSpacing(HeightClass h) => h switch
        {
            HeightClass.Tall => 10,
            _ => 8
        };

        // === Word Section ===

        /// <summary>Margin above the main word.</summary>
        public static double WordMarginTop(HeightClass h) => h switch
        {
            HeightClass.Tall => 12,
            HeightClass.Medium => 8,
            HeightClass.Short => 0,
            _ => -4
        };

        /// <summary>Margin below the main word.</summary>
        public static double WordMarginBottom(HeightClass h) => h switch
        {
            HeightClass.Tall => 12,
            HeightClass.Medium => 8,
            HeightClass.Short => 6,
            _ => 4
        };
        
        // === Badge ===

        /// <summary>Spacing between badge icon and text inside a badge.</summary>
        public static double BadgeIconTextSpacing(HeightClass h) => h switch
        {
            HeightClass.Tall => 6,
            HeightClass.Medium or HeightClass.Short => 5,
            _ => 4
        };

        /// <summary>Padding inside the badge border.</summary>
        public static Thickness BadgePadding(HeightClass h) => h switch
        {
            HeightClass.Tall => new Thickness(10, 4, 11, 4),
            HeightClass.Medium => new Thickness(9, 4, 10, 4),
            HeightClass.Short => new Thickness(8, 4, 9, 4),
            _ => new Thickness(7, 4, 8, 4)
        };

        /// <summary>Spacing between badges in the badge row.</summary>
        public static double BadgeRowSpacing(HeightClass h) => h switch
        {
            HeightClass.Tall => 7,
            HeightClass.Medium => 6,
            _ => 5
        };

        // === Answer Section ===

        /// <summary>Margin above the answer container (negative pulls it closer to badge/hint).</summary>
        public static double AnswerMarginTop(HeightClass h) => h switch
        {
            HeightClass.Tall => 4,
            HeightClass.Medium => 0,
            HeightClass.Short => -4,
            _ => -8
        };

        /// <summary>Spacing between answer lines.</summary>
        public static double AnswerLineSpacing(HeightClass h) => h switch
        {
            HeightClass.Tall => 4,
            HeightClass.Medium => 3,
            HeightClass.Short => 2,
            _ => 1
        };

        // === Translation Block ===

        /// <summary>Horizontal padding inside translation block.</summary>
        public static double TranslationPaddingH(HeightClass h) => h switch
        {
            HeightClass.Tall => 24,
            HeightClass.Medium => 20,
            HeightClass.Short => 16,
            _ => 12
        };

        /// <summary>Vertical padding inside translation block.</summary>
        public static double TranslationPaddingV(HeightClass h) => h switch
        {
            HeightClass.Tall => 16,
            HeightClass.Medium => 12,
            _ => 8
        };

        /// <summary>Spacing between translation text and pagination.</summary>
        public static double TranslationContentSpacing(HeightClass h) => h switch
        {
            HeightClass.Tall => 8,
            HeightClass.Medium => 6,
            HeightClass.Short => 4,
            _ => 2
        };

        // === Example Section ===

        /// <summary>Spacing between example sentence and reference text.</summary>
        public static double ExampleLineSpacing => 4;

        // === Buttons ===

        /// <summary>Spacing between button icon and text.</summary>
        public static double ButtonIconTextSpacing(HeightClass h) => h switch
        {
            HeightClass.Tall => 8,
            HeightClass.Medium => 7,
            HeightClass.Short => 6,
            _ => 5
        };

        /// <summary>Horizontal padding for action buttons (reveal, hard, easy).</summary>
        public static double ActionButtonPaddingH(HeightClass h) => h switch
        {
            HeightClass.Tall => 16,
            HeightClass.Medium => 14,
            HeightClass.Short => 12,
            _ => 10
        };

        /// <summary>Vertical padding for action buttons.</summary>
        public static double ActionButtonPaddingV(HeightClass h) => h switch
        {
            HeightClass.Tall => 10,
            HeightClass.Medium => 9,
            HeightClass.Short => 8,
            _ => 7
        };

        // === Daily Goal ===

        /// <summary>Spacing between daily goal text and progress bar.</summary>
        public static double DailyGoalSpacing => 4;

        /// <summary>Top margin above daily goal bar (spacing from nav buttons).</summary>
        public static double NavToDailyGoalMargin(HeightClass h) => h switch
        {
            HeightClass.Tall => 12,
            HeightClass.Medium => 10,
            HeightClass.Short => 8,
            _ => 6
        };

        /// <summary>Bottom padding under daily goal bar (spacing to screen edge).</summary>
        public static double DailyGoalBottomPadding(HeightClass h) => h switch
        {
            HeightClass.Tall => 16,
            HeightClass.Medium => 12,
            HeightClass.Short => 8,
            _ => 4
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
        public static double AnswerLineThickness => 2;

        /// <summary>Height of the daily goal progress bar.</summary>
        public static double ProgressBarHeight => 8;

        /// <summary>Stroke thickness for navigation buttons (app bar, about contact).</summary>
        public static double NavigationButtonStrokeThickness => 1.75;

        /// <summary>Stroke thickness for practice action buttons (reveal, hard, easy).</summary>
        public static double PracticeButtonStrokeThickness => 1.75;

        /// <summary>Stroke thickness for start page buttons.</summary>
        public static double StartPageStrokeThickness => 1.75;
    }

    #endregion
}

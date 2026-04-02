using Microsoft.UI.Text;
using PaliPractice.Localization;
using PaliPractice.Presentation.Common;
using PaliPractice.Themes;

namespace PaliPractice.Presentation.Practice.Common;

/// <summary>
/// Measures worst-case badge widths using ghost UI elements.
/// Used to determine if badges need abbreviation before rendering.
/// Similar pattern to TextBalancer - cached measurement elements, UI thread only.
/// </summary>
public static class BadgeWidthMeasurer
{
    // Cached measurement TextBlock (reused for performance)
    // Note: Must only be called from UI thread (e.g., SizeChanged handlers)
    static TextBlock? _measureText;

    /// <summary>
    /// Measures the worst-case width needed for noun badges (Case + Gender + Number).
    /// Uses ghost measurement - creates invisible UI to measure actual rendered width.
    /// </summary>
    public static double MeasureNounBadges(HeightClass heightClass)
    {
        var fonts = LayoutConstants.PracticeFontSizes.Get(heightClass);
        var badgeSpacing = LayoutConstants.Gaps.BadgeRowSpacing(heightClass);

        var caseWidth = MeasureMaxBadge(GrammarText.GetAllCaseLabels(), fonts.Badge);
        var genderWidth = MeasureMaxBadge(GrammarText.GetAllGenderLabels(), fonts.Badge);
        var numberWidth = MeasureMaxBadge(GrammarText.GetAllNumberLabels(), fonts.Badge);

        // Total = badges + spacing between them (2 gaps for 3 badges)
        return caseWidth + genderWidth + numberWidth + (badgeSpacing * 2);
    }

    /// <summary>
    /// Measures the worst-case width needed for verb badges (Tense + Person + Number + optional Voice).
    /// </summary>
    public static double MeasureVerbBadges(bool hasVoice, HeightClass heightClass)
    {
        var fonts = LayoutConstants.PracticeFontSizes.Get(heightClass);
        var badgeSpacing = LayoutConstants.Gaps.BadgeRowSpacing(heightClass);

        var tenseWidth = MeasureMaxBadge(GrammarText.GetAllTenseLabels(), fonts.Badge);
        var personWidth = MeasureMaxBadge(GrammarText.GetAllPersonLabels(), fonts.Badge);
        var numberWidth = MeasureMaxBadge(GrammarText.GetAllNumberLabels(), fonts.Badge);

        if (!hasVoice)
        {
            // 3 badges, 2 gaps
            return tenseWidth + personWidth + numberWidth + (badgeSpacing * 2);
        }

        var voiceWidth = MeasureSingleBadge(GrammarText.GetVoice(Voice.Reflexive), fonts.Badge);

        // 4 badges, 3 gaps
        return tenseWidth + personWidth + numberWidth + voiceWidth + (badgeSpacing * 3);
    }

    /// <summary>
    /// Determines if badges should be abbreviated based on available width.
    /// </summary>
    /// <param name="availableWidth">Width available for badges (card width - 2 * card padding)</param>
    /// <param name="practiceType">Declension or Conjugation</param>
    /// <param name="hasVoice">For conjugation, whether voice badge is shown</param>
    /// <param name="heightClass">Current height class for font sizes</param>
    public static bool ShouldAbbreviate(
        double availableWidth,
        PracticeType practiceType,
        bool hasVoice,
        HeightClass heightClass)
    {
        var requiredWidth = practiceType == PracticeType.Declension
            ? MeasureNounBadges(heightClass)
            : MeasureVerbBadges(hasVoice, heightClass);

        return requiredWidth > availableWidth;
    }

    /// <summary>
    /// Measures a single badge's width including padding and icon.
    /// Badge structure: SquircleBorder [Padding: (10,4,11,4)]
    ///   └─ StackPanel [Spacing: 6, Horizontal]
    ///        ├─ BitmapIcon [Height matches font size, ~16pt wide assumed]
    ///        └─ TextBlock [FontSize, Medium weight]
    /// </summary>
    static double MeasureSingleBadge(string label, double fontSize)
    {
        var textWidth = MeasureText(label, fontSize);

        // Badge padding: left=10, right=11
        const double paddingLeft = 10;
        const double paddingRight = 11;

        // Icon size approximately matches font size (square icons)
        // Using fontSize as icon width estimate
        var iconWidth = fontSize;

        // Spacing between icon and text
        const double iconTextSpacing = 6;

        return paddingLeft + iconWidth + iconTextSpacing + textWidth + paddingRight;
    }

    static double MeasureMaxBadge(IEnumerable<string> labels, double fontSize)
        => labels.Max(label => MeasureSingleBadge(label, fontSize));

    /// <summary>
    /// Measures actual rendered width of text using SourceSans font with Medium weight.
    /// </summary>
    static double MeasureText(string text, double fontSize)
    {
        _measureText ??= new TextBlock();
        _measureText.Text = text;
        _measureText.FontSize = fontSize;
        _measureText.FontFamily = FontPaths.SourceSans;
        _measureText.FontWeight = FontWeights.Medium;
        _measureText.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        return _measureText.DesiredSize.Width;
    }
}

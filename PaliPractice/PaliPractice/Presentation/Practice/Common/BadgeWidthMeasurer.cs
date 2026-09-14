using Microsoft.UI.Text;
using PaliPractice.Presentation.Common;
using PaliPractice.Themes;

namespace PaliPractice.Presentation.Practice.Common;

/// <summary>
/// Measures the current badge labels with the same font and spacing as the visible badges.
/// </summary>
public static class BadgeWidthMeasurer
{
    // Reused on the UI thread from layout callbacks.
    static TextBlock? _measureText;

    public static int SelectAbbreviatedMask(
        double availableWidth, IReadOnlyList<BadgeLabelOption> labels, HeightClass heightClass)
    {
        var fontSize = LayoutConstants.PracticeFontSizes.Get(heightClass).Badge;
        var widths = labels.Select(label => new BadgeWidths(
            MeasureBadge(label.Full, fontSize, heightClass),
            MeasureBadge(label.Short, fontSize, heightClass))).ToArray();
        return BadgeLabelSelector.SelectAbbreviatedMask(
            availableWidth, widths, LayoutConstants.Gaps.BadgeRowSpacing(heightClass));
    }

    static double MeasureBadge(string label, double fontSize, HeightClass heightClass)
    {
        var padding = LayoutConstants.Gaps.BadgePadding(heightClass);
        return padding.Left + fontSize + LayoutConstants.Gaps.BadgeIconTextSpacing(heightClass) +
            MeasureText(label, fontSize) + padding.Right;
    }

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

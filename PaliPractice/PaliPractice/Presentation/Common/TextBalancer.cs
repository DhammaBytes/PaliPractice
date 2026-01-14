using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace PaliPractice.Presentation.Common;

/// <summary>
/// Balances text across multiple lines to avoid orphan words.
/// Measures actual text width to determine where natural line breaks occur,
/// then adjusts to prevent single words on the last line.
/// </summary>
public static class TextBalancer
{
    // Cached measurement TextBlock (reused for performance)
    static TextBlock? _measureBlock;

    /// <summary>
    /// Balances text for display, avoiding orphaned words on the last line.
    /// Uses actual text measurement to determine line breaks.
    /// </summary>
    /// <param name="text">The text to balance</param>
    /// <param name="availableWidth">The available width for text</param>
    /// <param name="fontSize">Font size being used</param>
    /// <param name="fontFamily">Font family being used</param>
    /// <returns>Balanced text with newlines inserted if needed</returns>
    public static string Balance(string? text, double availableWidth, double fontSize, FontFamily fontFamily)
    {
        if (string.IsNullOrWhiteSpace(text) || availableWidth <= 0)
            return text ?? string.Empty;

        // Split into words
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= 2)
            return text; // Too few words to have issues

        // Measure each word's width
        var wordWidths = MeasureWords(words, fontSize, fontFamily);
        var spaceWidth = MeasureText(" ", fontSize, fontFamily);

        // Simulate natural line wrapping
        var lines = SimulateWrapping(words, wordWidths, spaceWidth, availableWidth);

        // Single line - no rebalancing needed
        if (lines.Count < 2)
            return text;

        // Check for issues that need fixing:
        // 1. Mid-phrase breaks (line break not at semicolon boundary)
        // 2. Orphan words (single word on last line)
        var hasMidPhraseBreak = false;
        var hasOrphan = false;

        // Check for mid-phrase breaks: if a line ends without semicolon, it's mid-phrase
        for (var i = 0; i < lines.Count - 1; i++)
        {
            var lastWordInLine = lines[i][^1];
            if (!lastWordInLine.EndsWith(';'))
            {
                hasMidPhraseBreak = true;
                break;
            }
        }

        // Check for orphan on last line (single word)
        if (lines[^1].Count == 1)
            hasOrphan = true;

        if (!hasMidPhraseBreak && !hasOrphan)
            return text;

        // Try to rebalance by breaking at semicolon boundaries
        // Pass hasOrphan flag to ensure we get 2+ words on last line when fixing orphans
        var rebalanced = TryRebalance(words, wordWidths, spaceWidth, availableWidth, requireMultipleWordsOnLastLine: hasOrphan);
        return rebalanced ?? text;
    }

    static double[] MeasureWords(string[] words, double fontSize, FontFamily fontFamily)
    {
        var widths = new double[words.Length];
        for (var i = 0; i < words.Length; i++)
            widths[i] = MeasureText(words[i], fontSize, fontFamily);
        return widths;
    }

    static double MeasureText(string text, double fontSize, FontFamily fontFamily)
    {
        _measureBlock ??= new TextBlock();
        _measureBlock.Text = text;
        _measureBlock.FontSize = fontSize;
        _measureBlock.FontFamily = fontFamily;
        _measureBlock.FontWeight = FontWeights.Medium;
        _measureBlock.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        return _measureBlock.DesiredSize.Width;
    }

    static List<List<string>> SimulateWrapping(string[] words, double[] widths, double spaceWidth, double availableWidth)
    {
        var lines = new List<List<string>>();
        var currentLine = new List<string>();
        var currentWidth = 0.0;

        for (var i = 0; i < words.Length; i++)
        {
            var wordWidth = widths[i];
            var neededWidth = currentLine.Count > 0 ? spaceWidth + wordWidth : wordWidth;

            if (currentWidth + neededWidth > availableWidth && currentLine.Count > 0)
            {
                // Start new line
                lines.Add(currentLine);
                currentLine = [words[i]];
                currentWidth = wordWidth;
            }
            else
            {
                currentLine.Add(words[i]);
                currentWidth += neededWidth;
            }
        }

        if (currentLine.Count > 0)
            lines.Add(currentLine);

        return lines;
    }

    static string? TryRebalance(string[] words, double[] widths, double spaceWidth, double availableWidth, bool requireMultipleWordsOnLastLine)
    {
        // Strategy: break only at semicolon boundaries, finding the optimal split
        // that keeps all lines within width

        // Find all potential break points (after semicolons)
        var breakPoints = new List<int>();
        for (var i = 0; i < words.Length - 1; i++)
        {
            if (words[i].EndsWith(';'))
                breakPoints.Add(i + 1); // Break after this word
        }

        if (breakPoints.Count == 0)
            return null; // No semicolon boundaries to break at

        // Try to find the best break configuration
        // Start by trying single break points (2 lines)
        for (var bp = breakPoints.Count - 1; bp >= 0; bp--)
        {
            var breakAt = breakPoints[bp];
            var wordsOnLastLine = words.Length - breakAt;

            // If we need multiple words on last line, skip break points that leave only 1
            if (requireMultipleWordsOnLastLine && wordsOnLastLine < 2)
                continue;

            // Calculate width of first line (before break)
            var firstLineWidth = CalculateLineWidth(words, widths, spaceWidth, 0, breakAt);

            // Calculate width of last line (after break)
            var lastLineWidth = CalculateLineWidth(words, widths, spaceWidth, breakAt, words.Length);

            // Good break: both lines fit
            if (firstLineWidth <= availableWidth && lastLineWidth <= availableWidth)
            {
                var beforeBreak = string.Join(" ", words.Take(breakAt));
                var afterBreak = string.Join(" ", words.Skip(breakAt));
                return beforeBreak + "\n" + afterBreak;
            }
        }

        // If single break doesn't work, try multiple breaks
        // Find all valid line segments (each ending at semicolon, fitting within width)
        var result = TryMultipleBreaks(words, widths, spaceWidth, availableWidth, breakPoints, requireMultipleWordsOnLastLine);
        return result;
    }

    static double CalculateLineWidth(string[] words, double[] widths, double spaceWidth, int start, int end)
    {
        var width = 0.0;
        for (var i = start; i < end; i++)
            width += (i > start ? spaceWidth : 0) + widths[i];
        return width;
    }

    static string? TryMultipleBreaks(string[] words, double[] widths, double spaceWidth, double availableWidth, List<int> breakPoints, bool requireMultipleWordsOnLastLine)
    {
        // Greedy approach: build lines from start, breaking at semicolons when needed
        var lines = new List<string>();
        var currentStart = 0;

        foreach (var bp in breakPoints)
        {
            var segmentWidth = CalculateLineWidth(words, widths, spaceWidth, currentStart, bp);

            if (segmentWidth > availableWidth && currentStart < bp)
            {
                // This segment is too wide - need to find an earlier break
                // Look for the last break point that fits
                var foundBreak = false;
                for (var i = breakPoints.IndexOf(bp) - 1; i >= 0; i--)
                {
                    var earlierBp = breakPoints[i];
                    if (earlierBp <= currentStart) break;

                    var earlierWidth = CalculateLineWidth(words, widths, spaceWidth, currentStart, earlierBp);
                    if (earlierWidth <= availableWidth)
                    {
                        lines.Add(string.Join(" ", words.Skip(currentStart).Take(earlierBp - currentStart)));
                        currentStart = earlierBp;
                        foundBreak = true;
                        break;
                    }
                }

                if (!foundBreak)
                {
                    // Can't break properly at semicolons - fall back to natural wrap
                    return null;
                }
            }
        }

        // Add remaining words as last line
        if (currentStart < words.Length)
        {
            var lastLineWordCount = words.Length - currentStart;
            var lastLineWidth = CalculateLineWidth(words, widths, spaceWidth, currentStart, words.Length);

            // Check if we need to rebreak for orphan prevention or width
            var needsRebreak = lastLineWidth > availableWidth ||
                              (requireMultipleWordsOnLastLine && lastLineWordCount < 2);

            if (needsRebreak)
            {
                // Try to find a better break point
                for (var i = breakPoints.Count - 1; i >= 0; i--)
                {
                    var bp = breakPoints[i];
                    if (bp <= currentStart) continue;
                    if (bp >= words.Length) continue;

                    var wordsAfterBp = words.Length - bp;
                    if (requireMultipleWordsOnLastLine && wordsAfterBp < 2)
                        continue;

                    var beforeWidth = CalculateLineWidth(words, widths, spaceWidth, currentStart, bp);
                    var afterWidth = CalculateLineWidth(words, widths, spaceWidth, bp, words.Length);

                    if (beforeWidth <= availableWidth && afterWidth <= availableWidth)
                    {
                        lines.Add(string.Join(" ", words.Skip(currentStart).Take(bp - currentStart)));
                        lines.Add(string.Join(" ", words.Skip(bp)));
                        return string.Join("\n", lines);
                    }
                }

                // If we couldn't fix the issue and it's just an orphan (not width), accept it
                if (lastLineWidth <= availableWidth)
                {
                    lines.Add(string.Join(" ", words.Skip(currentStart)));
                    return lines.Count > 1 ? string.Join("\n", lines) : null;
                }

                return null; // Can't fit
            }
            lines.Add(string.Join(" ", words.Skip(currentStart)));
        }

        return lines.Count > 1 ? string.Join("\n", lines) : null;
    }
}

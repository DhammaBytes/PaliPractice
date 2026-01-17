using Microsoft.UI.Text;

namespace PaliPractice.Presentation.Practice.Common;

/// <summary>
/// Balances text across multiple lines to avoid orphan words.
/// Measures actual text width to determine where natural line breaks occur,
/// then adjusts to prevent single words on the last line.
///
/// Goals:
/// 1. Never break mid-phrase (only break after semicolons)
/// 2. Avoid orphans (single word on last line) when possible
/// </summary>
public static class TextBalancer
{
    // Cached measurement TextBlock (reused for performance)
    // Note: Must only be called from UI thread (e.g., SizeChanged handlers)
    static TextBlock? _measureBlock;
    static double _fontSize;
    static FontFamily? _fontFamily;

    /// <summary>
    /// Balances text for display, avoiding orphaned words on the last line.
    /// Uses actual text measurement to determine line breaks.
    /// </summary>
    public static string Balance(string? text, double availableWidth, double fontSize, FontFamily fontFamily)
    {
        if (string.IsNullOrWhiteSpace(text) || availableWidth <= 0)
            return text ?? string.Empty;

        // Cache font settings for MeasureLine calls
        _fontSize = fontSize;
        _fontFamily = fontFamily;

        // Split into words
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= 2)
            return text; // Too few words to have issues

        // Simulate natural line wrapping using full line measurement
        var lines = SimulateWrapping(words, availableWidth);

        // Single line - no rebalancing needed
        if (lines.Count < 2)
            return text;

        // Check for issues that need fixing:
        // 1. Mid-phrase breaks (line break not at semicolon boundary)
        // 2. Orphan words (single word on last line)
        var hasMidPhraseBreak = false;
        var hasOrphan = lines[^1].Count == 1;

        for (var i = 0; i < lines.Count - 1; i++)
        {
            var lastWordInLine = lines[i][^1];
            if (!lastWordInLine.EndsWith(';'))
            {
                hasMidPhraseBreak = true;
                break;
            }
        }

        if (!hasMidPhraseBreak && !hasOrphan)
            return text;

        // Try to rebalance by breaking at semicolon boundaries
        var rebalanced = TryRebalance(words, availableWidth);
        return rebalanced ?? text;
    }

    /// <summary>
    /// Measures a complete line string (more accurate than summing word widths due to kerning).
    /// </summary>
    static double MeasureLine(string line)
    {
        _measureBlock ??= new TextBlock();
        _measureBlock.Text = line;
        _measureBlock.FontSize = _fontSize;
        _measureBlock.FontFamily = _fontFamily;
        _measureBlock.FontWeight = FontWeights.Medium;
        _measureBlock.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        return _measureBlock.DesiredSize.Width;
    }

    /// <summary>
    /// Builds a line string from word range.
    /// </summary>
    static string BuildLine(string[] words, int start, int end)
    {
        return string.Join(" ", words.Skip(start).Take(end - start));
    }

    /// <summary>
    /// Simulates natural word wrapping by measuring full lines.
    /// </summary>
    static List<List<string>> SimulateWrapping(string[] words, double availableWidth)
    {
        var lines = new List<List<string>>();
        var currentLine = new List<string>();

        for (var i = 0; i < words.Length; i++)
        {
            // Try adding this word to current line
            currentLine.Add(words[i]);
            var lineText = string.Join(" ", currentLine);
            var lineWidth = MeasureLine(lineText);

            if (lineWidth > availableWidth && currentLine.Count > 1)
            {
                // Doesn't fit - remove word and start new line
                currentLine.RemoveAt(currentLine.Count - 1);
                lines.Add(new List<string>(currentLine));
                currentLine = [words[i]];
            }
        }

        if (currentLine.Count > 0)
            lines.Add(currentLine);

        return lines;
    }

    static string? TryRebalance(string[] words, double availableWidth)
    {
        // Find all potential break points (after semicolons)
        var breakPoints = new List<int>();
        for (var i = 0; i < words.Length - 1; i++)
        {
            if (words[i].EndsWith(';'))
                breakPoints.Add(i + 1);
        }

        if (breakPoints.Count == 0)
            return null;

        // Try single break (2 lines) with two-pass approach:
        // Pass 1: Prefer breaks that leave 2+ words on last line
        // Pass 2: Allow single word on last line if no better option
        var singleBreakResult = TrySingleBreak(words, availableWidth, breakPoints, preferNonOrphan: true)
                             ?? TrySingleBreak(words, availableWidth, breakPoints, preferNonOrphan: false);

        if (singleBreakResult != null)
            return singleBreakResult;

        // Try multiple breaks for longer text
        return TryMultipleBreaks(words, availableWidth, breakPoints);
    }

    static string? TrySingleBreak(string[] words, double availableWidth, List<int> breakPoints, bool preferNonOrphan)
    {
        // Iterate from end to find latest valid break point
        for (var bp = breakPoints.Count - 1; bp >= 0; bp--)
        {
            var breakAt = breakPoints[bp];
            var wordsOnLastLine = words.Length - breakAt;

            // In first pass, skip breaks that leave orphan
            if (preferNonOrphan && wordsOnLastLine < 2)
                continue;

            var firstLine = BuildLine(words, 0, breakAt);
            var lastLine = BuildLine(words, breakAt, words.Length);

            // Measure actual line widths
            if (MeasureLine(firstLine) <= availableWidth && MeasureLine(lastLine) <= availableWidth)
                return firstLine + "\n" + lastLine;
        }

        return null;
    }

    static string? TryMultipleBreaks(string[] words, double availableWidth, List<int> breakPoints)
    {
        // Two-pass: first try to avoid orphan, then allow it
        return TryMultipleBreaksPass(words, availableWidth, breakPoints, avoidOrphan: true)
            ?? TryMultipleBreaksPass(words, availableWidth, breakPoints, avoidOrphan: false);
    }

    static string? TryMultipleBreaksPass(string[] words, double availableWidth, List<int> breakPoints, bool avoidOrphan)
    {
        // Greedy approach: pack lines from start, breaking at semicolons
        var lines = new List<string>();
        var currentStart = 0;

        foreach (var bp in breakPoints)
        {
            var segment = BuildLine(words, currentStart, bp);
            var segmentWidth = MeasureLine(segment);

            if (segmentWidth > availableWidth && currentStart < bp)
            {
                // Too wide - find earlier break that fits
                var foundBreak = false;
                for (var i = breakPoints.IndexOf(bp) - 1; i >= 0; i--)
                {
                    var earlierBp = breakPoints[i];
                    if (earlierBp <= currentStart) break;

                    var earlierSegment = BuildLine(words, currentStart, earlierBp);
                    if (MeasureLine(earlierSegment) <= availableWidth)
                    {
                        lines.Add(earlierSegment);
                        currentStart = earlierBp;
                        foundBreak = true;
                        break;
                    }
                }

                if (!foundBreak)
                    return null;
            }
        }

        // Handle remaining words
        if (currentStart < words.Length)
        {
            var lastLineWordCount = words.Length - currentStart;
            var lastLine = BuildLine(words, currentStart, words.Length);
            var lastLineWidth = MeasureLine(lastLine);

            // Check if we need to rebreak
            var needsRebreak = lastLineWidth > availableWidth ||
                              (avoidOrphan && lastLineWordCount < 2);

            if (needsRebreak)
            {
                // Try to find better break point
                for (var i = breakPoints.Count - 1; i >= 0; i--)
                {
                    var bp = breakPoints[i];
                    if (bp <= currentStart || bp >= words.Length) continue;

                    var wordsAfterBp = words.Length - bp;
                    if (avoidOrphan && wordsAfterBp < 2) continue;

                    var beforeLine = BuildLine(words, currentStart, bp);
                    var afterLine = BuildLine(words, bp, words.Length);

                    if (MeasureLine(beforeLine) <= availableWidth && MeasureLine(afterLine) <= availableWidth)
                    {
                        lines.Add(beforeLine);
                        lines.Add(afterLine);
                        return string.Join("\n", lines);
                    }
                }

                // Couldn't fix orphan but line fits - accept it (only in second pass)
                if (!avoidOrphan && lastLineWidth <= availableWidth)
                {
                    lines.Add(lastLine);
                    return lines.Count > 1 ? string.Join("\n", lines) : null;
                }

                return null;
            }

            lines.Add(lastLine);
        }

        return lines.Count > 1 ? string.Join("\n", lines) : null;
    }
}

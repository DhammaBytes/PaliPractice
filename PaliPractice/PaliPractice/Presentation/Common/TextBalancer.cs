namespace PaliPractice.Presentation.Common;

/// <summary>
/// Balances text across multiple lines to avoid orphan words.
/// Uses a simple post-processing approach: if the last line has only 1-2 words,
/// move words from the preceding line to balance it.
/// </summary>
public static class TextBalancer
{
    /// <summary>
    /// Balances text for display, avoiding single words hanging on their own line.
    /// </summary>
    /// <param name="text">The text to balance (semicolon-separated phrases)</param>
    /// <param name="maxCharsPerLine">Approximate max characters per line before wrapping</param>
    /// <returns>Balanced text with newlines inserted at optimal break points</returns>
    public static string Balance(string? text, int maxCharsPerLine = 32)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // First, do simple word-wrap based on maxCharsPerLine
        var wrapped = WrapText(text, maxCharsPerLine);
        var lines = wrapped.Split('\n');

        // If only one line or no orphan issue, return as-is
        if (lines.Length < 2)
            return wrapped;

        // Check last line for orphan words (1-2 words)
        var lastLine = lines[^1];
        var lastLineWords = CountWords(lastLine);

        if (lastLineWords >= 3)
            return wrapped; // No orphan issue

        // Check preceding line
        var prevLine = lines[^2];
        var prevLineWords = CountWords(prevLine);

        if (prevLineWords <= 3)
            return wrapped; // Not enough words to rebalance

        // Try to move words from preceding line to last line
        var rebalanced = TryRebalance(prevLine, lastLine);
        if (rebalanced is null)
            return wrapped; // Couldn't rebalance

        // Rebuild the result
        var result = new List<string>(lines.Take(lines.Length - 2));
        result.Add(rebalanced.Value.NewPrevLine);
        result.Add(rebalanced.Value.NewLastLine);

        return string.Join("\n", result);
    }

    static string WrapText(string text, int maxChars)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return text;

        var lines = new List<string>();
        var currentLine = new List<string>();
        var currentLength = 0;

        foreach (var word in words)
        {
            var wordLen = word.Length + (currentLine.Count > 0 ? 1 : 0);

            // Only wrap if:
            // 1. Line exceeds max chars
            // 2. Current line has at least 3 words (never leave fewer than 3)
            if (currentLength + wordLen > maxChars && currentLine.Count >= 3)
            {
                lines.Add(string.Join(" ", currentLine));
                currentLine = [word];
                currentLength = word.Length;
            }
            else
            {
                currentLine.Add(word);
                currentLength += wordLen;
            }
        }

        if (currentLine.Count > 0)
            lines.Add(string.Join(" ", currentLine));

        return string.Join("\n", lines);
    }

    static int CountWords(string line) =>
        line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

    static (string NewPrevLine, string NewLastLine)? TryRebalance(string prevLine, string lastLine)
    {
        var prevWords = prevLine.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        var lastWords = lastLine.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

        if (prevWords.Count < 2)
            return null;

        // Try moving 2 words first (only if it leaves >= 3 words on prev line)
        if (prevWords.Count >= 5) // Need at least 5 to leave 3 after moving 2
        {
            var twoWordsToMove = string.Join(" ", prevWords.TakeLast(2));

            // Check if those two words are NOT separated by semicolon from words before them
            // i.e., if the word before them ends with ';', we can safely move them
            var wordBeforeTwo = prevWords[^3];
            var canMoveTwo = wordBeforeTwo.EndsWith(';');

            if (canMoveTwo)
            {
                // Move two words
                var newPrev = string.Join(" ", prevWords.Take(prevWords.Count - 2));
                var newLast = twoWordsToMove + " " + string.Join(" ", lastWords);
                return (newPrev, newLast);
            }
        }

        // Try moving just 1 word (only if it leaves >= 3 words on prev line)
        if (prevWords.Count >= 4) // Need at least 4 to leave 3 after moving 1
        {
            var oneWordToMove = prevWords[^1];
            var wordBeforeOne = prevWords[^2];
            var canMoveOne = wordBeforeOne.EndsWith(';');

            if (canMoveOne)
            {
                var newPrev = string.Join(" ", prevWords.Take(prevWords.Count - 1));
                var newLast = oneWordToMove + " " + string.Join(" ", lastWords);
                return (newPrev, newLast);
            }
        }

        // Can't safely move without breaking a phrase or leaving < 3 words
        return null;
    }
}

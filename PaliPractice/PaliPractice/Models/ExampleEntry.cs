namespace PaliPractice.Models;

/// <summary>
/// Represents a single example entry in the carousel.
/// Combines a word with an example index (0 for Example1, 1 for Example2).
/// </summary>
public record ExampleEntry(IWord Word, int ExampleIndex)
{
    /// <summary>
    /// Gets the example text for this entry.
    /// </summary>
    public string Example => ExampleIndex == 0 ? Word.Example1 : Word.Example2;

    /// <summary>
    /// Gets the combined reference (source + sutta).
    /// Sutta newlines are converted to parenthetical format: "line1\nline2" → "line1 (line2)"
    /// </summary>
    public string Reference
    {
        get
        {
            var source = ExampleIndex == 0 ? Word.Source1 : Word.Source2;
            var sutta = ExampleIndex == 0 ? Word.Sutta1 : Word.Sutta2;

            // Format sutta: convert newlines to parenthetical format
            if (!string.IsNullOrEmpty(sutta))
            {
                var lines = sutta.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 1)
                {
                    var first = lines[0].Trim();
                    var rest = string.Join(", ", lines.Skip(1).Select(l => l.Trim()));
                    sutta = $"{first} ({rest})";
                }
            }

            if (string.IsNullOrEmpty(source)) return sutta ?? string.Empty;
            if (string.IsNullOrEmpty(sutta)) return source;
            return $"{source} {sutta}";
        }
    }

    /// <summary>
    /// Gets the meaning from the parent word.
    /// </summary>
    public string? Meaning => Word.Meaning;
}

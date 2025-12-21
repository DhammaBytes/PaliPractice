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
    /// Gets the source reference for this entry.
    /// Newlines are converted to parenthetical format: "line1\nline2" → "line1 (line2)"
    /// </summary>
    public string Source
    {
        get
        {
            var raw = ExampleIndex == 0 ? Word.Source1 : Word.Source2;
            if (string.IsNullOrEmpty(raw)) return raw;

            var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 1)
            {
                var first = lines[0].Trim();
                var rest = string.Join(", ", lines.Skip(1).Select(l => l.Trim()));
                return $"{first} ({rest})";
            }
            return raw;
        }
    }

    /// <summary>
    /// Gets the sutta reference for this entry.
    /// </summary>
    public string Sutta => ExampleIndex == 0 ? Word.Sutta1 : Word.Sutta2;

    /// <summary>
    /// Gets the combined reference (sutta or source+sutta).
    /// </summary>
    public string Reference
    {
        get
        {
            var source = Source;
            var sutta = Sutta;
            if (string.IsNullOrEmpty(source)) return sutta;
            if (string.IsNullOrEmpty(sutta)) return source;
            return $"{source} {sutta}";
        }
    }

    /// <summary>
    /// Gets the meaning from the parent word.
    /// </summary>
    public string? Meaning => Word.Meaning;
}

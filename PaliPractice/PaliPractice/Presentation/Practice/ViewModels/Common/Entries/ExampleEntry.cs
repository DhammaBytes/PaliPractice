using PaliPractice.Models.Words;

namespace PaliPractice.Presentation.Practice.ViewModels.Common.Entries;

/// <summary>
/// Represents a single example entry in the carousel.
/// Combines word details with an example index (0 for Example1, 1 for Example2).
/// </summary>
public record ExampleEntry(IWordDetails Details, int ExampleIndex)
{
    /// <summary>
    /// Gets the example text for this entry.
    /// </summary>
    public string Example => ExampleIndex == 0 ? Details.Example1 : Details.Example2;

    /// <summary>
    /// Gets the combined reference (source + sutta).
    /// Sutta newlines are converted to parenthetical format: "line1\nline2" → "line1 (line2)"
    /// </summary>
    public string Reference
    {
        get
        {
            var source = ExampleIndex == 0 ? Details.Source1 : Details.Source2;
            var sutta = ExampleIndex == 0 ? Details.Sutta1 : Details.Sutta2;

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
    /// Gets the meaning from the word details.
    /// </summary>
    public string? Meaning => Details.Meaning;
}

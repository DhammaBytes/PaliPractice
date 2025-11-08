using System.Text.RegularExpressions;

namespace PaliPractice.Tests.Inflection.Helpers;

/// <summary>
/// Helper class for parsing DPD inflections_html to extract expected endings.
/// </summary>
public static class HtmlParser
{
    /// <summary>
    /// Parse noun inflections HTML to extract endings for a specific grammatical combination.
    /// Returns endings in the order they appear in the HTML.
    /// </summary>
    /// <param name="html">The inflections_html from DPD</param>
    /// <param name="titlePattern">The title attribute to match (e.g., "masc nom sg")</param>
    /// <returns>Array of endings in order, or empty array if not found</returns>
    public static string[] ParseNounEndings(string html, string titlePattern)
    {
        // Find the <td> element with the matching title attribute
        // Pattern: <td title='titlePattern'>...content...</td>
        var cellPattern = $@"<td[^>]*title='{Regex.Escape(titlePattern)}'[^>]*>(.*?)</td>";
        var cellMatch = Regex.Match(html, cellPattern, RegexOptions.Singleline);

        if (!cellMatch.Success)
        {
            return Array.Empty<string>();
        }

        var cellContent = cellMatch.Groups[1].Value;

        // Split by <br> to handle multiple endings
        var parts = Regex.Split(cellContent, @"<br\s*/?>", RegexOptions.IgnoreCase);

        var endings = new List<string>();

        foreach (var part in parts)
        {
            // Extract text inside <b> tags
            // This handles both plain and gray forms: <b>ending</b> or <span class='gray'>stem<b>ending</b></span>
            var boldMatches = Regex.Matches(part, @"<b>(.*?)</b>");

            foreach (Match match in boldMatches)
            {
                var ending = match.Groups[1].Value;
                if (!string.IsNullOrEmpty(ending))
                {
                    endings.Add(ending);
                }
            }
        }

        return endings.ToArray();
    }

    /// <summary>
    /// Parse verb inflections HTML to extract endings for a specific grammatical combination.
    /// Returns endings in the order they appear in the HTML.
    /// </summary>
    /// <param name="html">The inflections_html from DPD</param>
    /// <param name="titlePattern">The title attribute to match (e.g., "pr 3rd sg", "reflx opt 1st pl")</param>
    /// <returns>Array of endings in order, or empty array if not found</returns>
    public static string[] ParseVerbEndings(string html, string titlePattern)
    {
        // Verb parsing is identical to noun parsing - same HTML structure
        return ParseNounEndings(html, titlePattern);
    }

    /// <summary>
    /// Parse all noun endings from HTML into a dictionary keyed by grammatical combination.
    /// Useful for debugging and comprehensive validation.
    /// </summary>
    public static Dictionary<string, string[]> ParseAllNounEndings(string html)
    {
        var results = new Dictionary<string, string[]>();

        // Find all <td> elements with title attributes
        var pattern = @"<td[^>]*title='([^']+)'[^>]*>(.*?)</td>";
        var matches = Regex.Matches(html, pattern, RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            var title = match.Groups[1].Value;
            var content = match.Groups[2].Value;

            // Skip empty cells or non-inflection cells (like "in comps")
            if (string.IsNullOrWhiteSpace(content) || title == "in comps" || title == "")
            {
                continue;
            }

            var endings = ParseNounEndings(html, title);
            if (endings.Length > 0)
            {
                results[title] = endings;
            }
        }

        return results;
    }

    /// <summary>
    /// Parse all verb endings from HTML into a dictionary keyed by grammatical combination.
    /// Useful for debugging and comprehensive validation.
    /// </summary>
    public static Dictionary<string, string[]> ParseAllVerbEndings(string html)
    {
        // Verb parsing is identical to noun parsing
        return ParseAllNounEndings(html);
    }
}

using System.Text.RegularExpressions;

namespace PaliPractice.Tests.DataIntegrity.Helpers;

/// <summary>
/// Represents an inflected form parsed from DPD HTML with its corpus status.
/// </summary>
public record FormWithCorpusStatus(
    string Title,        // Grammatical combination (e.g., "masc nom sg")
    string FullForm,     // Complete inflected form (stem + ending)
    string Ending,       // Just the ending
    bool IsGray,         // True if form is theoretical (gray in HTML)
    int EndingIndex      // 1-based index within this grammatical combination
);

/// <summary>
/// Validates InCorpus status by cross-checking DPD HTML gray forms against Tipitaka wordlists.
/// </summary>
/// <remarks>
/// This class parses DPD HTML and will fail loudly if the HTML structure changes.
/// Expected structure:
/// - Table cells: &lt;td title='...'&gt;...&lt;/td&gt;
/// - Gray class: class='gray' or class="gray"
/// - Endings: &lt;b&gt;ending&lt;/b&gt;
/// - Multiple forms separated by &lt;br&gt;
/// </remarks>
public static class InCorpusValidator
{
    // Expected DPD HTML structural patterns - if these stop matching, tests should fail
    private const string ExpectedGrayClass = "gray";
    private const string ExpectedTdPattern = @"<td[^>]*title='([^']+)'[^>]*>(.*?)</td>";
    private const string ExpectedBoldPattern = @"<b>(.*?)</b>";

    /// <summary>
    /// Validates that the DPD HTML structure matches our expected format.
    /// Call this once per test run to catch structural changes early.
    /// </summary>
    /// <param name="sampleHtml">A non-empty inflections_html sample from DPD</param>
    /// <exception cref="InvalidOperationException">Thrown if HTML structure has changed</exception>
    public static void ValidateHtmlStructure(string sampleHtml)
    {
        if (string.IsNullOrEmpty(sampleHtml))
            throw new ArgumentException("Sample HTML cannot be empty", nameof(sampleHtml));

        // Check for expected table structure
        var hasTdWithTitle = Regex.IsMatch(sampleHtml, ExpectedTdPattern, RegexOptions.Singleline);
        if (!hasTdWithTitle)
        {
            throw new InvalidOperationException(
                "DPD HTML structure changed: Expected <td title='...'> elements not found. " +
                "Update InCorpusValidator patterns to match new DPD format.");
        }

        // Check for expected bold tags
        var hasBoldTags = Regex.IsMatch(sampleHtml, ExpectedBoldPattern);
        if (!hasBoldTags)
        {
            throw new InvalidOperationException(
                "DPD HTML structure changed: Expected <b>...</b> elements not found. " +
                "Update InCorpusValidator patterns to match new DPD format.");
        }

        // Check for expected gray class (either format)
        var hasGrayClass = sampleHtml.Contains($"class='{ExpectedGrayClass}'") ||
                          sampleHtml.Contains($"class=\"{ExpectedGrayClass}\"");
        // Gray class might not be present in all samples, so this is informational
        // We only care that IF gray forms exist, they use the expected class name
    }

    /// <summary>
    /// Parse all inflected forms from DPD HTML, extracting gray status for each.
    /// </summary>
    /// <param name="html">The inflections_html from DPD</param>
    /// <param name="stem">The word stem to reconstruct full forms</param>
    /// <returns>List of all forms with their corpus status</returns>
    public static List<FormWithCorpusStatus> ParseAllFormsWithGrayStatus(string html, string stem)
    {
        var results = new List<FormWithCorpusStatus>();

        if (string.IsNullOrEmpty(html))
            return results;

        // Find all <td> elements with title attributes using expected pattern
        var cellMatches = Regex.Matches(html, ExpectedTdPattern, RegexOptions.Singleline);

        foreach (Match cellMatch in cellMatches)
        {
            var title = cellMatch.Groups[1].Value;
            var content = cellMatch.Groups[2].Value;

            // Skip empty cells or non-inflection cells
            if (string.IsNullOrWhiteSpace(content) || title == "in comps" || title == "")
                continue;

            // Split by <br> to handle multiple forms
            var parts = Regex.Split(content, @"<br\s*/?>", RegexOptions.IgnoreCase);

            var endingIndex = 0;
            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part))
                    continue;

                // Check if this part is wrapped in gray span (using expected class name)
                var isGray = part.Contains($"class='{ExpectedGrayClass}'") ||
                            part.Contains($"class=\"{ExpectedGrayClass}\"");

                // Extract the ending from <b> tag using expected pattern
                var boldMatch = Regex.Match(part, ExpectedBoldPattern);
                if (!boldMatch.Success)
                    continue;

                var ending = boldMatch.Groups[1].Value;
                if (string.IsNullOrEmpty(ending))
                    continue;

                endingIndex++;

                // Reconstruct full form: stem + ending
                var fullForm = stem + ending;

                results.Add(new FormWithCorpusStatus(
                    Title: title,
                    FullForm: fullForm,
                    Ending: ending,
                    IsGray: isGray,
                    EndingIndex: endingIndex
                ));
            }
        }

        return results;
    }

    /// <summary>
    /// Validate that InCorpus status from DPD HTML matches Tipitaka wordlist presence.
    /// </summary>
    /// <param name="html">The inflections_html from DPD</param>
    /// <param name="stem">The word stem</param>
    /// <param name="tipitakaWords">HashSet of all Tipitaka words</param>
    /// <returns>List of validation results</returns>
    public static List<InCorpusValidationResult> ValidateAgainstWordlist(
        string html,
        string stem,
        HashSet<string> tipitakaWords)
    {
        var forms = ParseAllFormsWithGrayStatus(html, stem);
        var results = new List<InCorpusValidationResult>();

        foreach (var form in forms)
        {
            var inWordlist = tipitakaWords.Contains(form.FullForm);
            var expectedInCorpus = !form.IsGray;
            var sourcesAgree = inWordlist == expectedInCorpus;

            results.Add(new InCorpusValidationResult(
                Title: form.Title,
                FullForm: form.FullForm,
                Ending: form.Ending,
                EndingIndex: form.EndingIndex,
                IsGrayInHtml: form.IsGray,
                InTipitakaWordlist: inWordlist,
                ExpectedInCorpus: expectedInCorpus,
                SourcesAgree: sourcesAgree
            ));
        }

        return results;
    }

    /// <summary>
    /// Get forms from DPD HTML that should be in corpus (non-gray).
    /// </summary>
    public static List<FormWithCorpusStatus> GetInCorpusForms(string html, string stem)
    {
        return ParseAllFormsWithGrayStatus(html, stem)
            .Where(f => !f.IsGray)
            .ToList();
    }

    /// <summary>
    /// Get forms from DPD HTML that are theoretical (gray).
    /// </summary>
    public static List<FormWithCorpusStatus> GetTheoreticalForms(string html, string stem)
    {
        return ParseAllFormsWithGrayStatus(html, stem)
            .Where(f => f.IsGray)
            .ToList();
    }
}

/// <summary>
/// Result of cross-validating a single form against both HTML and wordlist.
/// </summary>
public record InCorpusValidationResult(
    string Title,             // Grammatical combination
    string FullForm,          // Complete inflected form
    string Ending,            // Just the ending
    int EndingIndex,          // 1-based index
    bool IsGrayInHtml,        // Form is gray in DPD HTML (theoretical)
    bool InTipitakaWordlist,  // Form exists in Tipitaka wordlists
    bool ExpectedInCorpus,    // Expected corpus status (!IsGray)
    bool SourcesAgree         // Wordlist presence matches expected status
);

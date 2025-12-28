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
public static class InCorpusValidator
{
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

        // Find all <td> elements with title attributes
        var cellPattern = @"<td[^>]*title='([^']+)'[^>]*>(.*?)</td>";
        var cellMatches = Regex.Matches(html, cellPattern, RegexOptions.Singleline);

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

                // Check if this part is wrapped in gray span
                var isGray = part.Contains("class='gray'") || part.Contains("class=\"gray\"");

                // Extract the ending from <b> tag
                var boldMatch = Regex.Match(part, @"<b>(.*?)</b>");
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

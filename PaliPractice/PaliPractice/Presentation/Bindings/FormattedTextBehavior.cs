using System.Text.RegularExpressions;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Documents;

namespace PaliPractice.Presentation.Bindings;

/// <summary>
/// Attached behavior that parses simple HTML bold tags and builds TextBlock.Inlines.
/// Supports &lt;b&gt;text&lt;/b&gt; for bold formatting.
/// </summary>
public static partial class FormattedTextBehavior
{
    public static readonly DependencyProperty HtmlTextProperty =
        DependencyProperty.RegisterAttached(
            "HtmlText",
            typeof(string),
            typeof(FormattedTextBehavior),
            new PropertyMetadata(null, OnHtmlTextChanged));

    public static string GetHtmlText(DependencyObject obj) =>
        (string)obj.GetValue(HtmlTextProperty);

    public static void SetHtmlText(DependencyObject obj, string value) =>
        obj.SetValue(HtmlTextProperty, value);

    private static void OnHtmlTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock textBlock) return;

        var htmlText = e.NewValue as string;
        textBlock.Inlines.Clear();

        if (string.IsNullOrEmpty(htmlText))
            return;

        // Parse <b>...</b> tags and create Runs
        var segments = ParseBoldTags(htmlText);
        foreach (var (text, isBold) in segments)
        {
            var run = new Run { Text = text };
            if (isBold)
            {
                run.FontWeight = FontWeights.Bold;
            }
            textBlock.Inlines.Add(run);
        }
    }

    /// <summary>
    /// Parses text with &lt;b&gt;...&lt;/b&gt; tags into segments.
    /// </summary>
    private static List<(string Text, bool IsBold)> ParseBoldTags(string input)
    {
        var segments = new List<(string Text, bool IsBold)>();
        var regex = BoldTagRegex();

        int lastIndex = 0;
        foreach (Match match in regex.Matches(input))
        {
            // Add text before the match (not bold)
            if (match.Index > lastIndex)
            {
                var beforeText = input[lastIndex..match.Index];
                if (!string.IsNullOrEmpty(beforeText))
                    segments.Add((beforeText, false));
            }

            // Add the matched bold text
            var boldText = match.Groups[1].Value;
            if (!string.IsNullOrEmpty(boldText))
                segments.Add((boldText, true));

            lastIndex = match.Index + match.Length;
        }

        // Add remaining text after last match
        if (lastIndex < input.Length)
        {
            var remainingText = input[lastIndex..];
            if (!string.IsNullOrEmpty(remainingText))
                segments.Add((remainingText, false));
        }

        return segments;
    }

    [GeneratedRegex(@"<b>(.*?)</b>", RegexOptions.Singleline)]
    private static partial Regex BoldTagRegex();
}

using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using PaliPractice.Themes;

namespace PaliPractice.Presentation.Common;

/// <summary>
/// Helper for creating TextBlocks with clickable links using Markdown syntax.
/// Uses Markdig for proper Markdown parsing.
/// Supports: [link text](url), **bold**, *italic*
/// </summary>
public static class RichTextHelper
{
    static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().Build();

    /// <summary>
    /// Creates a TextBlock with regular font that has clickable links.
    /// Use Markdown syntax: [link text](url)
    /// </summary>
    public static TextBlock CreateRichText(string text)
    {
        var font = new FontFamily(FontPaths.SourceSans);
        var textBlock = new TextBlock()
            .FontFamily(font)
            .TextWrapping(TextWrapping.Wrap);

        PopulateInlines(textBlock, text, font);
        return textBlock;
    }

    /// <summary>
    /// Populates the Inlines collection of a TextBlock from Markdown text.
    /// </summary>
    static void PopulateInlines(TextBlock textBlock, string text, FontFamily font)
    {
        var document = Markdown.Parse(text, Pipeline);

        foreach (var block in document)
        {
            if (block is ParagraphBlock paragraph)
            {
                ProcessInlineContainer(textBlock, paragraph.Inline, font);
                textBlock.Inlines.Add(new LineBreak());
            }
        }

        // Remove trailing line break if present
        if (textBlock.Inlines.Count > 0 && textBlock.Inlines[^1] is LineBreak)
        {
            textBlock.Inlines.RemoveAt(textBlock.Inlines.Count - 1);
        }
    }

    /// <summary>
    /// Processes a container of inline elements.
    /// </summary>
    static void ProcessInlineContainer(TextBlock textBlock, ContainerInline? container, FontFamily font)
    {
        if (container is null) return;

        foreach (var inline in container)
        {
            ProcessInline(textBlock, inline, font);
        }
    }

    /// <summary>
    /// Processes a single inline element and adds appropriate XAML inline.
    /// </summary>
    static void ProcessInline(TextBlock textBlock, Markdig.Syntax.Inlines.Inline inline, FontFamily font)
    {
        switch (inline)
        {
            case LiteralInline literal:
                textBlock.Inlines.Add(new Run { Text = literal.Content.ToString(), FontFamily = font });
                break;

            case LinkInline link:
                if (link.Url is not null && Uri.TryCreate(link.Url, UriKind.Absolute, out var uri))
                {
                    var hyperlink = new Hyperlink { NavigateUri = uri };

                    // Get link text from children
                    foreach (var child in link)
                    {
                        if (child is LiteralInline linkText)
                        {
                            hyperlink.Inlines.Add(new Run { Text = linkText.Content.ToString(), FontFamily = font });
                        }
                    }

                    textBlock.Inlines.Add(hyperlink);
                }
                else
                {
                    // Invalid URL - just show the text
                    foreach (var child in link)
                    {
                        ProcessInline(textBlock, child, font);
                    }
                }
                break;

            case EmphasisInline emphasis:
                // Handle *italic* and **bold**
                var span = new Span { FontFamily = font };
                if (emphasis.DelimiterCount == 2)
                {
                    span.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
                }
                else
                {
                    span.FontStyle = Windows.UI.Text.FontStyle.Italic;
                }

                foreach (var child in emphasis)
                {
                    if (child is LiteralInline emphasisText)
                    {
                        span.Inlines.Add(new Run { Text = emphasisText.Content.ToString(), FontFamily = font });
                    }
                }

                textBlock.Inlines.Add(span);
                break;

            case LineBreakInline:
                textBlock.Inlines.Add(new LineBreak());
                break;

            default:
                // For any other inline type, try to get its text content
                if (inline is ContainerInline container)
                {
                    ProcessInlineContainer(textBlock, container, font);
                }
                break;
        }
    }
}

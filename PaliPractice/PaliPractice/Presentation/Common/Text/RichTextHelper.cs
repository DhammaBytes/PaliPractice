using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using PaliPractice.Themes;

namespace PaliPractice.Presentation.Common.Text;

/// <summary>
/// Helper for creating rich content from Markdown syntax.
/// Uses Markdig for proper Markdown parsing.
/// Supports: paragraphs, lists (ordered/unordered), [link text](url), **bold**, *italic*
/// </summary>
public static class RichTextHelper
{
    static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().Build();

    // Spacing constants
    const double BlockSpacing = 8;
    const double ListItemSpacing = 4;
    const double ListIndent = 0; // No indent - bullet provides visual separation
    const double BulletWidth = 20;

    /// <summary>
    /// Creates a StackPanel with proper paragraph and list rendering.
    /// Supports paragraphs, ordered lists, unordered lists, links, bold, italic.
    /// </summary>
    public static StackPanel CreateRichContent(string text, double fontSize = 14)
    {
        var font = new FontFamily(FontPaths.SourceSans);
        var panel = new StackPanel();
        var document = Markdown.Parse(text, Pipeline);
        var blocks = document.ToList();

        for (int i = 0; i < blocks.Count; i++)
        {
            bool isLast = i == blocks.Count - 1;
            var element = ProcessBlock(blocks[i], font, fontSize, isLast);
            if (element is not null)
                panel.Children.Add(element);
        }

        return panel;
    }

    /// <summary>
    /// Processes a markdown block and returns the appropriate UI element.
    /// </summary>
    static UIElement? ProcessBlock(Markdig.Syntax.Block block, FontFamily font, double fontSize, bool isLast)
    {
        UIElement? element = block switch
        {
            ParagraphBlock para => CreateParagraphTextBlock(para, font, fontSize),
            ListBlock list => CreateListPanel(list, font, fontSize),
            _ => null
        };

        // Add spacing below, except for last element
        if (element is not null && !isLast)
        {
            if (element is FrameworkElement fe)
                fe.Margin(0, 0, 0, BlockSpacing);
        }

        return element;
    }

    /// <summary>
    /// Creates a TextBlock from a paragraph block.
    /// </summary>
    static TextBlock CreateParagraphTextBlock(ParagraphBlock paragraph, FontFamily font, double fontSize = 14)
    {
        var textBlock = new TextBlock()
            .FontFamily(font)
            .FontSize(fontSize)
            .TextWrapping(TextWrapping.Wrap);

        ProcessInlineContainer(textBlock, paragraph.Inline, font);
        return textBlock;
    }

    /// <summary>
    /// Creates a StackPanel containing list items.
    /// </summary>
    static StackPanel CreateListPanel(ListBlock list, FontFamily font, double fontSize = 14)
    {
        var panel = new StackPanel()
            .Margin(ListIndent, 0, 0, 0);

        var items = list.ToList();
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] is ListItemBlock listItem)
            {
                bool isLast = i == items.Count - 1;
                string bullet = list.IsOrdered ? $"{i + 1}." : "•";
                var itemElement = CreateListItemElement(listItem, font, fontSize, bullet, isLast);
                panel.Children.Add(itemElement);
            }
        }

        return panel;
    }

    /// <summary>
    /// Creates a Grid for a single list item with bullet/number and content.
    /// </summary>
    static Grid CreateListItemElement(ListItemBlock item, FontFamily font, double fontSize, string bullet, bool isLast)
    {
        // Get content from the list item (usually contains a ParagraphBlock)
        var contentPanel = new StackPanel();
        var itemBlocks = item.ToList();

        for (int i = 0; i < itemBlocks.Count; i++)
        {
            if (itemBlocks[i] is ParagraphBlock para)
            {
                var tb = CreateParagraphTextBlock(para, font, fontSize);
                // Add spacing between multiple paragraphs in same list item
                if (i > 0)
                    tb.Margin(0, ListItemSpacing, 0, 0);
                contentPanel.Children.Add(tb);
            }
            else if (itemBlocks[i] is ListBlock nestedList)
            {
                // Handle nested lists
                var nestedPanel = CreateListPanel(nestedList, font, fontSize);
                nestedPanel.Margin(0, ListItemSpacing, 0, 0);
                contentPanel.Children.Add(nestedPanel);
            }
        }

        var bulletTextBlock = new TextBlock { Text = bullet, FontFamily = font }
            .FontSize(fontSize)
            .Grid(column: 0)
            .VerticalAlignment(VerticalAlignment.Top);

        var grid = new Grid()
            .ColumnDefinitions($"{BulletWidth},*")
            .Children(
                bulletTextBlock,
                contentPanel.Grid(column: 1)
            );

        // Add spacing below, except for last item
        if (!isLast)
            grid.Margin(0, 0, 0, ListItemSpacing);

        return grid;
    }

    /// <summary>
    /// Creates a TextBlock with regular font that has clickable links.
    /// Use Markdown syntax: [link text](url)
    /// Legacy method - prefer CreateRichContent for new code.
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

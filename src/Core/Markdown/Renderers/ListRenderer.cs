using System.Text;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Spectre.Console;
using Spectre.Console.Rendering;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// List renderer that builds Spectre.Console objects directly instead of markup strings.
/// This eliminates VT escaping issues and avoids double-parsing overhead.
/// </summary>
internal static class ListRenderer
{
    private const string TaskCheckedEmoji = "✅ ";
    private const string TaskUncheckedEmoji = "⬜ ";  // More visible white square
    private const string UnorderedBullet = "• ";

    /// <summary>
    /// Renders a list block by building Spectre.Console objects directly.
    /// This approach eliminates VT escaping issues and improves performance.
    /// </summary>
    /// <param name="list">The list block to render</param>
    /// <param name="theme">Theme for styling</param>
    /// <returns>Rendered list as a Paragraph with proper styling</returns>
    public static IRenderable Render(ListBlock list, Theme theme)
    {
        var paragraph = new Paragraph();
        int number = 1;
        bool isFirstItem = true;

        foreach (ListItemBlock item in list.Cast<ListItemBlock>())
        {
            // Add line break between items (except for the first)
            if (!isFirstItem)
                paragraph.Append("\n", Style.Plain);

            // Check if this is a task list item using Markdig's native TaskList support
            var (isTaskList, isChecked) = DetectTaskListItem(item);

            // Build prefix and append it
            var prefixText = CreateListPrefixText(list.IsOrdered, isTaskList, isChecked, ref number);
            paragraph.Append(prefixText, Style.Plain);

            // Extract and append the item content directly as styled text
            AppendListItemContent(paragraph, item, theme);

            isFirstItem = false;
        }

        return paragraph;
    }

    /// <summary>
    /// Detects if a list item is a task list item using Markdig's native TaskList support.
    /// </summary>
    private static (bool isTaskList, bool isChecked) DetectTaskListItem(ListItemBlock item)
    {
        if (item.FirstOrDefault() is ParagraphBlock paragraph && paragraph.Inline is not null)
        {
            foreach (Inline inline in paragraph.Inline)
            {
                if (inline is TaskList taskList)
                {
                    return (true, taskList.Checked);
                }
            }
        }

        return (false, false);
    }

    /// <summary>
    /// Creates the appropriate prefix text for list items.
    /// </summary>
    private static string CreateListPrefixText(bool isOrdered, bool isTaskList, bool isChecked, ref int number)
    {
        if (isTaskList)
        {
            return isChecked ? TaskCheckedEmoji : TaskUncheckedEmoji;
        }
        else if (isOrdered)
        {
            return $"{number++}. ";
        }
        else
        {
            return UnorderedBullet;
        }
    }

    /// <summary>
    /// Creates the appropriate prefix for list items as styled Text objects.
    /// </summary>
    private static Text CreateListPrefix(bool isOrdered, bool isTaskList, bool isChecked, ref int number)
    {
        if (isTaskList)
        {
            var emoji = isChecked ? TaskCheckedEmoji : TaskUncheckedEmoji;
            return new Text(emoji, Style.Plain);
        }
        else if (isOrdered)
        {
            var numberText = $"{number++}. ";
            return new Text(numberText, Style.Plain);
        }
        else
        {
            return new Text(UnorderedBullet, Style.Plain);
        }
    }

    /// <summary>
    /// Appends list item content directly to the paragraph using styled Text objects.
    /// This eliminates the need for markup parsing and VT escaping.
    /// </summary>
    private static void AppendListItemContent(Paragraph paragraph, ListItemBlock item, Theme theme)
    {
        foreach (Block subBlock in item)
        {
            switch (subBlock)
            {
                case ParagraphBlock subPara:
                    AppendInlineContent(paragraph, subPara.Inline, theme);
                    break;

                case CodeBlock subCode:
                    var codeText = subCode.Lines.ToString();
                    paragraph.Append(codeText, Style.Plain);
                    break;

                case ListBlock nestedList:
                    // For nested lists, render as indented text content
                    var nestedContent = RenderNestedListAsText(nestedList, theme, 1);
                    if (!string.IsNullOrEmpty(nestedContent))
                    {
                        paragraph.Append("\n", Style.Plain);
                        paragraph.Append(nestedContent, Style.Plain);
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Processes inline content and appends it directly to the paragraph with proper styling.
    /// This method builds Text objects directly instead of markup strings.
    /// </summary>
    private static void AppendInlineContent(Paragraph paragraph, Markdig.Syntax.Inlines.ContainerInline? inlines, Theme theme)
    {
        if (inlines is null) return;

        foreach (Inline inline in inlines)
        {
            switch (inline)
            {
                case Markdig.Syntax.Inlines.LiteralInline literal:
                    var literalText = literal.Content.ToString();
                    paragraph.Append(literalText, Style.Plain);
                    break;

                case Markdig.Syntax.Inlines.EmphasisInline emphasis:
                    var emphasisStyle = emphasis.DelimiterCount == 2
                        ? new Style(decoration: Decoration.Bold)
                        : new Style(decoration: Decoration.Italic);

                    var emphasisContent = ExtractInlineText(emphasis);
                    paragraph.Append(emphasisContent, emphasisStyle);
                    break;

                case Markdig.Syntax.Inlines.CodeInline code:
                    var codeStyle = new Style(foreground: Color.Yellow, background: Color.Grey11);
                    paragraph.Append(code.Content, codeStyle);
                    break;

                case Markdig.Syntax.Inlines.LinkInline link:
                    var linkStyle = new Style(foreground: Color.Blue, decoration: Decoration.Underline);
                    var linkText = ExtractInlineText(link) ?? link.Url ?? "";
                    paragraph.Append(linkText, linkStyle);
                    break;

                case TaskList taskList:
                    // TaskList inlines are handled at the item level, skip here
                    break;

                default:
                    // Fallback for unknown inline types
                    var defaultText = ExtractInlineText(inline) ?? "";
                    paragraph.Append(defaultText, Style.Plain);
                    break;
            }
        }
    }

    /// <summary>
    /// Extracts plain text from inline elements without markup.
    /// </summary>
    private static string ExtractInlineText(Inline inline)
    {
        var builder = new StringBuilder();
        ExtractInlineTextRecursive(inline, builder);
        return builder.ToString();
    }

    /// <summary>
    /// Recursively extracts text from inline elements.
    /// </summary>
    private static void ExtractInlineTextRecursive(Inline inline, StringBuilder builder)
    {
        switch (inline)
        {
            case Markdig.Syntax.Inlines.LiteralInline literal:
                builder.Append(literal.Content.ToString());
                break;

            case Markdig.Syntax.Inlines.ContainerInline container:
                foreach (var child in container)
                {
                    ExtractInlineTextRecursive(child, builder);
                }
                break;

            case Markdig.Syntax.Inlines.LeafInline leaf:
                // For leaf inlines like CodeInline, extract their content
                if (leaf is Markdig.Syntax.Inlines.CodeInline code)
                {
                    builder.Append(code.Content);
                }
                break;
        }
    }

    /// <summary>
    /// Renders nested lists as indented text content.
    /// </summary>
    private static string RenderNestedListAsText(ListBlock list, Theme theme, int indentLevel)
    {
        var builder = new StringBuilder();
        var indent = new string(' ', indentLevel * 2);
        int number = 1;
        bool isFirstItem = true;

        foreach (ListItemBlock item in list)
        {
            if (!isFirstItem)
                builder.Append('\n');

            builder.Append(indent);

            var (isTaskList, isChecked) = DetectTaskListItem(item);

            if (isTaskList)
            {
                builder.Append(isChecked ? TaskCheckedEmoji : TaskUncheckedEmoji);
            }
            else if (list.IsOrdered)
            {
                builder.Append(System.Globalization.CultureInfo.InvariantCulture, $"{number++}. ");
            }
            else
            {
                builder.Append(UnorderedBullet);
            }

            // Extract item text without complex inline processing for nested items
            var itemText = ExtractListItemTextSimple(item);
            builder.Append(itemText.Trim());

            isFirstItem = false;
        }

        return builder.ToString();
    }

    /// <summary>
    /// Simple text extraction for nested list items.
    /// </summary>
    private static string ExtractListItemTextSimple(ListItemBlock item)
    {
        var builder = new StringBuilder();

        foreach (Block subBlock in item)
        {
            if (subBlock is ParagraphBlock subPara && subPara.Inline is not null)
            {
                foreach (Inline inline in subPara.Inline)
                {
                    if (inline is not TaskList) // Skip TaskList markers
                    {
                        builder.Append(ExtractInlineText(inline));
                    }
                }
            }
            else if (subBlock is CodeBlock subCode)
            {
                builder.Append(subCode.Lines.ToString());
            }
        }

        return builder.ToString();
    }
}

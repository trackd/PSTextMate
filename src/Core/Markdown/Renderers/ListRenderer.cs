using System.Text;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Spectre.Console;
using Spectre.Console.Rendering;
using TextMateSharp.Themes;
using PwshSpectreConsole.TextMate.Helpers;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// List renderer that builds Spectre.Console objects directly instead of markup strings.
/// This eliminates VT escaping issues and avoids double-parsing overhead.
/// </summary>
internal static class ListRenderer {
    private const string TaskCheckedEmoji = "✅ ";
    private const string TaskUncheckedEmoji = "⬜ ";  // More visible white square
    private const string UnorderedBullet = "• ";

    /// <summary>
    /// Renders a list block by building Spectre.Console objects directly.
    /// This approach eliminates VT escaping issues and improves performance.
    /// </summary>
    /// <param name="list">The list block to render</param>
    /// <param name="theme">Theme for styling</param>
    /// <returns>Rendered list as Rows containing separate Paragraphs for each item</returns>
    public static IRenderable Render(ListBlock list, Theme theme) {
        var renderables = new List<IRenderable>();
        int number = 1;

        foreach (ListItemBlock item in list.Cast<ListItemBlock>()) {
            var itemParagraph = new Paragraph();

            (bool isTaskList, bool isChecked) = DetectTaskListItem(item);

            string prefixText = CreateListPrefixText(list.IsOrdered, isTaskList, isChecked, ref number);
            itemParagraph.Append(prefixText, Style.Plain);

            List<IRenderable> nestedRenderables = AppendListItemContent(itemParagraph, item, theme);

            renderables.Add(itemParagraph);
            if (nestedRenderables.Count > 0) {
                renderables.AddRange(nestedRenderables);
            }
        }

        // Rows will add a single line break after each child
        return new Rows([.. renderables]);
    }

    /// <summary>
    /// Detects if a list item is a task list item using Markdig's native TaskList support.
    /// </summary>
    private static (bool isTaskList, bool isChecked) DetectTaskListItem(ListItemBlock item) {
        if (item.FirstOrDefault() is ParagraphBlock paragraph && paragraph.Inline is not null) {
            foreach (Inline inline in paragraph.Inline) {
                if (inline is TaskList taskList) {
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
        => isTaskList ? isChecked ? TaskCheckedEmoji : TaskUncheckedEmoji : isOrdered ? $"{number++}. " : UnorderedBullet;

    /// <summary>
    /// Creates the appropriate prefix for list items as styled Text objects.
    /// </summary>
    private static Text CreateListPrefix(bool isOrdered, bool isTaskList, bool isChecked, ref int number) {
        if (isTaskList) {
            string emoji = isChecked ? TaskCheckedEmoji : TaskUncheckedEmoji;
            return new Text(emoji, Style.Plain);
        }
        else if (isOrdered) {
            string numberText = $"{number++}. ";
            return new Text(numberText, Style.Plain);
        }
        else {
            return new Text(UnorderedBullet, Style.Plain);
        }
    }

    /// <summary>
    /// Appends list item content directly to the paragraph using styled Text objects.
    /// This eliminates the need for markup parsing and VT escaping.
    /// </summary>
    private static List<IRenderable> AppendListItemContent(Paragraph paragraph, ListItemBlock item, Theme theme) {
        var nestedRenderables = new List<IRenderable>();

        foreach (Block subBlock in item) {
            switch (subBlock) {
                case ParagraphBlock subPara:
                    AppendInlineContent(paragraph, subPara.Inline, theme);
                    break;

                case CodeBlock subCode:
                    string codeText = subCode.Lines.ToString();
                    paragraph.Append(codeText, Style.Plain);
                    break;

                case ListBlock nestedList:
                    string nestedContent = RenderNestedListAsText(nestedList, theme, 1);
                    if (!string.IsNullOrEmpty(nestedContent)) {
                        var nestedParagraph = new Paragraph();
                        nestedParagraph.Append(nestedContent, Style.Plain);
                        nestedRenderables.Add(nestedParagraph);
                    }
                    break;
                default:
                    break;
            }
        }

        return nestedRenderables;
    }

    /// <summary>
    /// Processes inline content and appends it directly to the paragraph with proper styling.
    /// This method builds Text objects directly instead of markup strings.
    /// </summary>
    private static void AppendInlineContent(Paragraph paragraph, ContainerInline? inlines, Theme theme) {
        if (inlines is null) return;

        // Skip LineBreakInline for list items; Rows handles separation between list entries
        ParagraphRenderer.ProcessInlineElements(paragraph, inlines, theme, skipLineBreaks: true);
    }

    /// <summary>
    /// Extracts plain text from inline elements without markup.
    /// </summary>
    private static string ExtractInlineText(Inline inline) {
        StringBuilder builder = StringBuilderPool.Rent();
        try {
            ExtractInlineTextRecursive(inline, builder);
            return builder.ToString();
        }
        finally {
            StringBuilderPool.Return(builder);
        }
    }

    /// <summary>
    /// Recursively extracts text from inline elements.
    /// </summary>
    private static void ExtractInlineTextRecursive(Inline inline, StringBuilder builder) {
        switch (inline) {
            case LiteralInline literal:
                builder.Append(literal.Content.ToString());
                break;

            case ContainerInline container:
                foreach (Inline child in container) {
                    ExtractInlineTextRecursive(child, builder);
                }
                break;

            case LeafInline leaf:
                // For leaf inlines like CodeInline, extract their content
                if (leaf is CodeInline code) {
                    builder.Append(code.Content);
                }
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Renders nested lists as indented text content.
    /// </summary>
    private static string RenderNestedListAsText(ListBlock list, Theme theme, int indentLevel) {
        StringBuilder builder = StringBuilderPool.Rent();
        try {
            string indent = new(' ', indentLevel * 4);
            int number = 1;
            bool isFirstItem = true;

            foreach (ListItemBlock item in list.Cast<ListItemBlock>()) {
                if (!isFirstItem) {
                    builder.Append('\n');
                }

                builder.Append(indent);

                (bool isTaskList, bool isChecked) = DetectTaskListItem(item);

                if (isTaskList) {
                    builder.Append(isChecked ? TaskCheckedEmoji : TaskUncheckedEmoji);
                }
                else if (list.IsOrdered) {
                    builder.Append(System.Globalization.CultureInfo.InvariantCulture, $"{number++}. ");
                }
                else {
                    builder.Append(UnorderedBullet);
                }

                // Extract item text without complex inline processing for nested items
                string itemText = ExtractListItemTextSimple(item);
                builder.Append(itemText.Trim());

                isFirstItem = false;
            }

            return builder.ToString();
        }
        finally {
            StringBuilderPool.Return(builder);
        }
    }

    /// <summary>
    /// Simple text extraction for nested list items.
    /// </summary>
    private static string ExtractListItemTextSimple(ListItemBlock item) {
        StringBuilder builder = StringBuilderPool.Rent();
        try {
            foreach (Block subBlock in item) {
                if (subBlock is ParagraphBlock subPara && subPara.Inline is not null) {
                    foreach (Inline inline in subPara.Inline) {
                        if (inline is not TaskList) {
                            // Skip TaskList markers
                            builder.Append(ExtractInlineText(inline));
                        }
                    }
                }
                else if (subBlock is CodeBlock subCode) {
                    builder.Append(subCode.Lines.ToString());
                }
            }

            return builder.ToString();
        }
        finally {
            StringBuilderPool.Return(builder);
        }
    }
}

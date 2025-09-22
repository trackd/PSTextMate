using System.Text;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Spectre.Console;
using Spectre.Console.Rendering;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Renders markdown list blocks including task lists.
/// </summary>
internal static class ListRenderer
{
    /// <summary>
    /// Renders a list block with support for ordered lists, unordered lists, and task lists.
    /// </summary>
    /// <param name="list">The list block to render</param>
    /// <param name="theme">Theme for styling</param>
    /// <returns>Rendered list markup</returns>
    public static IRenderable Render(ListBlock list, Theme theme)
    {
        var items = new List<string>();
        int number = 1;

        foreach (ListItemBlock item in list.Cast<ListItemBlock>())
        {
            // Check if this is a task list item using Markdig's native TaskList support
            var (isTaskList, isChecked) = DetectTaskListItem(item);

            // Extract the text content (TaskList inlines are automatically excluded by InlineProcessor)
            string itemText = ExtractListItemText(item, theme);

            string prefix;
            if (isTaskList)
            {
                prefix = isChecked ? "✅ " : "⬜ ";  // More visible white square
            }
            else if (list.IsOrdered)
            {
                prefix = $"{number++}. ";
            }
            else
            {
                prefix = "• ";
            }

            items.Add(prefix + Markup.Escape(itemText.Trim()));
        }

        return new Markup(string.Join("\n", items));
    }

    /// <summary>
    /// Detects if a list item is a task list item using Markdig's native TaskList support.
    /// </summary>
    /// <param name="item">The list item to check</param>
    /// <returns>A tuple indicating if it's a task list and if it's checked</returns>
    private static (bool isTaskList, bool isChecked) DetectTaskListItem(ListItemBlock item)
    {
        // Look for TaskList inline elements in the first paragraph
        if (item.FirstOrDefault() is ParagraphBlock paragraph && paragraph.Inline is not null)
        {
            foreach (Inline inline in paragraph.Inline)
            {
                // Use proper type checking with Markdig's TaskList type
                if (inline is TaskList taskList)
                {
                    return (true, taskList.Checked);
                }
            }
        }

        return (false, false);
    }

    /// <summary>
    /// Extracts text content from a list item block.
    /// </summary>
    private static string ExtractListItemText(ListItemBlock item, Theme theme)
    {
        string itemText = string.Empty;

        foreach (Block subBlock in item)
        {
            if (subBlock is ParagraphBlock subPara)
            {
                var itemBuilder = new StringBuilder();
                InlineProcessor.ExtractInlineText(subPara.Inline, theme, itemBuilder);
                itemText += itemBuilder.ToString();
            }
            else if (subBlock is CodeBlock subCode)
            {
                itemText += subCode.Lines.ToString();
            }
            else if (subBlock is ListBlock nestedList)
            {
                // Handle nested lists by building the text content directly
                itemText += RenderNestedList(nestedList, theme, 1);
            }
        }

        return itemText;
    }

    /// <summary>
    /// Renders a nested list with proper indentation.
    /// </summary>
    /// <param name="list">The nested list to render</param>
    /// <param name="theme">Theme for styling</param>
    /// <param name="indentLevel">Current indentation level</param>
    /// <returns>Formatted nested list text</returns>
    private static string RenderNestedList(ListBlock list, Theme theme, int indentLevel)
    {
        var items = new List<string>();
        int number = 1;
        string indent = new(' ', indentLevel * 2);

        foreach (ListItemBlock item in list)
        {
            // Check if this is a task list item using Markdig's native TaskList support
            var (isTaskList, isChecked) = DetectTaskListItem(item);

            // Extract the text content
            string itemText = ExtractNestedListItemText(item, theme, indentLevel);

            string prefix;
            if (isTaskList)
            {
                prefix = isChecked ? "✅ " : "⬜ ";  // More visible white square
            }
            else if (list.IsOrdered)
            {
                prefix = $"{number++}. ";
            }
            else
            {
                prefix = "• ";
            }

            items.Add(indent + prefix + itemText.Trim());
        }

        return string.Join("\n", items);
    }

    /// <summary>
    /// Extracts text content from a nested list item block.
    /// </summary>
    /// <param name="item">The list item to extract text from</param>
    /// <param name="theme">Theme for styling</param>
    /// <param name="indentLevel">Current indentation level</param>
    /// <returns>Extracted text content</returns>
    private static string ExtractNestedListItemText(ListItemBlock item, Theme theme, int indentLevel)
    {
        string itemText = string.Empty;

        foreach (Block subBlock in item)
        {
            if (subBlock is ParagraphBlock subPara)
            {
                var itemBuilder = new StringBuilder();
                InlineProcessor.ExtractInlineText(subPara.Inline, theme, itemBuilder);
                itemText += itemBuilder.ToString();
            }
            else if (subBlock is CodeBlock subCode)
            {
                itemText += subCode.Lines.ToString();
            }
            else if (subBlock is ListBlock nestedList)
            {
                // Handle deeply nested lists recursively
                itemText += RenderNestedList(nestedList, theme, indentLevel + 1);
            }
        }

        return itemText;
    }
}

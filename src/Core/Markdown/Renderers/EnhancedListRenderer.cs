using System.Text;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Spectre.Console;
using Spectre.Console.Rendering;
using PwshSpectreConsole.TextMate.Extensions;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Enhanced list renderer with span-based optimizations for better performance.
/// Reduces string allocations during list item processing and text concatenation.
/// </summary>
internal static class EnhancedListRenderer
{
    private static readonly char[] TaskListPrefixes = ['[', ' ', 'x', 'X', ']'];
    private const string TaskCheckedEmoji = "✅ ";
    private const string TaskUncheckedEmoji = "☐ ";
    private const string UnorderedBullet = "• ";

    /// <summary>
    /// Renders a list block with span-optimized processing for better performance.
    /// </summary>
    /// <param name="list">The list block to render</param>
    /// <param name="theme">Theme for styling</param>
    /// <returns>Rendered list markup</returns>
    public static IRenderable Render(ListBlock list, Theme theme)
    {
        // Pre-calculate capacity to reduce allocations
        int estimatedCapacity = list.Count * 50; // Rough estimate per item
        var resultBuilder = new StringBuilder(estimatedCapacity);
        int number = 1;

        foreach (ListItemBlock item in list)
        {
            if (resultBuilder.Length > 0)
                resultBuilder.Append('\n');

            (bool isTaskList, bool isChecked) = DetectTaskListOptimized(item);
            string itemText = ExtractListItemTextOptimized(item, theme);

            // Build prefix using spans to avoid multiple concatenations
            AppendListPrefix(resultBuilder, list.IsOrdered, isTaskList, isChecked, ref number);
            resultBuilder.Append(Markup.Escape(itemText.TrimOptimized()));
        }

        return new Markup(resultBuilder.ToString());
    }

    /// <summary>
    /// Detects task list items using Markdig's native TaskList support.
    /// </summary>
    private static (bool isTaskList, bool isChecked) DetectTaskListOptimized(ListItemBlock item)
    {
        if (item.FirstOrDefault() is not ParagraphBlock paragraph || paragraph.Inline is null)
            return (false, false);

        // Use proper type checking with Markdig's TaskList type
        foreach (Inline inline in paragraph.Inline)
        {
            if (inline is TaskList taskList)
            {
                return (true, taskList.Checked);
            }
        }

        return (false, false);
    }    /// <summary>
    /// Extracts list item text using span-optimized operations.
    /// </summary>
    private static string ExtractListItemTextOptimized(ListItemBlock item, Theme theme)
    {
        var textBuilder = new StringBuilder(256); // Pre-allocate reasonable capacity

        foreach (var subBlock in item)
        {
            switch (subBlock)
            {
                case ParagraphBlock subPara:
                    InlineProcessor.ExtractInlineText(subPara.Inline, theme, textBuilder);
                    break;

                case CodeBlock subCode:
                    // Use span to avoid ToString() allocation where possible
                    string codeText = subCode.Lines.ToString();
                    textBuilder.Append(codeText.AsSpan());
                    break;

                case ListBlock nestedList:
                    string nestedText = RenderNestedListOptimized(nestedList, theme, 1);
                    textBuilder.Append(nestedText.AsSpan());
                    break;
            }
        }

        return textBuilder.ToString();
    }

    /// <summary>
    /// Renders nested lists with span-optimized indentation and processing.
    /// </summary>
    private static string RenderNestedListOptimized(ListBlock list, Theme theme, int indentLevel)
    {
        // Calculate capacity based on nesting and item count
        int estimatedCapacity = list.Count * (50 + indentLevel * 2);
        var resultBuilder = new StringBuilder(estimatedCapacity);

        // Pre-calculate indent string
        ReadOnlySpan<char> indentSpan = new string(' ', indentLevel * 2).AsSpan();
        int number = 1;

        foreach (ListItemBlock item in list)
        {
            if (resultBuilder.Length > 0)
                resultBuilder.Append('\n');

            // Use span for efficient indentation
            resultBuilder.Append(indentSpan);

            (bool isTaskList, bool isChecked) = DetectTaskListOptimized(item);
            string itemText = ExtractNestedListItemTextOptimized(item, theme, indentLevel);

            AppendListPrefix(resultBuilder, list.IsOrdered, isTaskList, isChecked, ref number);
            resultBuilder.Append(itemText.TrimOptimized().AsSpan());
        }

        return resultBuilder.ToString();
    }

    /// <summary>
    /// Extracts nested list item text with span optimizations.
    /// </summary>
    private static string ExtractNestedListItemTextOptimized(ListItemBlock item, Theme theme, int indentLevel)
    {
        var textBuilder = new StringBuilder(256);

        foreach (Block subBlock in item)
        {
            switch (subBlock)
            {
                case ParagraphBlock subPara:
                    InlineProcessor.ExtractInlineText(subPara.Inline, theme, textBuilder);
                    break;

                case CodeBlock subCode:
                    string? codeText = subCode.Lines.ToString();
                    textBuilder.Append(codeText.AsSpan());
                    break;

                case ListBlock nestedList:
                    string? nestedText = RenderNestedListOptimized(nestedList, theme, indentLevel + 1);
                    textBuilder.Append(nestedText.AsSpan());
                    break;
            }
        }

        return textBuilder.ToString();
    }

    /// <summary>
    /// Efficiently appends list prefixes using span operations.
    /// </summary>
    private static void AppendListPrefix(StringBuilder builder, bool isOrdered, bool isTaskList, bool isChecked, ref int number)
    {
        if (isTaskList)
        {
            ReadOnlySpan<char> prefix = isChecked ? TaskCheckedEmoji.AsSpan() : TaskUncheckedEmoji.AsSpan();
            builder.Append(prefix);
        }
        else if (isOrdered)
        {
            // Use span formatting for better performance
            Span<char> numberBuffer = stackalloc char[16]; // Sufficient for reasonable list numbers
            if (number.TryFormat(numberBuffer, out int charsWritten, provider: System.Globalization.CultureInfo.InvariantCulture))
            {
                builder.Append(numberBuffer[..charsWritten]);
                builder.Append(". ");
            }
            else
            {
                builder.Append(number); // Use strongly-typed overload
                builder.Append(". ");
            }
            number++;
        }
        else
        {
            builder.Append(UnorderedBullet.AsSpan());
        }
    }
}

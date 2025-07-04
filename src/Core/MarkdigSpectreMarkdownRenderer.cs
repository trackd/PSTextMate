using System;
using System.Collections.Generic;
using Markdig;
using Spectre.Console;
using Spectre.Console.Rendering;
using PwshSpectreConsole.TextMate.Core;
using TextMateSharp.Grammars;
using TextMateSharp.Model;
using TextMateSharp.Registry;
using TextMateSharp.Themes;
using PwshSpectreConsole.TextMate.Extensions;

namespace PwshSpectreConsole.TextMate.Core;

/// <summary>
/// Alternative Markdown renderer using Markdig for parsing and Spectre.Console for rendering.
/// Renders GitHub-style markdown as closely as possible in the console.
/// </summary>
internal static class MarkdigSpectreMarkdownRenderer
{
    // Helper to extract plain text from Markdig inline elements
    private static string ExtractInlineText(Markdig.Syntax.Inlines.ContainerInline? container)
    {
        if (container == null) return string.Empty;
        var result = new List<string>();
        foreach (var inline in container)
        {
            switch (inline)
            {
                case Markdig.Syntax.Inlines.LiteralInline literal:
                    var span = literal.Content.Text.AsSpan(literal.Content.Start, literal.Content.Length);
                    result.Add(span.ToString());
                    break;
                case Markdig.Syntax.Inlines.LinkInline link:
                    var linkText = ExtractInlineText(link);
                    if (!string.IsNullOrEmpty(link.Url))
                        result.Add(MarkdownLinkFormatter.WriteMarkdownLink(link.Url, linkText));
                    else
                        result.Add(linkText);
                    break;
                case Markdig.Syntax.Inlines.EmphasisInline emph:
                    var marker = emph.DelimiterCount == 2 ? "**" : "*";
                    result.Add(marker);
                    result.Add(ExtractInlineText(emph));
                    result.Add(marker);
                    break;
                case Markdig.Syntax.Inlines.CodeInline code:
                    result.Add('`' + code.Content + '`');
                    break;
                case Markdig.Syntax.Inlines.LineBreakInline:
                    result.Add("\n");
                    break;
                default:
                    if (inline is Markdig.Syntax.Inlines.ContainerInline childContainer)
                        result.Add(ExtractInlineText(childContainer));
                    break;
            }
        }
        return string.Concat(result);
    }
    /// <summary>
    /// Renders markdown content using Markdig and Spectre.Console.
    /// </summary>
    /// <param name="markdown">Markdown text (can be multi-line)</param>
    /// <returns>Rows object for Spectre.Console rendering</returns>
    public static Rows Render(string markdown, TextMateSharp.Themes.Theme theme)
    {
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UsePipeTables()
            .UseEmphasisExtras()
            .UseAutoLinks()
            .UseTaskLists()
            .Build();

        var document = Markdig.Markdown.Parse(markdown, pipeline);
        var rows = new List<IRenderable>();
        bool lastWasContent = false;
        foreach (var block in document)
        {
            var renderable = RenderBlock(block, theme);
            if (renderable != null)
            {
                if (lastWasContent)
                {
                    // Insert a blank line between blocks for readability
                    rows.Add(Text.Empty);
                }
                rows.Add(renderable);
                lastWasContent = true;
            }
            else
            {
                lastWasContent = false;
            }
        }
        return new Rows(rows.ToArray());
    }

    private static IRenderable? RenderBlock(Markdig.Syntax.Block block, TextMateSharp.Themes.Theme theme)
    {
        switch (block)
        {
            case Markdig.Syntax.HeadingBlock heading:
                var headingText = ExtractInlineText(heading.Inline);
                var style = heading.Level switch
                {
                    1 => new Style(foreground: Color.Blue, decoration: Decoration.Bold),
                    2 => new Style(foreground: Color.Cyan1, decoration: Decoration.Bold),
                    3 => new Style(foreground: Color.Green, decoration: Decoration.Bold),
                    _ => new Style(decoration: Decoration.Bold)
                };
                return new Markup(headingText, style);
            case Markdig.Syntax.ParagraphBlock para:
                var paraText = ExtractInlineText(para.Inline);
                if (string.IsNullOrWhiteSpace(paraText))
                    return Text.Empty;
                return new Markup(paraText);
            case Markdig.Syntax.ListBlock list:
                var items = new List<string>();
                foreach (Markdig.Syntax.ListItemBlock item in list)
                {
                    string itemText = string.Empty;
                    foreach (var subBlock in item)
                    {
                        if (subBlock is Markdig.Syntax.ParagraphBlock subPara)
                            itemText += ExtractInlineText(subPara.Inline);
                        else if (subBlock is Markdig.Syntax.CodeBlock subCode)
                            itemText += subCode.Lines.ToString();
                    }
                    items.Add((list.IsOrdered ? "1. " : "- ") + Markup.Escape(itemText.Trim()));
                }
                return new Markup(string.Join("\n", items));
            case Markdig.Syntax.FencedCodeBlock fencedCode:
            {
                // Always use TextMateProcessor for syntax highlighting if language is specified
                var codeLines = new List<string>();
                foreach (var line in fencedCode.Lines.Lines)
                {
                    var slice = line.Slice;
                    codeLines.Add(slice.ToString());
                }
                var lang = (fencedCode.Info ?? string.Empty).Trim();
                if (!string.IsNullOrEmpty(lang))
                {
                    try
                    {
                        var rows = TextMateProcessor.ProcessLines(codeLines.ToArray(), ThemeName.Dark, lang, false);
                        if (rows != null)
                        {
                            return new Panel(rows).Border(BoxBorder.Rounded).Header(lang, Justify.Left);
                        }
                    }
                    catch
                    {
                        // Fallback to plain rendering below
                    }
                }
                // Fallback: plain code panel (escape text to avoid Spectre markup errors)
                var fallbackText = Markup.Escape(string.Join("\n", codeLines));
                var fallbackStyle = new Style(foreground: Color.Grey, background: Color.Black);
                return new Panel(new Markup(fallbackText, fallbackStyle)).Border(BoxBorder.Rounded).Header(!string.IsNullOrEmpty(lang) ? lang : "code", Justify.Left);
            }
            case Markdig.Syntax.CodeBlock code:
            {
                // Indented code block (no language)
                var codeText = Markup.Escape(code.Lines.ToString());
                var codeStyle = new Style(foreground: Color.Grey, background: Color.Black);
                return new Panel(new Markup(codeText, codeStyle)).Border(BoxBorder.Rounded).Header("code", Justify.Left);
            }
            case Markdig.Extensions.Tables.Table table:
                var tables = new List<Table>();
                Table? spectreTable = null;
                List<string>? headerCells = null;
                bool headerAdded = false;
                var allRows = new List<(bool isHeader, List<string> cells)>();
                foreach (Markdig.Extensions.Tables.TableRow row in table)
                {
                    var cells = new List<string>();
                    foreach (Markdig.Extensions.Tables.TableCell cell in row)
                    {
                        string cellText = string.Empty;
                        foreach (var cellBlock in cell)
                        {
                            if (cellBlock is Markdig.Syntax.ParagraphBlock para)
                                cellText += ExtractInlineText(para.Inline);
                            else
                                cellText += cellBlock.ToString();
                        }
                        cells.Add(cellText);
                    }
                    if (row.IsHeader && headerCells == null)
                        headerCells = new List<string>(cells);
                    allRows.Add((row.IsHeader, cells));
                }
                int colCount = headerCells?.Count ?? 0;
                foreach (var (isHeader, cells) in allRows)
                {
                    if (isHeader)
                    {
                        if (spectreTable != null && headerAdded)
                        {
                            tables.Add(spectreTable);
                            spectreTable = null;
                            headerAdded = false;
                        }
                        if (spectreTable == null)
                            spectreTable = new Table();
                        for (int i = 0; i < colCount; i++)
                            spectreTable.AddColumn(Markup.Escape(cells[i]));
                        headerAdded = true;
                        continue;
                    }
                    if (spectreTable == null)
                        continue;
                    var rowCells = new List<IRenderable>();
                    for (int i = 0; i < colCount; i++)
                        rowCells.Add(new Markup(Markup.Escape(i < cells.Count ? cells[i] : "")));
                    spectreTable.AddRow(rowCells);
                }
                if (spectreTable != null && headerAdded)
                    tables.Add(spectreTable);
                if (tables.Count == 1)
                    return tables[0];
                else if (tables.Count > 1)
                    return new Rows(tables.ToArray());
                else
                    return null;
            case Markdig.Syntax.QuoteBlock quote:
                string quoteText = string.Empty;
                foreach (var subBlock in quote)
                {
                    if (subBlock is Markdig.Syntax.ParagraphBlock para)
                        quoteText += ExtractInlineText(para.Inline);
                    else
                        quoteText += subBlock.ToString();
                }
                return new Panel(new Markup(Markup.Escape(quoteText))).Border(BoxBorder.Double).Header("quote", Justify.Left);
            default:
                return null;
        }
    }
}

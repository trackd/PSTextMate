using System;
using System.Collections.Generic;
using System.Text;
using Markdig;
using Spectre.Console;
using System.Linq;
using Spectre.Console.Rendering;
using PwshSpectreConsole.TextMate.Core;
using PwshSpectreConsole.TextMate.Extensions;
using TextMateSharp.Grammars;
using TextMateSharp.Model;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core;

/// <summary>
/// Alternative Markdown renderer using Markdig for parsing and Spectre.Console for rendering.
/// Renders GitHub-style markdown as closely as possible in the console.
/// </summary>
internal static class MarkdigSpectreMarkdownRenderer
{
    // Helper to extract plain text from Markdig inline elements
    private static void ExtractInlineText(Markdig.Syntax.Inlines.ContainerInline? container, TextMateSharp.Themes.Theme theme, StringBuilder builder)
    {
        if (container is null) return;

        foreach (var inline in container)
        {
            switch (inline)
            {
                case Markdig.Syntax.Inlines.LiteralInline literal:
                    var span = literal.Content.Text.AsSpan(literal.Content.Start, literal.Content.Length);
                    builder.Append(span);
                    break;
                case Markdig.Syntax.Inlines.LinkInline link:
                    if (!string.IsNullOrEmpty(link.Url))
                    {
                        var linkBuilder = new StringBuilder();
                        ExtractInlineText(link, theme, linkBuilder);

                        // Check if this is an image link
                        if (link.IsImage)
                        {
                            // Render images with a special icon and styling
                            var imageScopes = MarkdigTextMateScopeMapper.GetInlineScopes("Image");
                            var (ifg, ibg, ifStyle) = TokenProcessor.ExtractThemeProperties(new MarkdownToken(imageScopes), theme);

                            var imageColor = ifg != -1 ? StyleHelper.GetColor(ifg, theme) : Color.Blue;
                            var imageStyle = new Style(imageColor);

                            builder.Append("🖼️ "); // Image emoji
                            builder.AppendWithStyle(imageStyle, "[Image: " + linkBuilder + "]");
                            builder.Append(" (").Append(link.Url).Append(')');
                        }
                        else
                        {
                            builder.AppendLink(link.Url, linkBuilder.ToString());
                        }
                    }
                    else
                    {
                        ExtractInlineText(link, theme, builder);
                    }
                    break;
                case Markdig.Syntax.Inlines.EmphasisInline emph:
                    var emphScopes = MarkdigTextMateScopeMapper.GetInlineScopes("Emphasis", emph.DelimiterCount);
                    var (efg, ebg, efStyle) = TokenProcessor.ExtractThemeProperties(new MarkdownToken(emphScopes), theme);

                    var emphBuilder = new StringBuilder();
                    ExtractInlineText(emph, theme, emphBuilder);

                    // Apply the theme colors/style to the emphasis text
                    if (efg != -1 || ebg != -1 || efStyle != TextMateSharp.Themes.FontStyle.NotSet)
                    {
                        var emphColor = efg != -1 ? StyleHelper.GetColor(efg, theme) : Color.Default;
                        var emphBgColor = ebg != -1 ? StyleHelper.GetColor(ebg, theme) : Color.Default;
                        var emphDecoration = StyleHelper.GetDecoration(efStyle);

                        var emphStyle = new Style(emphColor, emphBgColor, emphDecoration);
                        builder.AppendWithStyle(emphStyle, emphBuilder.ToString());
                    }
                    else
                    {
                        builder.Append(emphBuilder);
                    }
                    break;
                case Markdig.Syntax.Inlines.CodeInline code:
                    var codeScopes = MarkdigTextMateScopeMapper.GetInlineScopes("CodeInline");
                    var (cfg, cbg, cfStyle) = TokenProcessor.ExtractThemeProperties(new MarkdownToken(codeScopes), theme);

                    // Apply the theme colors/style to the inline code
                    if (cfg != -1 || cbg != -1 || cfStyle != TextMateSharp.Themes.FontStyle.NotSet)
                    {
                        var codeColor = cfg != -1 ? StyleHelper.GetColor(cfg, theme) : Color.Default;
                        var codeBgColor = cbg != -1 ? StyleHelper.GetColor(cbg, theme) : Color.Default;
                        var codeDecoration = StyleHelper.GetDecoration(cfStyle);

                        var codeStyle = new Style(codeColor, codeBgColor, codeDecoration);
                        builder.AppendWithStyle(codeStyle, code.Content);
                    }
                    else
                    {
                        builder.Append(code.Content.EscapeMarkup());
                    }
                    break;
                case Markdig.Syntax.Inlines.LineBreakInline:
                    builder.Append('\n');
                    break;
                default:
                    if (inline is Markdig.Syntax.Inlines.ContainerInline childContainer)
                        ExtractInlineText(childContainer, theme, builder);
                    break;
            }
        }
    }
    /// <summary>
    /// Renders markdown content using Markdig and Spectre.Console.
    /// </summary>
    /// <param name="markdown">Markdown text (can be multi-line)</param>
    /// <param name="theme">Theme object for styling</param>
    /// <param name="themeName">Theme name for TextMateProcessor</param>
    /// <returns>Rows object for Spectre.Console rendering</returns>
    public static Rows Render(string markdown, TextMateSharp.Themes.Theme theme, ThemeName themeName)
    {
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UsePipeTables()
            .UseEmphasisExtras()
            .UseAutoLinks()
            .UseTaskLists()
            .EnableTrackTrivia() // Enable HTML support
            .Build();

        var document = Markdig.Markdown.Parse(markdown, pipeline);
        var rows = new List<IRenderable>();
        bool lastWasContent = false;

        for (int i = 0; i < document.Count; i++)
        {
            var block = document[i];
            var renderable = RenderBlock(block, theme, themeName);

            if (renderable is not null)
            {
                // Add spacing before certain block types or when there was previous content
                bool needsSpacing = lastWasContent ||
                                   block is Markdig.Syntax.HeadingBlock ||
                                   block is Markdig.Syntax.FencedCodeBlock ||
                                   block is Markdig.Extensions.Tables.Table ||
                                   block is Markdig.Syntax.QuoteBlock;

                if (needsSpacing && rows.Count > 0)
                {
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

    private static IRenderable? RenderBlock(Markdig.Syntax.Block block, TextMateSharp.Themes.Theme theme, ThemeName themeName)
    {
        switch (block)
        {
          case Markdig.Syntax.HeadingBlock heading:
                var headingScopes = MarkdigTextMateScopeMapper.GetBlockScopes("Heading", heading.Level);
                var (hfg, hbg, hfs) = TokenProcessor.ExtractThemeProperties(new MarkdownToken(headingScopes), theme);

                var headingBuilder = new StringBuilder();
                ExtractInlineText(heading.Inline, theme, headingBuilder);

                // Apply the theme colors/style to the heading
                if (hfg != -1 || hbg != -1 || hfs != TextMateSharp.Themes.FontStyle.NotSet)
                {
                    var headingColor = hfg != -1 ? StyleHelper.GetColor(hfg, theme) : Color.Default;
                    var headingBgColor = hbg != -1 ? StyleHelper.GetColor(hbg, theme) : Color.Default;
                    var headingDecoration = StyleHelper.GetDecoration(hfs);

                    var headingStyle = new Style(headingColor, headingBgColor, headingDecoration);
                    var styledBuilder = new StringBuilder();
                    styledBuilder.AppendWithStyle(headingStyle, headingBuilder.ToString());
                    return new Markup(styledBuilder.ToString());
                }
                else
                {
                    return new Markup(headingBuilder.ToString().EscapeMarkup());
                }
          case Markdig.Syntax.ParagraphBlock para:
                var paraScopes = MarkdigTextMateScopeMapper.GetBlockScopes("Paragraph");
                var (pfg, pbg, pfs) = TokenProcessor.ExtractThemeProperties(new MarkdownToken(paraScopes), theme);

                var paraBuilder = new StringBuilder();
                ExtractInlineText(para.Inline, theme, paraBuilder);

                if (paraBuilder.Length == 0 || string.IsNullOrWhiteSpace(paraBuilder.ToString()))
                    return Text.Empty;

                // Apply the theme colors/style to the paragraph
                if (pfg != -1 || pbg != -1 || pfs != TextMateSharp.Themes.FontStyle.NotSet)
                {
                    var paraColor = pfg != -1 ? StyleHelper.GetColor(pfg, theme) : Color.Default;
                    var paraBgColor = pbg != -1 ? StyleHelper.GetColor(pbg, theme) : Color.Default;
                    var paraDecoration = StyleHelper.GetDecoration(pfs);

                    var paraStyle = new Style(paraColor, paraBgColor, paraDecoration);
                    var styledBuilder = new StringBuilder();
                    styledBuilder.AppendWithStyle(paraStyle, paraBuilder.ToString());
                    return new Markup(styledBuilder.ToString());
                }
                else
                {
                    return new Markup(paraBuilder.ToString());
                }          case Markdig.Syntax.ListBlock list:
                var items = new List<string>();
                int number = 1;
                foreach (Markdig.Syntax.ListItemBlock item in list)
                {
                    string itemText = string.Empty;
                    string prefix = "";

                    // Extract all text from the list item
                    foreach (var subBlock in item)
                    {
                        if (subBlock is Markdig.Syntax.ParagraphBlock subPara)
                        {
                            var itemBuilder = new StringBuilder();
                            ExtractInlineText(subPara.Inline, theme, itemBuilder);
                            itemText += itemBuilder.ToString();
                        }
                        else if (subBlock is Markdig.Syntax.CodeBlock subCode)
                            itemText += subCode.Lines.ToString();
                    }

                    // Check if this is a task list item by looking at the text content
                    var trimmedText = itemText.Trim();
                    if (trimmedText.StartsWith("[x] ", StringComparison.Ordinal) || trimmedText.StartsWith("[X] ", StringComparison.Ordinal))
                    {
                        // Checked task
                        prefix = "☑️ ";
                        itemText = trimmedText.Substring(4); // Remove "[x] "
                    }
                    else if (trimmedText.StartsWith("[ ] ", StringComparison.Ordinal))
                    {
                        // Unchecked task
                        prefix = "☐ ";
                        itemText = trimmedText.Substring(4); // Remove "[ ] "
                    }
                    else
                    {
                        // Regular list item
                        prefix = list.IsOrdered ? $"{number}. " : "• ";
                        if (list.IsOrdered) number++;
                    }

                    items.Add(prefix + Markup.Escape(itemText.Trim()));
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
                        var rows = TextMateProcessor.ProcessLinesCodeBlock(codeLines.ToArray(), themeName, lang, false);
                        if (rows is not null)
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
                var fallbackStyle = theme.ToSpectreStyle();
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
                            {
                                var cellBuilder = new StringBuilder();
                                ExtractInlineText(para.Inline, theme, cellBuilder);
                                cellText += cellBuilder.ToString();
                            }
                            else
                                cellText += cellBlock.ToString();
                        }
                        cells.Add(cellText);
                    }
                    if (row.IsHeader && headerCells is null)
                        headerCells = new List<string>(cells);
                    allRows.Add((row.IsHeader, cells));
                }
                int colCount = headerCells?.Count ?? 0;
                foreach (var (isHeader, cells) in allRows)
                {
                    if (isHeader)
                    {
                        if (spectreTable is not null && headerAdded)
                        {
                            tables.Add(spectreTable);
                            spectreTable = null;
                            headerAdded = false;
                        }
                        if (spectreTable is null)
                            spectreTable = new Table();
                        for (int i = 0; i < colCount; i++)
                            spectreTable.AddColumn(Markup.Escape(cells[i]));
                        headerAdded = true;
                        continue;
                    }
                    if (spectreTable is null)
                        continue;
                    var rowCells = new List<IRenderable>();
                    for (int i = 0; i < colCount; i++)
                        rowCells.Add(new Markup(Markup.Escape(i < cells.Count ? cells[i] : "")));
                    spectreTable.AddRow(rowCells);
                }
                if (spectreTable is not null && headerAdded)
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
                    {
                        var quoteBuilder = new StringBuilder();
                        ExtractInlineText(para.Inline, theme, quoteBuilder);
                        quoteText += quoteBuilder.ToString();
                    }
                    else
                        quoteText += subBlock.ToString();
                }
                return new Panel(new Markup(Markup.Escape(quoteText))).Border(BoxBorder.Double).Header("quote", Justify.Left);
            case Markdig.Syntax.HtmlBlock htmlBlock:
                // Render HTML blocks as code with HTML syntax highlighting
                var htmlLines = new List<string>();
                for (int i = 0; i < htmlBlock.Lines.Count; i++)
                {
                    var line = htmlBlock.Lines.Lines[i];
                    htmlLines.Add(line.Slice.ToString());
                }

                try
                {
                    var htmlRows = TextMateProcessor.ProcessLinesCodeBlock(htmlLines.ToArray(), themeName, "html", false);
                    if (htmlRows is not null)
                    {
                        return new Panel(htmlRows).Border(BoxBorder.Rounded).Header("html", Justify.Left);
                    }
                }
                catch
                {
                    // Fallback to plain rendering
                }

                var htmlText = Markup.Escape(string.Join("\n", htmlLines));
                return new Panel(new Markup(htmlText)).Border(BoxBorder.Rounded).Header("html", Justify.Left);
            case Markdig.Syntax.ThematicBreakBlock:
                // Render horizontal rules as a decorative line
                return new Rule().RuleStyle(Style.Parse("grey"));
            default:
                return null;
        }
    }
}

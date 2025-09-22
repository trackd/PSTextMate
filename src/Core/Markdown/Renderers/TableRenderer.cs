using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Spectre.Console;
using Spectre.Console.Rendering;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Table renderer that builds Spectre.Console objects directly instead of markup strings.
/// This eliminates VT escaping issues and provides proper color support.
/// </summary>
internal static class TableRenderer
{
    /// <summary>
    /// Renders a markdown table by building Spectre.Console Table objects directly.
    /// This approach provides proper theme color support and eliminates VT escaping issues.
    /// </summary>
    /// <param name="table">The table block to render</param>
    /// <param name="theme">Theme for styling</param>
    /// <returns>Rendered table with proper styling</returns>
    public static IRenderable? Render(Markdig.Extensions.Tables.Table table, Theme theme)
    {
        var spectreTable = new Spectre.Console.Table();
        spectreTable.ShowFooters = false;

        // Configure table appearance
        spectreTable.Border = TableBorder.Rounded;
        spectreTable.BorderStyle = GetTableBorderStyle(theme);

        List<(bool isHeader, List<TableCellContent> cells)> allRows = ExtractTableDataOptimized(table, theme);

        if (allRows.Count == 0)
            return null;

        // Add headers if present
        (bool isHeader, List<TableCellContent> cells) headerRow = allRows.FirstOrDefault(r => r.isHeader);
        if (headerRow.cells?.Count > 0)
        {
            for (int i = 0; i < headerRow.cells.Count; i++)
            {
                TableCellContent cell = headerRow.cells[i];
                // Use constructor to set header text; this is the most compatible way
                var column = new TableColumn(cell.Text);
                // Apply alignment if Markdig specified one for the column
                if (i < table.ColumnDefinitions.Count)
                {
                    column.Alignment = table.ColumnDefinitions[i].Alignment switch
                    {
                        TableColumnAlign.Left => Justify.Left,
                        TableColumnAlign.Center => Justify.Center,
                        TableColumnAlign.Right => Justify.Right,
                        _ => Justify.Left
                    };
                }
                spectreTable.AddColumn(column);
            }
        }
        else
        {
            // No explicit headers, use first row as headers
            (bool isHeader, List<TableCellContent> cells) firstRow = allRows.FirstOrDefault();
            if (firstRow.cells?.Count > 0)
            {
                for (int i = 0; i < firstRow.cells.Count; i++)
                {
                    TableCellContent cell = firstRow.cells[i];
                    var column = new TableColumn(cell.Text);
                    if (i < table.ColumnDefinitions.Count)
                    {
                        column.Alignment = table.ColumnDefinitions[i].Alignment switch
                        {
                            TableColumnAlign.Left => Justify.Left,
                            TableColumnAlign.Center => Justify.Center,
                            TableColumnAlign.Right => Justify.Right,
                            _ => Justify.Left
                        };
                    }
                    spectreTable.AddColumn(column);
                }
                allRows = allRows.Skip(1).ToList();
            }
        }

        // Add data rows
        foreach ((bool isHeader, List<TableCellContent>? cells) in allRows.Where(r => !r.isHeader))
        {
            if (cells?.Count > 0)
            {
                var rowCells = new List<IRenderable>();
                foreach (TableCellContent? cell in cells)
                {
                    Style cellStyle = GetCellStyle(theme);
                    rowCells.Add(new Text(cell.Text, cellStyle));
                }
                spectreTable.AddRow(rowCells.ToArray());
            }
        }

        return spectreTable;
    }

    /// <summary>
    /// Represents the content and styling of a table cell.
    /// </summary>
    private sealed record TableCellContent(string Text, TableColumnAlign? Alignment);

    /// <summary>
    /// Extracts table data with optimized cell content processing.
    /// </summary>
    private static List<(bool isHeader, List<TableCellContent> cells)> ExtractTableDataOptimized(
        Markdig.Extensions.Tables.Table table, Theme theme)
    {
        var result = new List<(bool isHeader, List<TableCellContent> cells)>();

        foreach (Markdig.Extensions.Tables.TableRow row in table)
        {
            bool isHeader = row.IsHeader;
            var cells = new List<TableCellContent>();

            for (int i = 0; i < row.Count; i++)
            {
                if (row[i] is TableCell cell)
                {
                    string cellText = ExtractCellTextOptimized(cell, theme);
                    TableColumnAlign? alignment = i < table.ColumnDefinitions.Count ? table.ColumnDefinitions[i].Alignment : null;
                    cells.Add(new TableCellContent(cellText, alignment));
                }
            }

            result.Add((isHeader, cells));
        }

        return result;
    }

    /// <summary>
    /// Extracts text from table cells using optimized inline processing.
    /// </summary>
    private static string ExtractCellTextOptimized(TableCell cell, Theme theme)
    {
        var textBuilder = new System.Text.StringBuilder();

        foreach (Block block in cell)
        {
            if (block is ParagraphBlock paragraph && paragraph.Inline is not null)
            {
                ExtractInlineTextOptimized(paragraph.Inline, textBuilder);
            }
            else if (block is Markdig.Syntax.CodeBlock code)
            {
                textBuilder.Append(code.Lines.ToString());
            }
        }

        return textBuilder.ToString().Trim();
    }

    /// <summary>
    /// Extracts text from inline elements optimized for table cells.
    /// </summary>
    private static void ExtractInlineTextOptimized(ContainerInline inlines, System.Text.StringBuilder builder)
    {
        foreach (Inline inline in inlines)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    builder.Append(literal.Content.ToString());
                    break;

                case EmphasisInline emphasis:
                    // For tables, we extract just the text content
                    ExtractInlineTextRecursive(emphasis, builder);
                    break;

                case Markdig.Syntax.Inlines.CodeInline code:
                    builder.Append(code.Content);
                    break;

                case Markdig.Syntax.Inlines.LinkInline link:
                    ExtractInlineTextRecursive(link, builder);
                    break;

                default:
                    ExtractInlineTextRecursive(inline, builder);
                    break;
            }
        }
    }

    /// <summary>
    /// Recursively extracts text from inline elements.
    /// </summary>
    private static void ExtractInlineTextRecursive(Inline inline, System.Text.StringBuilder builder)
    {
        switch (inline)
        {
            case LiteralInline literal:
                builder.Append(literal.Content.ToString());
                break;

            case ContainerInline container:
                foreach (Inline child in container)
                {
                    ExtractInlineTextRecursive(child, builder);
                }
                break;

            case Markdig.Syntax.Inlines.LeafInline leaf:
                if (leaf is Markdig.Syntax.Inlines.CodeInline code)
                {
                    builder.Append(code.Content);
                }
                break;
        }
    }

    /// <summary>
    /// Gets the border style for tables based on theme.
    /// </summary>
    private static Style GetTableBorderStyle(Theme theme)
    {
        // Get theme colors for table borders
        string[] borderScopes = new[] { "punctuation.definition.table" };
        (int borderFg, int borderBg, FontStyle borderFs) = TokenProcessor.ExtractThemeProperties(
            new MarkdownToken(borderScopes), theme);

        if (borderFg != -1)
        {
            return new Style(foreground: StyleHelper.GetColor(borderFg, theme));
        }

        return new Style(foreground: Color.Grey);
    }

    /// <summary>
    /// Gets the header style for table headers.
    /// </summary>
    private static Style GetHeaderStyle(Theme theme)
    {
        // Get theme colors for table headers
        string[] headerScopes = new[] { "markup.heading.table" };
        (int headerFg, int headerBg, FontStyle headerFs) = TokenProcessor.ExtractThemeProperties(
            new MarkdownToken(headerScopes), theme);

        Color? foregroundColor = headerFg != -1 ? StyleHelper.GetColor(headerFg, theme) : Color.Yellow;
        Color? backgroundColor = headerBg != -1 ? StyleHelper.GetColor(headerBg, theme) : null;
        Decoration decoration = StyleHelper.GetDecoration(headerFs) | Decoration.Bold;

        return new Style(foregroundColor, backgroundColor, decoration);
    }

    /// <summary>
    /// Gets the cell style for table data cells.
    /// </summary>
    private static Style GetCellStyle(Theme theme)
    {
        // Get theme colors for table cells
        string[] cellScopes = new[] { "markup.table.cell" };
        (int cellFg, int cellBg, FontStyle cellFs) = TokenProcessor.ExtractThemeProperties(
            new MarkdownToken(cellScopes), theme);

        Color? foregroundColor = cellFg != -1 ? StyleHelper.GetColor(cellFg, theme) : Color.White;
        Color? backgroundColor = cellBg != -1 ? StyleHelper.GetColor(cellBg, theme) : null;
        Decoration decoration = StyleHelper.GetDecoration(cellFs);

        return new Style(foregroundColor, backgroundColor, decoration);
    }
}

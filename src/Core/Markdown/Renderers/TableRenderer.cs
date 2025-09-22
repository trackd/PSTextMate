using System.Text;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Spectre.Console;
using Spectre.Console.Rendering;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Renders markdown table blocks.
/// </summary>
internal static class TableRenderer
{
    /// <summary>
    /// Renders a markdown table with proper headers and rows.
    /// </summary>
    /// <param name="table">The table block to render</param>
    /// <param name="theme">Theme for styling</param>
    /// <returns>Rendered table or collection of tables</returns>
    public static IRenderable? Render(Markdig.Extensions.Tables.Table table, Theme theme)
    {
        var tables = new List<Spectre.Console.Table>();
        Spectre.Console.Table? spectreTable = null;
        List<string>? headerCells = null;
        bool headerAdded = false;

        List<(bool isHeader, List<string> cells)> allRows = ExtractTableData(table, theme);
        int colCount = headerCells?.Count ?? 0;

        foreach ((bool isHeader, List<string> cells) in allRows)
        {
            if (isHeader)
            {
                (spectreTable, headerAdded) = ProcessHeaderRow(cells, tables, spectreTable, headerAdded, ref colCount);
                headerCells ??= new List<string>(cells);
            }
            else
            {
                ProcessDataRow(cells, spectreTable, colCount);
            }
        }

        if (spectreTable is not null && headerAdded)
        {
            tables.Add(spectreTable);
        }

        return tables.Count switch
        {
            1 => tables[0],
            > 1 => new Rows(tables.ToArray()),
            _ => null
        };
    }

    /// <summary>
    /// Extracts all table data including headers and rows.
    /// </summary>
    private static List<(bool isHeader, List<string> cells)> ExtractTableData(Markdig.Extensions.Tables.Table table, Theme theme)
    {
        var allRows = new List<(bool isHeader, List<string> cells)>();

        foreach (Markdig.Extensions.Tables.TableRow row in table)
        {
            var cells = new List<string>();

            foreach (TableCell cell in row.Cast<TableCell>())
            {
                string cellText = ExtractCellText(cell, theme);
                cells.Add(cellText);
            }

            allRows.Add((row.IsHeader, cells));
        }

        return allRows;
    }

    /// <summary>
    /// Extracts text content from a table cell.
    /// </summary>
    private static string ExtractCellText(TableCell cell, Theme theme)
    {
        string cellText = string.Empty;

        foreach (Block cellBlock in cell)
        {
            if (cellBlock is ParagraphBlock para)
            {
                var cellBuilder = new StringBuilder();
                InlineProcessor.ExtractInlineText(para.Inline, theme, cellBuilder);
                cellText += cellBuilder.ToString();
            }
            else
            {
                cellText += cellBlock.ToString();
            }
        }

        return cellText;
    }

    /// <summary>
    /// Processes a header row in the table.
    /// </summary>
    private static (Spectre.Console.Table? table, bool headerAdded) ProcessHeaderRow(
        List<string> cells,
        List<Spectre.Console.Table> tables,
        Spectre.Console.Table? currentTable,
        bool headerAdded,
        ref int colCount)
    {
        if (currentTable is not null && headerAdded)
        {
            tables.Add(currentTable);
            currentTable = null;
            headerAdded = false;
        }

        currentTable ??= new Spectre.Console.Table();
        colCount = cells.Count;

        for (int i = 0; i < colCount; i++)
        {
            currentTable.AddColumn(Markup.Escape(cells[i]));
        }

        return (currentTable, true);
    }

    /// <summary>
    /// Processes a data row in the table.
    /// </summary>
    private static void ProcessDataRow(List<string> cells, Spectre.Console.Table? spectreTable, int colCount)
    {
        if (spectreTable is null) return;

        var rowCells = new List<IRenderable>();

        for (int i = 0; i < colCount; i++)
        {
            string cellContent = i < cells.Count ? cells[i] : "";
            rowCells.Add(new Markup(Markup.Escape(cellContent)));
        }

        spectreTable.AddRow(rowCells);
    }
}

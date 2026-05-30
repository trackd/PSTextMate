using PSTextMate.Terminal;
using PSTextMate.Utilities;
using PSTextMate.Core;
using Spectre.Console;
using Spectre.Console.Rendering;
using Spectre.Console.Testing;
using Xunit;

namespace PSTextMate.InteractiveTests;

public sealed class SpectreLiveTestingTests {
    [Fact]
    public void LiveDisplay_Start_ReturnsScriptBlockResult() {
        var console = new TestConsole();

        var table = new Table();
        table.AddColumn("Name");
        table.AddColumn("Value");
        table.AddRow("Test", "Value");

        int result = console.Live(table)
            .AutoClear(true)
            .Start(_ => 1);

        Assert.Equal(1, result);
    }

    [Fact]
    public void LiveDisplay_CanUpdateTargetDuringExecution() {
        var console = new TestConsole();

        _ = console.Live(new Markup("start"))
            .AutoClear(true)
            .Start(ctx => {
                ctx.UpdateTarget(new Markup("end"));
                ctx.Refresh();
                return 0;
            });

        Assert.Contains("end", console.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Pager_Show_WithTestConsoleAndQuitKey_ExitsAndRendersContent() {
        var console = new TestConsole();
        var keys = new Queue<ConsoleKeyInfo>([
            new ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false)
        ]);

        Markup[] renderables = [
            new Markup("alpha"),
            new Markup("beta")
        ];

        var pager = new Pager(
            renderables,
            console,
            () => keys.Count > 0 ? keys.Dequeue() : null,
            suppressTerminalControlSequences: true
        );
        pager.Show();

        Assert.Contains("alpha", console.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Pager_RenderableLineSlice_ExposesLaterLinesForTallPanel() {
        var panelRows = new Rows(
            Enumerable.Range(1, 60).Select(index => (IRenderable)new Text($"line {index:00}"))
        );

        IRenderable slice = new Pager.RenderableLineSlice(panelRows, startLine: 50, lineCount: 10);

        string rendered = Writer.WriteToString(slice, width: 80);

        Assert.Contains("line 60", rendered, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("line 01", rendered, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TokenProcessor_NormalizeTabsForRendering_ReplacesTabsWithSpaces() {
        string normalized = TokenProcessor.NormalizeTabsForRendering("\tparam(\t[string]$Name)");

        Assert.DoesNotContain("\t", normalized, StringComparison.Ordinal);
        Assert.Contains("    param(    [string]$Name)", normalized, StringComparison.Ordinal);
    }

    [Fact]
    public void Pager_Show_WithReportedSample_DoesNotDuplicateFooterInTestConsole() {
        var lines = Enumerable.Range(1, 27)
            .Select(index => $"line {index:00}")
            .ToList();

        var renderables = lines
            .Select(static line => (IRenderable)(line.Length == 0 ? Text.Empty : VTConversion.ToParagraph(line)))
            .ToList();

        var console = new TestConsole();
        console.Profile.Width = 111;

        var keys = new Queue<ConsoleKeyInfo>([
            new ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false)
        ]);

        var pager = new Pager(
            renderables,
            console,
            () => keys.Count > 0 ? keys.Dequeue() : null,
            suppressTerminalControlSequences: true
        );

        pager.Show();

        int helpCount = CountOccurrences(console.Output, "Press ? for help");
        int statusCount = CountOccurrences(console.Output, "1-27/27");

        Assert.True(helpCount == 1 && statusCount == 1,
            $"HelpCount={helpCount}; StatusCount={statusCount}{Environment.NewLine}{console.Output}");
    }

    private static int CountOccurrences(string value, string needle) {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(needle)) {
            return 0;
        }

        int count = 0;
        int index = 0;
        while ((index = value.IndexOf(needle, index, StringComparison.Ordinal)) >= 0) {
            count++;
            index += needle.Length;
        }

        return count;
    }
}

using PSTextMate.Terminal;
using PSTextMate.Utilities;
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
    public void Pager_Show_WithReportedSample_DoesNotDuplicateFooterInTestConsole() {
        string samplePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "testoutput.txt"));
        string sample = File.ReadAllText(samplePath);
        var lines = new List<string>();
        TextMateHelper.AddSplitLines(lines, sample, trimTrailingTerminatorEmptyLine: true);

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

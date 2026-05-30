namespace PSTextMate.Commands;

/// <summary>
/// Sends renderables or VT-formatted strings to the interactive pager.
/// </summary>
[Cmdlet(VerbsData.Out, "Page")]
[Alias("page")]
[OutputType(typeof(void))]
public sealed class OutPageCmdlet : PSCmdlet {
    private readonly List<IRenderable> _renderables = [];
    private readonly List<string?> _renderableSourceLines = [];
    private readonly List<object> _outStringInputs = [];
    private HighlightedText? _singleHighlightedText;
    private bool _sawNonHighlightedInput;

    [Parameter(
        ValueFromRemainingArguments = true,
        DontShow = true,
        Position = 0
    )]
    [System.Management.Automation.AllowNull]
    public PSObject[]? EaterOfArgs { get; set; }

    [Parameter(
        Mandatory = true,
        ValueFromPipeline = true,
        DontShow = true
    )]
    [System.Management.Automation.AllowNull]
    public PSObject? InputObject { get; set; }

    protected override void ProcessRecord() {
        if (InputObject?.BaseObject is null) {
            return;
        }

        object value = InputObject.BaseObject;

        if (value is HighlightedText highlightedText) {
            if (_singleHighlightedText is null && !_sawNonHighlightedInput && _renderables.Count == 0 && _outStringInputs.Count == 0) {
                _singleHighlightedText = highlightedText;
                return;
            }

            PromoteBufferedHighlightedText();
            FlushPendingOutStringInputs();
            _sawNonHighlightedInput = true;
            AddHighlightedText(highlightedText);
            return;
        }

        PromoteBufferedHighlightedText();
        _sawNonHighlightedInput = true;

        if (value is IRenderable renderable) {
            FlushPendingOutStringInputs();
            _renderables.Add(renderable);
            _renderableSourceLines.Add(null);
            return;
        }

        if (value is string text) {
            FlushPendingOutStringInputs();
            AddTextInput(text);
            return;
        }

        if (TryConvertForeignSpectreRenderable(value, out IRenderable? convertedRenderable)) {
            FlushPendingOutStringInputs();
            _renderables.Add(convertedRenderable);
            _renderableSourceLines.Add(null);
            return;
        }

        _outStringInputs.Add(InputObject);
    }

    protected override void EndProcessing() {
        if (_singleHighlightedText is not null && !_sawNonHighlightedInput && _renderables.Count == 0 && _outStringInputs.Count == 0) {
            var highlightedPager = new Pager(_singleHighlightedText);
            highlightedPager.Show();
            return;
        }

        FlushPendingOutStringInputs();

        if (_renderables.Count == 0) {
            return;
        }

        var pager = new Pager(_renderables, _renderableSourceLines, AnsiConsole.Console, null, suppressTerminalControlSequences: false);
        pager.Show();
    }

    private void PromoteBufferedHighlightedText() {
        if (_singleHighlightedText is null) {
            return;
        }

        AddHighlightedText(_singleHighlightedText);
        _singleHighlightedText = null;
    }

    private void AddHighlightedText(HighlightedText highlightedText) {
        _renderables.AddRange(highlightedText.Renderables);

        IReadOnlyList<string>? sourceLines = highlightedText.SourceLines;
        if (sourceLines is not null && sourceLines.Count == highlightedText.Renderables.Length) {
            for (int i = 0; i < sourceLines.Count; i++) {
                _renderableSourceLines.Add(sourceLines[i]);
            }

            return;
        }

        AddSourceLinePlaceholders(highlightedText.Renderables.Length);
    }

    private void AddSourceLinePlaceholders(int count) {
        for (int i = 0; i < count; i++) {
            _renderableSourceLines.Add(null);
        }
    }

    private void AddTextInput(string text) {
        var lines = new List<string>(Math.Min(16, (text.Length / 8) + 1));
        TextMateHelper.AddSplitLines(lines, text, trimTrailingTerminatorEmptyLine: true);

        foreach (string line in lines) {
            _renderables.Add(line.Length == 0 ? Text.Empty : VTConversion.ToParagraph(line));
            _renderableSourceLines.Add(line);
        }
    }

    private void FlushPendingOutStringInputs() {
        if (_outStringInputs.Count == 0) {
            return;
        }

        List<string> formattedLines = ConvertWithOutStringLines(_outStringInputs);
        if (formattedLines.Count > 0) {
            foreach (string line in formattedLines) {
                _renderables.Add(line.Length == 0 ? Text.Empty : VTConversion.ToParagraph(line));
                _renderableSourceLines.Add(line);
            }
        }
        else {
            foreach (object value in _outStringInputs) {
                string converted = LanguagePrimitives.ConvertTo<string>(value);
                _renderables.Add(new Text(converted));
                _renderableSourceLines.Add(converted);
            }
        }

        _outStringInputs.Clear();
    }

    private static List<string> ConvertWithOutStringLines(List<object> values) {
        if (values.Count == 0) {
            return [];
        }

        OutputRendering previousOutputRendering = PSStyle.Instance.OutputRendering;
        try {
            PSStyle.Instance.OutputRendering = OutputRendering.Ansi;

            using var ps = PowerShell.Create(RunspaceMode.CurrentRunspace);
            ps.AddCommand("Out-String")
            .AddParameter("Stream")
            .AddParameter("Width", GetOutStringWidth());

            Collection<PSObject> results = ps.Invoke(values);
            if (ps.HadErrors || results.Count == 0) {
                return [];
            }

            var lines = new List<string>(results.Count);
            foreach (PSObject? result in results) {
                if (result?.BaseObject is string text) {
                    AddLines(lines, text);
                }
                else {
                    AddLines(lines, result?.ToString() ?? string.Empty);
                }
            }

            return lines;
        }
        catch {
            return [];
        }
        finally {
            PSStyle.Instance.OutputRendering = previousOutputRendering;
        }
    }

    // Out-String -Stream commonly returns one chunk per logical line with a
    // trailing newline terminator. Trim only that final synthetic empty line.
    private static void AddLines(List<string> lines, string text) =>

        TextMateHelper.AddSplitLines(lines, text, trimTrailingTerminatorEmptyLine: true);

    private static int GetConsoleWidth() {
        try {
            return Console.WindowWidth > 0 ? Console.WindowWidth : 120;
        }
        catch {
            return 120;
        }
    }

    private static int GetOutStringWidth() => Math.Max(20, GetConsoleWidth() - 5);

    private static bool TryConvertForeignSpectreRenderable(
        object value,
        [NotNullWhen(true)] out IRenderable? renderable
    ) {
        renderable = null;

        Type valueType = value.GetType();
        string? fullName = valueType.FullName;
        return IsSpectreObject(fullName)
            && value is not IRenderable
            && SpectreRenderBridge.TryConvertToLocalRenderable(value, out renderable);
    }
    private static bool IsSpectreObject(string? str) {
        return !string.IsNullOrWhiteSpace(str)
            && (str.StartsWith("Spectre.Console.", StringComparison.Ordinal) ||
            str.StartsWith("PwshSpectreConsole.", StringComparison.Ordinal));
    }
}

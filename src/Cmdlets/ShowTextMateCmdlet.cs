using System.Management.Automation;
using PwshSpectreConsole.TextMate.Core;
using PwshSpectreConsole.TextMate.Extensions;
using Spectre.Console.Rendering;
using TextMateSharp.Grammars;

namespace PwshSpectreConsole.TextMate.Cmdlets;

/// <summary>
/// Cmdlet for displaying syntax-highlighted text using TextMate grammars.
/// Supports both string input and file processing with theme customization.
/// </summary>
[Cmdlet(VerbsCommon.Show, "TextMate", DefaultParameterSetName = "String")]
[Alias("st", "Show-Code")]
[OutputType(typeof(HighlightedText))]
public sealed class ShowTextMateCmdlet : PSCmdlet {
    private readonly List<string> _inputObjectBuffer = [];
    private string? _sourceExtensionHint;
    private string? _sourceBaseDirectory;

    /// <summary>
    /// String content to render with syntax highlighting.
    /// </summary>
    [Parameter(
        Mandatory = true,
        ValueFromPipeline = true,
        ParameterSetName = "String"
    )]
    [AllowEmptyString]
    [AllowNull]
    public string? InputObject { get; set; }

    /// <summary>
    /// Path to file to render with syntax highlighting.
    /// </summary>
    [Parameter(
        Mandatory = true,
        ValueFromPipelineByPropertyName = true,
        ParameterSetName = "Path",
        Position = 0
    )]
    [ValidateNotNullOrEmpty]
    [Alias("FullName")]
    public string? Path { get; set; }

    /// <summary>
    /// TextMate language ID for syntax highlighting (e.g., 'powershell', 'csharp', 'python').
    /// If not specified, detected from file extension (for files) or defaults to 'powershell' (for strings).
    /// </summary>
    [Parameter]
    [ArgumentCompleter(typeof(LanguageCompleter))]
    public string? Language { get; set; }

    /// <summary>
    /// Color theme to use for syntax highlighting.
    /// </summary>
    [Parameter]
    public ThemeName Theme { get; set; } = ThemeName.DarkPlus;

    /// <summary>
    /// Enables streaming mode for large files, processing in batches.
    /// </summary>
    [Parameter(ParameterSetName = "Path")]
    public SwitchParameter Stream { get; set; }

    /// <summary>
    /// Number of lines to process per batch when streaming (default: 1000).
    /// </summary>
    [Parameter(ParameterSetName = "Path")]
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Processes each input record from the pipeline.
    /// </summary>
    protected override void ProcessRecord() {
        WriteVerbose($"ParameterSet: {ParameterSetName}");

        if (ParameterSetName == "String" && InputObject is not null) {
            // Extract extension hint and base directory from PSPath if available
            if (_sourceExtensionHint is null || _sourceBaseDirectory is null) {
                GetSourceHint();
            }

            // Buffer the input string for later processing
            _inputObjectBuffer.Add(InputObject);
            return;
        }

        if (ParameterSetName == "Path" && Path is not null) {
            try {
                // Process file immediately when in Path parameter set
                foreach (HighlightedText result in ProcessPathInput()) {
                    // Output each renderable directly so pwshspectreconsole can format them
                    WriteObject(result.Renderables, enumerateCollection: true);
                }
            }
            catch (Exception ex) {
                WriteError(new ErrorRecord(ex, "ShowTextMateCmdlet", ErrorCategory.NotSpecified, Path));
            }
        }
    }

    /// <summary>
    /// Finalizes processing after all pipeline records have been processed.
    /// </summary>
    protected override void EndProcessing() {
        // Only process buffered strings in EndProcessing
        if (ParameterSetName != "String") {
            return;
        }

        try {
            if (_sourceExtensionHint is null || _sourceBaseDirectory is null) {
                GetSourceHint();
            }
            HighlightedText? result = ProcessStringInput();
            if (result is not null) {
                // Output each renderable directly so pwshspectreconsole can format them
                WriteObject(result.Renderables, enumerateCollection: true);
            }

        }
        catch (Exception ex) {
            WriteError(new ErrorRecord(ex, "ShowTextMateCmdlet", ErrorCategory.NotSpecified, MyInvocation.BoundParameters));
        }
    }

    private HighlightedText? ProcessStringInput() {
        if (_inputObjectBuffer.Count == 0) {
            WriteVerbose("No input provided");
            return null;
        }

        // Normalize buffered strings into lines
        string[] lines = NormalizeToLines(_inputObjectBuffer);

        if (lines.AllIsNullOrEmpty()) {
            WriteVerbose("All input strings are null or empty");
            return null;
        }

        // Resolve language (explicit parameter, pipeline extension hint, or default)
        string effectiveLanguage = !string.IsNullOrEmpty(Language) ? Language :
            !string.IsNullOrEmpty(_sourceExtensionHint) ? _sourceExtensionHint :
            "powershell";

        WriteVerbose($"effectiveLanguage: {effectiveLanguage}");

        (string? token, bool asExtension) = TextMateResolver.ResolveToken(effectiveLanguage);

        // Process and wrap in HighlightedText
        IRenderable[]? renderables = TextMateProcessor.ProcessLines(lines, Theme, token, isExtension: asExtension);

        return renderables is null
            ? null
            : new HighlightedText {
                Renderables = renderables
            };
    }

    private IEnumerable<HighlightedText> ProcessPathInput() {
        FileInfo filePath = new(GetUnresolvedProviderPathFromPSPath(Path));

        if (!filePath.Exists) {
            throw new FileNotFoundException($"File not found: {filePath.FullName}", filePath.FullName);
        }

        // Set the base directory for relative image path resolution in markdown
        // Use the full directory path or current directory if not available
        string markdownBaseDir = filePath.DirectoryName ?? Environment.CurrentDirectory;
        Core.Markdown.Renderers.ImageRenderer.CurrentMarkdownDirectory = markdownBaseDir;
        WriteVerbose($"Set markdown base directory for image resolution: {markdownBaseDir}");

        // Resolve language: explicit parameter > file extension
        (string token, bool asExtension) = !string.IsNullOrWhiteSpace(Language)
            ? TextMateResolver.ResolveToken(Language)
            : (filePath.Extension, true);

        if (Stream.IsPresent) {
            // Streaming mode - yield HighlightedText objects directly from processor
            WriteVerbose($"Streaming file: {filePath.FullName} with {(asExtension ? "extension" : "language")}: {token}, batch size: {BatchSize}");

            // Direct passthrough - processor returns HighlightedText now
            foreach (HighlightedText result in TextMateProcessor.ProcessFileInBatches(filePath.FullName, BatchSize, Theme, token, asExtension)) {
                yield return result;
            }
        }
        else {
            // Single file processing
            WriteVerbose($"Processing file: {filePath.FullName} with {(asExtension ? "extension" : "language")}: {token}");

            string[] lines = File.ReadAllLines(filePath.FullName);
            IRenderable[]? renderables = TextMateProcessor.ProcessLines(lines, Theme, token, isExtension: asExtension);

            if (renderables is not null) {
                yield return new HighlightedText {
                    Renderables = renderables
                };
            }
        }
    }

    private static string[] NormalizeToLines(List<string> buffer) {
        if (buffer.Count == 0) {
            return [];
        }

        // Multiple strings in buffer - treat each as a line
        if (buffer.Count > 1) {
            return [.. buffer];
        }

        // Single string - check if it contains newlines
        string single = buffer[0];
        if (string.IsNullOrEmpty(single)) {
            return [single];
        }

        // Split on newlines if present
        if (single.Contains('\n') || single.Contains('\r')) {
            return single.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
        }

        // Single string with no newlines
        return [single];
    }
    private void GetSourceHint() {
        if (GetVariableValue("_") is not PSObject current) {
            WriteVerbose("GetVariableValue failed to cast '_' to psobject");
            return;
        }

        string? hint = current.Properties["PSPath"]?.Value as string
                        ?? current.Properties["FullName"]?.Value as string;
        if (string.IsNullOrEmpty(hint)) {
            WriteVerbose($"hint empty?, {current}");
            return;
        }
        if (_sourceExtensionHint is null) {
            string ext = System.IO.Path.GetExtension(hint);
            if (!string.IsNullOrWhiteSpace(ext)) {
                _sourceExtensionHint = ext;
                WriteVerbose($"Detected extension hint from PSPath: {ext}");
            }
        }

        if (_sourceBaseDirectory is null) {
            string? baseDir = System.IO.Path.GetDirectoryName(hint);
            if (!string.IsNullOrWhiteSpace(baseDir)) {
                _sourceBaseDirectory = baseDir;
                Core.Markdown.Renderers.ImageRenderer.CurrentMarkdownDirectory = baseDir;
                WriteVerbose($"Set markdown base directory from PSPath: {baseDir}");
            }
        }
    }
}

using System.Management.Automation;
using TextMateSharp.Grammars;
using PwshSpectreConsole.TextMate.Extensions;
using Spectre.Console;
using PwshSpectreConsole.TextMate;

namespace PwshSpectreConsole.TextMate.Cmdlets;

/// <summary>
/// Cmdlet for displaying syntax-highlighted text using TextMate grammars.
/// Supports both string input and file processing with theme customization.
/// </summary>
[Cmdlet(VerbsCommon.Show, "TextMate", DefaultParameterSetName = "String")]
[Alias("st","Show-Code")]
[OutputType(typeof(Rows))]
public sealed class ShowTextMateCmdlet : PSCmdlet
{
    private static readonly string[] NewLineSplit = ["\r\n", "\n", "\r"];
    private readonly List<string> _inputObjectBuffer = [];
    private string? _sourceExtensionHint;

    [Parameter(
        Mandatory = true,
        ValueFromPipeline = true,
        ParameterSetName = "String"
    )]
    [AllowEmptyString]
    [ValidateNotNull]
    public string? InputObject { get; set; }

    [Parameter(
        Mandatory = true,
        ValueFromPipelineByPropertyName = true,
        ParameterSetName = "Path",
        Position = 0
    )]
    [ValidateNotNullOrEmpty]
    [Alias("FullName")]
    public string? Path { get; set; }

    [Parameter(
        ParameterSetName = "String"
    )]
    [Parameter(
        ParameterSetName = "Path"
    )]
    [ArgumentCompleter(typeof(LanguageCompleter))]
    public string? Language { get; set; }

    [Parameter()]
    public ThemeName Theme { get; set; } = ThemeName.DarkPlus;

    [Parameter]
    public SwitchParameter PassThru { get; set; }

    protected override void ProcessRecord()
    {
        if (ParameterSetName == "String" && InputObject is not null)
        {
            // Try to capture an extension hint from ETS note properties on the current pipeline object
            // (e.g., PSChildName/PSPath added by Get-Content)
            if (_sourceExtensionHint is null)
            {
                if (GetVariableValue("_") is PSObject current)
                {
                    string? hint = current.Properties["PSChildName"]?.Value as string
                                    ?? current.Properties["PSPath"]?.Value as string
                                    ?? current.Properties["Path"]?.Value as string
                                    ?? current.Properties["FullName"]?.Value as string;
                    if (!string.IsNullOrWhiteSpace(hint))
                    {
                        string ext = System.IO.Path.GetExtension(hint);
                        if (!string.IsNullOrWhiteSpace(ext))
                        {
                            _sourceExtensionHint = ext;
                        }
                    }
                }
            }
            _inputObjectBuffer.Add(InputObject);
            return;
        }

        if (ParameterSetName == "Path" && !string.IsNullOrWhiteSpace(Path))
        {
            try
            {
                Rows? result = ProcessPathInput();
                if (result is not null)
                {
                    WriteObject(result);
                    if (PassThru)
                    {
                        WriteVerbose($"Processed file '{Path}' with theme '{Theme}' {(string.IsNullOrWhiteSpace(Language) ? "(by extension)" : $"(token: {Language})")}");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "ShowTextMateCmdlet", ErrorCategory.NotSpecified, Path!));
            }
        }
    }

    protected override void EndProcessing()
    {
        // For Path parameter set, each record is processed in ProcessRecord to support streaming multiple files.
        // Only finalize buffered String input here.
        if (ParameterSetName != "String")
        {
            return;
        }

        try
        {
            Rows? result = ProcessStringInput();
            if (result is not null)
            {
                WriteObject(result);
                if (PassThru)
                {
                    WriteVerbose($"Processed {_inputObjectBuffer.Count} lines with theme '{Theme}' {(string.IsNullOrWhiteSpace(Language) ? "(by hint/default)" : $"(token: {Language})")}");
                }
            }
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(ex, "ShowTextMateCmdlet", ErrorCategory.NotSpecified, MyInvocation.BoundParameters));
        }
    }

    private Rows? ProcessStringInput()
    {
        if (_inputObjectBuffer.Count == 0)
        {
            WriteVerbose("No input provided");
            return null;
        }

        string[] strings = [.. _inputObjectBuffer];
        // If only one string and it contains any newline, split it into lines for correct rendering
        if (strings.Length == 1 && (strings[0].Contains('\n') || strings[0].Contains('\r')))
        {
            strings = strings[0].Split(NewLineSplit, StringSplitOptions.None);
        }
        if (strings.AllIsNullOrEmpty())
        {
            WriteVerbose("All input strings are null or empty");
            return null;
        }

        // If a Language token was provided, resolve it first (language id or extension)
        if (!string.IsNullOrWhiteSpace(Language))
        {
            (string? token, bool asExtension) = TextMateResolver.ResolveToken(Language!);
            return Converter.ProcessLines(strings, Theme, token, isExtension: asExtension);
        }

        // Otherwise prefer extension hint from ETS (PSChildName/PSPath)
        if (!string.IsNullOrWhiteSpace(_sourceExtensionHint))
        {
            return Converter.ProcessLines(strings, Theme, _sourceExtensionHint!, isExtension: true);
        }

        // Final fallback: default language
        return Converter.ProcessLines(strings, Theme, "powershell", isExtension: false);
    }

    private Rows? ProcessPathInput()
    {
        FileInfo filePath = new(GetUnresolvedProviderPathFromPSPath(Path));

        if (!filePath.Exists)
        {
            throw new FileNotFoundException($"File not found: {filePath.FullName}", filePath.FullName);
        }

        // Decide how to interpret based on precedence:
        // 1) Language token (can be a language id OR an extension)
        // 2) File extension
        string[] lines = File.ReadAllLines(filePath.FullName);
        if (!string.IsNullOrWhiteSpace(Language))
        {
            (string? token, bool asExtension) = TextMateResolver.ResolveToken(Language!);
            WriteVerbose($"Processing file: {filePath.FullName} with explicit token: {Language} (as {(asExtension ? "extension" : "language")})");
            return Converter.ProcessLines(lines, Theme, token, isExtension: asExtension);
        }
        string extension = filePath.Extension;
        WriteVerbose($"Processing file: {filePath.FullName} using file extension: {extension}");
        return Converter.ProcessLines(lines, Theme, extension, isExtension: true);
    }
}

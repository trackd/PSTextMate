using System.Management.Automation;
using TextMateSharp.Grammars;
using PwshSpectreConsole.TextMate.Extensions;
using Spectre.Console;

namespace PwshSpectreConsole.TextMate.Cmdlets;

/// <summary>
/// Cmdlet for displaying syntax-highlighted text using TextMate grammars.
/// Supports both string input and file processing with theme customization.
/// </summary>
[Cmdlet(VerbsCommon.Show, "TextMate", DefaultParameterSetName = "String")]
[Alias("st")]
[OutputType(typeof(Rows))]
public sealed class ShowTextMateCmdlet : PSCmdlet
{
    private static readonly string[] NewLineSplit = ["\r\n", "\n", "\r"];
    private readonly List<string> _inputObjectBuffer = [];

    [Parameter(
        Mandatory = true,
        ValueFromPipeline = true,
        ParameterSetName = "String"
    )]
    [AllowEmptyString]
    [ValidateNotNull]
    public object? InputObject { get; set; }

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
    [ValidateSet(typeof(TextMateLanguages))]
    public string? Language { get; set; } = "powershell";

    [Parameter()]
    public ThemeName Theme { get; set; } = ThemeName.DarkPlus;

    [Parameter(
        ParameterSetName = "Path"
    )]
    [TextMateExtensionTransform()]
    [ValidateSet(typeof(TextMateExtensions))]
    [Alias("As")]
    public string? ExtensionOverride { get; set; }

    [Parameter]
    public SwitchParameter PassThru { get; set; }

    protected override void BeginProcessing()
    {
        // Validate language support early
        if (ParameterSetName == "String" && !TextMateLanguages.IsSupportedLanguage(Language!))
        {
            WriteWarning($"Language '{Language}' may not be fully supported. Use Get-SupportedTextMate to see available languages.");
        }
    }

    protected override void ProcessRecord()
    {
        if (ParameterSetName == "String" && InputObject is not null)
        {
            object baseObj = InputObject;
            // Unwrap PSObject if needed
            if (baseObj is PSObject pso)
            {
                baseObj = pso.BaseObject;
            }
            switch (baseObj)
            {
                case string s:
                    _inputObjectBuffer.Add(s);
                    break;
                case string[] arr:
                    _inputObjectBuffer.AddRange(arr);
                    break;
                case IEnumerable<string> enumerable:
                    _inputObjectBuffer.AddRange(enumerable);
                    break;
                default:
                    WriteWarning($"InputObject of type '{baseObj.GetType().Name}' is not supported. Only string and string[] are accepted.");
                    break;
            }
        }
    }

    protected override void EndProcessing()
    {
        try
        {
            Rows? result = ParameterSetName switch
            {
                "String" => ProcessStringInput(),
                "Path" => ProcessPathInput(),
                _ => throw new InvalidOperationException($"Unknown parameter set: {ParameterSetName}")
            };

            if (result is not null)
            {
                WriteObject(result);

                if (PassThru)
                {
                    WriteVerbose($"Processed {(ParameterSetName == "String" ? _inputObjectBuffer.Count : "file")} lines with theme '{Theme}' and {(ParameterSetName == "String" ? $"language '{Language}'" : "extension detection")}");
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

        string[] strings = _inputObjectBuffer.ToArray();
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

        return Converter.ProcessLines(strings, Theme, Language ?? "powershell", isExtension: false);
    }

    private Rows? ProcessPathInput()
    {
        FileInfo filePath = new(GetUnresolvedProviderPathFromPSPath(Path));

        if (!filePath.Exists)
        {
            throw new FileNotFoundException($"File not found: {filePath.FullName}", filePath.FullName);
        }

        string extension = !string.IsNullOrEmpty(ExtensionOverride)
            ? ExtensionOverride
            : filePath.Extension;

        WriteVerbose($"Processing file: {filePath.FullName} with extension: {extension}");

        // Read file in cmdlet and pass to unified ProcessLines method
        string[] lines = File.ReadAllLines(filePath.FullName);
        return Converter.ProcessLines(lines, Theme, extension, isExtension: true);
    }
}

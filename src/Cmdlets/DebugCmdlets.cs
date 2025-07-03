using System.IO;
using System.Collections.Generic;
using System.Management.Automation;
using TextMateSharp.Grammars;
using PwshSpectreConsole.TextMate.Extensions;

namespace PwshSpectreConsole.TextMate.Cmdlets;

/// <summary>
/// Cmdlet for debugging TextMate processing and theme application.
/// Provides detailed diagnostic information for troubleshooting rendering issues.
/// </summary>
[Cmdlet(VerbsDiagnostic.Debug, "TextMate", DefaultParameterSetName = "String")]
public sealed class DebugTextMateCmdlet : PSCmdlet
{
    private readonly List<string> _inputObjectBuffer = new();

    [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "String")]
    [AllowEmptyString]
    public string InputObject { get; set; } = null!;

    [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Path", Position = 0)]
    [ValidateNotNullOrEmpty]
    [Alias("FullName")]
    public string Path { get; set; } = null!;

    [Parameter(ParameterSetName = "String")]
    [ValidateSet(typeof(TextMateLanguages))]
    public string Language { get; set; } = "powershell";

    [Parameter()]
    public ThemeName Theme { get; set; } = ThemeName.Dark;

    [Parameter(ParameterSetName = "Path")]
    [TextMateExtensionTransform()]
    [ValidateSet(typeof(TextMateExtensions))]
    [Alias("As")]
    public string ExtensionOverride { get; set; } = null!;

    protected override void ProcessRecord()
    {
        if (ParameterSetName == "String" && InputObject is not null)
        {
            _inputObjectBuffer.Add(InputObject);
        }
    }

    protected override void EndProcessing()
    {
        try
        {
            if (ParameterSetName == "String" && _inputObjectBuffer.Count > 0)
            {
                string[] strings = _inputObjectBuffer.ToArray();
                if (strings.AllIsNullOrEmpty())
                {
                    return;
                }
                var obj = Test.DebugTextMate(strings, Theme, Language);
                WriteObject(obj, true);
            }
            else if (ParameterSetName == "Path" && Path is not null)
            {
                FileInfo Filepath = new(GetUnresolvedProviderPathFromPSPath(Path));
                if (!Filepath.Exists)
                {
                    throw new FileNotFoundException("File not found", Filepath.FullName);
                }
                string ext = !string.IsNullOrEmpty(ExtensionOverride)
                    ? ExtensionOverride
                    : Filepath.Extension;
                string[] strings = File.ReadAllLines(Filepath.FullName);
                var obj = Test.DebugTextMate(strings, Theme, ext, true);
                WriteObject(obj, true);
            }
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(ex, "DebugTextMateError", ErrorCategory.InvalidOperation, null));
        }
    }
}

/// <summary>
/// Cmdlet for debugging individual TextMate tokens and their properties.
/// Provides low-level token analysis for detailed syntax highlighting inspection.
/// </summary>
[Cmdlet(VerbsDiagnostic.Debug, "TextMateTokens", DefaultParameterSetName = "String")]
public sealed class DebugTextMateTokensCmdlet : PSCmdlet
{
    private readonly List<string> _inputObjectBuffer = new();

    [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "String")]
    [AllowEmptyString]
    public string InputObject { get; set; } = null!;

    [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Path", Position = 0)]
    [ValidateNotNullOrEmpty]
    [Alias("FullName")]
    public string Path { get; set; } = null!;

    [Parameter(ParameterSetName = "String")]
    [ValidateSet(typeof(TextMateLanguages))]
    public string Language { get; set; } = "powershell";

    [Parameter()]
    public ThemeName Theme { get; set; } = ThemeName.Dark;

    [Parameter(ParameterSetName = "Path")]
    [TextMateExtensionTransform()]
    [ValidateSet(typeof(TextMateExtensions))]
    [Alias("As")]
    public string ExtensionOverride { get; set; } = null!;

    protected override void ProcessRecord()
    {
        if (ParameterSetName == "String" && InputObject is not null)
        {
            _inputObjectBuffer.Add(InputObject);
        }
    }

    protected override void EndProcessing()
    {
        try
        {
            if (ParameterSetName == "String" && _inputObjectBuffer.Count > 0)
            {
                string[] strings = _inputObjectBuffer.ToArray();
                if (strings.AllIsNullOrEmpty())
                {
                    return;
                }
                var obj = Test.DebugTextMateTokens(strings, Theme, Language);
                WriteObject(obj, true);
            }
            else if (ParameterSetName == "Path" && Path is not null)
            {
                FileInfo Filepath = new(GetUnresolvedProviderPathFromPSPath(Path));
                if (!Filepath.Exists)
                {
                    throw new FileNotFoundException("File not found", Filepath.FullName);
                }
                string ext = !string.IsNullOrEmpty(ExtensionOverride)
                    ? ExtensionOverride
                    : Filepath.Extension;
                string[] strings = File.ReadAllLines(Filepath.FullName);
                var obj = Test.DebugTextMateTokens(strings, Theme, ext, true);
                WriteObject(obj, true);
            }
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(ex, "DebugTextMateTokensError", ErrorCategory.InvalidOperation, null));
        }
    }
}

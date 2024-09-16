using System.IO;
using System.Collections.Generic;
using System.Management.Automation;
using TextMateSharp.Grammars;

namespace PwshSpectreConsole.TextMate;

[Cmdlet(VerbsCommon.Show, "TextMate", DefaultParameterSetName = "String")]
[Alias("st")]
public sealed class ShowTextMateCmdlet : PSCmdlet
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
        if (ParameterSetName == "String" && null != InputObject)
        {
            _inputObjectBuffer.Add(InputObject);
        }
    }
    protected override void EndProcessing()
    {
        if (ParameterSetName == "String" && _inputObjectBuffer.Count > 0)
        {
            string[] strings = _inputObjectBuffer.ToArray();
            // if all strings are empty, don't bother
            if (Converter.AllIsNullOrEmpty(strings))
            {
                return;
            }
            var rows = Converter.String(strings, Theme, Language);
            WriteObject(rows);
        }
        else if (ParameterSetName == "Path" && null != Path)
        {
            FileInfo Filepath = new(GetUnresolvedProviderPathFromPSPath(Path));
            if (!Filepath.Exists)
            {
                throw new FileNotFoundException("File not found", Filepath.FullName);
            }
            // extension override, it decides the grammar to use for highlighting
            string ext = !string.IsNullOrEmpty(ExtensionOverride)
                ? ExtensionOverride
                : Filepath.Extension;
            var rows = Converter.ReadFile(Filepath.FullName, Theme, ext);
            WriteObject(rows);
        }
    }
}

[Cmdlet(VerbsDiagnostic.Test, "SupportedTextMate")]
public sealed class TestTextMateCmdlet : PSCmdlet
{
    [Parameter()]
    public string? Extension { get; set; }

    [Parameter()]
    public string? Language { get; set; }

    [Parameter()]
    public string? File { get; set; }

    protected override void EndProcessing() {
        if (!string.IsNullOrEmpty(File)) {
            WriteObject(TextMateExtensions.IsSupportedFile(File));
        }
        if (!string.IsNullOrEmpty(Extension)) {
            WriteObject(TextMateExtensions.IsSupportedExtension(Extension));
        }
        if (!string.IsNullOrEmpty(Language)) {
            WriteObject(TextMateLanguages.IsSupportedLanguage(Language));
        }
    }
}

[OutputType(typeof(Language))]
[Cmdlet(VerbsCommon.Get, "SupportedTextMate")]
public sealed class GetTextMateCmdlet : PSCmdlet
{
    protected override void EndProcessing()
    {
        WriteObject(TextMateHelper.AvailableLanguages, enumerateCollection: true);
    }
}

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
        if (ParameterSetName == "String" && null != InputObject)
        {
            _inputObjectBuffer.Add(InputObject);
        }
    }
    protected override void EndProcessing()
    {
        if (ParameterSetName == "String" && _inputObjectBuffer.Count > 0)
        {
            string[] strings = _inputObjectBuffer.ToArray();
            if (Converter.AllIsNullOrEmpty(strings))
            {
                return;
            }
            var obj = Test.DebugTextMate(strings, Theme, Language);
            WriteObject(obj, true);
        }
        else if (ParameterSetName == "Path" && null != Path)
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
}

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
        if (ParameterSetName == "String" && null != InputObject)
        {
            _inputObjectBuffer.Add(InputObject);
        }
    }
    protected override void EndProcessing()
    {
        if (ParameterSetName == "String" && _inputObjectBuffer.Count > 0)
        {
            string[] strings = _inputObjectBuffer.ToArray();
            if (Converter.AllIsNullOrEmpty(strings))
            {
                return;
            }
            var obj = Test.DebugTextMateTokens(strings, Theme, Language);
            WriteObject(obj, true);

        }
        else if (ParameterSetName == "Path" && null != Path)
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
}

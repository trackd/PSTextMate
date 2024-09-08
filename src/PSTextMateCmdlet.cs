using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Management.Automation;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;

namespace PwshSpectreConsole;

[Cmdlet(VerbsCommon.Show, "TextMate", DefaultParameterSetName = "String")]
[Alias("st")]
public sealed class ShowTextMateCmdlet : PSCmdlet
{
    // private readonly List<string> _inputObjectBuffer = new List<string>();
    private readonly List<string> _inputObjectBuffer = new();

    [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "String")]
    [AllowEmptyString]
    public string InputObject { get; set; } = null!;

    [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "File", Position = 0)]
    [ValidateNotNullOrEmpty]
    [Alias("FullName")]
    public string File { get; set; } = null!;

    [Parameter(ParameterSetName = "String")]
    // todo: fix this..
    // [ValidateSet(nameof(TextMateLanguages), ErrorMessage = "Value '{0}' is invalid. Try one of: {1}")]
    public string Language { get; set; } = "powershell";

    [Parameter()]
    public ThemeName Theme { get; set; } = ThemeName.Dark;

    [Parameter(ParameterSetName = "File")]
    public string ExtensionOverride { get; set; } = null!;

    protected override void ProcessRecord()
    {
        if (ParameterSetName == "String" && null != InputObject)
        {
            // if someone is piping in a null string, we should still add it to the buffer..
            _inputObjectBuffer.Add(InputObject);
        }
    }
    protected override void EndProcessing()
    {
        if (ParameterSetName == "String" && _inputObjectBuffer.Count > 0)
        {
            string[] strings = _inputObjectBuffer.ToArray();
            // if all strings are empty, don't bother
            if (strings.All(s => string.IsNullOrEmpty(s)))
            {
                return;
            }
            var rows = TextMate.String(strings, Theme, Language);
            WriteObject(rows);
        }
        else if (ParameterSetName == "File" && null != File)
        {
            string ResolvedPath = GetUnresolvedProviderPathFromPSPath(File);
            FileInfo Filepath = new(ResolvedPath);
            if (!Filepath.Exists)
            {
                throw new FileNotFoundException("File not found", ResolvedPath);
            }
            // extension override, it decides the grammar to use for highlighting
            string ext = !string.IsNullOrEmpty(ExtensionOverride)
                ? (ExtensionOverride.StartsWith('.') ? ExtensionOverride : '.' + ExtensionOverride)
                : Filepath.Extension;
            var rows = TextMate.ReadFile(Filepath.FullName, Theme, ext);
            WriteObject(rows);
        }
    }
}
public class TextMateLanguages : IValidateSetValuesGenerator
{
    private static readonly string[] Lookup;
    static TextMateLanguages()
    {
        Lookup = new RegistryOptions(ThemeName.Dark).GetAvailableGrammarDefinitions().Select(x => x.Name).ToArray();
    }
    public string[] GetValidValues()
    {
        return Lookup;
    }
}

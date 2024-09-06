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
    private readonly List<string> _inputObjectBuffer = new List<string>();

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

    protected override void ProcessRecord()
    {
        if (ParameterSetName == "String" && null != InputObject)
        {
            _inputObjectBuffer.Add(InputObject);
        }
        // else if (ParameterSetName == "File" && null != File)
        // {
        //     string ResolvedPath = GetUnresolvedProviderPathFromPSPath(File);
        //     FileInfo Filepath = new(ResolvedPath);
        //     if (!System.IO.File.Exists(Filepath.FullName))
        //     {
        //         throw new FileNotFoundException("File not found", ResolvedPath);
        //     }
        //     var rows = TextMate.ReadFile(Filepath.FullName, Theme, Filepath.Extension);
        //     WriteObject(rows);
        // }
    }
    protected override void EndProcessing()
    {
        if (ParameterSetName == "String" && _inputObjectBuffer.Count > 0)
        {
            var rows = TextMate.String(_inputObjectBuffer.ToArray(), Theme, Language);
            WriteObject(rows);
        }
        else if (ParameterSetName == "File" && null != File)
        {
            string ResolvedPath = GetUnresolvedProviderPathFromPSPath(File);
            FileInfo Filepath = new(ResolvedPath);
            if (!System.IO.File.Exists(Filepath.FullName))
            {
                throw new FileNotFoundException("File not found", ResolvedPath);
            }
            var rows = TextMate.ReadFile(Filepath.FullName, Theme, Filepath.Extension);
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

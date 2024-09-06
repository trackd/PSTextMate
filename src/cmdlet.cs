﻿using System;
using System.IO;
using System.Management.Automation;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;
using System.Linq;
using Spectre.Console;

namespace PwshSpectreConsole.SyntaxHighlight;

[Cmdlet(VerbsCommon.Show, "TextMate", DefaultParameterSetName = "File")]
[Alias("st")]
public sealed class ShowTextMateCmdlet : PSCmdlet
{
    [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "String")]
    [ValidateNotNullOrEmpty]
    [Alias("InputString")]
    public string InputObject { get; set; } = null!;

    [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "File", Position = 0)]
    [ValidateNotNullOrEmpty]
    [Alias("FullName")]
    public string File { get; set; } = null!;

    [Parameter(ParameterSetName = "String")]
    // [ValidateSet(nameof(TextMateLanguages), ErrorMessage = "Value '{0}' is invalid. Try one of: {1}")]
    public string Language { get; set; } = "powershell";

    [Parameter()]
    public ThemeName Theme { get; set; } = ThemeName.Dark;

    protected override void ProcessRecord()
    {
        if (ParameterSetName == "String" && null != InputObject)
        {
            var StringArray = InputObject.Split(Environment.NewLine);
            var rows = Highlight.String(StringArray, Theme, Language);
            WriteObject(rows);
        }
        else if(ParameterSetName == "File" && null != File)
        {
            // this allows for relative paths to be used
            var Filepath = GetUnresolvedProviderPathFromPSPath(File);
            var rows = Highlight.ReadFile(Filepath, Theme, Path.GetExtension(Filepath));
            WriteObject(rows);
        }
    }
}
public class TextMateLanguages : IValidateSetValuesGenerator
{
    private static readonly string[] Lookup;
    static TextMateLanguages()
    {
        Lookup = new RegistryOptions(ThemeName.Red).GetAvailableGrammarDefinitions().Select(x => x.Name).ToArray();
    }
    public string[] GetValidValues()
    {
        return Lookup;
    }
}

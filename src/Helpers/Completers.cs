using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text.RegularExpressions;

namespace PwshSpectreConsole.TextMate;

public sealed class LanguageCompleter : IArgumentCompleter
{
    /// <summary>
    /// Offers completion for both TextMate language ids and file extensions.
    /// Examples: "powershell", "csharp", ".md", "md", ".ps1", "ps1".
    /// </summary>
    public IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        string input = wordToComplete ?? string.Empty;
        bool wantsExtensionsOnly = input.Length > 0 && input[0] == '.';

        // Prefer wildcard matching semantics; fall back to prefix/contains when empty
        WildcardPattern? pattern = null;
        if (!string.IsNullOrEmpty(input))
        {
            // Add trailing * if not already present to make incremental typing friendly
            string normalized = input[^1] == '*' ? input : input + "*";
            pattern = new WildcardPattern(normalized, WildcardOptions.IgnoreCase);
        }

        bool Match(string token)
        {
            if (pattern is null) return true; // no filter
            if (pattern.IsMatch(token)) return true;
            // Also test without a leading dot to match bare extensions like "ps1" against ".ps1"
            return token.StartsWith('.') && pattern.IsMatch(token[1..]);
        }

        // Build suggestions
        var results = new List<CompletionResult>();

        if (!wantsExtensionsOnly)
        {
            // Languages first
            foreach (string lang in TextMateHelper.Languages ?? [])
            {
                if (!Match(lang)) continue;
                results.Add(new CompletionResult(
                    completionText: lang,
                    listItemText: lang,
                    resultType: CompletionResultType.ParameterValue,
                    toolTip: "TextMate language"));
            }
        }

        // Extensions (always include if requested or no leading '.')
        foreach (string ext in TextMateHelper.Extensions ?? [])
        {
            if (!Match(ext)) continue;
            string completion = ext; // keep dot in completion
            string display = ext;
            results.Add(new CompletionResult(
                completionText: completion,
                listItemText: display,
                resultType: CompletionResultType.ParameterValue,
                toolTip: "File extension"));
        }

        // De-duplicate (in case of overlaps) and sort: languages first, then extensions, each alphabetically
        return results
            .GroupBy(r => r.CompletionText, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderByDescending(r => r.ToolTip.Equals("TextMate language", StringComparison.Ordinal))
            .ThenBy(r => r.CompletionText, StringComparer.OrdinalIgnoreCase);
    }
}
public class TextMateLanguages : IValidateSetValuesGenerator
    {
        public string[] GetValidValues()
        {
            return TextMateHelper.Languages;
        }
        public static bool IsSupportedLanguage(string language)
        {
            return TextMateHelper.Languages.Contains(language);
        }
    }
public class TextMateExtensions : IValidateSetValuesGenerator
{
    public string[] GetValidValues()
    {
        return TextMateHelper.Extensions;
    }
    public static bool IsSupportedExtension(string extension)
    {
        return TextMateHelper.Extensions?.Contains(extension) == true;
    }
    public static bool IsSupportedFile(string file)
    {
        string ext = Path.GetExtension(file);
        return TextMateHelper.Extensions?.Contains(ext) == true;
    }

}
public class TextMateExtensionTransform : ArgumentTransformationAttribute
{
    public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
    {
        if (inputData is string input)
        {
            return input.StartsWith('.') ? input : '.' + input;
        }
        throw new ArgumentException("Input must be a string representing a file extension., '.ext' format expected.", nameof(inputData));
    }

}

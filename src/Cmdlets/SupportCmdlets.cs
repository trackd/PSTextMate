using System.Management.Automation;
using TextMateSharp.Grammars;

namespace PwshSpectreConsole.TextMate.Cmdlets;

/// <summary>
/// Cmdlet for testing TextMate support for languages, extensions, and files.
/// Provides validation functionality to check compatibility before processing.
/// </summary>
[Cmdlet(VerbsDiagnostic.Test, "SupportedTextMate")]
public sealed class TestSupportedTextMateCmdlet : PSCmdlet {
    /// <summary>
    /// File extension to test for support (e.g., '.ps1').
    /// </summary>
    [Parameter()]
    public string? Extension { get; set; }

    /// <summary>
    /// Language ID to test for support (e.g., 'powershell').
    /// </summary>
    [Parameter()]
    public string? Language { get; set; }

    /// <summary>
    /// File path to test for support.
    /// </summary>
    [Parameter()]
    public string? File { get; set; }

    /// <summary>
    /// Finalizes processing and outputs support check results.
    /// </summary>
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

/// <summary>
/// Cmdlet for retrieving all supported TextMate languages and their configurations.
/// Returns detailed information about available grammars and extensions.
/// </summary>
[OutputType(typeof(Language))]
[Cmdlet(VerbsCommon.Get, "SupportedTextMate")]
public sealed class GetSupportedTextMateCmdlet : PSCmdlet {
    /// <summary>
    /// Finalizes processing and outputs all supported languages.
    /// </summary>
    protected override void EndProcessing() => WriteObject(TextMateHelper.AvailableLanguages, enumerateCollection: true);
}

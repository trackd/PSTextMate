using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using TextMateSharp.Grammars;

namespace PwshSpectreConsole.TextMate.Core.Validation;

/// <summary>
/// Provides validation utilities for markdown input and rendering parameters.
/// Helps prevent security issues and improves error handling.
/// </summary>
internal static partial class MarkdownInputValidator
{
    private const int MaxMarkdownLength = 1_000_000; // 1MB text limit
    private const int MaxLineCount = 10_000;
    private const int MaxLineLength = 50_000;

    [GeneratedRegex(@"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ScriptTagRegex();

    [GeneratedRegex(@"javascript:|data:|vbscript:", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex DangerousUrlRegex();

    /// <summary>
    /// Validates markdown input for security and size constraints.
    /// </summary>
    /// <param name="markdown">The markdown text to validate</param>
    /// <returns>Validation result with any errors</returns>
    public static ValidationResult ValidateMarkdownInput(string? markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return ValidationResult.Success!;

        var errors = new List<string>();

        // Check size limits
        if (markdown.Length > MaxMarkdownLength)
            errors.Add($"Markdown content exceeds maximum length of {MaxMarkdownLength:N0} characters");

        string[] lines = markdown.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length > MaxLineCount)
            errors.Add($"Markdown content exceeds maximum line count of {MaxLineCount:N0}");

        foreach (string line in lines)
        {
            if (line.Length > MaxLineLength)
            {
                errors.Add($"Line exceeds maximum length of {MaxLineLength:N0} characters");
                break;
            }
        }

        // Check for potentially dangerous content
        if (ScriptTagRegex().IsMatch(markdown))
            errors.Add("Markdown contains potentially dangerous script tags");

        // Check for dangerous URLs in links
        if (DangerousUrlRegex().IsMatch(markdown))
            errors.Add("Markdown contains potentially dangerous URLs");

        return errors.Count > 0
            ? new ValidationResult(string.Join("; ", errors))
            : ValidationResult.Success!;
    }

    /// <summary>
    /// Validates theme name parameter.
    /// </summary>
    /// <param name="themeName">The theme name to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidThemeName(ThemeName themeName)
    {
        return Enum.IsDefined(typeof(ThemeName), themeName);
    }

    /// <summary>
    /// Sanitizes URL input for link rendering.
    /// </summary>
    /// <param name="url">The URL to sanitize</param>
    /// <returns>Sanitized URL or null if dangerous</returns>
    public static string? SanitizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        // Remove dangerous protocols
        if (DangerousUrlRegex().IsMatch(url))
            return null;

        // Basic URL validation
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) &&
            !Uri.TryCreate(url, UriKind.Relative, out uri))
            return null;

        return url.Trim();
    }

    /// <summary>
    /// Validates language identifier for syntax highlighting.
    /// </summary>
    /// <param name="language">The language identifier</param>
    /// <returns>True if supported, false otherwise</returns>
    public static bool IsValidLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return false;

        // Check against known supported languages
        return TextMateLanguages.IsSupportedLanguage(language);
    }
}

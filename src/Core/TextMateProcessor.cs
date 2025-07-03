using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;
using PwshSpectreConsole.TextMate.Infrastructure;
using PwshSpectreConsole.TextMate.Extensions;
using Spectre.Console;
using Spectre.Console.Rendering;
using TextMateSharp.Grammars;
using TextMateSharp.Model;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core;

/// <summary>
/// Main entry point for TextMate processing operations.
/// Provides high-performance text processing using TextMate grammars and themes.
/// </summary>
public static class TextMateProcessor
{
    /// <summary>
    /// Processes string lines with specified theme and grammar for syntax highlighting.
    /// This is the unified method that handles all text processing scenarios.
    /// </summary>
    /// <param name="lines">Array of text lines to process</param>
    /// <param name="themeName">Theme to apply for styling</param>
    /// <param name="grammarId">Language ID or file extension for grammar selection</param>
    /// <param name="isExtension">True if grammarId is a file extension, false if it's a language ID</param>
    /// <returns>Rendered rows with syntax highlighting, or null if processing fails</returns>
    /// <exception cref="ArgumentNullException">Thrown when lines array is null</exception>
    /// <exception cref="UnsupportedGrammarException">Thrown when grammar cannot be found</exception>
    /// <exception cref="TextMateProcessingException">Thrown when processing encounters an error</exception>
    public static Rows? ProcessLines(string[] lines, ThemeName themeName, string grammarId, bool isExtension = false)
    {
        ArgumentNullException.ThrowIfNull(lines, nameof(lines));

        if (lines.Length == 0 || lines.AllIsNullOrEmpty())
        {
            return null;
        }

        return ProcessLines(lines, themeName, grammarId, isExtension, null);
    }

    public static Rows? ProcessLines(string[] lines, ThemeName themeName, string grammarId, bool isExtension, Action<TokenDebugInfo>? debugCallback)
    {
        ArgumentNullException.ThrowIfNull(lines, nameof(lines));

        if (lines.Length == 0 || lines.AllIsNullOrEmpty())
        {
            return null;
        }

        try
        {
            var (registry, theme) = CacheManager.GetCachedTheme(themeName);
            IGrammar? grammar = CacheManager.GetCachedGrammar(registry, grammarId, isExtension);

            if (grammar is null)
            {
                string errorMessage = isExtension
                    ? $"Grammar not found for file extension: {grammarId}"
                    : $"Grammar not found for language: {grammarId}";
                throw new InvalidOperationException(errorMessage);
            }

            // Use optimized rendering based on grammar type
            return grammar.GetName() == "Markdown"
                ? MarkdownRenderer.Render(lines, theme, grammar, debugCallback)
                : StandardRenderer.Render(lines, theme, grammar, debugCallback);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException($"Argument error processing lines with grammar '{grammarId}': {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Unexpected error processing lines with grammar '{grammarId}': {ex.Message}", ex);
        }
    }

    // ...existing code...
}

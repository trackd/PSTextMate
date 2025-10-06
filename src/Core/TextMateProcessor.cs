using System.Text;
using PwshSpectreConsole.TextMate.Infrastructure;
using PwshSpectreConsole.TextMate.Extensions;
using Spectre.Console;
using Spectre.Console.Rendering;
using TextMateSharp.Grammars;
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
            (TextMateSharp.Registry.Registry registry, Theme theme) = CacheManager.GetCachedTheme(themeName);
            // Resolve grammar using CacheManager which knows how to map language ids and extensions
            IGrammar? grammar = CacheManager.GetCachedGrammar(registry, grammarId, isExtension);
            if (grammar is null)
            {
                throw new InvalidOperationException(isExtension ? $"Grammar not found for file extension: {grammarId}" : $"Grammar not found for language: {grammarId}");
            }

            // Use optimized rendering based on grammar type
            return grammar.GetName() == "Markdown"
                ? MarkdownRenderer.Render(lines, theme, grammar, themeName, debugCallback)
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

    /// <summary>
    /// Processes string lines for code blocks without escaping markup characters.
    /// This preserves raw source code content for proper syntax highlighting.
    /// </summary>
    /// <param name="lines">Array of text lines to process</param>
    /// <param name="themeName">Theme to apply for styling</param>
    /// <param name="grammarId">Language ID or file extension for grammar selection</param>
    /// <param name="isExtension">True if grammarId is a file extension, false if it's a language ID</param>
    /// <returns>Rendered rows with syntax highlighting, or null if processing fails</returns>
    public static Rows? ProcessLinesCodeBlock(string[] lines, ThemeName themeName, string grammarId, bool isExtension = false)
    {
        ArgumentNullException.ThrowIfNull(lines, nameof(lines));

        try
        {
            (TextMateSharp.Registry.Registry registry, Theme theme) = CacheManager.GetCachedTheme(themeName);
            IGrammar? grammar = CacheManager.GetCachedGrammar(registry, grammarId, isExtension);

            if (grammar is null)
            {
                string errorMessage = isExtension
                    ? $"Grammar not found for file extension: {grammarId}"
                    : $"Grammar not found for language: {grammarId}";
                throw new InvalidOperationException(errorMessage);
            }

            // Always use StandardRenderer for code blocks, never MarkdownRenderer
            return RenderCodeBlock(lines, theme, grammar);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException($"Argument error processing code block with grammar '{grammarId}': {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Unexpected error processing code block with grammar '{grammarId}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Renders code block lines without escaping markup characters.
    /// </summary>
    private static Rows RenderCodeBlock(string[] lines, Theme theme, IGrammar grammar)
    {
        var builder = new StringBuilder();
        List<IRenderable> rows = new(lines.Length);
        IStateStack? ruleStack = null;

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex];
            ITokenizeLineResult result = grammar.TokenizeLine(line, ruleStack, TimeSpan.MaxValue);
            ruleStack = result.RuleStack;
            TokenProcessor.ProcessTokensBatchNoEscape(result.Tokens, line, theme, builder, null, lineIndex);
            string lineMarkup = builder.ToString();
            // Use Text (raw content) for code blocks so markup characters are preserved
            // and not interpreted by the Markup parser.
            rows.Add(string.IsNullOrEmpty(lineMarkup) ? Text.Empty : new Text(lineMarkup));
            builder.Clear();
        }

        return new Rows(rows.ToArray());
    }

    /// <summary>
    /// Processes an enumerable of lines in batches to support streaming/low-memory processing.
    /// Yields a Rows result for each processed batch.
    /// </summary>
    public static IEnumerable<RenderableBatch> ProcessLinesInBatches(IEnumerable<string> lines, int batchSize, ThemeName themeName, string grammarId, bool isExtension = false)
    {
        ArgumentNullException.ThrowIfNull(lines, nameof(lines));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize, nameof(batchSize));

        var buffer = new List<string>(batchSize);
        int batchIndex = 0;
        long fileOffset = 0; // starting line index for the next batch

        // Load theme and registry once and then resolve the requested grammar scope
        // directly on the registry. Avoid using the global grammar cache here because
        // TextMateSharp's Registry manages its own internal grammar store and repeated
        // LoadGrammar calls or cross-registry caching can cause duplicate-key exceptions.
        (TextMateSharp.Registry.Registry registry, Theme theme) = CacheManager.GetCachedTheme(themeName);
        // Resolve grammar using CacheManager which knows how to map language ids and extensions
        IGrammar? grammar = CacheManager.GetCachedGrammar(registry, grammarId, isExtension);
        if (grammar is null)
        {
            throw new InvalidOperationException(isExtension ? $"Grammar not found for file extension: {grammarId}" : $"Grammar not found for language: {grammarId}");
        }

        bool useMarkdownRenderer = grammar.GetName() == "Markdown";

        foreach (string? line in lines)
        {
            buffer.Add(line ?? string.Empty);
            if (buffer.Count >= batchSize)
            {
                // Render the batch using the already-loaded grammar and theme
                Rows? result = useMarkdownRenderer
                    ? MarkdownRenderer.Render([.. buffer], theme, grammar, themeName, null)
                    : StandardRenderer.Render([.. buffer], theme, grammar, null);
                if (result is not null)
                    yield return new RenderableBatch(result.Renderables, batchIndex: batchIndex++, fileOffset: fileOffset);
                buffer.Clear();
                fileOffset += batchSize;
            }
        }
        if (buffer.Count > 0)
        {
            Rows? result = useMarkdownRenderer
                ? MarkdownRenderer.Render([.. buffer], theme, grammar, themeName, null)
                : StandardRenderer.Render([.. buffer], theme, grammar, null);
            if (result is not null)
                yield return new RenderableBatch(result.Renderables, batchIndex: batchIndex++, fileOffset: fileOffset);
        }
    }

    /// <summary>
    /// Helper to stream a file by reading lines lazily and processing them in batches.
    /// </summary>
    public static IEnumerable<RenderableBatch> ProcessFileInBatches(string filePath, int batchSize, ThemeName themeName, string grammarId, bool isExtension = false)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException(filePath);
        return ProcessLinesInBatches(File.ReadLines(filePath), batchSize, themeName, grammarId, isExtension);
    }
}

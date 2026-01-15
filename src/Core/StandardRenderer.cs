using System.Text;
using PwshSpectreConsole.TextMate.Helpers;
using Spectre.Console;
using Spectre.Console.Rendering;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core;

/// <summary>
/// Provides optimized rendering for standard (non-Markdown) TextMate grammars.
/// Implements object pooling and batch processing for better performance.
/// </summary>
internal static class StandardRenderer {
    /// <summary>
    /// Renders text lines using standard TextMate grammar processing.
    /// Uses object pooling and batch processing for optimal performance.
    /// </summary>
    /// <param name="lines">Lines to render</param>
    /// <param name="theme">Theme to apply</param>
    /// <param name="grammar">Grammar for tokenization</param>
    /// <returns>Rendered rows with syntax highlighting</returns>
    public static IRenderable[] Render(string[] lines, Theme theme, IGrammar grammar) => Render(lines, theme, grammar, null);

    public static IRenderable[] Render(string[] lines, Theme theme, IGrammar grammar, Action<TokenDebugInfo>? debugCallback) {
        StringBuilder builder = StringBuilderPool.Rent();
        List<IRenderable> rows = new(lines.Length);

        try {
            IStateStack? ruleStack = null;
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++) {
                string line = lines[lineIndex];
                ITokenizeLineResult result = grammar.TokenizeLine(line, ruleStack, TimeSpan.MaxValue);
                ruleStack = result.RuleStack;
                TokenProcessor.ProcessTokensBatch(result.Tokens, line, theme, builder, debugCallback, lineIndex);
                string? lineMarkup = builder.ToString();
                rows.Add(string.IsNullOrEmpty(lineMarkup) ? Text.Empty : new Markup(lineMarkup));
                builder.Clear();
            }

            return [.. rows];
        }
        catch (ArgumentException ex) {
            throw new InvalidOperationException($"Argument error during rendering: {ex.Message}", ex);
        }
        catch (Exception ex) {
            throw new InvalidOperationException($"Unexpected error during rendering: {ex.Message}", ex);
        }
        finally {
            StringBuilderPool.Return(builder);
        }
    }
}

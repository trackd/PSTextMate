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
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core;

/// <summary>
/// Provides specialized rendering for Markdown content with enhanced link handling.
/// Includes special processing for Markdown links using Spectre Console link markup.
/// </summary>
internal static class MarkdownRenderer
{
    /// <summary>
    /// Renders Markdown content with special handling for links and enhanced formatting.
    /// </summary>
    /// <param name="lines">Lines to render</param>
    /// <param name="theme">Theme to apply</param>
    /// <param name="grammar">Markdown grammar</param>
    /// <returns>Rendered rows with markdown syntax highlighting</returns>
    // Set this to true to use the new Markdig renderer, false for the legacy renderer
    public static bool UseMarkdigRenderer { get; set; } = true;

    public static Rows Render(string[] lines, Theme theme, IGrammar grammar)
    {
        if (UseMarkdigRenderer)
        {
            string markdown = string.Join("\n", lines);
            return MarkdigSpectreMarkdownRenderer.Render(markdown, theme);
        }
        else
        {
            return RenderLegacy(lines, theme, grammar, null);
        }
    }

    public static Rows Render(string[] lines, Theme theme, IGrammar grammar, Action<TokenDebugInfo>? debugCallback)
    {
        if (UseMarkdigRenderer)
        {
            string markdown = string.Join("\n", lines);
            return MarkdigSpectreMarkdownRenderer.Render(markdown, theme);
        }
        else
        {
            return RenderLegacy(lines, theme, grammar, debugCallback);
        }
    }

    // The original legacy renderer logic
    private static Rows RenderLegacy(string[] lines, Theme theme, IGrammar grammar, Action<TokenDebugInfo>? debugCallback)
    {
        var builder = PoolManager.StringBuilderPool.Get();
        List<IRenderable> rows = new(lines.Length);

        try
        {
            IStateStack? ruleStack = null;

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];
                ITokenizeLineResult result = grammar.TokenizeLine(line, ruleStack, TimeSpan.MaxValue);
                ruleStack = result.RuleStack;

                ProcessMarkdownTokens(result.Tokens, line, theme, builder);

                debugCallback?.Invoke(new TokenDebugInfo
                {
                    LineIndex = lineIndex,
                    Text = line,
                    // You can add more fields if you refactor ProcessMarkdownTokens
                });

                var lineMarkup = builder.ToString();
                rows.Add(string.IsNullOrEmpty(lineMarkup) ? Text.Empty : new Markup(lineMarkup));
                builder.Clear();
            }

            return new Rows(rows.ToArray());
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException($"Argument error rendering markdown content: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Unexpected error rendering markdown content: {ex.Message}", ex);
        }
        finally
        {
            PoolManager.StringBuilderPool.Return(builder);
        }
    }

    /// <summary>
    /// Processes markdown tokens with special handling for links.
    /// </summary>
    /// <param name="tokens">Tokens to process</param>
    /// <param name="line">Source line text</param>
    /// <param name="theme">Theme for styling</param>
    /// <param name="builder">StringBuilder for output</param>
    private static void ProcessMarkdownTokens(IToken[] tokens, string line, Theme theme, StringBuilder builder)
    {
        string? url = null;
        string? title = null;

        for (int i = 0; i < tokens.Length; i++)
        {
            IToken token = tokens[i];

            if (token.Scopes.Contains("meta.link.inline.markdown"))
            {
                i++; // Skip first bracket token
                while (i < tokens.Length && tokens[i].Scopes.Contains("meta.link.inline.markdown"))
                {
                    if (tokens[i].Scopes.Contains("string.other.link.title.markdown"))
                    {
                        title = line.SubstringAtIndexes(tokens[i].StartIndex, tokens[i].EndIndex);
                    }
                    if (tokens[i].Scopes.Contains("markup.underline.link.markdown"))
                    {
                        url = line.SubstringAtIndexes(tokens[i].StartIndex, tokens[i].EndIndex);
                    }
                    if (title != null && url != null)
                    {
                        builder.Append(MarkdownLinkFormatter.WriteMarkdownLink(url, title));
                        title = null;
                        url = null;
                    }
                    i++;
                }
                continue;
            }

            int startIndex = Math.Min(token.StartIndex, line.Length);
            int endIndex = Math.Min(token.EndIndex, line.Length);

            if (startIndex >= endIndex) continue;

            var textSpan = line.SubstringAsSpan(startIndex, endIndex);
            var (foreground, background, fontStyle) = TokenProcessor.ExtractThemeProperties(token, theme);
            var (escapedText, style) = TokenProcessor.WriteTokenOptimized(textSpan, foreground, background, fontStyle, theme);

            builder.AppendWithStyle(style, escapedText);
        }
    }
}

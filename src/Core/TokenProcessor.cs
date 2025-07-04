using System.Text;
using Spectre.Console;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;
using PwshSpectreConsole.TextMate.Extensions;

namespace PwshSpectreConsole.TextMate.Core;

/// <summary>
/// Provides optimized token processing and styling operations.
/// Handles theme property extraction and token rendering with performance optimizations.
/// </summary>
internal static class TokenProcessor
{
    /// <summary>
    /// Processes tokens in batches for better cache locality and performance.
    /// </summary>
    /// <param name="tokens">Tokens to process</param>
    /// <param name="line">Source line text</param>
    /// <param name="theme">Theme for styling</param>
    /// <param name="builder">StringBuilder for output</param>
    public static void ProcessTokensBatch(
        IToken[] tokens,
        string line,
        Theme theme,
        StringBuilder builder,
        Action<TokenDebugInfo>? debugCallback = null,
        int? lineIndex = null)
    {
        foreach (IToken token in tokens)
        {
            int startIndex = Math.Min(token.StartIndex, line.Length);
            int endIndex = Math.Min(token.EndIndex, line.Length);

            if (startIndex >= endIndex) continue;

            var textSpan = line.SubstringAsSpan(startIndex, endIndex);
            var (foreground, background, fontStyle) = ExtractThemeProperties(token, theme);
            var (escapedText, style) = WriteTokenOptimized(textSpan, foreground, background, fontStyle, theme);

            builder.AppendWithStyle(style, escapedText);

            debugCallback?.Invoke(new TokenDebugInfo
            {
                LineIndex = lineIndex,
                StartIndex = startIndex,
                EndIndex = endIndex,
                Text = line.SubstringAtIndexes(startIndex, endIndex),
                Scopes = token.Scopes,
                Foreground = foreground,
                Background = background,
                FontStyle = fontStyle,
                Style = style,
                Theme = theme.GetGuiColorDictionary()
            });
        }
    }

    /// <summary>
    /// Processes tokens from TextMate grammar tokenization without escaping markup.
    /// Used for code blocks where we want to preserve raw content.
    /// </summary>
    /// <param name="tokens">Tokens to process</param>
    /// <param name="line">Source line text</param>
    /// <param name="theme">Theme for color resolution</param>
    /// <param name="builder">StringBuilder to append styled text to</param>
    /// <param name="debugCallback">Optional callback for debugging token information</param>
    /// <param name="lineIndex">Line index for debugging context</param>
    public static void ProcessTokensBatchNoEscape(
        IToken[] tokens,
        string line,
        Theme theme,
        StringBuilder builder,
        Action<TokenDebugInfo>? debugCallback = null,
        int? lineIndex = null)
    {
        foreach (IToken token in tokens)
        {
            int startIndex = Math.Min(token.StartIndex, line.Length);
            int endIndex = Math.Min(token.EndIndex, line.Length);

            if (startIndex >= endIndex) continue;

            var textSpan = line.SubstringAsSpan(startIndex, endIndex);
            var (foreground, background, fontStyle) = ExtractThemeProperties(token, theme);
            var (processedText, style) = WriteTokenOptimized(textSpan, foreground, background, fontStyle, theme, escapeMarkup: false);

            builder.AppendWithStyle(style, processedText);

            debugCallback?.Invoke(new TokenDebugInfo
            {
                LineIndex = lineIndex,
                StartIndex = startIndex,
                EndIndex = endIndex,
                Text = line.SubstringAtIndexes(startIndex, endIndex),
                Scopes = token.Scopes,
                Foreground = foreground,
                Background = background,
                FontStyle = fontStyle,
                Style = style,
                Theme = theme.GetGuiColorDictionary()
            });
        }
    }

    public static (int foreground, int background, FontStyle fontStyle) ExtractThemeProperties(IToken token, Theme theme)
    {
        int foreground = -1;
        int background = -1;
        FontStyle fontStyle = FontStyle.NotSet;

        foreach (var themeRule in theme.Match(token.Scopes))
        {
            if (foreground == -1 && themeRule.foreground > 0)
                foreground = themeRule.foreground;
            if (background == -1 && themeRule.background > 0)
                background = themeRule.background;
            if (fontStyle == FontStyle.NotSet && themeRule.fontStyle > 0)
                fontStyle = themeRule.fontStyle;
        }

        return (foreground, background, fontStyle);
    }
    public static (string escapedText, Style? style) WriteTokenOptimized(
        ReadOnlySpan<char> text,
        int foreground,
        int background,
        FontStyle fontStyle,
        Theme theme,
        bool escapeMarkup = true)
    {
        string processedText = escapeMarkup ? Markup.Escape(text.ToString()) : text.ToString();

        // Early return for no styling needed
        if (foreground == -1 && background == -1 && fontStyle == FontStyle.NotSet)
        {
            return (processedText, null);
        }

        Decoration decoration = StyleHelper.GetDecoration(fontStyle);
        Color backgroundColor = StyleHelper.GetColor(background, theme);
        Color foregroundColor = StyleHelper.GetColor(foreground, theme);
        Style style = new(foregroundColor, backgroundColor, decoration);

        return (processedText, style);
    }

}

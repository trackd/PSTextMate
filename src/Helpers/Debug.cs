
using System.Collections.ObjectModel;
using Spectre.Console;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

// this is just for debugging purposes.

namespace PwshSpectreConsole.TextMate;

/// <summary>
/// Provides debugging utilities for TextMate processing operations.
/// </summary>
public static class Test {
    /// <summary>
    /// Debug information wrapper for TextMate token and styling data.
    /// </summary>
    public class TextMateDebug {
        /// <summary>
        /// Line number of the token (zero-based).
        /// </summary>
        public int? LineIndex { get; set; }
        /// <summary>
        /// Starting character position of the token.
        /// </summary>
        public int StartIndex { get; set; }
        /// <summary>
        /// Ending character position of the token.
        /// </summary>
        public int EndIndex { get; set; }
        /// <summary>
        /// Text content of the token.
        /// </summary>
        public string? Text { get; set; }
        /// <summary>
        /// Scopes applying to this token for theme matching.
        /// </summary>
        public List<string>? Scopes { get; set; }
        /// <summary>
        /// Foreground color ID from the theme.
        /// </summary>
        public int Foreground { get; set; }
        /// <summary>
        /// Background color ID from the theme.
        /// </summary>
        public int Background { get; set; }
        /// <summary>
        /// Font style flags (bold, italic, underline).
        /// </summary>
        public FontStyle FontStyle { get; set; }
        /// <summary>
        /// Resolved Spectre.Console style for rendering.
        /// </summary>
        public Style? Style { get; set; }
        /// <summary>
        /// Theme color dictionary used for rendering.
        /// </summary>
        public ReadOnlyDictionary<string, string>? Theme { get; set; }
    }

    /// <summary>
    /// Debugs TextMate processing and returns styled token information.
    /// </summary>
    /// <param name="lines">Text lines to debug</param>
    /// <param name="themeName">Theme to apply</param>
    /// <param name="grammarId">Grammar language ID or file extension</param>
    /// <param name="FromFile">True if grammarId is a file extension, false for language ID</param>
    /// <returns>Array of debug information for all tokens</returns>
    public static TextMateDebug[]? DebugTextMate(string[] lines, ThemeName themeName, string grammarId, bool FromFile = false) {
        var debugList = new List<TextMateDebug>();
        Core.TextMateProcessor.ProcessLines(
            lines,
            themeName,
            grammarId,
            isExtension: FromFile,
            debugCallback: info => debugList.Add(new TextMateDebug {
                LineIndex = info.LineIndex,
                StartIndex = info.StartIndex,
                EndIndex = info.EndIndex,
                Text = info.Text,
                Scopes = info.Scopes,
                Foreground = info.Foreground,
                Background = info.Background,
                FontStyle = info.FontStyle,
                Style = info.Style,
                Theme = info.Theme
            })
        );
        return [.. debugList];
    }

    /// <summary>
    /// Returns detailed debug information for each token with styling applied.
    /// </summary>
    /// <param name="lines">Text lines to debug</param>
    /// <param name="themeName">Theme to apply</param>
    /// <param name="grammarId">Grammar language ID or file extension</param>
    /// <param name="FromFile">True if grammarId is a file extension, false for language ID</param>
    /// <returns>Array of token debug information</returns>
    public static Core.TokenDebugInfo[]? DebugTextMateTokens(string[] lines, ThemeName themeName, string grammarId, bool FromFile = false) {
        var debugList = new List<Core.TokenDebugInfo>();
        Core.TextMateProcessor.ProcessLines(
            lines,
            themeName,
            grammarId,
            isExtension: FromFile,
            debugCallback: debugList.Add
        );
        return [.. debugList];
    }
}

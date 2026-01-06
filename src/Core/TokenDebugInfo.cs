using System.Collections.ObjectModel;
using Spectre.Console;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core;

/// <summary>
/// Contains debug information for a single parsed token including position, scope, and styling details.
/// </summary>
public class TokenDebugInfo {
    /// <summary>
    /// Line number of this token (zero-based index).
    /// </summary>
    public int? LineIndex { get; set; }
    /// <summary>
    /// Starting character position of this token in the line.
    /// </summary>
    public int StartIndex { get; set; }
    /// <summary>
    /// Ending character position of this token in the line.
    /// </summary>
    public int EndIndex { get; set; }
    /// <summary>
    /// The actual text content of this token.
    /// </summary>
    public string? Text { get; set; }
    /// <summary>
    /// List of scopes that apply to this token (for theme matching).
    /// </summary>
    public List<string>? Scopes { get; set; }
    /// <summary>
    /// Foreground color ID from theme (negative if not set).
    /// </summary>
    public int Foreground { get; set; }
    /// <summary>
    /// Background color ID from theme (negative if not set).
    /// </summary>
    public int Background { get; set; }
    /// <summary>
    /// Font style applied to this token (bold, italic, underline).
    /// </summary>
    public FontStyle FontStyle { get; set; }
    /// <summary>
    /// Resolved Spectre.Console style for rendering this token.
    /// </summary>
    public Style? Style { get; set; }
    /// <summary>
    /// Theme color dictionary used for rendering this token.
    /// </summary>
    public ReadOnlyDictionary<string, string>? Theme { get; set; }
}

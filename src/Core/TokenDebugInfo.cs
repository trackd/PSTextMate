using System.Collections.ObjectModel;
using Spectre.Console;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core;

public class TokenDebugInfo
{
    public int? LineIndex { get; set; }
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public string? Text { get; set; }
    public List<string>? Scopes { get; set; }
    public int Foreground { get; set; }
    public int Background { get; set; }
    public FontStyle FontStyle { get; set; }
    public Style? Style { get; set; }
    public ReadOnlyDictionary<string, string>? Theme { get; set; }
}

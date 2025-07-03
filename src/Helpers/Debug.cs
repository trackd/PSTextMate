
using System;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;
using TextMateSharp.Registry;
using Spectre.Console;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PwshSpectreConsole.TextMate.Extensions;

// this is just for debugging purposes.

namespace PwshSpectreConsole.TextMate;

public static class Test
{
    public class TextMateDebug
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

    public static TextMateDebug[]? DebugTextMate(string[] lines, ThemeName themeName, string grammarId, bool FromFile = false)
    {
        var debugList = new List<TextMateDebug>();
        PwshSpectreConsole.TextMate.Core.TextMateProcessor.ProcessLines(
            lines,
            themeName,
            grammarId,
            isExtension: FromFile,
            debugCallback: info => debugList.Add(new TextMateDebug
            {
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
        return debugList.ToArray();
    }

    public static TextMateDebug[]? DebugTextMateTokens(string[] lines, ThemeName themeName, string grammarId, bool FromFile = false)
    {
        // For now, just alias to DebugTextMate for unified debug info
        return DebugTextMate(lines, themeName, grammarId, FromFile);
    }
}

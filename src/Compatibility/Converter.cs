using PwshSpectreConsole.TextMate.Core;
using Spectre.Console;
using TextMateSharp.Grammars;

namespace PwshSpectreConsole.TextMate;

public static class Converter
{
    public static Spectre.Console.Rows? ProcessLines(string[] lines, ThemeName themeName, string grammarId, bool isExtension = false)
    {
        var rows = TextMateProcessor.ProcessLines(lines, themeName, grammarId, isExtension);
        if (rows is null) return null;
        return new Spectre.Console.Rows(rows.Renderables);
    }
}

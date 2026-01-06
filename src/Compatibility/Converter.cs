using PwshSpectreConsole.TextMate.Core;
using Spectre.Console;
using TextMateSharp.Grammars;

namespace PwshSpectreConsole.TextMate;

/// <summary>
///
/// </summary>
public static class Converter {
    /// <summary>
    ///
    /// </summary>
    /// <param name="lines"></param>
    /// <param name="themeName"></param>
    /// <param name="grammarId"></param>
    /// <param name="isExtension"></param>
    /// <returns></returns>
    public static Spectre.Console.Rows? ProcessLines(string[] lines, ThemeName themeName, string grammarId, bool isExtension = false) {
        Core.Rows? rows = TextMateProcessor.ProcessLines(lines, themeName, grammarId, isExtension);
        return rows is null ? null : new Spectre.Console.Rows(rows.Renderables);
    }
}

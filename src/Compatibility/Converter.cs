using PwshSpectreConsole.TextMate.Core;
using Spectre.Console;
using Spectre.Console.Rendering;
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
    public static Rows? ProcessLines(string[] lines, ThemeName themeName, string grammarId, bool isExtension = false) {
        IRenderable[]? renderables = TextMateProcessor.ProcessLines(lines, themeName, grammarId, isExtension);
        return renderables is null ? null : new Rows(renderables);
    }
}

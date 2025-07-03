using System;
using PwshSpectreConsole.TextMate.Core;
using Spectre.Console;
using TextMateSharp.Grammars;

namespace PwshSpectreConsole.TextMate;

public static class Converter
{
    public static Rows? ProcessLines(string[] lines, ThemeName themeName, string grammarId, bool isExtension = false)
    {
        return TextMateProcessor.ProcessLines(lines, themeName, grammarId, isExtension);
    }
}

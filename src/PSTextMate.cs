using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Collections.Generic;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;
using TextMateSharp.Registry;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PwshSpectreConsole.TextMate;
public class Converter
{

    public static Rows? String(string[] lines, ThemeName themeName, string grammarId)
    {
        RegistryOptions options = new RegistryOptions(themeName);
        Registry registry = new Registry(options);
        Theme theme = registry.GetTheme();
        IGrammar grammar = registry.LoadGrammar(options.GetScopeByLanguageId(grammarId));
        if (grammar == null)
        {
            throw new Exception("Grammar not found for language: " + grammarId);
        }
        return Render(lines, theme, grammar);
    }

    public static Rows? ReadFile(string fullName, ThemeName themeName, string Extension)
    {
        string[] lines = File.ReadAllLines(fullName);
        RegistryOptions options = new RegistryOptions(themeName);
        Registry registry = new Registry(options);
        Theme theme = registry.GetTheme();
        IGrammar grammar = registry.LoadGrammar(options.GetScopeByExtension(Extension));
        if (grammar == null)
        {
            throw new Exception("Grammar not found for extension: " + Extension);
        }
        return Render(lines, theme, grammar);
    }

    internal static Rows? Render(string[] String, Theme theme, IGrammar grammar)
    {
        StringBuilder builder = new StringBuilder();
        List<IRenderable> rows = new();
        try
        {
            int ini = Environment.TickCount;
            int tokenizeIni = Environment.TickCount;
            IStateStack? ruleStack = null;
            foreach (string line in String)
            {
                ITokenizeLineResult result = grammar.TokenizeLine(line, ruleStack, TimeSpan.MaxValue);
                ruleStack = result.RuleStack;
                foreach (IToken token in result.Tokens)
                {
                    int startIndex = (token.StartIndex > line.Length) ? line.Length : token.StartIndex;
                    int endIndex = (token.EndIndex > line.Length) ? line.Length : token.EndIndex;
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
                    var (textEscaped, style) = WriteToken(line.SubstringAtIndexes(startIndex, endIndex), foreground, background, fontStyle, theme);
                    builder.AppendWithStyle(style, textEscaped);
                }
                var lineMarkup = builder.ToString();
                // Preserve empty lines in rows output by using Text.Empty, Markup is stripping them for some reason
                rows.Add(string.IsNullOrEmpty(lineMarkup) ? Text.Empty : new Markup(lineMarkup));
                builder.Clear();
            }
            return new Rows(rows.ToArray());
        }
        catch (Exception ex)
        {
            throw new Exception("ERROR: " + ex.Message);
        }
    }

    internal static (string textEscaped, Style? style) WriteToken(string text, int foreground, int background, FontStyle fontStyle, Theme theme)
    {
        string textEscaped = Markup.Escape(text);
        if (foreground == -1)
        {
            return (textEscaped, null);
        }
        Decoration decoration = GetDecoration(fontStyle);
        Color backgroundColor = GetColor(background, theme);
        Color foregroundColor = GetColor(foreground, theme);
        Style style = new Style(foregroundColor, backgroundColor, decoration);
        return (textEscaped, style);
    }

    internal static Color GetColor(int colorId, Theme theme)
    {
        if (colorId == -1)
        {
            return Color.Default;
        }
        return HexToColor(theme.GetColor(colorId));
    }

    internal static Decoration GetDecoration(FontStyle fontStyle)
    {
        Decoration result = Decoration.None;
        if (fontStyle == FontStyle.NotSet)
            return result;
        if ((fontStyle & FontStyle.Italic) != 0)
            result |= Decoration.Italic;
        if ((fontStyle & FontStyle.Underline) != 0)
            result |= Decoration.Underline;
        if ((fontStyle & FontStyle.Bold) != 0)
            result |= Decoration.Bold;
        return result;
    }

    internal static Color HexToColor(string hexString)
    {
        if (hexString.StartsWith("#"))
        {
            hexString = hexString.Substring(1);
        }
        byte r, g, b = 0;
        r = byte.Parse(hexString.Substring(0, 2), NumberStyles.AllowHexSpecifier);
        g = byte.Parse(hexString.Substring(2, 2), NumberStyles.AllowHexSpecifier);
        b = byte.Parse(hexString.Substring(4, 2), NumberStyles.AllowHexSpecifier);
        return new Color(r, g, b);
    }
    internal static bool AllIsNullOrEmpty(string[] strings)
    {
        if (strings == null)
        {
            return true;
        }

        foreach (string s in strings)
        {
            if (!string.IsNullOrEmpty(s))
            {
                return false;
            }
        }
        return true;
    }

}

internal static class StringExtensions
{
    internal static string SubstringAtIndexes(this string str, int startIndex, int endIndex)
    {
        return str.Substring(startIndex, endIndex - startIndex);
    }
}

internal static class StringBuilderExtensions
{
    public static StringBuilder AppendWithStyle(this StringBuilder builder, Style? style, int? value)
    {
        return AppendWithStyle(builder, style, value?.ToString(CultureInfo.InvariantCulture));
    }
    public static StringBuilder AppendWithStyle(this StringBuilder builder, Style? style, string? value)
    {
        value ??= string.Empty;
        if (style != null)
        {
            return builder.Append('[')
            .Append(style.ToMarkup())
            .Append(']')
            .Append(value.EscapeMarkup())
            .Append("[/]");
        }
        return builder.Append(value);
    }
}

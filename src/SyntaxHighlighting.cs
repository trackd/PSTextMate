using System;
using System.Globalization;
using System.IO;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;
using TextMateSharp.Registry;
using Spectre.Console;
using System.Linq;

namespace PwshSpectreConsole.SyntaxHighlight;
public class Highlight
{
    public static ThemeName GetTheme(string themeName)
    {
        return Enum.Parse<ThemeName>(themeName, true);
    }
    public static void Code(string[] lines, ThemeName themeName, string grammarId)
    {
        RegistryOptions options = new RegistryOptions(themeName);
        Registry registry = new Registry(options);
        Theme theme = registry.GetTheme();
        IGrammar grammar = registry.LoadGrammar(options.GetScopeByLanguageId(grammarId));
        Write(lines, theme, grammar);
    }
    public static void ReadFile(string fullName, ThemeName themeName, string Extension)
    {
        try
        {
            string[] lines = File.ReadAllLines(fullName);
            RegistryOptions options = new RegistryOptions(themeName);
            Registry registry = new Registry(options);
            Theme theme = registry.GetTheme();
            IGrammar grammar = registry.LoadGrammar(options.GetScopeByExtension(Extension));
            Write(lines, theme, grammar);
        }
        catch (Exception ex)
        {
            throw new Exception("ERROR: " + ex.Message);
        }
    }

    public static void Write(string[] lines, Theme theme, IGrammar grammar)
    {
        try
        {
            int ini = Environment.TickCount;
            if (grammar == null)
            {
                throw new Exception("Grammar not found.");
            }

            int tokenizeIni = Environment.TickCount;
            IStateStack? ruleStack = null;

            foreach (string line in lines)
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

                    WriteToken(line.SubstringAtIndexes(startIndex, endIndex), foreground, background, fontStyle, theme);
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("ERROR: " + ex.Message);
        }
    }
    static void WriteToken(string text, int foreground, int background, FontStyle fontStyle, Theme theme)
    {
        if (foreground == -1)
        {
            Console.Write(text);
            return;
        }

        Decoration decoration = GetDecoration(fontStyle);

        Color backgroundColor = GetColor(background, theme);
        Color foregroundColor = GetColor(foreground, theme);

        Style style = new Style(foregroundColor, backgroundColor, decoration);
        Markup markup = new Markup(text.Replace("[", "[[").Replace("]", "]]"), style);

        AnsiConsole.Write(markup);
    }

    static Color GetColor(int colorId, Theme theme)
    {
        if (colorId == -1)
            return Color.Default;

        return HexToColor(theme.GetColor(colorId));
    }

    static Decoration GetDecoration(FontStyle fontStyle)
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

    static Color HexToColor(string hexString)
    {
        //replace # occurences
        if (hexString.IndexOf('#') != -1)
            hexString = hexString.Replace("#", "");

        byte r, g, b = 0;

        r = byte.Parse(hexString.Substring(0, 2), NumberStyles.AllowHexSpecifier);
        g = byte.Parse(hexString.Substring(2, 2), NumberStyles.AllowHexSpecifier);
        b = byte.Parse(hexString.Substring(4, 2), NumberStyles.AllowHexSpecifier);

        return new Color(r, g, b);
    }
}

internal static class StringExtensions
{
    internal static string SubstringAtIndexes(this string str, int startIndex, int endIndex)
    {
        return str.Substring(startIndex, endIndex - startIndex);
    }
}

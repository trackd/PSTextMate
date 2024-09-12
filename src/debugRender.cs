
using System;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;
using TextMateSharp.Registry;
using Spectre.Console;

// this is just for debugging purposes.

namespace PwshSpectreConsole.TextMate;

public class Debug
{
    private static string? url; // Declare 'url' as a class-level variable

    public static void RenderDebug(string[] lines, ThemeName themeName, string grammarId)
    {
        RegistryOptions options = new(themeName);
        Registry registry = new(options);
        Theme theme = registry.GetTheme();
        IGrammar grammar = registry.LoadGrammar(options.GetScopeByLanguageId(grammarId));
        IStateStack? ruleStack = null;

        foreach (string line in lines)
        {
            ITokenizeLineResult result = grammar.TokenizeLine(line, ruleStack, TimeSpan.MaxValue);

            ruleStack = result.RuleStack;

            foreach (IToken token in result.Tokens)
            {
                int startIndex = (token.StartIndex > line.Length) ?
                    line.Length : token.StartIndex;
                int endIndex = (token.EndIndex > line.Length) ?
                    line.Length : token.EndIndex;

                int foreground = -1;
                int background = -1;
                FontStyle fontStyle = FontStyle.NotSet;
                Console.WriteLine(string.Format(
                    "Tokens: token from {0} to {1} -->{2}<-- with scopes {3}, token index {4} to {5}",
                    startIndex,
                    endIndex,
                    line.SubstringAtIndexes(startIndex, endIndex),
                    string.Join(", ", token.Scopes),
                    token.StartIndex,
                    token.EndIndex
                ));

                if (token.Scopes.Contains("string.other.link.title.markdown"))
                {
                    // super hacky way to get the url..
                    url = line.SubstringAtIndexes(startIndex, endIndex);
                }
                if (token.Scopes.Contains("markup.underline.link.markdown") && url != null)
                {
                    WriteUrl(line.SubstringAtIndexes(startIndex, endIndex), url);
                    url = null!;
                }

                foreach (var themeRule in theme.Match(token.Scopes))
                {
                    if (foreground == -1 && themeRule.foreground > 0)
                        foreground = themeRule.foreground;

                    if (background == -1 && themeRule.background > 0)
                        background = themeRule.background;

                    if (fontStyle == FontStyle.NotSet && themeRule.fontStyle > 0)
                        fontStyle = themeRule.fontStyle;
                }

                WriteDebug(line.SubstringAtIndexes(startIndex, endIndex), foreground, background, fontStyle, theme);
            }

            Console.WriteLine();
        }

        var colorDictionary = theme.GetGuiColorDictionary();
        if (colorDictionary is { Count: > 0 })
        {
            Console.WriteLine("Gui Control Colors");
            foreach (var kvp in colorDictionary)
            {
                Console.WriteLine($"  {kvp.Key}, {kvp.Value}");
            }
        }
    }

    static void WriteUrl(string Title, string url)
    {
        // just highlight it in red
        string ESC = "\u001b";
        Console.Write(ESC + "[101m");
        AnsiConsole.Markup($"[link={Title}]{url}[/]");
        Console.WriteLine(ESC + "[0m");
    }

    static void WriteDebug(string text, int foreground, int background, FontStyle fontStyle, Theme theme)
    {
        // Console.WriteLine("WriteDebug: text: {0}, fg: {1}, bg: {2}, style: {3}",
        //     text, foreground, background, fontStyle);
        // if (foreground == -1)
        // {
        //     Console.Write(text);
        //     return;
        // }
        // string textEscaped = Markup.Escape(text);

        // Decoration decoration = Converter.GetDecoration(fontStyle);

        // Color backgroundColor = Converter.GetColor(background, theme);
        // Color foregroundColor = Converter.GetColor(foreground, theme);

        // Style style = new(foregroundColor, backgroundColor, decoration);
        // Markup markup = new(textEscaped, style);

        // AnsiConsole.Write(markup);
    }
}

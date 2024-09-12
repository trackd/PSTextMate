
using System;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;
using TextMateSharp.Registry;
using Spectre.Console;

// this is just for debugging purposes.

namespace PwshSpectreConsole.TextMate;

public class Debug
{
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
            for (int i = 0; i < result.Tokens.Length; i++)
            {
                IToken token = result.Tokens[i];
                if (token.Scopes.Contains("string.other.link.title.markdown"))
                {
                    // hacky way to get the url in one go.
                    // token 0 = [
                    // token 1 = linktext // we detect this
                    // token 2 = ]
                    // token 3 = (
                    // token 4 = url // check if this is a url
                    // token 5 = )
                    IToken urltoken = result.Tokens[i + 3];
                    if (urltoken.Scopes.Contains("markup.underline.link.markdown"))
                    {
                        WriteUrl(line.SubstringAtIndexes(urltoken.StartIndex, urltoken.EndIndex), line.SubstringAtIndexes(token.StartIndex, token.EndIndex));
                        // skip ahead, we dont need to parse this again.
                        i += 4;
                        continue;
                    }
                }
                int startIndex = (token.StartIndex > line.Length) ?
                    line.Length : token.StartIndex;
                int endIndex = (token.EndIndex > line.Length) ?
                    line.Length : token.EndIndex;

                int foreground = -1;
                int background = -1;
                FontStyle fontStyle = FontStyle.NotSet;
                Console.WriteLine(string.Format(
                    "Tokens:{0} token from {1} to {2} -->{3}<-- with scopes {4}",
                    i,
                    startIndex,
                    endIndex,
                    line.SubstringAtIndexes(startIndex, endIndex),
                    string.Join(", ", token.Scopes),
                    token.StartIndex,
                    token.EndIndex
                ));
                foreach (var themeRule in theme.Match(token.Scopes))
                {
                    if (foreground == -1 && themeRule.foreground > 0)
                        foreground = themeRule.foreground;

                    if (background == -1 && themeRule.background > 0)
                        background = themeRule.background;

                    if (fontStyle == FontStyle.NotSet && themeRule.fontStyle > 0)
                        fontStyle = themeRule.fontStyle;
                }
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
}

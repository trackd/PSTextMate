
using System;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;
using TextMateSharp.Registry;
using Spectre.Console;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// this is just for debugging purposes.

namespace PwshSpectreConsole.TextMate;

public static class Test
{
    public class TextMateDebug
    {
        public int[]? Index { get; set; }
        public string? Text { get; set; }
        public List<string>? Scopes { get; set; }
        public int[]? color { get; set; }
        public FontStyle FontStyle { get; set; }
        public ReadOnlyDictionary<string, string>? Theme { get; set; }
    }
    public class TokenDebug
    {
        public string? text { get; set; }
        public Style? style { get; set; }
    }

    public static TextMateDebug[]? DebugTextMate(string[] lines, ThemeName themeName, string grammarId, bool FromFile = false)
    {
        RegistryOptions options = new(themeName);
        Registry registry = new(options);
        Theme theme = registry.GetTheme();
        IGrammar grammar = null!;
        if (FromFile)
        {
            grammar = registry.LoadGrammar(options.GetScopeByExtension(grammarId));
        }
        else {
            grammar = registry.LoadGrammar(options.GetScopeByLanguageId(grammarId));
        }
        IStateStack? ruleStack = null;
        List<TextMateDebug> debugList = new();
        int lineIndex = 0;
        foreach (string line in lines)
        {
            ITokenizeLineResult result = grammar.TokenizeLine(line, ruleStack, TimeSpan.MaxValue);
            ruleStack = result.RuleStack;
            for (int i = 0; i < result.Tokens.Length; i++)
            {
                IToken token = result.Tokens[i];
                int startIndex = (token.StartIndex > line.Length) ?
                    line.Length : token.StartIndex;
                int endIndex = (token.EndIndex > line.Length) ?
                    line.Length : token.EndIndex;
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
                ReadOnlyDictionary<string, string>? colorDictionary = theme.GetGuiColorDictionary();
                int[] index = { lineIndex, startIndex, endIndex };
                int[] color = { foreground, background };
                TextMateDebug textMateDebug = new() {
                    Index = index,
                    Text = line.SubstringAtIndexes(startIndex, endIndex),
                    Scopes = token.Scopes,
                    color = color,
                    FontStyle = fontStyle,
                    Theme = colorDictionary
                };
                debugList.Add(textMateDebug);
            }
            lineIndex++;
        }
        return debugList.ToArray();
    }
    public static TokenDebug[]? DebugTextMateTokens(string[] lines, ThemeName themeName, string grammarId, bool FromFile = false)
    {
        RegistryOptions options = new(themeName);
        Registry registry = new(options);
        Theme theme = registry.GetTheme();
        IGrammar grammar = null!;
        if (FromFile)
        {
            grammar = registry.LoadGrammar(options.GetScopeByExtension(grammarId));
        }
        else
        {
            grammar = registry.LoadGrammar(options.GetScopeByLanguageId(grammarId));
        }
        IStateStack? ruleStack = null;
        List<TokenDebug> tokenDebug = new();

        foreach (string line in lines)
        {
            ITokenizeLineResult result = grammar.TokenizeLine(line, ruleStack, TimeSpan.MaxValue);
            ruleStack = result.RuleStack;
            for (int i = 0; i < result.Tokens.Length; i++)
            {
                IToken token = result.Tokens[i];
                int startIndex = (token.StartIndex > line.Length) ?
                    line.Length : token.StartIndex;
                int endIndex = (token.EndIndex > line.Length) ?
                    line.Length : token.EndIndex;

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
                var (textEscaped, style) = Converter.WriteToken(line.SubstringAtIndexes(startIndex, endIndex), foreground, background, fontStyle, theme);
                TokenDebug tokenDebugItem = new() {
                    text = textEscaped,
                    style = style
                };
                tokenDebug.Add(tokenDebugItem);
            }
        }
        return tokenDebug.ToArray();
    }
}

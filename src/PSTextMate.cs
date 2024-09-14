using System;
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
        RegistryOptions options = new(themeName);
        Registry registry = new(options);
        Theme theme = registry.GetTheme();
        IGrammar grammar = registry.LoadGrammar(options.GetScopeByLanguageId(grammarId));
        if (grammar == null)
        {
            throw new Exception("Grammar not found for language: " + grammarId);
        }
        if (grammar.GetName() == "Markdown")
        {
            return RenderMarkdown(lines, theme, grammar);
        }
        return Render(lines, theme, grammar);
    }

    public static Rows? ReadFile(string fullName, ThemeName themeName, string Extension)
    {
        string[] lines = File.ReadAllLines(fullName);
        RegistryOptions options = new(themeName);
        Registry registry = new(options);
        Theme theme = registry.GetTheme();
        IGrammar grammar = registry.LoadGrammar(options.GetScopeByExtension(Extension));
        if (grammar == null)
        {
            throw new Exception("Grammar not found for extension: " + Extension);
        }
        if (grammar.GetName() == "Markdown")
        {
            return RenderMarkdown(lines, theme, grammar);
        }
        return Render(lines, theme, grammar);
    }
    // specialcase markdown for spectre link rendering.. maybe more in the future..
    // prefer to do this with TextMate grammar, need to check if that is possible.
    internal static Rows? RenderMarkdown(string[] String, Theme theme, IGrammar grammar)
    {
        StringBuilder builder = new();
        List<IRenderable> rows = new();
        string? url = null!;
        string? title = null!;
        try
        {
            IStateStack? ruleStack = null;
            foreach (string line in String)
            {
                ITokenizeLineResult result = grammar.TokenizeLine(line, ruleStack, TimeSpan.MaxValue);
                ruleStack = result.RuleStack;
                for (int i = 0; i < result.Tokens.Length; i++)
                {
                    IToken token = result.Tokens[i];
                    if (token.Scopes.Contains("meta.link.inline.markdown"))
                    {
                        i++; // first token should just be a bracket
                        while (i < result.Tokens.Length && result.Tokens[i].Scopes.Contains("meta.link.inline.markdown"))
                        {
                            // while loop is a bit hacky, but if someone has multiple links back to back.. it should work.
                            if (result.Tokens[i].Scopes.Contains("string.other.link.title.markdown"))
                            {
                                title = line.SubstringAtIndexes(result.Tokens[i].StartIndex, result.Tokens[i].EndIndex);
                            }
                            if (result.Tokens[i].Scopes.Contains("markup.underline.link.markdown"))
                            {
                                url = line.SubstringAtIndexes(result.Tokens[i].StartIndex, result.Tokens[i].EndIndex);
                            }
                            if (title != null && url != null)
                            {
                                (string _text, Style _style) =  WriteMarkdownLinkWStyle(url, title);
                                builder.AppendWithStyleN(_style, _text);
                                title = null;
                                url = null;
                            }
                            i++;
                        }
                        continue;
                    }
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

    internal static Rows? Render(string[] String, Theme theme, IGrammar grammar)
    {
        StringBuilder builder = new();
        List<IRenderable> rows = new();
        try
        {
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
        Style style = new(foregroundColor, backgroundColor, decoration);
        return (textEscaped, style);
    }
    internal static string WriteMarkdownLink(string url, string linkText)
    {
        // string EscapedText = Markup.Escape(linkText);
        string mdlink = $"[link={url}]{linkText}[/]";
        // Console.WriteLine(mdlink);
        return mdlink;
    }
    internal static (string textEscaped, Style style) WriteMarkdownLinkWStyle(string url, string linkText)
    {
        string mdlink = $"[link={url}]{Markup.Escape(linkText)}[/]";
        Style style = new(Color.Blue, Color.Default);
        return (mdlink, style);
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
            hexString = hexString[1..];
        }

        var c = Convert.FromHexString(hexString);
        return new Color(c[0], c[1], c[2]);
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

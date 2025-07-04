using Markdig.Syntax;
using Spectre.Console;
using Spectre.Console.Rendering;
using PwshSpectreConsole.TextMate.Extensions;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Renders markdown code blocks with syntax highlighting.
/// </summary>
internal static class CodeBlockRenderer
{
    /// <summary>
    /// Renders a fenced code block with syntax highlighting when possible.
    /// </summary>
    /// <param name="fencedCode">The fenced code block to render</param>
    /// <param name="theme">Theme for styling</param>
    /// <param name="themeName">Theme name for TextMateProcessor</param>
    /// <returns>Rendered code block in a panel</returns>
    public static IRenderable RenderFencedCodeBlock(FencedCodeBlock fencedCode, Theme theme, ThemeName themeName)
    {
        var codeLines = ExtractCodeLines(fencedCode.Lines);
        var language = (fencedCode.Info ?? string.Empty).Trim();

        if (!string.IsNullOrEmpty(language))
        {
            try
            {
                var rows = TextMateProcessor.ProcessLinesCodeBlock(codeLines.ToArray(), themeName, language, false);
                if (rows is not null)
                {
                    return new Panel(rows)
                        .Border(BoxBorder.Rounded)
                        .Header(language, Justify.Left);
                }
            }
            catch
            {
                // Fallback to plain rendering below
            }
        }

        // Fallback: plain code panel (escape text to avoid Spectre markup errors)
        return CreateFallbackCodePanel(codeLines, language, theme);
    }

    /// <summary>
    /// Renders an indented code block (no specific language).
    /// </summary>
    /// <param name="code">The code block to render</param>
    /// <param name="theme">Theme for styling</param>
    /// <returns>Rendered code block in a panel</returns>
    public static IRenderable RenderCodeBlock(CodeBlock code, Theme theme)
    {
        var codeText = Markup.Escape(code.Lines.ToString());
        var codeStyle = new Style(foreground: Color.Grey, background: Color.Black);

        return new Panel(new Markup(codeText, codeStyle))
            .Border(BoxBorder.Rounded)
            .Header("code", Justify.Left);
    }

    /// <summary>
    /// Extracts code lines from a code block's line collection.
    /// </summary>
    private static List<string> ExtractCodeLines(Markdig.Helpers.StringLineGroup lines)
    {
        var codeLines = new List<string>();

        foreach (var line in lines.Lines)
        {
            var slice = line.Slice;
            codeLines.Add(slice.ToString());
        }

        return codeLines;
    }

    /// <summary>
    /// Creates a fallback code panel when syntax highlighting fails.
    /// </summary>
    private static Panel CreateFallbackCodePanel(List<string> codeLines, string language, Theme theme)
    {
        var fallbackText = Markup.Escape(string.Join("\n", codeLines));
        var fallbackStyle = theme.ToSpectreStyle();
        var headerText = !string.IsNullOrEmpty(language) ? language : "code";

        return new Panel(new Markup(fallbackText, fallbackStyle))
            .Border(BoxBorder.Rounded)
            .Header(headerText, Justify.Left);
    }
}

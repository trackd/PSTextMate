using System.Text;
using Markdig.Syntax;
using Spectre.Console;
using Spectre.Console.Rendering;
using PwshSpectreConsole.TextMate.Extensions;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Renders markdown heading blocks.
/// </summary>
internal static class HeadingRenderer
{
    /// <summary>
    /// Renders a heading block with theme-aware styling.
    /// </summary>
    /// <param name="heading">The heading block to render</param>
    /// <param name="theme">Theme for styling</param>
    /// <returns>Rendered heading markup</returns>
    public static IRenderable Render(HeadingBlock heading, Theme theme)
    {
        var headingScopes = MarkdigTextMateScopeMapper.GetBlockScopes("Heading", heading.Level);
        var (hfg, hbg, hfs) = TokenProcessor.ExtractThemeProperties(new MarkdownToken(headingScopes), theme);

        var headingBuilder = new StringBuilder();
        InlineProcessor.ExtractInlineText(heading.Inline, theme, headingBuilder);

        // Apply the theme colors/style to the heading
        if (hfg != -1 || hbg != -1 || hfs != TextMateSharp.Themes.FontStyle.NotSet)
        {
            var headingColor = hfg != -1 ? StyleHelper.GetColor(hfg, theme) : Color.Default;
            var headingBgColor = hbg != -1 ? StyleHelper.GetColor(hbg, theme) : Color.Default;
            var headingDecoration = StyleHelper.GetDecoration(hfs);

            var headingStyle = new Style(headingColor, headingBgColor, headingDecoration);
            var styledBuilder = new StringBuilder();
            styledBuilder.AppendWithStyle(headingStyle, headingBuilder.ToString());
            return new Markup(styledBuilder.ToString());
        }
        else
        {
            return new Markup(headingBuilder.ToString().EscapeMarkup());
        }
    }
}

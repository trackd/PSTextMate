using System.Text;
using Markdig.Syntax;
using Spectre.Console;
using Spectre.Console.Rendering;
using PwshSpectreConsole.TextMate.Extensions;
using PwshSpectreConsole.TextMate.Core.Markdown.Types;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Enhanced heading renderer with improved error handling and performance optimizations.
/// </summary>
internal static class EnhancedHeadingRenderer
{
    private const int MaxHeadingLevel = 6;
    private const int MinHeadingLevel = 1;

    /// <summary>
    /// Renders a heading block with comprehensive error handling and validation.
    /// </summary>
    /// <param name="heading">The heading block to render</param>
    /// <param name="options">Render options with theme and configuration</param>
    /// <returns>A render result indicating success or failure</returns>
    public static MarkdownRenderResult Render(HeadingBlock heading, MarkdownRenderOptions options)
    {
        try
        {
            options.Validate();

            if (heading is null)
                return MarkdownRenderResult.CreateFailure("Heading block cannot be null", MarkdownBlockType.Heading);

            if (heading.Level < MinHeadingLevel || heading.Level > MaxHeadingLevel)
            {
                return MarkdownRenderResult.CreateFailure(
                    $"Heading level {heading.Level} is not supported. Must be between {MinHeadingLevel} and {MaxHeadingLevel}",
                    MarkdownBlockType.Heading);
            }

            Markup renderable = RenderHeadingInternal(heading, options);
            return MarkdownRenderResult.CreateSuccess(renderable, MarkdownBlockType.Heading);
        }
        catch (Exception ex)
        {
            return MarkdownRenderResult.CreateFailure(
                $"Failed to render heading: {ex.Message}",
                MarkdownBlockType.Heading);
        }
    }

    private static Markup RenderHeadingInternal(HeadingBlock heading, MarkdownRenderOptions options)
    {
        string[] headingScopes = MarkdigTextMateScopeMapper.GetBlockScopes("Heading", heading.Level);
        (int hfg, int hbg, FontStyle hfs) = TokenProcessor.ExtractThemeProperties(new MarkdownToken(headingScopes), options.Theme);

        // Pre-allocate StringBuilder with estimated capacity for better performance
        var headingBuilder = new StringBuilder(capacity: 256);
        var context = new InlineRenderContext { Theme = options.Theme };

        InlineProcessor.ExtractInlineText(heading.Inline, context.Theme, headingBuilder);

        // Apply theme styling if available
        if (hfg != -1 || hbg != -1 || hfs != TextMateSharp.Themes.FontStyle.NotSet)
        {
            Color headingColor = hfg != -1 ? StyleHelper.GetColor(hfg, options.Theme) : Color.Default;
            Color headingBgColor = hbg != -1 ? StyleHelper.GetColor(hbg, options.Theme) : Color.Default;
            Decoration headingDecoration = StyleHelper.GetDecoration(hfs);

            var headingStyle = new Style(headingColor, headingBgColor, headingDecoration);
            var styledBuilder = new StringBuilder(capacity: headingBuilder.Length + 50);
            styledBuilder.AppendWithStyle(headingStyle, headingBuilder.ToString());
            return new Markup(styledBuilder.ToString());
        }
        else
        {
            return new Markup(headingBuilder.ToString().EscapeMarkup());
        }
    }
}

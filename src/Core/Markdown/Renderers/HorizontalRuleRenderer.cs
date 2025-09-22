using Spectre.Console;
using Spectre.Console.Rendering;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Renders markdown horizontal rules (thematic breaks).
/// </summary>
internal static class HorizontalRuleRenderer
{
    /// <summary>
    /// Renders a horizontal rule as a styled line.
    /// </summary>
    /// <returns>Rendered horizontal rule</returns>
    public static IRenderable Render()
    {
        return new Rule().RuleStyle(Style.Parse("grey"));
    }
}

using System.Text;
using Markdig.Syntax;
using Spectre.Console;
using Spectre.Console.Rendering;
using TextMateSharp.Themes;
using PwshSpectreConsole.TextMate.Helpers;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Renders markdown quote blocks.
/// </summary>
internal static class QuoteRenderer {
    /// <summary>
    /// Renders a quote block with a bordered panel.
    /// </summary>
    /// <param name="quote">The quote block to render</param>
    /// <param name="theme">Theme for styling</param>
    /// <returns>Rendered quote in a bordered panel</returns>
    public static IRenderable Render(QuoteBlock quote, Theme theme) {
        string quoteText = ExtractQuoteText(quote, theme);

        return new Panel(new Markup(Markup.Escape(quoteText)))
            .Border(BoxBorder.Heavy)
            .Header("quote", Justify.Left);
    }

    /// <summary>
    /// Extracts text content from all blocks within the quote.
    /// </summary>
    private static string ExtractQuoteText(QuoteBlock quote, Theme theme) {
        string quoteText = string.Empty;
        bool isFirstParagraph = true;

        foreach (Block subBlock in quote) {
            if (subBlock is ParagraphBlock para) {
                // Add newline between multiple paragraphs
                if (!isFirstParagraph)
                    quoteText += "\n";

                StringBuilder quoteBuilder = StringBuilderPool.Rent();
                try {
                    InlineProcessor.ExtractInlineText(para.Inline, theme, quoteBuilder);
                    quoteText += quoteBuilder.ToString();
                }
                finally {
                    StringBuilderPool.Return(quoteBuilder);
                }

                isFirstParagraph = false;
            }
            else {
                quoteText += subBlock.ToString();
            }
        }

        // Trim trailing whitespace/newlines
        quoteText = quoteText.TrimEnd();

        return quoteText;
    }
}

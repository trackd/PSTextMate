using Spectre.Console.Rendering;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Types;

/// <summary>
/// Represents the result of rendering a markdown block element.
/// Provides type safety and better error handling for rendering operations.
/// </summary>
public sealed record MarkdownRenderResult
{
    /// <summary>
    /// The rendered element that can be displayed by Spectre.Console.
    /// </summary>
    public IRenderable? Renderable { get; init; }

    /// <summary>
    /// Indicates whether the rendering was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if rendering failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The type of markdown block that was processed.
    /// </summary>
    public MarkdownBlockType BlockType { get; init; }

    /// <summary>
    /// Creates a successful render result.
    /// </summary>
    public static MarkdownRenderResult CreateSuccess(IRenderable renderable, MarkdownBlockType blockType) =>
        new() { Renderable = renderable, Success = true, BlockType = blockType };

    /// <summary>
    /// Creates a failed render result.
    /// </summary>
    public static MarkdownRenderResult CreateFailure(string errorMessage, MarkdownBlockType blockType) =>
        new() { Success = false, ErrorMessage = errorMessage, BlockType = blockType };

    /// <summary>
    /// Creates a result for unsupported block types.
    /// </summary>
    public static MarkdownRenderResult CreateUnsupported(MarkdownBlockType blockType) =>
        new() { Success = false, ErrorMessage = $"Block type '{blockType}' is not supported", BlockType = blockType };
}

/// <summary>
/// Enumeration of supported markdown block types for better type safety.
/// </summary>
public enum MarkdownBlockType
{
    Unknown,
    Heading,
    Paragraph,
    List,
    FencedCodeBlock,
    CodeBlock,
    Table,
    Quote,
    HtmlBlock,
    ThematicBreak,
    TaskList
}

/// <summary>
/// Configuration options for markdown rendering with validation.
/// </summary>
public sealed record MarkdownRenderOptions
{
    /// <summary>
    /// The theme to use for rendering.
    /// </summary>
    public required Theme Theme { get; init; }

    /// <summary>
    /// The theme name for TextMate processing.
    /// </summary>
    public required ThemeName ThemeName { get; init; }

    /// <summary>
    /// Whether to enable debug output.
    /// </summary>
    public bool EnableDebug { get; init; }

    /// <summary>
    /// Maximum rendering depth to prevent stack overflow.
    /// </summary>
    public int MaxRenderingDepth { get; init; } = 100;

    /// <summary>
    /// Whether to add spacing between block elements.
    /// </summary>
    public bool AddBlockSpacing { get; init; } = true;

    /// <summary>
    /// Validates the render options.
    /// </summary>
    public void Validate()
    {
        if (MaxRenderingDepth <= 0)
            throw new ArgumentException("MaxRenderingDepth must be greater than 0", nameof(MaxRenderingDepth));
    }
}

/// <summary>
/// Represents inline rendering context with type safety.
/// </summary>
public sealed record InlineRenderContext
{
    /// <summary>
    /// The theme for styling.
    /// </summary>
    public required Theme Theme { get; init; }

    /// <summary>
    /// Current nesting depth.
    /// </summary>
    public int Depth { get; init; }

    /// <summary>
    /// Whether markup escaping is enabled.
    /// </summary>
    public bool EscapeMarkup { get; init; } = true;

    /// <summary>
    /// Creates a new context with incremented depth.
    /// </summary>
    public InlineRenderContext WithIncrementedDepth() => this with { Depth = Depth + 1 };

    /// <summary>
    /// Creates a new context with disabled markup escaping.
    /// </summary>
    public InlineRenderContext WithoutMarkupEscaping() => this with { EscapeMarkup = false };
}

using Markdig;

namespace PwshSpectreConsole.TextMate.Core.Markdown;

/// <summary>
/// Provides reusable, pre-configured Markdown pipelines.
/// Pipelines are expensive to create (plugin registration), so they're cached statically.
/// </summary>
internal static class MarkdownPipelines {
    /// <summary>
    /// Standard pipeline with all common extensions enabled.
    /// Suitable for most Markdown rendering tasks.
    /// </summary>
    public static readonly MarkdownPipeline Standard = BuildStandardPipeline();

    private static MarkdownPipeline BuildStandardPipeline() {
        return new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UsePipeTables()
            .UseEmphasisExtras()
            .UseAutoLinks()
            .UseTaskLists()
            .EnableTrackTrivia()
            .Build();
    }
}

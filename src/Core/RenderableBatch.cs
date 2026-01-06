using Spectre.Console;
using Spectre.Console.Rendering;

namespace PwshSpectreConsole.TextMate.Core;

/// <summary>
/// A batch container for Spectre.Console renderables used for streaming output in multiple chunks.
/// </summary>
public sealed class RenderableBatch(IRenderable[] renderables, int batchIndex = 0, long fileOffset = 0) : IRenderable {
    /// <summary>
    /// Array of renderables that comprise this batch.
    /// </summary>
    public IRenderable[] Renderables { get; } = renderables ?? [];

    /// <summary>
    /// Zero-based batch index for ordering when streaming.
    /// </summary>
    public int BatchIndex { get; } = batchIndex;

    /// <summary>
    /// Zero-based file offset (starting line number) for this batch.
    /// </summary>
    public long FileOffset { get; } = fileOffset;

    /// <summary>
    /// Number of rendered lines (rows) in this batch.
    /// </summary>
    public int LineCount => Renderables?.Length ?? 0;

    /// <summary>
    /// Renders all contained renderables as segments for Spectre.Console output.
    /// </summary>
    /// <param name="options">Render options specifying terminal constraints</param>
    /// <param name="maxWidth">Maximum width available for rendering</param>
    /// <returns>Enumerable of render segments from all renderables in this batch</returns>
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) {
        foreach (IRenderable r in Renderables) {
            foreach (Segment s in r.Render(options, maxWidth))
                yield return s;
        }
    }

    /// <summary>
    /// Measures the rendering dimensions of all renderables in this batch.
    /// </summary>
    /// <param name="options">Render options specifying terminal constraints</param>
    /// <param name="maxWidth">Maximum width available for measurement</param>
    /// <returns>Measurement indicating minimum and maximum width needed</returns>
    public Measurement Measure(RenderOptions options, int maxWidth) =>
        // Return a conservative, permissive measurement: min = 0, max = maxWidth.
        // This avoids depending on concrete Measurement properties across Spectre.Console versions.
        new(0, maxWidth);

    /// <summary>
    /// Converts this batch to a Spectre.Console Rows object for rendering.
    /// </summary>
    /// <returns>Spectre.Console Rows containing all renderables from this batch</returns>
    public Spectre.Console.Rows ToSpectreRows() => new(Renderables);
}

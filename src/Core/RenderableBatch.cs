using Spectre.Console.Rendering;
using Spectre.Console;

namespace PwshSpectreConsole.TextMate.Core;

public sealed class RenderableBatch : IRenderable
{
    public IRenderable[] Renderables { get; }

    /// <summary>
    /// Zero-based batch index for ordering when streaming.
    /// </summary>
    public int BatchIndex { get; }

    /// <summary>
    /// Zero-based file offset (starting line number) for this batch.
    /// </summary>
    public long FileOffset { get; }

    /// <summary>
    /// Number of rendered lines (rows) in this batch.
    /// </summary>
    public int LineCount => Renderables?.Length ?? 0;

    public RenderableBatch(IRenderable[] renderables, int batchIndex = 0, long fileOffset = 0)
    {
        Renderables = renderables ?? [];
        BatchIndex = batchIndex;
        FileOffset = fileOffset;
    }

    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        foreach (IRenderable r in Renderables)
        {
            foreach (Segment s in r.Render(options, maxWidth))
                yield return s;
        }
    }

    public Measurement Measure(RenderOptions options, int maxWidth)
    {
        // Return a conservative, permissive measurement: min = 0, max = maxWidth.
        // This avoids depending on concrete Measurement properties across Spectre.Console versions.
        return new Measurement(0, maxWidth);
    }

    public Spectre.Console.Rows ToSpectreRows() => new(Renderables);
}

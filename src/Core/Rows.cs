using Spectre.Console.Rendering;

namespace PwshSpectreConsole.TextMate.Core;

/// <summary>
/// Container for rendered rows returned by renderers.
/// </summary>
public sealed record Rows(IRenderable[] Renderables) {
    /// <summary>
    /// Returns an empty Rows container with no renderables.
    /// </summary>
    public static Rows Empty { get; } = new Rows([]);
}

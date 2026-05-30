namespace PSTextMate.Terminal;

internal readonly record struct PagerViewportWindow(int Top, int Count, int EndExclusive, bool HasImages);

internal sealed class PagerViewportEngine {
    private readonly IReadOnlyList<IRenderable> _renderables;
    private readonly HighlightedText? _sourceHighlightedText;
    private bool _containsImages;
    private List<int> _renderableHeights = [];
    private int _lastWidth = -1;
    private int _lastContentRows = -1;
    private int _lastWindowHeight = -1;
    private int _lastRenderableCount = -1;

    public PagerViewportEngine(IReadOnlyList<IRenderable> renderables, HighlightedText? sourceHighlightedText) {
        _renderables = renderables ?? throw new ArgumentNullException(nameof(renderables));
        _sourceHighlightedText = sourceHighlightedText;
        _containsImages = renderables.Any(IsImageRenderable);
    }

    public void NoteRenderableAppended(IRenderable renderable) {
        ArgumentNullException.ThrowIfNull(renderable);

        if (IsImageRenderable(renderable)) {
            _containsImages = true;
        }
    }

    public void RecalculateHeights(int width, int contentRows, int windowHeight, IAnsiConsole console) {
        ArgumentNullException.ThrowIfNull(console);

        bool layoutAffectsMeasurement = _containsImages;
        if (_renderableHeights.Count == _renderables.Count
            && _lastWidth == width
            && (!layoutAffectsMeasurement || (_lastContentRows == contentRows && _lastWindowHeight == windowHeight))
            && _lastRenderableCount == _renderables.Count) {
            return;
        }

        _renderableHeights = new List<int>(_renderables.Count);
        Capabilities capabilities = console.Profile.Capabilities;
        int measurementHeight = windowHeight > 0 ? windowHeight : Math.Max(1, contentRows + 3);
        int contentWidth = GetRenderableContentWidth(width);
        var size = new Size(contentWidth, measurementHeight);
        var options = new RenderOptions(capabilities, size);

        for (int i = 0; i < _renderables.Count; i++) {
            IRenderable? renderable = _renderables[i];
            if (renderable is null) {
                _renderableHeights.Add(1);
                continue;
            }

            if (IsImageRenderable(renderable)) {
                if (renderable is PixelImage pixelImage) {
                    // In pager mode, clamp image width and height to the viewport so the sixel
                    // payload stays within screen bounds and does not overflow content rows.
                    pixelImage.MaxWidth = pixelImage.MaxWidth is int existingWidth && existingWidth > 0
                        ? Math.Min(existingWidth, width)
                        : width;
                    pixelImage.MaxHeight = Math.Max(1, contentRows / 3);
                }

                _renderableHeights.Add(EstimateImageHeight(renderable, width, contentRows, options));
                continue;
            }

            try {
                int lines = CountRenderedLines(renderable, options, contentWidth);
                _renderableHeights.Add(Math.Max(1, lines));
            }
            catch (InvalidOperationException) {
                _renderableHeights.Add(EstimateRenderableHeight(renderable, options, contentWidth));
            }
            catch (IOException) {
                _renderableHeights.Add(EstimateRenderableHeight(renderable, options, contentWidth));
            }
        }

        _lastWidth = width;
        _lastContentRows = contentRows;
        _lastWindowHeight = windowHeight;
        _lastRenderableCount = _renderables.Count;
    }

    public PagerViewportWindow BuildViewport(int proposedTop, int contentRows) {
        if (_renderables.Count == 0) {
            return new PagerViewportWindow(0, 0, 0, false);
        }

        int clampedTop = Math.Clamp(proposedTop, 0, _renderables.Count - 1);
        int rowsUsed = 0;
        int count = 0;
        bool hasImages = false;

        for (int i = clampedTop; i < _renderables.Count; i++) {
            bool isImage = IsImageRenderable(_renderables[i]);
            int height = Math.Clamp(GetRenderableHeight(i), 1, contentRows);

            if (count > 0 && rowsUsed + height > contentRows) {
                break;
            }

            rowsUsed += height;
            count++;
            hasImages |= isImage;

            if (rowsUsed >= contentRows) {
                break;
            }
        }

        if (count == 0) {
            count = 1;
            hasImages = IsImageRenderable(_renderables[clampedTop]);
        }

        return new PagerViewportWindow(clampedTop, count, clampedTop + count, hasImages);
    }

    public int GetMaxTop(int contentRows) {
        if (_renderables.Count == 0) {
            return 0;
        }

        int top = _renderables.Count - 1;
        int rows = Math.Clamp(GetRenderableHeight(top), 1, contentRows);

        while (top > 0) {
            int previousHeight = Math.Clamp(GetRenderableHeight(top - 1), 1, contentRows);
            if (rows + previousHeight > contentRows) {
                break;
            }

            rows += previousHeight;
            top--;
        }

        return top;
    }

    public int ScrollTop(int currentTop, int delta, int contentRows) {
        if (_renderables.Count == 0) {
            return currentTop;
        }

        int direction = Math.Sign(delta);
        if (direction == 0) {
            return currentTop;
        }

        int maxTop = GetMaxTop(Math.Max(1, contentRows));
        return Math.Clamp(currentTop + direction, 0, maxTop);
    }

    public int PageDownTop(int currentTop, int contentRows) {
        if (_renderables.Count == 0) {
            return currentTop;
        }

        PagerViewportWindow viewport = BuildViewport(currentTop, contentRows);
        int maxTop = GetMaxTop(contentRows);
        return viewport.EndExclusive >= _renderables.Count ? maxTop : Math.Min(viewport.EndExclusive, maxTop);
    }

    public int PageUpTop(int currentTop, int contentRows) {
        if (_renderables.Count == 0) {
            return currentTop;
        }

        int rowsSkipped = 0;
        int idx = currentTop - 1;
        int nextTop = currentTop;
        while (idx >= 0 && rowsSkipped < contentRows) {
            rowsSkipped += Math.Clamp(GetRenderableHeight(idx), 1, contentRows);
            nextTop = idx;
            idx--;
        }

        return Math.Clamp(nextTop, 0, _renderables.Count - 1);
    }

    public int GetRenderableHeightAt(int index)
        => GetRenderableHeightCore(index);

    private int GetRenderableHeight(int index)
        => GetRenderableHeightCore(index);

    private int GetRenderableHeightCore(int index)
        => index < 0 || index >= _renderableHeights.Count ? 1 : Math.Max(1, _renderableHeights[index]);

    private bool IsImageRenderable(IRenderable? renderable) {
        if (renderable is null) {
            return false;
        }

        if (_sourceHighlightedText is not null && !IsMarkdownSource()) {
            return false;
        }

        string name = renderable.GetType().Name;
        return name.Contains("Sixel", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Pixel", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Image", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsMarkdownSource() {
        if (_sourceHighlightedText is null) {
            return false;
        }

        string language = _sourceHighlightedText.Language;
        return language.Contains("markdown", StringComparison.OrdinalIgnoreCase)
            || language.Equals(".md", StringComparison.OrdinalIgnoreCase)
            || language.Equals(".markdown", StringComparison.OrdinalIgnoreCase)
            || language.Equals(".mdown", StringComparison.OrdinalIgnoreCase);
    }

    private int GetRenderableContentWidth(int width) {
        int availableWidth = Math.Max(1, width);
        if (_sourceHighlightedText is null || !_sourceHighlightedText.ShowLineNumbers) {
            return availableWidth;
        }

        int lineNumberWidth = ResolveLineNumberWidth();
        int gutterWidth = lineNumberWidth + _sourceHighlightedText.GutterSeparator.Length;
        return Math.Max(1, availableWidth - gutterWidth);
    }

    private int ResolveLineNumberWidth() {
        if (_sourceHighlightedText is null) {
            return 0;
        }

        if (_sourceHighlightedText.LineNumberWidth is int explicitWidth && explicitWidth > 0) {
            return explicitWidth;
        }

        int lastLineNumber = _sourceHighlightedText.LineNumberStart + Math.Max(0, _renderables.Count - 1);
        return lastLineNumber.ToString(CultureInfo.InvariantCulture).Length;
    }

    private static int CountRenderedLines(IRenderable renderable, RenderOptions options, int width) {
        List<SegmentLine> lines = Segment.SplitLines(renderable.Render(options, width), Math.Max(1, width));
        return Math.Max(1, lines.Count);
    }

    private static int EstimateRenderableHeight(IRenderable renderable, RenderOptions options, int width) {
        try {
            Measurement measurement = renderable.Measure(options, width);
            int measuredWidth = Math.Max(1, measurement.Max);
            return Math.Max(1, (int)Math.Ceiling((double)measuredWidth / Math.Max(1, width)));
        }
        catch (InvalidOperationException) {
            return 1;
        }
        catch (IOException) {
            return 1;
        }
    }

    private static int EstimateImageHeight(IRenderable renderable, int width, int contentRows, RenderOptions options) {
        if (renderable is PixelImage pixelImage) {
            int imagePixelWidth = pixelImage.Width;
            int imagePixelHeight = pixelImage.Height;
            int cellWidth = pixelImage.MaxWidth is int maxWidth && maxWidth > 0
                ? Math.Min(width, maxWidth)
                : width;

            if (imagePixelWidth > 0 && imagePixelHeight > 0) {
                double imageAspect = (double)imagePixelHeight / imagePixelWidth;
                double cellAspectRatio = GetTerminalCellAspectRatio();
                int estimatedRows = (int)Math.Ceiling(imageAspect * Math.Max(1, cellWidth) * cellAspectRatio);
                int maxHeight = pixelImage.MaxHeight ?? contentRows;
                return Math.Clamp(Math.Max(1, estimatedRows), 1, maxHeight);
            }
        }

        Measurement measure;
        try {
            measure = renderable.Measure(options, width);
        }
        catch (InvalidOperationException) {
            return Math.Clamp(contentRows, 1, contentRows);
        }
        catch (IOException) {
            return Math.Clamp(contentRows, 1, contentRows);
        }

        int cellWidthFallback = Math.Max(1, Math.Min(width, measure.Max));

        // Last fallback: keep as atomic item, but estimate from measured width.
        return Math.Clamp(Math.Max(1, (int)Math.Ceiling((double)cellWidthFallback / Math.Max(1, width))), 1, contentRows);
    }

    private static double GetTerminalCellAspectRatio() {
        CellSize cellSize = Compatibility.GetCellSize();
        return cellSize.PixelWidth <= 0 || cellSize.PixelHeight <= 0
            ? 0.5d
            : (double)cellSize.PixelWidth / cellSize.PixelHeight;
    }
}

namespace PSTextMate.Sixel;

/// <summary>
/// Represents a renderable image, with pixel rendering (ie sub-cell).
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PixelImage"/> class.
/// </remarks>
internal sealed class PixelImage : IRenderable {
    private const char ESC = '\u001b';
    /// <summary>
    /// Gets the image width in pixels.
    /// </summary>
    public int Width => Image.Width;

    /// <summary>
    /// Gets the image height in pixels.
    /// </summary>
    public int Height => Image.Height;

    /// <summary>
    /// Gets or sets the render width of the canvas in terminal cells.
    /// </summary>
    public int? MaxWidth { get; set; }

    /// <summary>
    /// Gets or sets the maximum render height in terminal cells.
    /// When set, the image will be scaled down to fit within this height.
    /// Takes precedence over the automatic terminal-height clamping.
    /// </summary>
    public int? MaxHeight { get; set; }

    /// <summary>
    /// Gets the render width of the canvas. This is hard coded to 1 for sixel images.
    /// </summary>
    public int PixelWidth { get; } = 1;

    /// <summary>
    /// Gets a value indicating whether the image should be animated.
    /// </summary>
    public bool AnimationDisabled { get; init; }

    /// <summary>
    /// Gets or sets the current frame of the image.
    /// </summary>
    public int FrameToRender {
        get => _frameToRender;
        set {
            if (value < 0) {
                throw new InvalidOperationException("Frame to render must be greater than zero.");
            }

            if (value >= Image.Frames.Count) {
                throw new InvalidOperationException("Frame to render must be less than the total number of frames in the image.");
            }

            _frameToRender = value;
        }
    }

    internal SixLabors.ImageSharp.Image<Rgba32> Image { get; private set; }
    private readonly Dictionary<(int width, int height), Sixel> _cachedSixels = [];
    private int _frameToRender;

    public PixelImage(string filename, bool animationDisabled = false) {
        AnimationDisabled = animationDisabled;
        Image = SixImage.Load<Rgba32>(filename);
    }

    /// <inheritdoc/>
    public Measurement Measure(RenderOptions options, int maxWidth) {
        if (PixelWidth < 0) {
            throw new InvalidOperationException("Pixel width must be greater than zero.");
        }

        int width = MaxWidth ?? Width;
        return maxWidth < width * PixelWidth ? new Measurement(maxWidth, maxWidth) : new Measurement(width * PixelWidth, width * PixelWidth);
    }

    /// <inheritdoc/>
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) {
        // Got a max width smaller than the render max width?
        // When MaxWidth is explicitly set by the user, use it and don't constrain height.
        // When MaxWidth is not set, constrain the image to the terminal height so tall images
        // don't cause sixel scrolling artifacts.
        if (MaxWidth != null && MaxWidth < maxWidth) {
            maxWidth = MaxWidth.Value;
        }

        // Determine height constraint: explicit MaxHeight takes precedence, then fall back
        // to 1/3 of the terminal height so images don't dominate the screen by default.
        int? maxCellHeight = MaxHeight;
        if (maxCellHeight is null) {
            int terminalHeight = Compatibility.GetTerminalHeight();
            if (terminalHeight > 0) {
                maxCellHeight = Math.Max(1, terminalHeight / 3);
            }
        }

        // Write the sixel data as a control segment.
        // Parsing is expensive, cache the result for the current width + height combination.
        // Height also affects sixel output (aspect-ratio scaling), so both dimensions are part of the key.
        (int maxWidth, int) cacheKey = (maxWidth, maxCellHeight ?? -1);
        if (!_cachedSixels.TryGetValue(cacheKey, out Sixel sixel)) {
            sixel = SixelRender.ImageToSixel(Image, maxWidth, AnimationDisabled, maxCellHeight);
            _cachedSixels.Add(cacheKey, sixel);
        }

        // Draw a transparent renderable to take up the space the sixel is drawn in.
        // This allows Spectre.Console to render the image and not write overtop of it with space characters while padding panel borders etc.
        var canvas = new ImageCanvas(sixel.CellWidth, sixel.CellHeight) {
            MaxWidth = sixel.CellWidth,
            PixelWidth = PixelWidth,
            Scale = false,
        };

        // The segment list is a transparent canvas followed by a couple of zero-width control segments for sixel data output.
        // Rendering the sixel data after the canvas allows the canvas to be truncated in a layout without destroying the layout.
        var segments = canvas.Render(options, maxWidth).ToList();

        // Remove the final line break from the canvas so the sixel data can be rendered relative to the top left of the canvas.
        // Leaving the line break in means when this is rendered with IAlignable the cursor position after the canvas is in the wrong location.
        Segment finalSegment = segments.TakeLast(1).First();
        if (finalSegment.IsLineBreak) {
            segments.RemoveAt(segments.Count - 1);
        }

        // After rendering the canvas, send the cursor to the top left of the canvas to render the sixel data.
        segments.Add(Segment.Control($"{ESC}[{sixel.CellHeight - 1}A{ESC}[{sixel.CellWidth}D"));

        // Render the sixel data.
        segments.Add(Segment.Control(sixel.SixelStrings[FrameToRender]));

        // Reposition the cursor to the bottom right of the canvas after the sixel rendering leaves it at the bottom left.
        segments.Add(Segment.Control($"{ESC}[1A{ESC}[{sixel.CellWidth}C"));

        // Add the line break stolen from the canvas.
        segments.Add(Segment.LineBreak);

        // Update animation frame.
        FrameToRender = (FrameToRender + 1) % sixel.SixelStrings.Length;

        return segments;
    }
}

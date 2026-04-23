namespace PSTextMate.Terminal;

internal interface IPagerDisplayContext {
    void UpdateTarget(IRenderable target);

    void Refresh();
}

internal interface IPagerDisplayHost {
    bool RefreshReplacesViewport { get; }

    void Run(IAnsiConsole console, IRenderable initialTarget, Action<IPagerDisplayContext> action);
}

internal sealed class DirectAnsiPagerDisplayHost : IPagerDisplayHost {
    public static DirectAnsiPagerDisplayHost Instance { get; } = new();

    public bool RefreshReplacesViewport => true;

    private DirectAnsiPagerDisplayHost() {
    }

    public void Run(IAnsiConsole console, IRenderable initialTarget, Action<IPagerDisplayContext> action) {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(initialTarget);
        ArgumentNullException.ThrowIfNull(action);

        action(new DirectAnsiPagerDisplayContext(console, initialTarget));
    }

    private sealed class DirectAnsiPagerDisplayContext : IPagerDisplayContext {
        private const string ViewportResetSequence = "\x1b[2J\x1b[H";
        private readonly object _syncRoot = new();
        private readonly IAnsiConsole _console;
        private IRenderable _target;
        private int _previousFrameLineCount;
        private bool _hasRenderedFrame;

        public DirectAnsiPagerDisplayContext(IAnsiConsole console, IRenderable initialTarget) {
            _console = console ?? throw new ArgumentNullException(nameof(console));
            _target = initialTarget ?? throw new ArgumentNullException(nameof(initialTarget));
        }

        public void UpdateTarget(IRenderable target) {
            ArgumentNullException.ThrowIfNull(target);

            lock (_syncRoot) {
                _target = target;
                RefreshCore();
            }
        }

        public void Refresh() {
            lock (_syncRoot) {
                RefreshCore();
            }
        }

        private void RefreshCore() {
            List<string> frameLines = RenderFrameLines();
            TextWriter writer = _console.Profile.Out.Writer;

            if (!_hasRenderedFrame) {
                writer.Write(ViewportResetSequence);
                _hasRenderedFrame = true;
            }

            int rowsToRewrite = Math.Max(frameLines.Count, _previousFrameLineCount);
            for (int row = 0; row < rowsToRewrite; row++) {
                WriteRowPrefix(writer, row + 1);

                if (row < frameLines.Count && frameLines[row].Length > 0) {
                    writer.Write(frameLines[row]);
                }
            }

            writer.Flush();
            _previousFrameLineCount = frameLines.Count;
        }

        private List<string> RenderFrameLines() {
            string frame = Writer.WriteToString(_target, _console.Profile.Width);
            var lines = new List<string>(Math.Max(8, _previousFrameLineCount));
            TextMateHelper.AddSplitLines(lines, frame, trimTrailingTerminatorEmptyLine: true);
            return lines;
        }

        private static void WriteRowPrefix(TextWriter writer, int row) {
            writer.Write("\x1b[");
            writer.Write(row.ToString(CultureInfo.InvariantCulture));
            writer.Write(";1H\x1b[2K");
        }
    }
}

internal sealed class SpectreLivePagerDisplayHost : IPagerDisplayHost {
    public static SpectreLivePagerDisplayHost Instance { get; } = new();

    public bool RefreshReplacesViewport => false;

    private SpectreLivePagerDisplayHost() {
    }

    public void Run(IAnsiConsole console, IRenderable initialTarget, Action<IPagerDisplayContext> action) {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(initialTarget);
        ArgumentNullException.ThrowIfNull(action);

        console.Live(initialTarget)
            .AutoClear(true)
            .Overflow(VerticalOverflow.Crop)
            .Cropping(VerticalOverflowCropping.Bottom)
            .Start(context => action(new SpectreLivePagerDisplayContext(context)));
    }

    private sealed class SpectreLivePagerDisplayContext : IPagerDisplayContext {
        private readonly LiveDisplayContext _context;

        public SpectreLivePagerDisplayContext(LiveDisplayContext context) {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void UpdateTarget(IRenderable target) {
            ArgumentNullException.ThrowIfNull(target);
            _context.UpdateTarget(target);
        }

        public void Refresh()
            => _context.Refresh();
    }
}

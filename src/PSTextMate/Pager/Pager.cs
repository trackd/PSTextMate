namespace PSTextMate.Terminal;

/// <summary>
/// Simple interactive pager implemented with Spectre.Console Live display.
/// Interaction keys:
/// - Up/Down or j/k: move one renderable item
/// - PageUp/PageDown/Space or h/l: move by one viewport of items
/// - Home/End: go to start/end
/// - / or Ctrl+F: prompt for search query
/// - n / N: next / previous match
/// - c: clear active search
/// - ?: show/hide keybindings
/// - q or Escape: quit
/// </summary>
public sealed class Pager {
    private static readonly PagerExclusivityMode s_pagerExclusivityMode = new();
    private static readonly Panel s_helpOverlayPanel = CreateHelpOverlayPanel();
    private readonly object _stateLock = new();
    private readonly IAnsiConsole _console;
    private readonly Func<ConsoleKeyInfo?>? _tryReadKeyOverride;
    private readonly bool _suppressTerminalControlSequences;
    private readonly IReadOnlyList<IRenderable> _renderables;
    private readonly PagerDocument _document;
    private readonly PagerSearchSession _search;
    private readonly PagerViewportEngine _viewportEngine;
    private readonly HighlightedText? _sourceHighlightedText;
    private readonly int? _originalLineNumberStart;
    private readonly int? _originalLineNumberWidth;
    private readonly int? _stableLineNumberWidth;
    private readonly int _statusColumnWidth;
    private int _top;
    private int WindowHeight;
    private int WindowWidth;
    private int _lastRenderedRows;
    private bool _lastPageHadImages;
    private int _singleRenderableLineOffset;
    private string _searchStatusText = string.Empty;
    private bool _isSearchInputActive;
    private bool _isHelpOverlayActive;
    private readonly StringBuilder _searchInputBuffer = new(64);
    private static readonly Style SearchRowTextStyle = new(Color.White, Color.Grey);
    private static readonly Style SearchMatchTextStyle = new(Color.Black, Color.Orange1);
    private const int KeyPollingIntervalMs = 50;
    private const int MaxSearchQueryLength = 256;
    private int _contentVersion;
    private int _lastPublishedContentVersion = -1;
    private bool _exitRequested;

    private bool TryReadKey(out ConsoleKeyInfo key) {
        if (_tryReadKeyOverride is not null) {
            ConsoleKeyInfo? injected = _tryReadKeyOverride();
            if (injected.HasValue) {
                key = injected.Value;
                return true;
            }

            key = default;
            return false;
        }

        return TryReadKeyFromConsole(out key);
    }

    private static bool TryReadKeyFromConsole(out ConsoleKeyInfo key) {
        try {
            if (!Console.KeyAvailable) {
                key = default;
                return false;
            }

            key = Console.ReadKey(true);
            return true;
        }
        catch (IOException) {
            key = default;
            return false;
        }
        catch (InvalidOperationException) {
            key = default;
            return false;
        }
    }

    private bool UseRichFooter(int footerWidth)
        => footerWidth >= GetMinimumRichFooterWidth();

    private int GetFooterHeight(int footerWidth)
        => UseRichFooter(footerWidth) ? 3 : 1;

    private int GetSearchInputHeight()
        => _isSearchInputActive ? 3 : 0;

    private int GetMinimumRichFooterWidth() {
        const int keySectionMinWidth = 38;
        const int chartSectionMinWidth = 12;
        const int layoutOverhead = 10;
        return keySectionMinWidth + _statusColumnWidth + chartSectionMinWidth + layoutOverhead;
    }

    private static int GetStatusColumnWidth(int totalItems) {
        int digits = Math.Max(1, totalItems.ToString(CultureInfo.InvariantCulture).Length);
        return (digits * 3) + 4;
    }

    private sealed class PagerExclusivityMode : IExclusivityMode {
        private readonly object _syncRoot = new();

        public T Run<T>(Func<T> func) {
            ArgumentNullException.ThrowIfNull(func);

            lock (_syncRoot) {
                return func();
            }
        }

        public async Task<T> RunAsync<T>(Func<Task<T>> func) {
            ArgumentNullException.ThrowIfNull(func);

            Task<T> task;
            lock (_syncRoot) {
                task = func();
            }

            return await task.ConfigureAwait(false);
        }
    }

    private sealed class RenderableSliceRows : Renderable {
        private readonly IReadOnlyList<IRenderable> _source;
        private readonly int _start;
        private readonly int _count;

        public RenderableSliceRows(IReadOnlyList<IRenderable> source, int start, int count) {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _start = Math.Max(0, start);
            _count = Math.Max(0, count);
        }

        protected override Measurement Measure(RenderOptions options, int maxWidth) {
            int min = 0;
            int max = 0;
            int end = GetEndIndex();

            for (int i = _start; i < end; i++) {
                IRenderable child = _source[i];
                Measurement measurement = child.Measure(options, maxWidth);
                min = Math.Max(min, measurement.Min);
                max = Math.Max(max, measurement.Max);
            }

            return new Measurement(min, max);
        }

        protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth) {
            int end = GetEndIndex();
            for (int i = _start; i < end; i++) {
                IRenderable child = _source[i];
                using IEnumerator<Segment> segments = child.Render(options, maxWidth).GetEnumerator();
                if (!segments.MoveNext()) {
                    continue;
                }

                while (true) {
                    Segment current = segments.Current;
                    bool hasMore = segments.MoveNext();

                    yield return current;

                    if (!hasMore) {
                        if (!current.IsLineBreak && child is not ControlCode) {
                            yield return Segment.LineBreak;
                        }

                        break;
                    }
                }
            }
        }

        private int GetEndIndex()
            => Math.Clamp(_start + _count, _start, _source.Count);
    }

    internal sealed class RenderableLineSlice : Renderable {
        private readonly IRenderable _source;
        private readonly int _startLine;
        private readonly int _lineCount;

        public RenderableLineSlice(IRenderable source, int startLine, int lineCount) {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _startLine = Math.Max(0, startLine);
            _lineCount = Math.Max(0, lineCount);
        }

        protected override Measurement Measure(RenderOptions options, int maxWidth)
            => _source.Measure(options, maxWidth);

        protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth) {
            List<SegmentLine> lines = Segment.SplitLines(_source.Render(options, maxWidth), Math.Max(1, maxWidth));
            if (lines.Count == 0) {
                yield break;
            }

            int begin = Math.Clamp(_startLine, 0, lines.Count);
            int end = Math.Clamp(begin + _lineCount, begin, lines.Count);

            for (int lineIndex = begin; lineIndex < end; lineIndex++) {
                foreach (Segment segment in lines[lineIndex]) {
                    yield return segment;
                }

                if (lineIndex + 1 < end) {
                    yield return Segment.LineBreak;
                }
            }
        }
    }


    public Pager(HighlightedText highlightedText) {
        _console = AnsiConsole.Console;
        _tryReadKeyOverride = null;
        _suppressTerminalControlSequences = false;
        _sourceHighlightedText = highlightedText;

        int totalLines = highlightedText.LineCount;
        int lastLineNumber = highlightedText.LineNumberStart + Math.Max(0, totalLines - 1);
        _stableLineNumberWidth = highlightedText.LineNumberWidth ?? lastLineNumber.ToString(CultureInfo.InvariantCulture).Length;
        _originalLineNumberStart = highlightedText.LineNumberStart;
        _originalLineNumberWidth = highlightedText.LineNumberWidth;

        _document = PagerDocument.FromHighlightedText(highlightedText);
        _renderables = _document.Renderables;
        _search = new PagerSearchSession(_document);
        _viewportEngine = new PagerViewportEngine(_renderables, _sourceHighlightedText);
        _statusColumnWidth = GetStatusColumnWidth(_renderables.Count);
        _top = 0;
    }

    public Pager(IEnumerable<IRenderable> renderables)
        : this(renderables, AnsiConsole.Console, null, suppressTerminalControlSequences: false) {
    }

    internal Pager(
        IEnumerable<IRenderable> renderables,
        IReadOnlyList<string?>? sourceLines,
        IAnsiConsole console,
        Func<ConsoleKeyInfo?>? tryReadKeyOverride = null,
        bool suppressTerminalControlSequences = false
    ) {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _tryReadKeyOverride = tryReadKeyOverride;
        _suppressTerminalControlSequences = suppressTerminalControlSequences;
        _document = new PagerDocument(renderables ?? [], sourceLines);
        _renderables = _document.Renderables;
        _search = new PagerSearchSession(_document);
        _viewportEngine = new PagerViewportEngine(_renderables, _sourceHighlightedText);
        _statusColumnWidth = GetStatusColumnWidth(_renderables.Count);
        _top = 0;
    }

    internal Pager(
        IEnumerable<IRenderable> renderables,
        IAnsiConsole console,
        Func<ConsoleKeyInfo?>? tryReadKeyOverride = null,
        bool suppressTerminalControlSequences = false
    )
        : this(renderables, sourceLines: null, console, tryReadKeyOverride, suppressTerminalControlSequences) {
    }

    private void Navigate(LiveDisplayContext ctx) {
        bool running = true;
        bool useTerminalControlSequences = !_suppressTerminalControlSequences;
        (WindowWidth, WindowHeight) = GetPagerSize();
        bool forceRedraw = false;
        int lastRenderedContentVersion;

        lock (_stateLock) {
            lastRenderedContentVersion = _lastPublishedContentVersion;
        }

        if (useTerminalControlSequences) {
            VTHelpers.BeginSynchronizedOutput();
            try {
                ctx.Refresh();
            }
            finally {
                VTHelpers.EndSynchronizedOutput();
            }
        }

        while (running) {
            int currentContentVersion;
            lock (_stateLock) {
                if (_exitRequested) {
                    break;
                }

                currentContentVersion = _contentVersion;
            }

            if (currentContentVersion != lastRenderedContentVersion) {
                forceRedraw = true;
            }

            (int width, int pageHeight) = GetPagerSize();
            int footerHeight = GetFooterHeight(width);
            int searchInputHeight = GetSearchInputHeight();
            int contentRows = Math.Max(1, pageHeight - footerHeight - searchInputHeight);

            bool resized = width != WindowWidth || pageHeight != WindowHeight;
            if (resized) {
                _console.Profile.Width = width;

                WindowWidth = width;
                WindowHeight = pageHeight;
                forceRedraw = true;
                _singleRenderableLineOffset = 0;
            }

            // Redraw if needed (initial, resize, or after navigation)
            if (resized || forceRedraw) {
                PagerViewportWindow viewport;
                IRenderable target;
                bool fullClear;
                int publishedContentVersion;

                lock (_stateLock) {
                    _viewportEngine.RecalculateHeights(width, contentRows, WindowHeight, _console);
                    _top = Math.Clamp(_top, 0, _viewportEngine.GetMaxTop(contentRows));
                    viewport = _viewportEngine.BuildViewport(_top, contentRows);
                    _top = viewport.Top;
                    if (_renderables.Count != 1 || _viewportEngine.GetRenderableHeightAt(viewport.Top) <= contentRows) {
                        _singleRenderableLineOffset = 0;
                    }

                    fullClear = resized || viewport.HasImages || _lastPageHadImages;
                    target = BuildRenderable(viewport, width, contentRows);
                    _lastPageHadImages = viewport.HasImages;
                    publishedContentVersion = _contentVersion;
                    _lastPublishedContentVersion = publishedContentVersion;
                }

                if (useTerminalControlSequences) {
                    VTHelpers.BeginSynchronizedOutput();
                }

                try {
                    if (useTerminalControlSequences) {
                        VTHelpers.ClearScreen();
                    }

                    ctx.UpdateTarget(target);
                    ctx.Refresh();

                    // Clear any stale lines after a terminal shrink.
                    if (useTerminalControlSequences && _lastRenderedRows > pageHeight) {
                        for (int r = pageHeight + 1; r <= _lastRenderedRows; r++) {
                            VTHelpers.ClearRow(r);
                        }
                    }
                }
                finally {
                    if (useTerminalControlSequences) {
                        VTHelpers.EndSynchronizedOutput();
                    }
                }

                _lastRenderedRows = pageHeight;
                forceRedraw = false;
                lastRenderedContentVersion = publishedContentVersion;
            }

            // Wait for input, checking for resize while idle.
            if (!TryReadKey(out ConsoleKeyInfo key)) {
                Thread.Sleep(KeyPollingIntervalMs);
                continue;
            }

            ProcessKey(key, contentRows, ref running, ref forceRedraw);
        }
    }

    internal void Append(IRenderable renderable, string? sourceLine = null) {
        ArgumentNullException.ThrowIfNull(renderable);

        lock (_stateLock) {
            _document.Append(renderable, sourceLine);
            _viewportEngine.NoteRenderableAppended(renderable);
            _search.AppendPendingEntries();
            _contentVersion++;
        }
    }

    internal void AppendRange(IReadOnlyList<IRenderable> renderables, IReadOnlyList<string?>? sourceLines) {
        ArgumentNullException.ThrowIfNull(renderables);

        if (renderables.Count == 0) {
            return;
        }

        lock (_stateLock) {
            _document.AppendRange(renderables, sourceLines);
            for (int index = 0; index < renderables.Count; index++) {
                _viewportEngine.NoteRenderableAppended(renderables[index]);
            }

            _search.AppendPendingEntries();
            _contentVersion++;
        }
    }

    internal void RequestExit() {
        lock (_stateLock) {
            _exitRequested = true;
            _contentVersion++;
        }
    }

    private (int width, int height) GetPagerSize() {
        try {
            int width = Console.WindowWidth > 0
                ? Console.WindowWidth
                : _console.Profile.Width > 0
                    ? _console.Profile.Width
                    : 80;
            int height = Console.WindowHeight > 0 ? Console.WindowHeight : 40;
            return (width, height);
        }
        catch (IOException) {
            return (80, 40);
        }
        catch (InvalidOperationException) {
            return (80, 40);
        }
    }

    private void ScrollRenderable(int delta, int contentRows) {
        if (TryScrollSingleOversizedRenderable(delta)) {
            return;
        }

        _top = _viewportEngine.ScrollTop(_top, delta, contentRows);
    }

    private void PageDown(int contentRows) {
        if (TryPageScrollSingleOversizedRenderable(contentRows)) {
            return;
        }

        _top = _viewportEngine.PageDownTop(_top, contentRows);
    }

    private void PageUp(int contentRows) {
        if (TryPageScrollSingleOversizedRenderable(-contentRows)) {
            return;
        }

        _top = _viewportEngine.PageUpTop(_top, contentRows);
    }

    private void GoToTop() => _top = 0;

    private void ProcessKey(ConsoleKeyInfo key, int contentRows, ref bool running, ref bool forceRedraw) {
        lock (_stateLock) {
            if (_exitRequested) {
                running = false;
                return;
            }

            if (_isSearchInputActive) {
                HandleSearchInputKey(key, ref forceRedraw);
                return;
            }

            if (_isHelpOverlayActive) {
                if (key.Key == ConsoleKey.Q) {
                    running = false;
                    return;
                }

                _isHelpOverlayActive = false;
                forceRedraw = true;
                return;
            }

            bool isCtrlF = key.Key == ConsoleKey.F && (key.Modifiers & ConsoleModifiers.Control) != 0;
            if (key.KeyChar == '/' || isCtrlF) {
                BeginSearchInput();
                forceRedraw = true;
                return;
            }

            if (key.KeyChar == '?') {
                _isHelpOverlayActive = true;
                forceRedraw = true;
                return;
            }

            switch (key.Key) {
                case ConsoleKey.DownArrow:
                case ConsoleKey.J:
                    ScrollRenderable(1, contentRows);
                    forceRedraw = true;
                    break;
                case ConsoleKey.UpArrow:
                case ConsoleKey.K:
                    ScrollRenderable(-1, contentRows);
                    forceRedraw = true;
                    break;
                case ConsoleKey.Spacebar:
                case ConsoleKey.PageDown:
                case ConsoleKey.L:
                    PageDown(contentRows);
                    forceRedraw = true;
                    break;
                case ConsoleKey.PageUp:
                case ConsoleKey.H:
                    PageUp(contentRows);
                    forceRedraw = true;
                    break;
                case ConsoleKey.Home:
                    GoToTop();
                    forceRedraw = true;
                    break;
                case ConsoleKey.End:
                    GoToEnd(contentRows);
                    forceRedraw = true;
                    break;
                case ConsoleKey.N:
                    if ((key.Modifiers & ConsoleModifiers.Shift) != 0) {
                        JumpToPreviousMatch();
                    }
                    else {
                        JumpToNextMatch();
                    }

                    forceRedraw = true;
                    break;
                case ConsoleKey.C:
                    if (_search.HasQuery) {
                        ClearSearch();
                        forceRedraw = true;
                    }

                    break;
                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    running = false;
                    break;
            }
        }
    }

    private void BeginSearchInput() {
        _isSearchInputActive = true;
        _searchInputBuffer.Clear();
        if (!string.IsNullOrEmpty(_search.Query)) {
            _searchInputBuffer.Append(_search.Query);
        }
    }

    private void HandleSearchInputKey(ConsoleKeyInfo key, ref bool forceRedraw) {
        switch (key.Key) {
            case ConsoleKey.Enter:
                _isSearchInputActive = false;
                ApplySearchQuery(_searchInputBuffer.ToString());
                forceRedraw = true;
                return;
            case ConsoleKey.Escape:
                _isSearchInputActive = false;
                forceRedraw = true;
                return;
            case ConsoleKey.Backspace:
                if (_searchInputBuffer.Length > 0) {
                    _searchInputBuffer.Length--;
                    forceRedraw = true;
                }

                return;
        }

        if (!char.IsControl(key.KeyChar)) {
            if (_searchInputBuffer.Length < MaxSearchQueryLength) {
                _searchInputBuffer.Append(key.KeyChar);
                forceRedraw = true;
            }
        }
    }

    private void ApplySearchQuery(string query) {
        if (query.Length > MaxSearchQueryLength) {
            query = query[..MaxSearchQueryLength];
        }

        _search.SetQuery(query);
        if (!_search.HasQuery) {
            _searchStatusText = string.Empty;
            return;
        }

        PagerSearchHit? hit = _search.MoveNext(_top);
        if (hit is null) {
            _searchStatusText = $"/{_search.Query} (no matches)";
            return;
        }

        _top = hit.RenderableIndex;
        _searchStatusText = BuildSearchStatus();
    }

    private void ClearSearch() {
        _search.SetQuery(string.Empty);
        _searchInputBuffer.Clear();
        _searchStatusText = string.Empty;
    }

    private void JumpToNextMatch() {
        if (!_search.HasQuery) {
            _searchStatusText = "No active search. Press / to search.";
            return;
        }

        PagerSearchHit? hit = _search.MoveNext(_top);
        if (hit is null) {
            _searchStatusText = $"/{_search.Query} (no matches)";
            return;
        }

        _top = hit.RenderableIndex;
        _searchStatusText = BuildSearchStatus();
    }

    private void JumpToPreviousMatch() {
        if (!_search.HasQuery) {
            _searchStatusText = "No active search. Press / to search.";
            return;
        }

        PagerSearchHit? hit = _search.MovePrevious(_top);
        if (hit is null) {
            _searchStatusText = $"/{_search.Query} (no matches)";
            return;
        }

        _top = hit.RenderableIndex;
        _searchStatusText = BuildSearchStatus();
    }

    private string BuildSearchStatus() {
        PagerSearchHit? hit = _search.CurrentHit;
        if (hit is null) {
            return $"/{_search.Query} (0 matches)";
        }

        int current = _search.CurrentHitIndex + 1;
        int line = hit.Line + 1;
        int column = hit.Column + 1;
        return $"/{_search.Query} [{current}/{_search.HitCount}] line {line}, col {column}";
    }

    private void GoToEnd(int contentRows) {
        if (TryGoToEndSingleOversizedRenderable(contentRows)) {
            return;
        }

        _top = _viewportEngine.GetMaxTop(contentRows);
    }

    private Layout BuildRenderable(PagerViewportWindow viewport, int width, int contentRows) {
        int footerHeight = GetFooterHeight(width);
        int searchInputHeight = GetSearchInputHeight();
        IRenderable content = _isHelpOverlayActive
            ? BuildHelpOverlayPanel()
            : viewport.Count <= 0
                ? Text.Empty
                : BuildContentRenderable(viewport, contentRows);

        IRenderable footer = BuildFooter(width, viewport);
        var root = new Layout("root");
        Layout bodyLayout = new Layout("body").Ratio(1).Update(content);
        if (_isSearchInputActive) {
            root.SplitRows(
                new Layout("search").Size(searchInputHeight).Update(BuildSearchInputPanel()),
                bodyLayout,
                new Layout("footer").Size(footerHeight).Update(footer)
            );
        }
        else {
            root.SplitRows(
                bodyLayout,
                new Layout("footer").Size(footerHeight).Update(footer)
            );
        }

        return root;
    }

    private Panel BuildSearchInputPanel() {
        string inputText = Markup.Escape(_searchInputBuffer.ToString());
        string prompt = $"[bold]/[/]{inputText}[grey]_[/]";
        var content = new Markup(prompt);
        return new Panel(content) {
            Header = new PanelHeader("Search", Justify.Left),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 0, 1, 0),
            Expand = true
        };
    }

    private static Panel BuildHelpOverlayPanel()
        => s_helpOverlayPanel;

    private static Panel CreateHelpOverlayPanel() {
        var helpRows = new Rows(
            new Text("Keybindings", new Style(Color.White, decoration: Decoration.Bold)),
            Text.Empty,
            new Text("  Up/Down or j/k Move one item", new Style(Color.Grey)),
            new Text("  PgUp/PgDn/h/l Page navigation", new Style(Color.Grey)),
            new Text("  Home/End      Jump to start/end", new Style(Color.Grey)),
            new Text("  / or Ctrl+F   Search", new Style(Color.Grey)),
            new Text("  n / N         Next / previous match", new Style(Color.Grey)),
            new Text("  c             Clear active search", new Style(Color.Grey)),
            new Text("  ?             Toggle this help", new Style(Color.Grey)),
            new Text("  q or Esc      Quit pager", new Style(Color.Grey)),
            Text.Empty,
            new Text("Press ? or Esc to close help.", new Style(Color.Yellow))
        );

        return new Panel(new Align(helpRows, HorizontalAlignment.Left, VerticalAlignment.Middle)) {
            Header = new PanelHeader("Pager Help", Justify.Left),
            Border = BoxBorder.Rounded,
            Padding = new Padding(2, 1, 2, 1),
            Expand = true
        };
    }

    private IRenderable BuildContentRenderable(PagerViewportWindow viewport, int contentRows) {
        List<IRenderable> visibleItems = _search.HasQuery
            ? BuildSearchAwareItems(viewport)
            : SnapshotViewportItems(viewport);

        if (visibleItems.Count == 1) {
            int renderableHeight = _viewportEngine.GetRenderableHeightAt(viewport.Top);
            if (renderableHeight > contentRows) {
                int maxOffset = renderableHeight - contentRows;
                int clampedOffset = Math.Clamp(_singleRenderableLineOffset, 0, maxOffset);

                if (_singleRenderableLineOffset != clampedOffset) {
                    _singleRenderableLineOffset = clampedOffset;
                }

                return new RenderableLineSlice(visibleItems[0], clampedOffset, contentRows);
            }
        }

        if (_sourceHighlightedText is not null && _sourceHighlightedText.ShowLineNumbers) {
            _sourceHighlightedText.SetView(visibleItems, 0, visibleItems.Count);

            _sourceHighlightedText.LineNumberStart = (_originalLineNumberStart ?? 1) + viewport.Top;
            _sourceHighlightedText.LineNumberWidth = _stableLineNumberWidth;
            return _sourceHighlightedText;
        }

        return new RenderableSliceRows(visibleItems, 0, visibleItems.Count);
    }

    private bool TryScrollSingleOversizedRenderable(int delta) {
        if (!IsSingleOversizedRenderable(out int contentRows, out int maxOffset)) {
            return false;
        }

        int direction = Math.Sign(delta);
        if (direction == 0) {
            return true;
        }

        _singleRenderableLineOffset = Math.Clamp(_singleRenderableLineOffset + direction, 0, maxOffset);
        return true;
    }

    private bool TryPageScrollSingleOversizedRenderable(int delta) {
        if (!IsSingleOversizedRenderable(out int contentRows, out int maxOffset)) {
            return false;
        }

        if (delta == 0) {
            return true;
        }

        _singleRenderableLineOffset = Math.Clamp(_singleRenderableLineOffset + delta, 0, maxOffset);
        return true;
    }

    private bool TryGoToEndSingleOversizedRenderable(int contentRows) {
        if (!IsSingleOversizedRenderable(out int actualContentRows, out int maxOffset)) {
            return false;
        }

        _singleRenderableLineOffset = maxOffset;
        return true;
    }

    private bool IsSingleOversizedRenderable(out int contentRows, out int maxOffset) {
        contentRows = 0;
        maxOffset = 0;

        if (_renderables.Count != 1) {
            return false;
        }

        (int width, int pageHeight) = GetPagerSize();
        int footerHeight = GetFooterHeight(width);
        int searchInputHeight = GetSearchInputHeight();
        contentRows = Math.Max(1, pageHeight - footerHeight - searchInputHeight);

        int height = _viewportEngine.GetRenderableHeightAt(0);
        if (height <= contentRows) {
            return false;
        }

        maxOffset = height - contentRows;
        return true;
    }

    private List<IRenderable> SnapshotViewportItems(PagerViewportWindow viewport) {
        var items = new List<IRenderable>(viewport.Count);
        for (int i = 0; i < viewport.Count; i++) {
            items.Add(_renderables[viewport.Top + i]);
        }

        return items;
    }

    private List<IRenderable> BuildSearchAwareItems(PagerViewportWindow viewport) {
        var items = new List<IRenderable>(viewport.Count);
        for (int i = 0; i < viewport.Count; i++) {
            int renderableIndex = viewport.Top + i;
            IRenderable highlighted = ApplySearchHighlight(renderableIndex, _renderables[renderableIndex]);
            items.Add(highlighted);
        }

        return items;
    }

    private IRenderable ApplySearchHighlight(int renderableIndex, IRenderable renderable)
        => !_search.HasQuery || !_search.HasHitsForRenderable(renderableIndex)
            ? renderable
            : PagerHighlighting.BuildSegmentHighlightRenderable(
                renderable,
                _search.Query,
                SearchRowTextStyle,
                SearchMatchTextStyle,
                highlightLinkedLabelsOnNoDirectMatch: true
            );

    private IRenderable BuildFooter(int width, PagerViewportWindow viewport)
        => UseRichFooter(width)
            ? BuildRichFooter(width, viewport)
            : BuildSimpleFooter(viewport);

    private Text BuildSimpleFooter(PagerViewportWindow viewport) {
        int total = _renderables.Count;
        int start = total == 0 ? 0 : viewport.Top + 1;
        int end = viewport.EndExclusive;
        string baseText = $"{start}-{end}/{total}";
        string defaultHelp = !_isSearchInputActive && string.IsNullOrEmpty(_searchStatusText)
            ? "   Press ? for help"
            : string.Empty;
        string inputHelp = _isSearchInputActive ? "   Search: Enter Apply  Esc Cancel" : string.Empty;
        return string.IsNullOrEmpty(_searchStatusText)
            ? new Text(baseText + defaultHelp + inputHelp, new Style(Color.Grey))
            : new Text($"{baseText}{inputHelp}   {_searchStatusText}", new Style(Color.Grey));
    }

    private Panel BuildRichFooter(int width, PagerViewportWindow viewport) {
        int total = _renderables.Count;
        int start = total == 0 ? 0 : viewport.Top + 1;
        int end = viewport.EndExclusive;
        int safeTotal = Math.Max(1, total);
        int digits = Math.Max(1, safeTotal.ToString(CultureInfo.InvariantCulture).Length);

        string keyText = "Press ? for help";
        string statusText = $"{start.ToString(CultureInfo.InvariantCulture).PadLeft(digits)}-{end.ToString(CultureInfo.InvariantCulture).PadLeft(digits)}/{total.ToString(CultureInfo.InvariantCulture).PadLeft(digits)}".PadLeft(_statusColumnWidth);
        if (_isSearchInputActive) {
            keyText = "Search: Enter Apply  Esc Cancel";
        }

        if (!string.IsNullOrEmpty(_searchStatusText)) {
            keyText = string.IsNullOrEmpty(keyText)
                ? _searchStatusText
                : $"{keyText}  {_searchStatusText}";
        }

        int chartWidth = Math.Clamp(width / 4, 14, 40);
        double progressUnits = total == 0 ? 0d : (double)end / safeTotal * chartWidth;
        double chartValue = end <= 0 ? 0d : Math.Clamp(Math.Ceiling(progressUnits), Math.Min(4d, chartWidth), chartWidth);
        BarChart chart = new BarChart()
            .Width(chartWidth)
            .WithMaxValue(chartWidth)
            .HideValues()
            .AddItem(" ", chartValue, Color.Lime);

        var footerBody = new Layout("footer-body");
        footerBody.SplitColumns(
            new Layout("keys").Ratio(1).Update(new Text(keyText, new Style(Color.Grey))),
            new Layout("status").Size(_statusColumnWidth).Update(new Align(new Markup($"[bold]{statusText}[/]"), HorizontalAlignment.Right)),
            new Layout("chart").Size(chartWidth).Update(chart)
        );

        return new Panel(footerBody) {
            Border = BoxBorder.Rounded,
            Padding = new Padding(0, 0, 0, 0),
            Expand = true
        };
    }

    public void Show() {
        s_pagerExclusivityMode.Run(() => {
            if (_suppressTerminalControlSequences) {
                ShowCore();
                return 0;
            }

            try {
                _console.AlternateScreen(ShowCore);
            }
            catch (NotSupportedException) {
                // Some hosts report no alternate-buffer/ANSI capability.
                // Keep pager functional by running on the main screen.
                ShowCore();
            }
            catch (IOException) {
                // Certain PTY hosts report invalid console handles for alternate screen.
                // Fall back to normal screen rendering so pager still works.
                ShowCore();
            }
            catch (InvalidOperationException) {
                // Console state can be partially unavailable in test/PTY environments.
                ShowCore();
            }

            return 0;
        });
    }

    private void ShowCore() {
        bool useTerminalControlSequences = !_suppressTerminalControlSequences;
        if (useTerminalControlSequences) {
            VTHelpers.HideCursor();
            VTHelpers.EnableAlternateScroll();
        }

        try {
            (int width, int pageHeight) = GetPagerSize();
            int footerHeight = GetFooterHeight(width);
            int searchInputHeight = GetSearchInputHeight();
            int contentRows = Math.Max(1, pageHeight - footerHeight - searchInputHeight);
            WindowWidth = width;
            WindowHeight = pageHeight;

            // Initial target for Spectre Live (footer included in target renderable)
            PagerViewportWindow initialViewport;
            IRenderable initial;

            lock (_stateLock) {
                _console.Profile.Width = width;
                _viewportEngine.RecalculateHeights(width, contentRows, WindowHeight, _console);
                initialViewport = _viewportEngine.BuildViewport(_top, contentRows);
                _top = initialViewport.Top;
                initial = BuildRenderable(initialViewport, width, contentRows);
                _lastRenderedRows = pageHeight;
                _lastPageHadImages = initialViewport.HasImages;
                _lastPublishedContentVersion = _contentVersion;
            }

            // If the initial page contains images, clear appropriately to ensure safe image rendering
            if (initialViewport.HasImages) {
                if (!_suppressTerminalControlSequences) {
                    VTHelpers.BeginSynchronizedOutput();
                }

                try {
                    if (!_suppressTerminalControlSequences) {
                        VTHelpers.ClearScreen();
                    }
                }
                finally {
                    if (!_suppressTerminalControlSequences) {
                        VTHelpers.EndSynchronizedOutput();
                    }
                }
            }
            // Enter interactive loop using the live display context
            _console.Live(initial)
                .AutoClear(true)
                .Overflow(VerticalOverflow.Crop)
                .Cropping(VerticalOverflowCropping.Bottom)
                .Start(Navigate);
        }
        finally {
            // Clear any active view on the source highlighted text to avoid
            // leaving its state mutated after the pager exits, and restore
            // original line-number settings.
            if (_sourceHighlightedText is not null) {
                _sourceHighlightedText.ClearView();
                _sourceHighlightedText.LineNumberStart = _originalLineNumberStart ?? 1;
                _sourceHighlightedText.LineNumberWidth = _originalLineNumberWidth;
            }

            lock (_stateLock) {
                _exitRequested = true;
            }

            if (useTerminalControlSequences) {
                VTHelpers.DisableAlternateScroll();
                VTHelpers.ShowCursor();
            }
        }
    }

}

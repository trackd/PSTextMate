namespace PSTextMate.Terminal;

internal static class PagerHighlighting {
    internal static IRenderable BuildSegmentHighlightRenderable(
        IRenderable renderable,
        string query,
        Style rowStyle,
        Style matchStyle,
        bool highlightLinkedLabelsOnNoDirectMatch = false
    ) => new SegmentHighlightRenderable(renderable, query, rowStyle, matchStyle, highlightLinkedLabelsOnNoDirectMatch);

    internal static string NormalizeText(string? text) {
        return string.IsNullOrEmpty(text)
            ? string.Empty
            : text.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .TrimEnd('\n');
    }

    private sealed class SegmentHighlightRenderable : IRenderable {
        private readonly IRenderable _inner;
        private readonly string _query;
        private readonly Style _rowStyle;
        private readonly Style _matchStyle;
        private readonly bool _highlightLinkedLabelsOnNoDirectMatch;

        public SegmentHighlightRenderable(
            IRenderable inner,
            string query,
            Style rowStyle,
            Style matchStyle,
            bool highlightLinkedLabelsOnNoDirectMatch
        ) {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _query = query ?? string.Empty;
            _rowStyle = rowStyle;
            _matchStyle = matchStyle;
            _highlightLinkedLabelsOnNoDirectMatch = highlightLinkedLabelsOnNoDirectMatch;
        }

        public Measurement Measure(RenderOptions options, int maxWidth)
            => _inner.Measure(options, maxWidth);

        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) {
            List<Segment> source = [.. _inner.Render(options, maxWidth)];
            if (source.Count == 0 || string.IsNullOrEmpty(_query)) {
                return source;
            }

            bool highlightLinkedLabels = _highlightLinkedLabelsOnNoDirectMatch && HasSegmentLinkMatch(source, _query);
            string plainText = BuildPlainText(source);
            if (plainText.Length == 0) {
                return source;
            }

            bool[] matchMask = new bool[plainText.Length];
            bool[] lineHasMatch = new bool[CountLines(plainText)];
            bool hasDirectHits = BuildHighlightMasks(plainText, _query, matchMask, lineHasMatch);

            if (!hasDirectHits && !highlightLinkedLabels) {
                return source;
            }

            return RebuildSegmentsWithHighlights(source, matchMask, lineHasMatch, highlightLinkedLabels);
        }

        private static bool HasSegmentLinkMatch(IEnumerable<Segment> segments, string query) {
            foreach (Segment segment in segments) {
                if (SegmentLinkMatchesQuery(segment, query)) {
                    return true;
                }
            }

            return false;
        }

        private static bool SegmentLinkMatchesQuery(Segment segment, string query) {
            if (string.IsNullOrWhiteSpace(query) || segment.IsControlCode || segment.IsLineBreak) {
                return false;
            }

            string? link = segment.Style.Link;
            return !string.IsNullOrWhiteSpace(link)
                && link.Contains(query, StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildPlainText(IEnumerable<Segment> segments) {
            StringBuilder builder = StringBuilderPool.Rent();
            try {
                foreach (Segment segment in segments) {
                    if (segment.IsControlCode) {
                        continue;
                    }

                    if (segment.IsLineBreak) {
                        builder.Append('\n');
                        continue;
                    }

                    builder.Append(segment.Text);
                }

                return builder.ToString();
            }
            finally {
                StringBuilderPool.Return(builder);
            }
        }

        private static int CountLines(string plainText) {
            int lines = 1;
            foreach (char current in plainText) {
                if (current == '\n') {
                    lines++;
                }
            }

            return lines;
        }

        private static bool BuildHighlightMasks(string plainText, string query, bool[] matchMask, bool[] lineHasMatch) {
            if (plainText.Length == 0 || query.Length == 0) {
                return false;
            }

            bool hasHit = false;
            int searchStart = 0;
            int currentLine = 0;
            int nextLineBreak = plainText.IndexOf('\n');

            while (searchStart <= plainText.Length - query.Length) {
                int hitOffset = plainText.IndexOf(query, searchStart, StringComparison.OrdinalIgnoreCase);
                if (hitOffset < 0) {
                    break;
                }

                while (nextLineBreak >= 0 && nextLineBreak < hitOffset) {
                    currentLine++;
                    nextLineBreak = plainText.IndexOf('\n', nextLineBreak + 1);
                }

                int start = Math.Clamp(hitOffset, 0, plainText.Length);
                int length = Math.Clamp(query.Length, 0, plainText.Length - start);
                for (int i = 0; i < length; i++) {
                    matchMask[start + i] = true;
                }

                lineHasMatch[Math.Clamp(currentLine, 0, lineHasMatch.Length - 1)] = true;
                hasHit = true;
                searchStart = hitOffset + Math.Max(1, query.Length);
            }

            return hasHit;
        }

        private List<Segment> RebuildSegmentsWithHighlights(
            List<Segment> source,
            bool[] matchMask,
            bool[] lineHasMatch,
            bool highlightLinkedLabels
        ) {
            var output = new List<Segment>(source.Count * 2);
            int absolute = 0;
            int line = 0;
            StringBuilder chunk = StringBuilderPool.Rent();

            try {
                foreach (Segment segment in source) {
                    if (segment.IsControlCode) {
                        output.Add(segment);
                        continue;
                    }

                    if (segment.IsLineBreak) {
                        output.Add(segment);
                        if (absolute < matchMask.Length) {
                            absolute++;
                        }

                        line = Math.Min(line + 1, lineHasMatch.Length - 1);
                        continue;
                    }

                    if (segment.Text.Length == 0) {
                        continue;
                    }

                    bool segmentLinkMatchesQuery = highlightLinkedLabels && SegmentLinkMatchesQuery(segment, _query);

                    chunk.Clear();
                    Style? chunkStyle = null;

                    foreach (char ch in segment.Text) {
                        if (ch == '\n') {
                            FlushChunk(output, chunk, chunkStyle);
                            output.Add(Segment.LineBreak);
                            if (absolute < matchMask.Length) {
                                absolute++;
                            }

                            line = Math.Min(line + 1, lineHasMatch.Length - 1);
                            continue;
                        }

                        bool inMatch = absolute >= 0 && absolute < matchMask.Length && matchMask[absolute];
                        bool inMatchedLine = line >= 0 && line < lineHasMatch.Length && lineHasMatch[line];
                        Style style;
                        if (ch == '│') {
                            // leave borders as is.
                            style = segment.Style;
                        }
                        else if (inMatch || segmentLinkMatchesQuery) {
                            style = _matchStyle;
                        }
                        else if (inMatchedLine) {
                            style = _rowStyle;
                        }
                        else {
                            style = segment.Style;
                        }

                        if (chunkStyle is null || !chunkStyle.Equals(style)) {
                            FlushChunk(output, chunk, chunkStyle);
                            chunkStyle = style;
                        }

                        chunk.Append(ch);
                        absolute++;
                    }

                    FlushChunk(output, chunk, chunkStyle);
                }
            }
            finally {
                StringBuilderPool.Return(chunk);
            }

            return output;
        }

        private static void FlushChunk(List<Segment> output, StringBuilder chunk, Style? style) {
            if (chunk.Length == 0 || style is null) {
                return;
            }

            output.Add(new Segment(chunk.ToString(), style));
            chunk.Clear();
        }
    }

}

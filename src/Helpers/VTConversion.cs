using System.Runtime.CompilerServices;
using Spectre.Console;

namespace PwshSpectreConsole.TextMate.Core.Helpers;

/// <summary>
/// Efficient parser for VT (Virtual Terminal) escape sequences that converts them to Spectre.Console objects.
/// Supports RGB colors, 256-color palette, 3-bit colors, and text decorations.
/// </summary>
public static class VTParser
{
    private const char ESC = '\x1B';
    private const char CSI_START = '[';
    private const char OSC_START = ']';
    private const char SGR_END = 'm';
    private const char ST = '\x1B'; // String Terminator (ESC in this context)

    /// <summary>
    /// Parses a string containing VT escape sequences and returns a Paragraph object.
    /// This is more efficient than ToMarkup() as it directly constructs the Paragraph
    /// without intermediate markup string generation and parsing.
    /// </summary>
    /// <param name="input">Input string with VT escape sequences</param>
    /// <returns>Paragraph object with parsed styles applied</returns>
    public static Paragraph ToParagraph(string input)
    {
        if (string.IsNullOrEmpty(input))
            return new Paragraph();

        List<TextSegment> segments = ParseToSegments(input);
        if (segments.Count == 0)
            return new Paragraph(input, Style.Plain);

        var paragraph = new Paragraph();
        foreach (TextSegment segment in segments)
        {
            if (segment.HasStyle)
            {
                // Style class supports links directly via constructor parameter
                paragraph.Append(segment.Text, segment.Style.ToSpectreStyle());
            }
            else
            {
                paragraph.Append(segment.Text, Style.Plain);
            }
        }

        return paragraph;
    }

    /// <summary>
    /// Parses input string into styled text segments.
    /// </summary>
    private static List<TextSegment> ParseToSegments(string input)
    {
        var segments = new List<TextSegment>();
        ReadOnlySpan<char> span = input.AsSpan();
        var currentStyle = new StyleState();
        int textStart = 0;
        int i = 0;

        while (i < span.Length)
        {
            if (span[i] == ESC && i + 1 < span.Length)
            {
                if (span[i + 1] == CSI_START)
                {
                    // Add text segment before escape sequence
                    if (i > textStart)
                    {
                        string text = input.Substring(textStart, i - textStart);
                        segments.Add(new TextSegment(text, currentStyle.Clone()));
                    }

                    // Parse CSI escape sequence
                    int escapeEnd = ParseEscapeSequence(span, i, ref currentStyle);
                    if (escapeEnd > i)
                    {
                        i = escapeEnd;
                        textStart = i;
                    }
                    else
                    {
                        i++;
                    }
                }
                else if (span[i + 1] == OSC_START)
                {
                    // Add text segment before OSC sequence
                    if (i > textStart)
                    {
                        string text = input.Substring(textStart, i - textStart);
                        segments.Add(new TextSegment(text, currentStyle.Clone()));
                    }

                    // Parse OSC sequence
                    OscResult oscResult = ParseOscSequence(span, i, ref currentStyle);
                    if (oscResult.End > i)
                    {
                        // If we found hyperlink text, add it as a segment
                        if (!string.IsNullOrEmpty(oscResult.LinkText))
                        {
                            segments.Add(new TextSegment(oscResult.LinkText, currentStyle.Clone()));
                        }
                        i = oscResult.End;
                        textStart = i;
                    }
                    else
                    {
                        i++;
                    }
                }
                else
                {
                    i++;
                }
            }
            else
            {
                i++;
            }
        }

        // Add remaining text
        if (textStart < span.Length)
        {
            string text = input.Substring(textStart);
            segments.Add(new TextSegment(text, currentStyle.Clone()));
        }

        return segments;
    }

    /// <summary>
    /// Parses a single VT escape sequence and updates the style state.
    /// Returns the index after the escape sequence.
    /// </summary>
    private static int ParseEscapeSequence(ReadOnlySpan<char> span, int start, ref StyleState style)
    {
        int i = start + 2; // Skip ESC[
        var parameters = new List<int>();
        int currentNumber = 0;
        bool hasNumber = false;

        // Parse parameters (numbers separated by semicolons)
        while (i < span.Length && span[i] != SGR_END)
        {
            if (IsDigit(span[i]))
            {
                currentNumber = currentNumber * 10 + (span[i] - '0');
                hasNumber = true;
            }
            else if (span[i] == ';')
            {
                parameters.Add(hasNumber ? currentNumber : 0);
                currentNumber = 0;
                hasNumber = false;
            }
            else
            {
                // Invalid character, abort parsing
                return start + 1;
            }
            i++;
        }

        if (i >= span.Length || span[i] != SGR_END)
        {
            return start + 1; // Invalid sequence
        }

        // Add the last parameter
        parameters.Add(hasNumber ? currentNumber : 0);

        // Apply SGR parameters to style
        ApplySgrParameters(parameters, ref style);

        return i + 1; // Return position after 'm'
    }

    /// <summary>
    /// Result of parsing an OSC sequence.
    /// </summary>
    private readonly struct OscResult
    {
        public readonly int End;
        public readonly string? LinkText;

        public OscResult(int end, string? linkText = null)
        {
            End = end;
            LinkText = linkText;
        }
    }

    /// <summary>
    /// Parses an OSC (Operating System Command) sequence and updates the style state.
    /// Returns the result containing end position and any link text found.
    /// </summary>
    private static OscResult ParseOscSequence(ReadOnlySpan<char> span, int start, ref StyleState style)
    {
        int i = start + 2; // Skip ESC]

        // Check if this is OSC 8 (hyperlink)
        if (i < span.Length && span[i] == '8' && i + 1 < span.Length && span[i + 1] == ';')
        {
            i += 2; // Skip "8;"

            // Parse hyperlink sequence: ESC]8;params;url ESC\text ESC]8;; ESC\
            int urlEnd = -1;

            // Find the semicolon that separates params from URL
            while (i < span.Length && span[i] != ';')
            {
                i++;
            }

            if (i < span.Length && span[i] == ';')
            {
                i++; // Skip the semicolon
                int urlStart = i;

                // Find the end of the URL (look for ESC\)
                while (i < span.Length - 1)
                {
                    if (span[i] == ESC && span[i + 1] == '\\')
                    {
                        urlEnd = i;
                        break;
                    }
                    i++;
                }

                if (urlEnd > urlStart)
                {
                    string url = span.Slice(urlStart, urlEnd - urlStart).ToString();
                    i = urlEnd + 2; // Skip ESC\

                    // Check if this is a link start (has URL) or link end (empty)
                    if (!string.IsNullOrEmpty(url))
                    {
                        // This is a link start - find the link text and end sequence
                        int linkTextStart = i;
                        int linkTextEnd = -1;

                        // Look for the closing OSC sequence: ESC]8;;ESC\
                        while (i < span.Length - 6) // Need at least 6 chars for ESC]8;;ESC\
                        {
                            if (span[i] == ESC && span[i + 1] == OSC_START &&
                                span[i + 2] == '8' && span[i + 3] == ';' &&
                                span[i + 4] == ';' && span[i + 5] == ESC &&
                                span[i + 6] == '\\')
                            {
                                linkTextEnd = i;
                                break;
                            }
                            i++;
                        }

                        if (linkTextEnd > linkTextStart)
                        {
                            string linkText = span.Slice(linkTextStart, linkTextEnd - linkTextStart).ToString();
                            style.Link = url;
                            return new OscResult(linkTextEnd + 7, linkText); // Skip ESC]8;;ESC\
                        }
                    }
                    else
                    {
                        // This is likely a link end sequence: ESC]8;;ESC\
                        style.Link = null;
                        return new OscResult(i);
                    }
                }
            }
        }

        // If we can't parse the OSC sequence, skip to the next ESC\ or end of string
        while (i < span.Length - 1)
        {
            if (span[i] == ESC && span[i + 1] == '\\')
            {
                return new OscResult(i + 2);
            }
            i++;
        }

        return new OscResult(start + 1); // Failed to parse, advance by 1
    }

    /// <summary>
    /// Applies SGR (Select Graphic Rendition) parameters to the style state.
    /// </summary>
    private static void ApplySgrParameters(List<int> parameters, ref StyleState style)
    {
        for (int i = 0; i < parameters.Count; i++)
        {
            int param = parameters[i];

            switch (param)
            {
                case 0: // Reset
                    style.Reset();
                    break;
                case 1: // Bold
                    style.Decoration |= Decoration.Bold;
                    break;
                case 2: // Dim
                    style.Decoration |= Decoration.Dim;
                    break;
                case 3: // Italic
                    style.Decoration |= Decoration.Italic;
                    break;
                case 4: // Underline
                    style.Decoration |= Decoration.Underline;
                    break;
                case 5: // Slow blink
                    style.Decoration |= Decoration.SlowBlink;
                    break;
                case 6: // Rapid blink
                    style.Decoration |= Decoration.RapidBlink;
                    break;
                case 7: // Reverse video
                    style.Decoration |= Decoration.Invert;
                    break;
                case 8: // Conceal
                    style.Decoration |= Decoration.Conceal;
                    break;
                case 9: // Strikethrough
                    style.Decoration |= Decoration.Strikethrough;
                    break;
                case 22: // Normal intensity (not bold or dim)
                    style.Decoration &= ~(Decoration.Bold | Decoration.Dim);
                    break;
                case 23: // Not italic
                    style.Decoration &= ~Decoration.Italic;
                    break;
                case 24: // Not underlined
                    style.Decoration &= ~Decoration.Underline;
                    break;
                case 25: // Not blinking
                    style.Decoration &= ~(Decoration.SlowBlink | Decoration.RapidBlink);
                    break;
                case 27: // Not reversed
                    style.Decoration &= ~Decoration.Invert;
                    break;
                case 28: // Not concealed
                    style.Decoration &= ~Decoration.Conceal;
                    break;
                case 29: // Not strikethrough
                    style.Decoration &= ~Decoration.Strikethrough;
                    break;
                case >= 30 and <= 37: // 3-bit foreground colors
                    style.Foreground = GetConsoleColor(param);
                    break;
                case 38: // Extended foreground color
                    if (i + 1 < parameters.Count)
                    {
                        int colorType = parameters[i + 1];
                        if (colorType == 2 && i + 4 < parameters.Count) // RGB
                        {
                            byte r = (byte)Math.Clamp(parameters[i + 2], 0, 255);
                            byte g = (byte)Math.Clamp(parameters[i + 3], 0, 255);
                            byte b = (byte)Math.Clamp(parameters[i + 4], 0, 255);
                            style.Foreground = new Color(r, g, b);
                            i += 4;
                        }
                        else if (colorType == 5 && i + 2 < parameters.Count) // 256-color
                        {
                            int colorIndex = parameters[i + 2];
                            style.Foreground = Get256Color(colorIndex);
                            i += 2;
                        }
                    }
                    break;
                case 39: // Default foreground color
                    style.Foreground = null;
                    break;
                case >= 40 and <= 47: // 3-bit background colors
                    style.Background = GetConsoleColor(param);
                    break;
                case 48: // Extended background color
                    if (i + 1 < parameters.Count)
                    {
                        int colorType = parameters[i + 1];
                        if (colorType == 2 && i + 4 < parameters.Count) // RGB
                        {
                            byte r = (byte)Math.Clamp(parameters[i + 2], 0, 255);
                            byte g = (byte)Math.Clamp(parameters[i + 3], 0, 255);
                            byte b = (byte)Math.Clamp(parameters[i + 4], 0, 255);
                            style.Background = new Color(r, g, b);
                            i += 4;
                        }
                        else if (colorType == 5 && i + 2 < parameters.Count) // 256-color
                        {
                            int colorIndex = parameters[i + 2];
                            style.Background = Get256Color(colorIndex);
                            i += 2;
                        }
                    }
                    break;
                case 49: // Default background color
                    style.Background = null;
                    break;
                case >= 90 and <= 97: // High intensity 3-bit foreground colors
                    style.Foreground = GetConsoleColor(param);
                    break;
                case >= 100 and <= 107: // High intensity 3-bit background colors
                    style.Background = GetConsoleColor(param);
                    break;
            }
        }
    }

    /// <summary>
    /// Gets a Color object for standard console colors.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Color GetConsoleColor(int code) => code switch
    {
        // 30 or 40 => Color.Black,
        // 31 or 41 => Color.Red,
        // 32 or 42 => Color.Green,
        // 33 or 43 => Color.Yellow,
        // 34 or 44 => Color.Blue,
        // 35 or 45 => Color.Purple,
        // 36 or 46 => Color.Teal,
        // 37 or 47 => Color.White,
        // 90 or 100 => Color.Grey,
        // 91 or 101 => Color.Red1,
        // 92 or 102 => Color.Green1,
        // 93 or 103 => Color.Yellow1,
        // 94 or 104 => Color.Blue1,
        // 95 or 105 => Color.Fuchsia,
        // 96 or 106 => Color.Aqua,
        // 97 or 107 => Color.White,
        // _ => Color.Default
        // From ConvertFrom-ConsoleColor.ps1
        30 => Color.Black,
        31 => Color.DarkRed,
        32 => Color.DarkGreen,
        33 => Color.Olive,
        34 => Color.DarkBlue,
        35 => Color.Purple,
        36 => Color.Teal,
        37 => Color.Silver,
        40 => Color.Black,
        41 => Color.DarkRed,
        42 => Color.DarkGreen,
        43 => Color.Olive,
        44 => Color.DarkBlue,
        45 => Color.Purple,
        46 => Color.Teal,
        47 => Color.Silver,
        90 => Color.Grey,
        91 => Color.Red,
        92 => Color.Green,
        93 => Color.Yellow,
        94 => Color.Blue,
        95 => Color.Fuchsia,
        96 => Color.Aqua,
        97 => Color.White,
        100 => Color.Grey,
        101 => Color.Red,
        102 => Color.Green,
        103 => Color.Yellow,
        104 => Color.Blue,
        105 => Color.Fuchsia,
        106 => Color.Aqua,
        107 => Color.White,
        _ => Color.Default
    };

    /// <summary>
    /// Gets a Color object for 256-color palette.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Color Get256Color(int index)
    {
        if (index < 0 || index > 255)
            return Color.Default;

        // Standard 16 colors
        if (index < 16)
        {
            return index switch
            {
                0 => Color.Black,
                1 => Color.Maroon,
                2 => Color.Green,
                3 => Color.Olive,
                4 => Color.Navy,
                5 => Color.Purple,
                6 => Color.Teal,
                7 => Color.Silver,
                8 => Color.Grey,
                9 => Color.Red,
                10 => Color.Lime,
                11 => Color.Yellow,
                12 => Color.Blue,
                13 => Color.Fuchsia,
                14 => Color.Aqua,
                15 => Color.White,
                _ => Color.Default
            };
        }

        // 216 color cube (16-231)
        if (index < 232)
        {
            int colorIndex = index - 16;
            byte r = (byte)((colorIndex / 36) * 51);
            byte g = (byte)(((colorIndex % 36) / 6) * 51);
            byte b = (byte)((colorIndex % 6) * 51);
            return new Color(r, g, b);
        }

        // Grayscale (232-255)
        byte gray = (byte)((index - 232) * 10 + 8);
        return new Color(gray, gray, gray);
    }

    /// <summary>
    /// Checks if a character is a digit.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDigit(char c) => (uint)(c - '0') <= 9;

    /// <summary>
    /// Represents a text segment with an associated style.
    /// </summary>
    private readonly struct TextSegment
    {
        public readonly string Text;
        public readonly StyleState Style;
        public readonly bool HasStyle;

        public TextSegment(string text, StyleState style)
        {
            Text = text;
            Style = style;
            HasStyle = style.HasAnyStyle;
        }
    }

    /// <summary>
    /// Represents the current style state during parsing.
    /// </summary>
    private struct StyleState
    {
        public Color? Foreground;
        public Color? Background;
        public Decoration Decoration;
        public string? Link;

        public readonly bool HasAnyStyle => Foreground.HasValue || Background.HasValue || Decoration != Decoration.None || !string.IsNullOrEmpty(Link);

        public void Reset()
        {
            Foreground = null;
            Background = null;
            Decoration = Decoration.None;
            Link = null;
        }

        public readonly StyleState Clone() => new()
        {
            Foreground = Foreground,
            Background = Background,
            Decoration = Decoration,
            Link = Link
        };

        public readonly Style ToSpectreStyle()
        {
            return new Style(Foreground, Background, Decoration, Link);
        }

        public readonly string ToMarkup()
        {
            var parts = new List<string>();

            if (Foreground.HasValue)
            {
                parts.Add(Foreground.Value.ToMarkup());
            }
            else
            {
                parts.Add("Default ");

            }

            if (Background.HasValue)
                parts.Add($"on {Background.Value.ToMarkup()}");

            if (Decoration != Decoration.None)
            {
                if ((Decoration & Decoration.Bold) != 0) parts.Add("bold");
                if ((Decoration & Decoration.Dim) != 0) parts.Add("dim");
                if ((Decoration & Decoration.Italic) != 0) parts.Add("italic");
                if ((Decoration & Decoration.Underline) != 0) parts.Add("underline");
                if ((Decoration & Decoration.Strikethrough) != 0) parts.Add("strikethrough");
                if ((Decoration & Decoration.SlowBlink) != 0) parts.Add("slowblink");
                if ((Decoration & Decoration.RapidBlink) != 0) parts.Add("rapidblink");
                if ((Decoration & Decoration.Invert) != 0) parts.Add("invert");
                if ((Decoration & Decoration.Conceal) != 0) parts.Add("conceal");
            }

            if (!string.IsNullOrEmpty(Link))
            {
                parts.Add($"link={Link}");
            }

            return string.Join(" ", parts);
        }
    }
}

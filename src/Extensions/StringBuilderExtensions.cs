using System;
using System.Globalization;
using System.Text;
using Spectre.Console;

namespace PwshSpectreConsole.TextMate.Extensions;

/// <summary>
/// Provides optimized StringBuilder extension methods for text rendering operations.
/// Reduces string allocations during the markup generation process.
/// </summary>
public static class StringBuilderExtensions
{
    public static StringBuilder AppendWithStyle(this StringBuilder builder, Style? style, int? value)
    {
        return AppendWithStyle(builder, style, value?.ToString(CultureInfo.InvariantCulture));
    }

    public static StringBuilder AppendWithStyle(this StringBuilder builder, Style? style, string? value)
    {
        value ??= string.Empty;
        if (style != null)
        {
            return builder.Append('[')
                .Append(style.ToMarkup())
                .Append(']')
                .Append(value.EscapeMarkup())
                .Append("[/]");
        }
        return builder.Append(value);
    }

    public static StringBuilder AppendWithStyleN(this StringBuilder builder, Style? style, string? value)
    {
        value ??= string.Empty;
        if (style != null)
        {
            return builder.Append('[')
                .Append(style.ToMarkup())
                .Append(']')
                .Append(value)
                .Append("[/] ");
        }
        return builder.Append(value);
    }

    /// <summary>
    /// Efficiently appends text with optional style markup using spans to reduce allocations.
    /// This method is optimized for the common pattern of conditional style application.
    /// </summary>
    /// <param name="builder">StringBuilder to append to</param>
    /// <param name="style">Optional style to apply</param>
    /// <param name="value">Text content to append</param>
    /// <returns>The same StringBuilder for method chaining</returns>
    public static StringBuilder AppendWithStyle(this StringBuilder builder, Style? style, ReadOnlySpan<char> value)
    {
        if (style != null)
        {
            return builder.Append('[')
                .Append(style.ToMarkup())
                .Append(']')
                .Append(value)
                .Append("[/]");
        }
        return builder.Append(value);
    }
}

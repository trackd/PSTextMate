using System;
using System.Text;
using System.Globalization;
using Spectre.Console;


namespace PwshSpectreConsole.TextMate;
internal static class StringExtensions
{
  internal static string SubstringAtIndexes(this string str, int startIndex, int endIndex)
  {
    return str[startIndex..endIndex];
  }
}

internal static class StringBuilderExtensions
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

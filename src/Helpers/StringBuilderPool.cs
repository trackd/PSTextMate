using System.Collections.Concurrent;
using System.Text;

namespace PwshSpectreConsole.TextMate.Helpers;

internal static class StringBuilderPool
{
    private static readonly ConcurrentBag<StringBuilder> _bag = [];

    public static StringBuilder Rent()
    {
        if (_bag.TryTake(out StringBuilder? sb)) return sb;
        return new StringBuilder();
    }

    public static void Return(StringBuilder sb)
    {
        if (sb is null) return;
        sb.Clear();
        _bag.Add(sb);
    }
}

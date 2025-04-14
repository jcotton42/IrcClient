using System;

namespace IrcClient;

public static class SpanExtensions
{
    public static ReadOnlySpan<char> SkipAll(this ReadOnlySpan<char> s, char c) => s.IndexOfAnyExcept(c) switch
    {
        < 0 => ReadOnlySpan<char>.Empty,
        var i => s[i..],
    };

    public static DoubleReadOnlySpan<char> SplitOnceConsecutive(this ReadOnlySpan<char> s, char c)
    {
        var cIndex = s.IndexOf(c);
        if (cIndex < 0)
        {
            return new DoubleReadOnlySpan<char>(s, ReadOnlySpan<char>.Empty);
        }

        return new DoubleReadOnlySpan<char>(s[..cIndex], s[cIndex..].SkipAll(c));
    }
}

public readonly ref struct DoubleReadOnlySpan<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second)
{
    public readonly ReadOnlySpan<T> First = first;
    public readonly ReadOnlySpan<T> Second = second;

    public void Deconstruct(out ReadOnlySpan<T> first, out ReadOnlySpan<T> second)
    {
        first = First;
        second = Second;
    }
}

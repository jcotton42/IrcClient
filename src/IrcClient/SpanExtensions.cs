using System;

namespace IrcClient;

public static class SpanExtensions
{
    public static ReadOnlySpan<T> SkipAll<T>(this ReadOnlySpan<T> s, T value)
        where T : IEquatable<T>
        => s.IndexOfAnyExcept(value) switch
        {
            < 0 => ReadOnlySpan<T>.Empty,
            var i => s[i..],
        };

    public static DoubleReadOnlySpan<T> SplitOnce<T>(this ReadOnlySpan<T> s, T value)
        where T : IEquatable<T>
    {
        var index = s.IndexOf(value);
        return index < 0
            ? new DoubleReadOnlySpan<T>(s, ReadOnlySpan<T>.Empty)
            : new DoubleReadOnlySpan<T>(s[..index], s[(index + 1)..]);
    }

    public static DoubleReadOnlySpan<T> SplitOnceConsecutive<T>(this ReadOnlySpan<T> s, T value)
        where T : IEquatable<T>
    {
        var index = s.IndexOf(value);
        return index < 0
            ? new DoubleReadOnlySpan<T>(s, ReadOnlySpan<T>.Empty)
            : new DoubleReadOnlySpan<T>(s[..index], s[index..].SkipAll(value));
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

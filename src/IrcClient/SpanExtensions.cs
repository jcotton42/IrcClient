using System.Collections;

namespace IrcClient;

public static class SpanExtensions
{
    public static SplitEnumerable<T> SplitEnumerate<T>(this ReadOnlySpan<T> span, T item) where T : IEquatable<T> =>
        new SplitEnumerable<T>(span, item);
}

public readonly ref struct SplitEnumerable<T> where T : IEquatable<T>
{
    private readonly ReadOnlySpan<T> _span;
    private readonly T _item;

    public SplitEnumerable(ReadOnlySpan<T> span, T item)
    {
        _span = span;
        _item = item;
    }

    public SplitEnumerator<T> GetEnumerator() => new SplitEnumerator<T>(_span, _item);
}

public ref struct SplitEnumerator<T> where T : IEquatable<T>
{
    private ReadOnlySpan<T> _span;
    private readonly T _item;

    public SplitEnumerator(ReadOnlySpan<T> span, T item)
    {
        _span = span;
        _item = item;
    }

    public ReadOnlySpan<T> Current { get; private set; }
    public bool MoveNext()
    {
        if (_span.Length == 0) return false;
        if (_span.IndexOf(_item) is var index and not -1)
        {
            Current = _span[..index];
            _span = _span[(index + 1)..];
        }
        else
        {
            Current = _span;
            _span = default;
        }
        return true;
    }
}

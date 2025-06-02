namespace IrcClient.Tests;

public sealed class ArrayEqualityComparer<T> : IEqualityComparer<T[]>
{
    public static ArrayEqualityComparer<T> Instance { get; } = new();

    private ArrayEqualityComparer() { }

    public bool Equals(T[]? x, T[]? y) =>
        ReferenceEquals(x, y)
        || (x is not null && y is not null && x.SequenceEqual(y));

    public int GetHashCode(T[] obj)
    {
        var hash = new HashCode();
        foreach (var o in obj)
        {
            hash.Add(o);
        }

        return hash.ToHashCode();
    }
}

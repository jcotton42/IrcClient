using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace IrcClient.Generators;

public sealed class EquatableList<T> : IList<T>, IEquatable<EquatableList<T>>
{
    private readonly List<T> list = new();

    public IEnumerator<T> GetEnumerator() => list.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)list).GetEnumerator();

    public void Add(T item) => list.Add(item);

    public void Clear() => list.Clear();

    public bool Contains(T item) => list.Contains(item);

    public void CopyTo(T[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);

    public bool Remove(T item) => list.Remove(item);

    public int Count => list.Count;

    public bool IsReadOnly => false;

    public int IndexOf(T item) => list.IndexOf(item);

    public void Insert(int index, T item) => list.Insert(index, item);

    public void RemoveAt(int index) => list.RemoveAt(index);

    public T this[int index]
    {
        get => list[index];
        set => list[index] = value;
    }

    public override bool Equals(object? obj) => obj is EquatableList<T> other && Equals(other);

    public bool Equals(EquatableList<T> other) =>
        ReferenceEquals(list, other.list)
        || list.SequenceEqual(other.list);

    public override int GetHashCode()
    {
        var hash = new SystemHashCode();
        foreach (var item in list)
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
    }
}

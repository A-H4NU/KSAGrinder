using System.Collections;

namespace dsgen;

/// <summary>
/// Array wrapper that implements a proper GetHashCode() method.
/// </summary>
internal class EquatableArray : IEquatable<EquatableArray>, IEnumerable
{
    private readonly object[] _array;

    public int Length => _array.Length;

    public object this[int index]
    {
        get => _array[index];
    }

    public EquatableArray()
    {
        _array = Array.Empty<object>();
    }

    public EquatableArray(object[] array)
    {
        _array = new object[array.Length];
        Span<object> span = _array;
        for (int i = 0; i < array.Length; i++)
        {
            span[i] = array[i];
        }
    }

    public bool Equals(EquatableArray? other)
    {
        if (other is null || this.Length != other.Length)
            return false;
        for (int i = 0; i < this.Length; i++)
        {
            if (!this._array[i].Equals(other._array[i]))
                return false;
        }
        return true;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
            return true;
        return obj is EquatableArray other && Equals(other);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();
        for (int i = 0; i < Length; i++)
            hash.Add(_array[i]);
        return hash.ToHashCode();
    }

    public IEnumerator GetEnumerator()
    {
        return _array.GetEnumerator();
    }

    public static bool operator ==(EquatableArray a, EquatableArray b)
        => a.Equals(b);

    public static bool operator !=(EquatableArray a, EquatableArray b)
        => !(a == b);
}

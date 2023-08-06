namespace dsgen;

/// <summary>
/// Array wrapper that implements a proper GetHashCode() method.
/// </summary>
internal class EquatableArray : IEquatable<EquatableArray>
{
    private object[] _array;

    public int Length => _array.Length;

    public object this[int index]
    {
        get => _array[index];
        set => _array[index] = value;
    }

    public EquatableArray()
    {
        _array = Array.Empty<object>();
    }

    public EquatableArray(int length)
    {
        _array = new object[length];
    }

    public bool Equals(EquatableArray? other)
    {
        if (other is null || this.Length != other.Length)
            return false;
        for (int i = 0; i < this.Length; i++)
        {
            if (this._array[i] != other._array[i])
                return false;
        }
        return true;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;
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
}

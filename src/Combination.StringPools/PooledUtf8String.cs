namespace Combination.StringPools;

public readonly struct PooledUtf8String : IEquatable<PooledUtf8String>, IComparable<PooledUtf8String>
{
    private readonly ulong handle;

    public static PooledUtf8String Empty => default;

    internal PooledUtf8String(ulong handle)
    {
        this.handle = unchecked(handle + 1);
    }

    public static implicit operator string(PooledUtf8String s) => Utf8StringPool.Get(unchecked(s.handle - 1));

    public static bool operator ==(PooledUtf8String a, PooledUtf8String b)
    {
        return a.handle == b.handle || a.Equals(b);
    }

    public static bool operator !=(PooledUtf8String a, PooledUtf8String b)
    {
        if (a.handle == b.handle)
        {
            return false;
        }

        return !a.Equals(b);
    }

    public bool Equals(PooledUtf8String other)
    {
        return handle == other.handle || Utf8StringPool.StringsEqual(unchecked(handle - 1), unchecked(other.handle - 1));
    }

    public int CompareTo(PooledUtf8String other)
    {
        return Utf8StringPool.StringsCompare(unchecked(handle - 1), unchecked(other.handle - 1));
    }

    public override bool Equals(object? obj)
    {
        return obj is PooledUtf8String other && Equals(other);
    }

    public override int GetHashCode()
        => unchecked((int)StringHash.Compute(Utf8StringPool.GetBytes(unchecked(handle - 1))));

    public override string ToString() => Utf8StringPool.Get(unchecked(handle - 1));

    public int Length => Utf8StringPool.GetLength(unchecked(handle - 1));

    public IUtf8StringPool? StringPool => Utf8StringPool.GetStringPool(unchecked(handle - 1));

    public long Handle => unchecked((long)(handle ^ 0xaaaaaaaaaaaaaaaaUL));

    public bool IsEmpty => handle == 0;
}

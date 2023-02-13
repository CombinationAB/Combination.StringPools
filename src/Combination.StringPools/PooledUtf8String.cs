namespace Combination.StringPools;

public readonly struct PooledUtf8String : IEquatable<PooledUtf8String>
{
    private readonly ulong handle;

    public static PooledUtf8String Empty => new(ulong.MaxValue);

    internal PooledUtf8String(ulong handle)
    {
        this.handle = handle;
    }

    public static implicit operator string(PooledUtf8String s) => Utf8StringPool.Get(s.handle);

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
        return handle == other.handle || Utf8StringPool.StringsEqual(handle, other.handle);
    }

    public override bool Equals(object? obj)
    {
        return obj is PooledUtf8String other && Equals(other);
    }

    public override int GetHashCode()
        => unchecked((int)StringHash.Compute(Utf8StringPool.Get(handle)));

    public override string ToString() => Utf8StringPool.Get(handle);

    public int Length => Utf8StringPool.GetLength(handle);

    public IUtf8StringPool? StringPool => Utf8StringPool.GetStringPool(handle);

    public long Handle => unchecked((long)(handle ^ 0xaaaaaaaaaaaaaaaaUL));
}

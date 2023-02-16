namespace Combination.StringPools;

/// <summary>
/// Represents a pool of UTF-8 strings that supports deduplication of equal strings.
/// </summary>
public interface IUtf8DeduplicatedStringPool : IUtf8StringPool
{
    /// <summary>
    /// Returns a pooled string if it exists in the pool already, otherwise returns null.
    /// </summary>
    /// <param name="value">The .NET string or char sequence</param>
    /// <returns>The string pool reference, if it exists, otherwise null.</returns>
    PooledUtf8String? TryGet(ReadOnlySpan<char> value);

    /// <summary>
    /// Returns a pooled string if it exists in the pool already, otherwise returns null.
    /// </summary>
    /// <param name="value">The sequence of UTF-8 bytes</param>
    /// <returns>The string pool reference, if it exists, otherwise null.</returns>
    PooledUtf8String? TryGet(ReadOnlySpan<byte> value);
}

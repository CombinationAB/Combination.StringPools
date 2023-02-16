namespace Combination.StringPools;

/// <summary>
/// Represents a pool of UTF-8 strings.
/// </summary>
public interface IUtf8StringPool : IStringPool
{
    /// <summary>
    /// Adds a string to the pool.
    /// </summary>
    /// <param name="value">The sequence of chars to add.</param>
    /// <returns>A reference to the pooled string. Note that the same reference may be returned for several calls to Add if
    /// deduplication is used.</returns>
    PooledUtf8String Add(ReadOnlySpan<char> value);

    /// <summary>
    /// Adds a sequence of UTF-8 bytes to the pool. No validation is performed that the bytes are valid UTF-8.
    /// </summary>
    /// <param name="value">The sequence of bytes to add.</param>
    /// <returns>A reference to the pooled string. Note that the same reference may be returned for several calls to Add if
    /// deduplication is used.</returns>
    PooledUtf8String Add(ReadOnlySpan<byte> value);
}

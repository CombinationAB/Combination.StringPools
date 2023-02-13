namespace Combination.StringPools;

/// <summary>
/// Represents a pool of UTF-8 strings that supports deduplication of equal strings.
/// </summary>
public interface IUtf8DeduplicatedStringPool : IUtf8StringPool
{
    /// <summary>
    /// Returns a pooled string if it exists in the pool already, otherwise returns null.
    /// </summary>
    /// <param name="value">The .NET string</param>
    /// <returns>The string pool reference, if it exists, otherwise null.</returns>
    PooledUtf8String? TryGet(string value);
}

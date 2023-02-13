namespace Combination.StringPools;

/// <summary>
/// Represents a pool of UTF-8 strings.
/// </summary>
public interface IUtf8StringPool : IStringPool
{
    /// <summary>
    /// Adds a string to the pool.
    /// </summary>
    /// <param name="value">The .NET string to add.</param>
    /// <returns>A reference to the pooled string. Note that the same reference may be returned for several calls to Add if
    /// deduplication is used.</returns>
    PooledUtf8String Add(string value);
}

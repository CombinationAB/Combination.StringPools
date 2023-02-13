namespace Combination.StringPools;

/// <summary>
/// Stable hash functions.
/// </summary>
public static class StringHash
{
    private const ulong OffsetBasis = 2166136261UL;
    private const ulong Prime = 16777619UL;

    /// <summary>
    /// Computes the Fowler–Noll–Vo 1a hash of the given character range.
    /// </summary>
    /// <param name="span">The range of characters</param>
    /// <returns>The hash value</returns>
    public static ulong Compute(ReadOnlySpan<char> span)
    {
        var result = OffsetBasis;
        foreach (var t in span)
        {
            result = unchecked((result ^ t) * Prime);
        }

        return result;
    }
}

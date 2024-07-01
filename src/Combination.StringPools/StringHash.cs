using System.Text;
using System.IO.Hashing;
using System.Runtime.CompilerServices;

namespace Combination.StringPools;

/// <summary>
/// Stable hash functions.
/// </summary>
public static class StringHash
{
    /// <summary>
    /// Computes the XxHash3 hash of the given character range.
    /// </summary>
    /// <param name="span">The range of characters</param>
    /// <returns>The hash value</returns>
    public static ulong Compute(ReadOnlySpan<char> span)
        => Compute(Encoding.UTF8.GetBytes(span.ToArray()));

    /// Computes the XxHash3 hash of the given character range.
    /// </summary>
    /// <param name="span">The range of characters</param>
    /// <returns>The hash value</returns>
    public static ulong Compute(ReadOnlySpan<char> span) => Compute(Encoding.UTF8.GetBytes(span.ToArray()));

    /// <summary>
    /// Computes the XxHash3 hash of the given range of bytes representing an UTF-8 encoded string.
    /// </summary>
    /// <param name="span">The range of characters</param>
    /// <returns>The hash value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static ulong Compute(ReadOnlySpan<byte> span)
    {
        return XxHash3.HashToUInt64(span);
    }
}

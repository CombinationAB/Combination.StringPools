using System.Runtime.CompilerServices;
using System.Text;

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
        => ComputeRemainder(OffsetBasis, span);

    /// <summary>
    /// Computes the Fowler–Noll–Vo 1a hash of the given range of bytes representing an UTF-8 encoded string.
    /// </summary>
    /// <param name="span">The range of characters</param>
    /// <returns>The hash value</returns>
    public static ulong Compute(ReadOnlySpan<byte> span)
    {
        var result = OffsetBasis;
        var index = 0;
        foreach (var utf8Char in span)
        {
            if ((utf8Char & 0x80) != 0) // If not ASCII, we need to do UTF-8 decoding on remainder
            {
                if (span.Length > 1024)
                {
                    var chars = new char[span.Length - index];
                    var l = Encoding.UTF8.GetChars(span[index..], chars);
                    return ComputeRemainder(result, chars[..l]);
                }
                else
                {
                    // ReSharper disable once StackAllocInsideLoop - Always returns
                    Span<char> chars = stackalloc char[span.Length - index];
                    var l = Encoding.UTF8.GetChars(span[index..], chars);
                    return ComputeRemainder(result, chars[..l]);
                }
            }

            result = unchecked((result ^ utf8Char) * Prime);
            ++index;
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static ulong ComputeRemainder(ulong seed, ReadOnlySpan<char> span)
    {
        var result = seed;
        foreach (var t in span)
        {
            result = unchecked((result ^ t) * Prime);
        }

        return result;
    }
}

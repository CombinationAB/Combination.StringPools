namespace Combination.StringPools;

/// <summary>
/// Represents an abstract string pool.
/// </summary>
public interface IStringPool : IDisposable
{
    /// <summary>
    /// Number of bytes added to this string pool. This is the sum of the length of all strings that were added, plus
    /// the overhead for the string headers.
    /// </summary>
    long AddedBytes { get; }

    /// <summary>
    /// The number of bytes used for strings in this pool. This is typically less than <see cref="IStringPool.AddedBytes"/> if
    /// deduplication is used, as this only counts the unique strings that were actually added to the pool.
    /// </summary>
    long UsedBytes { get; }

    /// <summary>
    /// The total number of bytes allocated for this pool. This is typically larger than <see cref="IStringPool.UsedBytes"/> as
    /// it includes the unused but allocated pages.
    /// </summary>
    long AllocatedBytes { get; }
}

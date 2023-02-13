namespace Combination.StringPools;

/// <summary>
/// Creates instances of <see cref="IUtf8StringPool"/> and <see cref="IUtf8DeduplicatedStringPool"/>.
/// </summary>
public static class StringPool
{
    private const int DefaultPageSize = 16384; // Size of a pool memory page in bytes


    /// <summary>
    /// A Utf8 string pool that does not deduplicate strings, created with default page size and initial page count.
    /// </summary>
    /// <returns>The newly create string pool.</returns>
    public static IUtf8StringPool Utf8() => Utf8(DefaultPageSize, 1);

    /// <summary>
    /// A Utf8 string pool that does not deduplicate strings, created with specified page size and initial page count.
    /// </summary>
    /// <param name="pageSize">The size of the memory pages to allocate (allocation increment quantum).</param>
    /// <param name="initialPageCount">The initial number of pages to allocate.</param>
    /// <returns>The newly create string pool.</returns>
    public static IUtf8StringPool Utf8(int pageSize, int initialPageCount) => new Utf8StringPool(pageSize, initialPageCount, false);

    /// <summary>
    /// A Utf8 string pool that de-duplicates equal strings, created with default page size and initial page count.
    /// </summary>
    /// <returns>The newly create string pool.</returns>
    public static IUtf8DeduplicatedStringPool DeduplicatedUtf8() => DeduplicatedUtf8(DefaultPageSize, 1);

    /// <summary>
    /// A Utf8 string pool that de-duplicates equal strings, created with specified page size and initial page count.
    /// </summary>
    /// <param name="pageSize">The size of the memory pages to allocate (allocation increment quantum).</param>
    /// <param name="initialPageCount">The initial number of pages to allocate.</param>
    /// <returns>The newly create string pool.</returns>
    public static IUtf8DeduplicatedStringPool DeduplicatedUtf8(int pageSize, int initialPageCount) => new Utf8StringPool(pageSize, initialPageCount, true);

    /// <summary>
    /// The total number of bytes allocated in all string pools.
    /// </summary>
    public static long TotalAllocatedBytes => Utf8StringPool.totalAllocatedBytes;

    /// <summary>
    /// The total number of bytes used for strings in all string pools. This is typically less than <see cref="TotalAllocatedBytes"/>, as
    /// the latter includes the unused but allocated pages.
    /// </summary>
    public static long TotalUsedBytes => Utf8StringPool.totalUsedBytes;

    /// <summary>
    /// The total count of the strings added to this pool. In case of deduplicated string pools, this can be significantly larger than
    /// <see cref="TotalUsedBytes"/> since this metric is counting each instance of a similar string.
    /// </summary>
    public static long TotalAddedBytes => Utf8StringPool.totalAddedBytes;

    /// <summary>
    /// This event is fired when allocation changes (either a new page is allocated or a pages are released as a pool is disposed).
    ///
    /// Typically used for diagnostics and monitoring.
    /// </summary>
    public static event EventHandler AllocationChanged
    {
        add => Utf8StringPool.AllocationChanged += value;
        remove => Utf8StringPool.AllocationChanged -= value;
    }

    /// <summary>
    /// This event is fired when a string is added to the pool. In case of deduplicated string pools, this will only be fired for the first
    /// instance of a string.
    ///
    /// Typically used for diagnostics and monitoring.
    /// </summary>
    public static event EventHandler StringAdded
    {
        add => Utf8StringPool.StringAdded += value;
        remove => Utf8StringPool.StringAdded -= value;
    }
}

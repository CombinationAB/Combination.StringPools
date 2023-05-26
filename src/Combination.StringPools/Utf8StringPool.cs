using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

[assembly: InternalsVisibleTo("Combination.StringPools.Tests")]

// ReSharper disable InconsistentNaming - We used internal static fields to avoid unnecessary wrapping

namespace Combination.StringPools;

internal sealed class Utf8StringPool : IUtf8DeduplicatedStringPool
{
    private const int
        PoolIndexBits =
            24; // Number of bits to use for pool index in handle (more bits = more pools, but less strings per pool)

    private static readonly List<Utf8StringPool?> Pools = new();
    internal static long totalAllocatedBytes, totalUsedBytes, totalAddedBytes;

    private readonly List<nint> pages = new();
    private readonly int index;
    private long writePosition, usedBytes, addedBytes;

    private ulong[]? deduplicationTable;
    private int fillRate;
    private int deduplicationTableBits;

    private readonly int pageSize;

    private readonly object writeLock = new();
    private readonly object hashLock = new();

    private bool disposed = false;

    // ReSharper disable once MemberCanBePrivate.Global
    public Utf8StringPool(int pageSize, int initialPageCount, bool deduplicateStrings, int deduplicationTableBits)
    {
        if (pageSize < 16)
        {
            // We need at least 16 bytes to store the length of the string and an actual string
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }

        if (initialPageCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialPageCount));
        }

        if (deduplicateStrings)
        {
            deduplicationTable = new ulong[1 << deduplicationTableBits];
            this.deduplicationTableBits = deduplicationTableBits;
        }

        this.pageSize = pageSize;

        bool didAlloc;
        lock (Pools)
        {
            if (Pools.Count >= (1 << PoolIndexBits) - 1)
            {
                throw new InvalidOperationException("Too many string pools allocated in process");
            }

            Pools.Add(this);
            index = Pools.Count - 1;
            didAlloc = EnsureCapacity(initialPageCount);
        }

        if (didAlloc)
        {
            AllocationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    PooledUtf8String IUtf8StringPool.Add(ReadOnlySpan<char> value)
    {
        var utf8ByteCount = Encoding.UTF8.GetByteCount(value);
        if (utf8ByteCount < 16384)
        {
            // Use the stack for small strings
            Span<byte> utf8 = stackalloc byte[utf8ByteCount];
            Encoding.UTF8.GetBytes(value, utf8);
            return AddInternal(utf8);
        }

        var buffer = new byte[utf8ByteCount];
        Encoding.UTF8.GetBytes(value, buffer);
        return AddInternal(buffer);
    }

    PooledUtf8String IUtf8StringPool.Add(ReadOnlySpan<byte> value)
        => AddInternal(value);

    public static int GetAllocationSize(int length) => length + 1 + (BitOperations.Log2((uint)length) / 7);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private PooledUtf8String AddInternal(ReadOnlySpan<byte> value)
    {
        var length = value.Length;
        if (length == 0)
        {
            return PooledUtf8String.Empty;
        }

        var structLength = GetAllocationSize(length);
        if (structLength > pageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "String is too long to be pooled");
        }

        var stringHash = unchecked((int)StringHash.Compute(value));
        var didAlloc = false;

        Interlocked.Add(ref totalAddedBytes, structLength);
        Interlocked.Add(ref addedBytes, structLength);

        if (TryDeduplicate(stringHash, value, out var result))
        {
            return new PooledUtf8String(result);
        }

        ulong handle;
        nint writePtr;
        int pageStartOffset;
        lock (writeLock)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("String pool is already disposed");
            }

            var currentPageIndex = checked((int)(writePosition / pageSize));
            var pageWritePosition = writePosition % pageSize;

            if (pageSize - pageWritePosition >= structLength)
            {
                if (pageWritePosition == 0)
                {
                    didAlloc = EnsureCapacity(currentPageIndex + 1);
                }

                writePtr = pages[currentPageIndex];
                pageStartOffset = (int)pageWritePosition;
                writePosition += structLength;
            }
            else
            {
                ++currentPageIndex;
                writePosition = (currentPageIndex * pageSize) + structLength;
                didAlloc = EnsureCapacity(currentPageIndex + 1);
                writePtr = pages[currentPageIndex];
                pageStartOffset = 0;
            }

            handle = ((ulong)index << (64 - PoolIndexBits)) | (ulong)(writePosition - structLength);
        }

        Interlocked.Add(ref totalUsedBytes, structLength);
        Interlocked.Add(ref usedBytes, structLength);
        unsafe
        {
            var ptr = (byte*)(writePtr + pageStartOffset);
            var write = length;
            while (true)
            {
                if (write > 0x7f)
                {
                    *ptr++ = unchecked((byte)(0x80 | (write & 0x7f)));
                }
                else
                {
                    *ptr++ = unchecked((byte)write);
                    break;
                }

                write >>= 7;
            }

            var stringWritePtr = new Span<byte>(ptr, length);
            value.CopyTo(stringWritePtr);
        }

        AddToDeduplicationTable(stringHash, handle, value);

        if (didAlloc)
        {
            AllocationChanged?.Invoke(this, EventArgs.Empty);
        }

        StringAdded?.Invoke(this, EventArgs.Empty);

        return new PooledUtf8String(handle);
    }

    long IStringPool.AddedBytes => addedBytes;

    long IStringPool.UsedBytes => usedBytes;

    long IStringPool.AllocatedBytes => pages.Count * pageSize;

    PooledUtf8String? IUtf8DeduplicatedStringPool.TryGet(ReadOnlySpan<char> value)
    {
        var utf8ByteCount = Encoding.UTF8.GetByteCount(value);
        if (utf8ByteCount < 16384)
        {
            // Use the stack for small strings
            Span<byte> utf8 = stackalloc byte[utf8ByteCount];
            Encoding.UTF8.GetBytes(value, utf8);
            return TryGetInternal(utf8);
        }

        var buffer = new byte[utf8ByteCount];
        Encoding.UTF8.GetBytes(value, buffer);
        return TryGetInternal(buffer);
    }

    PooledUtf8String? IUtf8DeduplicatedStringPool.TryGet(ReadOnlySpan<byte> value)
        => TryGetInternal(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private PooledUtf8String? TryGetInternal(ReadOnlySpan<byte> value)
    {
        if (value.Length == 0)
        {
            return PooledUtf8String.Empty;
        }

        if (deduplicationTable is null)
        {
            throw new InvalidOperationException("Deduplication is not enabled for this pool");
        }

        if (!TryDeduplicate(unchecked((int)StringHash.Compute(value)), value, out var result))
        {
            return null;
        }

        return new PooledUtf8String(result);
    }

    private bool TryDeduplicate(int stringHash, ReadOnlySpan<byte> value, out ulong offset)
    {
        ulong[]? table;
        int tableIndex;
        lock (hashLock)
        {
            table = deduplicationTable;
            tableIndex = stringHash & ((1 << deduplicationTableBits) - 1);
        }

        if (table == null)
        {
            offset = ulong.MaxValue;
            return false;
        }

        var initialTableIndex = tableIndex;
        foreach (var limit in new[] { table.Length, initialTableIndex })
        {
            while (tableIndex < limit)
            {
                var val = table[tableIndex];
                if (val == 0)
                {
                    break;
                }

                var handle = val - 1;

                var poolOffset = handle & ((1UL << (64 - PoolIndexBits)) - 1);
                var poolBytes = GetStringBytes(poolOffset);
                if (poolBytes.Length == value.Length && value.SequenceEqual(poolBytes))
                {
                    offset = handle;
                    return true;
                }

                ++tableIndex;
            }

            tableIndex = 0;
        }


        offset = ulong.MaxValue;
        return false;
    }

    private ulong AddToDeduplicationTable(int stringHash, ulong handle, ReadOnlySpan<byte> value)
    {
        var storedHandle = handle + 1;
        lock (hashLock)
        {
            if (deduplicationTable == null)
            {
                return ulong.MaxValue;
            }

            if (1 + (fillRate * 2) >= deduplicationTable.Length)
            {
                var old = deduplicationTable;
                deduplicationTableBits++;
                deduplicationTable = new ulong[1 << deduplicationTableBits];
                fillRate = 0;
                for (var i = 0; i < old.Length; ++i)
                {
                    var val = old[i];
                    if (val != 0)
                    {
                        var copyHandle = val - 1;
                        var poolOffset = copyHandle & ((1UL << (64 - PoolIndexBits)) - 1);
                        var poolBytes = GetStringBytes(poolOffset);
                        AddToDeduplicationTable((int)StringHash.Compute(poolBytes), copyHandle, poolBytes);
                    }
                }
            }

            var tableIndex = stringHash & ((1 << deduplicationTableBits) - 1);
            var initialTableIndex = tableIndex;
            foreach (var limit in new[] { deduplicationTable.Length, initialTableIndex })
            {
                while (tableIndex < limit)
                {
                    var existing = deduplicationTable[tableIndex];
                    if (existing == 0)
                    {
                        deduplicationTable[tableIndex] = storedHandle;
                        ++fillRate;
                        return handle;
                    }

                    try
                    {
                        var poolOffset = (existing - 1) & ((1UL << (64 - PoolIndexBits)) - 1);
                        var poolBytes = GetStringBytes(poolOffset);
                        if (value.SequenceEqual(poolBytes))
                        {
                            return existing - 1;
                        }
                    }
                    catch (Exception e)
                    {
                        var existingPoolIndex = handle >> (64 - PoolIndexBits);
                        throw new InvalidOperationException($"Bad index {existing:x16} {tableIndex:x16} {limit:x16} {existingPoolIndex} of {index}", e);
                    }

                    ++tableIndex;
                }

                tableIndex = 0;
            }
        }

        throw new InvalidOperationException("Table is full. Internal error");
    }

    public static string Get(ulong handle)
    {
        if (handle == ulong.MaxValue)
        {
            return string.Empty;
        }

        return Encoding.UTF8.GetString(GetBytes(handle));
    }

    public static ReadOnlySpan<byte> GetBytes(ulong handle)
    {
        if (handle == ulong.MaxValue)
        {
            return Array.Empty<byte>();
        }

        var poolIndex = handle >> (64 - PoolIndexBits);
        if (poolIndex >= (ulong)Pools.Count)
        {
            throw new ArgumentException("Bad string pool offset", nameof(handle));
        }

        var pool = Pools[(int)poolIndex];
        if (pool is null)
        {
            throw new ObjectDisposedException("String pool is disposed");
        }

        return pool.GetFromPool(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private ReadOnlySpan<byte> GetFromPool(ulong handle)
    {
        if (disposed)
        {
            throw new ObjectDisposedException("String pool is disposed");
        }

        var offset = handle & ((1UL << (64 - PoolIndexBits)) - 1);
#if DEBUG
            var poolIndex = handle >> (64 - PoolIndexBits);
            var refPool = Pools[(int)poolIndex];
            if (refPool != this)
            {
                throw new InvalidOperationException($"Internal error: Deduplicated string pool mismatch ({index} != {poolIndex})");
            }
#endif
        return GetStringBytes(offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private ReadOnlySpan<byte> GetStringBytes(ulong offset)
    {
        var page = checked((int)(offset / (ulong)pageSize));
        var pageOffset = (int)(offset % (ulong)pageSize);
        if (page < 0 || page >= pages.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                $"Invalid handle value, page {page} is out of range 0..{pages.Count}, strings {addedBytes} {usedBytes}  --- {offset}");
        }

        unsafe
        {
            var ptr = (byte*)(pages[page] + pageOffset);
            var length = 0;
            var shl = 0;
            while (true)
            {
                var t = *ptr++;
                length += (t & 0x7f) << shl;
                shl += 7;
                if ((t & 0x80) == 0)
                {
                    break;
                }
            }

            return new ReadOnlySpan<byte>(ptr, length);
        }
    }

    public static int GetLength(ulong handle)
    {
        if (handle == ulong.MaxValue)
        {
            return 0;
        }

        var poolIndex = (handle >> (64 - PoolIndexBits)) & ((1 << PoolIndexBits) - 1);
        if (poolIndex >= (ulong)Pools.Count)
        {
            throw new ArgumentException("Bad string pool offset", nameof(handle));
        }

        var pool = Pools[(int)poolIndex];
        if (pool is null)
        {
            throw new ObjectDisposedException("String pool is disposed");
        }

        return pool.GetStringLength(handle & ((1L << (64 - PoolIndexBits)) - 1));
    }

    public static IUtf8StringPool? GetStringPool(ulong handle)
    {
        if (handle == ulong.MaxValue)
        {
            return null;
        }

        var poolIndex = handle >> (64 - PoolIndexBits);
        return poolIndex >= (ulong)Pools.Count ? null : Pools[(int)poolIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int GetStringLength(ulong offset)
    {
        var page = checked((int)(offset / (ulong)pageSize));
        var pageOffset = (int)(offset % (ulong)pageSize);
        if (page < 0 || page >= pages.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), $"Invalid handle value, page {page} is out of range 0..{pages.Count}");
        }

        unsafe
        {
            var ptr = (byte*)(pages[page] + pageOffset);
            var length = 0;
            var shl = 0;
            while (true)
            {
                var t = *ptr++;
                length += (t & 0x7f) << shl;
                shl += 7;
                if ((t & 0x80) == 0)
                {
                    break;
                }
            }

            return length;
        }
    }

    private bool EnsureCapacity(int numPages)
    {
        if (numPages <= pages.Count)
        {
            return false;
        }

        for (var i = pages.Count; i < numPages; i++)
        {
            pages.Add(Marshal.AllocHGlobal(pageSize));
            Interlocked.Add(ref totalAllocatedBytes, pageSize);
        }

        return true;
    }

    public static event EventHandler? AllocationChanged;

    public static event EventHandler? StringAdded;

    ~Utf8StringPool()
    {
        Deallocate();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Deallocate();
    }

    private void Deallocate()
    {
        disposed = true;
        lock (Pools)
        {
            Pools[index] = null;
        }

        lock (writeLock)
        {
            foreach (var page in pages)
            {
                Interlocked.Add(ref totalAllocatedBytes, -pageSize);
                Marshal.FreeHGlobal(page);
            }

            Interlocked.Add(ref totalUsedBytes, -usedBytes);
            Interlocked.Add(ref totalAddedBytes, -addedBytes);

            usedBytes = addedBytes = 0;

            pages.Clear();
        }

        // Don't send this here since we are being deallocated
        AllocationChanged?.Invoke(null, EventArgs.Empty);
    }

    internal static bool StringsEqual(ulong a, ulong b)
    {
        return StringsCompare(a, b) == 0;
    }

    public override string ToString() =>
        $"Utf8StringPool(bits={deduplicationTableBits}, dedup={deduplicationTable is not null}, pages={pages.Count}, used={usedBytes}, added={addedBytes})";

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int StringsCompare(ulong a, ulong b)
    {
        if (a == ulong.MaxValue)
        {
            return b == ulong.MaxValue ? 0 : -1;
        }

        if (b == ulong.MaxValue)
        {
            return 1;
        }

        if (a == b)
        {
            return 0;
        }

        var aPoolIndex = a >> (64 - PoolIndexBits);
        var bPoolIndex = b >> (64 - PoolIndexBits);
        if (aPoolIndex >= (ulong)Pools.Count)
        {
            throw new ArgumentException("Bad string pool offset", nameof(a));
        }

        if (bPoolIndex >= (ulong)Pools.Count)
        {
            throw new ArgumentException("Bad string pool offset", nameof(b));
        }

        var aOffset = a & ((1L << (64 - PoolIndexBits)) - 1);
        var bOffset = b & ((1L << (64 - PoolIndexBits)) - 1);
        var aPool = Pools[(int)aPoolIndex];
        if (aPool is null)
        {
            throw new ObjectDisposedException("String pool is disposed");
        }

        if (aPoolIndex != bPoolIndex)
        {
            var bPool = Pools[(int)bPoolIndex];
            if (bPool is null)
            {
                throw new ObjectDisposedException("String pool is disposed");
            }

            return aPool.GetStringBytes(aOffset).SequenceCompareTo(bPool.GetStringBytes(bOffset));
        }

        return aPool.GetStringBytes(aOffset).SequenceCompareTo(aPool.GetStringBytes(bOffset));
    }
}

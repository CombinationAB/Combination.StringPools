using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

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

    private readonly List<ulong>?[]? deduplicationTable;
    private readonly DisposeLock disposeLock = new();
    private readonly int deduplicationTableBits; // Number of bits to use for deduplication table (2^bits entries)
    private readonly int pageSize;

    private readonly object writeLock = new();

    // ReSharper disable once MemberCanBePrivate.Global
    public Utf8StringPool(int pageSize, int initialPageCount, bool deduplicateStrings, int deduplicationTableBits)
    {
        if (pageSize < 16)
        {
            // We need at least 16 bytes to store the length of the string and an actual string
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }

        if (deduplicationTableBits < 2 || deduplicationTableBits > 24)
        {
            throw new ArgumentOutOfRangeException(nameof(deduplicationTableBits));
        }

        if (initialPageCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialPageCount));
        }

        this.deduplicationTableBits = deduplicationTableBits;
        this.pageSize = pageSize;
        if (deduplicateStrings)
        {
            deduplicationTable = new List<ulong>?[1 << deduplicationTableBits];
        }

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

    PooledUtf8String IUtf8StringPool.Add(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return PooledUtf8String.Empty;
        }

        var length = Encoding.UTF8.GetByteCount(value);
        var structLength = length + 2;
        if (structLength > 0xffff || structLength > pageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "String is too long to be pooled");
        }

        var stringHash = 0;
        var didAlloc = false;
        var oldSize = usedBytes;

        Interlocked.Add(ref totalAddedBytes, structLength);
        Interlocked.Add(ref addedBytes, structLength);

        if (deduplicationTable is not null)
        {
            stringHash = unchecked((int)StringHash.Compute(value));
            if (TryDeduplicate(stringHash, length, value, out var result))
            {
                return new PooledUtf8String(result);
            }
        }

        lock (writeLock)
        {
            if (disposeLock.IsDisposed)
            {
                throw new ObjectDisposedException("String pool is already disposed");
            }

            if (oldSize != usedBytes && TryDeduplicate(stringHash, length, value, out var result))
            {
                return new PooledUtf8String(result);
            }

            var currentPageIndex = checked((int)(writePosition / pageSize));
            var pageWritePosition = writePosition % pageSize;

            nint writePtr;
            int pageStartOffset;
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

            Interlocked.Add(ref totalUsedBytes, structLength);
            Interlocked.Add(ref usedBytes, structLength);
            unsafe
            {
                *(ushort*)(writePtr + pageStartOffset) = checked((ushort)length);
                var stringWritePtr = new Span<byte>((byte*)(writePtr + pageStartOffset + 2), length);
                Encoding.UTF8.GetBytes(value, stringWritePtr);
            }

            var handle = ((ulong)index << (64 - PoolIndexBits)) | (ulong)(writePosition - structLength);

            if (deduplicationTable is not null)
            {
                AddToDeduplicationTable(stringHash, handle);
            }

            if (didAlloc)
            {
                AllocationChanged?.Invoke(this, EventArgs.Empty);
            }

            StringAdded?.Invoke(this, EventArgs.Empty);

            var ret = new PooledUtf8String(handle);
#if DEBUG
            var rVal = ret.ToString();

            if (usedBytes > pages.Count * pageSize)
            {
                throw new InvalidOperationException("Internal error: Pooled beyond allocation");
            }

            if (!rVal.Equals(value, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Internal error: Incorrect string pooled: '{value}' != '{rVal}'");
            }
#endif

            return ret;
        }
    }

    long IStringPool.AddedBytes => addedBytes;

    long IStringPool.UsedBytes => usedBytes;

    long IStringPool.AllocatedBytes => pages.Count * pageSize;

    PooledUtf8String? IUtf8DeduplicatedStringPool.TryGet(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return PooledUtf8String.Empty;
        }

        if (deduplicationTable is null)
        {
            throw new InvalidOperationException("Deduplication is not enabled for this pool");
        }

        var length = Encoding.UTF8.GetByteCount(value);
        if (!TryDeduplicate(unchecked((int)StringHash.Compute(value)), length, value, out var result))
        {
            return null;
        }

        return new PooledUtf8String(result);
    }

    private bool TryDeduplicate(int stringHash, int utf8ByteCount, string value, out ulong offset)
    {
        using (disposeLock.PreventDispose())
        {
            if (deduplicationTable == null)
            {
                offset = ulong.MaxValue;
                return false;
            }

            var tableIndex = stringHash & ((1 << deduplicationTableBits) - 1);
            var table = deduplicationTable[tableIndex];
            if (table is null)
            {
                offset = ulong.MaxValue;
                return false;
            }

            Span<byte> utf8 = stackalloc byte[utf8ByteCount];
            Encoding.UTF8.GetBytes(value, utf8);
            var ct = table.Count;
            for (var i = 0; i < ct; ++i)
            {
                var handle = table[i];
                var poolOffset = handle & ((1UL << (64 - PoolIndexBits)) - 1);
                var poolBytes = GetStringBytes(poolOffset);
                if (poolBytes.Length != utf8ByteCount || !utf8.SequenceEqual(poolBytes))
                {
                    continue;
                }

                offset = handle;
                return true;
            }

            offset = ulong.MaxValue;
            return false;
        }
    }

    private void AddToDeduplicationTable(int stringHash, ulong handle)
    {
        if (deduplicationTable == null)
        {
            return;
        }

        var tableIndex = stringHash & ((1 << deduplicationTableBits) - 1);
        var table = deduplicationTable[tableIndex] ?? (deduplicationTable[tableIndex] = new List<ulong>());

        table.Add(handle);
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
        using (disposeLock.PreventDispose())
        {
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
            var length = ((ushort*)(pages[page] + pageOffset))[0];
            return new ReadOnlySpan<byte>((byte*)(pages[page] + pageOffset + 2), length);
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
        using (disposeLock.PreventDispose())
        {
            var page = checked((int)(offset / (ulong)pageSize));
            var pageOffset = (int)(offset % (ulong)pageSize);
            if (page < 0 || page >= pages.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), $"Invalid handle value, page {page} is out of range 0..{pages.Count}");
            }

            return unchecked((ushort)Marshal.ReadInt16(pages[page] + pageOffset));
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
        disposeLock.BeginDispose();
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
        if (a == ulong.MaxValue)
        {
            return b == ulong.MaxValue;
        }

        if (b == ulong.MaxValue)
        {
            return false;
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

            using (aPool.disposeLock.PreventDispose())
            {
                using (bPool.disposeLock.PreventDispose())
                {
                    return aPool.GetStringBytes(aOffset).SequenceEqual(bPool.GetStringBytes(bOffset));
                }
            }
        }

        if (aPool.deduplicationTable is not null)
        {
            // If the strings are in the same deduplicated pool, we can just compare the offsets
            return aOffset == bOffset;
        }

        using (aPool.disposeLock.PreventDispose())
        {
            return aPool.GetStringBytes(aOffset).SequenceEqual(aPool.GetStringBytes(bOffset));
        }
    }

    public override string ToString() =>
        $"Utf8StringPool(bits={deduplicationTableBits}, dedup={deduplicationTable is not null}, pages={pages.Count}, used={usedBytes}, added={addedBytes})";
}

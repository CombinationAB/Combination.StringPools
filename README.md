# Combination.StringPools

Efficient string pools, used to reduce GC overhead, heap size and generally working set for applications with a large number of strings (especially a large number of equal strings). This library is optimized for read, while
the adding of strings to the pool is considered less frequent.


## Design goals

### UTF-8 representation

By using UTF-8 to represent strings instead of the default UTF-16 normally used by .NET. Expected savings are almost 50% since non-latin characters are rare. The .NET string type represents each character with 2 bytes, while UTF-8 represents all characters in the ASCII data set with only one byte. Unless in some special cases, such as Asian scripts, UTF-8 is a more compact representation for strings.

### Deduplication

In many applications, there are a lot of duplicate strings. While strings are typically immutable and passed by reference, the .NET runtime already does sort of deduplicate them, but this does not work when strings are created by transforming other strings, or deserializing them from data streams.

There is a built in string pool in .NET, which can be used by calling `string.Intern` on a string, but this is a global pool that never gets released. Additionally, string objects are large, so this is inefficient when dealing with a huge number of small strings.

All string pools in this library are letting the application control their lifecycle by implementing `IDisposable`. When trying to use a string from a disposed pool, an `ObjectDisposedException` will be thrown.

Note that a deduplicated string pool can be used as an efficient hash set. By using `TryGet`, it deterministically answers if the string is in the pool or not.

### Avoid GC pressure

This library uses compact unmanaged memory pages for the string pool. This will alleviate some of the problems with mixing long-lived and short-lived objects in the GC. Additionally, the overhead of a string is just 2 bytes over the UTF-8 string length. The .NET string is at least 32 bytes, plus alignment overhead. It makes a difference when dealing with millions of strings.

### Compact representation of reference

The reference to a `PooledUtf8String` is just a 64-bit struct, same as a pointer (although, it is indeed not a real pointer). This makes representing the strings compact and because it is a value type, the object allocation overhead (and size overhead) is avoided.

### Lock-free on read

The string pool is designed to be thread safe on all public methods. The read paths are written to be entirely lock free, avoiding lock contention when using an already populated pool. When adding strings to the pool, a lock will be taken around the updating of pointers, but the actual copying and deduplication happens outside of the lock, so there is still a performance benefit of using multiple threads to populate the pool.

## Limitations

### Maximum string length

It is not possible to pool strings larger than the page size (minus 2 bytes). The page size can be configured, but the absolute maximum length for a string is 65536 bytes in any case.

### Strings must be contiguous, i.e. fit within a page

If a string does not fit in the remainder of the page, a new page will be allocated. No attempt will be made to fill the gap with smaller strings later, so this space is lost. When creating string pools, use pages that are significantly larger than the average string length to avoid this problem.

### Strings cannot be removed from the pool

The string pool is intended to be safe to use and never return corrupt data. Because of this, once a string has been added to a pool it may never be removed. The pool itself can be disposed and recreated though (but see next section).

### Maximum number of string pools in a process

To help safeguard against old references to disposed string pools being reused, an identifier for a string pool will never be reused. This limits the number of string pools ever created in a process to the maximum number of string pools representable by their index (currently 2^24 or about 16 million).

### Maximum size of a string pool

The maximum size of a string pool is 2^40 or about one terabyte. Depending on the overhead of pages, the amount of usable strings may be less.

### Deduplication uses a fixed size hash table
The deduplication works by hashing the string and then in the associated bucket, performing a linear scan of the strings in the bucket. If the size of the string pool is known beforehand, the number of buckets in the hash table may be adjusted to optimize the tradeoff between many buckets (memory) and linear scanning (time taken to deduplicate).

## Benchmarks

The concrete numbers were established using the provided Performance project using BenchmarkDotNet (`Apple M1 Pro, 1 CPU, 10 logical and 10 physical cores, .NET SDK=7.0.100`).

### Additions

As with any hash table, the size of the table needs to be balanced against the number of items in the table.

| Number of strings | Mean | Error | StdDev |
|-|-|-|-|-|
| 1000000 | 10 | **437.56 ns** | **8.702 ns**  | **280.286 ns** |
| 100000  | 12 | **500.62 ns** | **10.979 ns** | **15.912 ns**  |
| 10000   | 16 | **105.96 ns** |  **2.142 ns** | **3.917 ns**   |

### Hashes

| String              | Mean     | Error    | StdDev   |
|-------------------- |---------:|---------:|---------:|
| Some ASCII string   | 13.35 ns | 0.252 ns | 0.377 ns |
| Some ünicöde string | 13.22 ns | 0.139 ns | 0.148 ns |

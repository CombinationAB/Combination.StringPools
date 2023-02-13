# Combination.StringPools

Efficient string pools, used to reduce GC overhead, heap size and generally working set for applications with a large number of strings (especially a large number of equal strings). This library is optimized for read, while
the adding of strings to the pool is considered less frequent.

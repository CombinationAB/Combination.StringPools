using BenchmarkDotNet.Attributes;
using Combination.StringPools;

namespace Performance;

#pragma warning disable CS8618
public class Hashing
{
    private static IUtf8DeduplicatedStringPool pool = StringPool.DeduplicatedUtf8(4096, 1, 12);

    [ParamsSource(nameof(CreateStrings))]
    public PooledUtf8String String;

    [Benchmark]
    public int Hash() => String.GetHashCode();

    public IEnumerable<PooledUtf8String> CreateStrings()
        => new[] { pool.Add("Some ASCII string"), pool.Add("Some ünicöde string") };
}

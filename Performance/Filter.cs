using BenchmarkDotNet.Attributes;
using Combination.StringPools;

namespace Performance;

#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CS8618
public class Filter
{
    internal static readonly Random Random = new();

    [Params(10_000, 32_525, 100_000, 1_000_000)]
    public int PoolSize;

    private IUtf8DeduplicatedStringPool filledPool;

    [GlobalSetup]
    public void Setup()
    {
        filledPool = StringPool.DeduplicatedUtf8(4096, 1);
        for (var i = 0; i < PoolSize; i++)
        {
            filledPool.Add(Guid.NewGuid().ToString());
        }
    }

    [Benchmark]
    public PooledUtf8String? DoTryGet()
    {
        return filledPool.TryGet("not-in-pool");
    }
}

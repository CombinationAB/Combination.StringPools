using System.Text;

namespace Combination.StringPools.Tests;

public class Test_Deduplication
{
    [Theory]
    [InlineData(10000, 10, 100)]
    public void Equal_Strings_Deduplicated(int numStrings, int numUniqueStrings, int stringSize)
    {
        using var pool = StringPool.DeduplicatedUtf8(1024, 1);
        Assert.Equal(0, pool.UsedBytes);
        Assert.Equal(1024, pool.AllocatedBytes);
        for (var i = 0; i < numStrings; ++i)
        {
            var someString = new string(Convert.ToChar('a' + (i % numUniqueStrings)), stringSize);
            pool.Add(someString);
        }

        Assert.Equal((stringSize + 2) * numUniqueStrings, pool.UsedBytes);
        for (var i = 0; i < numUniqueStrings * 2; ++i)
        {
            var someString = new string(Convert.ToChar('a' + i), stringSize);
            if (i < numUniqueStrings)
            {
                Assert.NotNull(pool.TryGet(someString));
            }
            else
            {
                Assert.Null(pool.TryGet(someString));
            }
        }
    }

    [Theory]
    [InlineData(2, 10000, 16)]
    [InlineData(100, 1000, 16)]
    public void Equal_Strings_Deduplicated_Thread_Safe(int numThreads, int numStrings, int stringSize)
    {
        // ReSharper disable AccessToDisposedClosure
        using var pool = StringPool.DeduplicatedUtf8(4096, 1);
        var threads = new List<Thread>();
        for (var ti = 0; ti < numThreads; ++ti)
        {
            var seed = ti;
            var t = new Thread(
                () =>
                {
                    for (var i = 0; i < numStrings; ++i)
                    {
                        var someString = new string(Convert.ToChar('a' + ((i + seed) % 10)), stringSize);
                        var str = pool.Add(someString);
                        Assert.Same(pool, str.StringPool);
                        Assert.Equal(someString, str);
                        Assert.Equal(str, pool.TryGet(someString));
                    }

                    Assert.Equal(10 * (2 + stringSize), pool.UsedBytes);
                });
            t.Start();
            threads.Add(t);
        }

        foreach (var t in threads)
        {
            t.Join();
        }

        Assert.Equal(10 * (2 + stringSize), pool.UsedBytes);
    }
}

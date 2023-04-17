using System.Text;

namespace Combination.StringPools.Tests;

public class Test_Deduplication
{
    [Theory]
    [InlineData(10000, 10, 100)]
    [InlineData(10000, 10, 9000)]
    public void Equal_Strings_Deduplicated(int numStrings, int numUniqueStrings, int stringSize)
    {
        using var pool = StringPool.DeduplicatedUtf8(stringSize < 1024 ? 1024 : 32768, 1);
        Assert.Equal(0, pool.UsedBytes);
        Assert.Equal(stringSize < 1024 ? 1024 : 32768, pool.AllocatedBytes);
        for (var i = 0; i < numStrings; ++i)
        {
            var someString = new string(Convert.ToChar('ä' + (i % numUniqueStrings)), stringSize);
            pool.Add(someString);
        }

        Assert.Equal(((stringSize * 2) + 2) * numUniqueStrings, pool.UsedBytes);
        for (var i = 0; i < numUniqueStrings * 2; ++i)
        {
            var someString = new string(Convert.ToChar('ä' + i), stringSize);
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
    [InlineData(10000, 10, 100)]
    public void Equal_Strings_Deduplicated_Bytes(int numStrings, int numUniqueStrings, int stringSize)
    {
        using var pool = StringPool.DeduplicatedUtf8(1024, 1);
        Assert.Equal(0, pool.UsedBytes);
        Assert.Equal(1024, pool.AllocatedBytes);
        for (var i = 0; i < numStrings; ++i)
        {
            var someString = new string(Convert.ToChar('ä' + (i % numUniqueStrings)), stringSize);
            var bytes = Encoding.UTF8.GetBytes(someString);
            pool.Add(bytes);
        }

        Assert.Equal(((stringSize * 2) + 2) * numUniqueStrings, pool.UsedBytes);
        for (var i = 0; i < numUniqueStrings * 2; ++i)
        {
            var someString = new string(Convert.ToChar('ä' + i), stringSize);
            var bytes = Encoding.UTF8.GetBytes(someString);
            if (i < numUniqueStrings)
            {
                Assert.NotNull(pool.TryGet(bytes));
            }
            else
            {
                Assert.Null(pool.TryGet(bytes));
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
                        var str2 = pool.TryGet(someString);
                        Assert.NotNull(str2);
                        var str2Value = str2.Value;
                        if (!str.Equals(str2Value))
                        {
                            Assert.True(str.Equals(str2Value));
                        }

                        Assert.Equal(str, str2);
                    }
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

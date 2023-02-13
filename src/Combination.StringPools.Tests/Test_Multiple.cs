namespace Combination.StringPools.Tests;

public class Test_Multiple
{
    [Fact]
    public void Alloc_Free()
    {
        var pool1 = StringPool.Utf8(4096, 1);
        Assert.Equal(0, pool1.UsedBytes);
        Assert.Equal(4096, pool1.AllocatedBytes);
        var pool2 = StringPool.Utf8(4096, 1);
        Assert.Equal(0, pool1.UsedBytes);
        Assert.Equal(4096, pool1.AllocatedBytes);
        Assert.Equal(0, pool2.UsedBytes);
        Assert.Equal(4096, pool2.AllocatedBytes);
        pool1.Dispose();
        Assert.Equal(0, pool1.UsedBytes);
        Assert.Equal(0, pool1.AllocatedBytes);
        Assert.Equal(0, pool2.UsedBytes);
        Assert.Equal(4096, pool2.AllocatedBytes);
        pool2.Dispose();
        Assert.Equal(0, pool2.UsedBytes);
        Assert.Equal(0, pool2.AllocatedBytes);
    }

    [Fact]
    public void String_Belongs_To_Pool()
    {
        using var pool = StringPool.Utf8(4096, 1);
        var str = pool.Add("Hello");
        Assert.Same(pool, str.StringPool);
    }

    [Fact]
    public void Empty_String_Belongs_To_No_Pool()
    {
        using var pool = StringPool.Utf8(4096, 1);
        var str = pool.Add("");
        Assert.Null(str.StringPool);
    }

    [Fact]
    public void Empty_String_Not_Disposed_With_Pool()
    {
        var pool = StringPool.Utf8(4096, 1);
        var str = pool.Add("");
        Assert.Equal(string.Empty, (string)str);
        Assert.Equal(0, str.Length);
        pool.Dispose();
        Assert.Equal(string.Empty, (string)str);
        Assert.Equal(0, str.Length);
    }

    [Theory]
    [InlineData(2, 10000, 16)]
    [InlineData(100, 1000, 16)]
    public void Multiple_Pools_Kept_Separate(int numPools, int stringsPerPool, int stringSize)
    {
        var pools = Enumerable.Range(0, numPools).Select(_ => StringPool.Utf8(4096, 1)).ToArray();
        for (var i = 0; i < stringsPerPool; ++i)
        {
            var someString = new string(Convert.ToChar('a' + (i % 10)), stringSize);
            foreach (var pool in pools)
            {
                var str = pool.Add(someString);
                Assert.Same(pool, str.StringPool);
            }
        }

        foreach (var pool in pools)
        {
            Assert.Equal((stringSize + 2) * stringsPerPool, pool.UsedBytes);
            pool.Dispose();
            Assert.Equal(0, pool.UsedBytes);
        }
    }

    [Theory]
    [InlineData(2, 10000, 16)]
    [InlineData(100, 1000, 16)]
    public void Multiple_Pools_Kept_Separate_Thread_Safe(int numThreads, int stringsPerPool, int stringSize)
    {
        var threads = new List<Thread>();
        for (var ti = 0; ti < numThreads; ++ti)
        {
            var t = new Thread(
                () =>
                {
                    using var pool = StringPool.Utf8(4096, 1);
                    for (var i = 0; i < stringsPerPool; ++i)
                    {
                        var someString = new string(Convert.ToChar('a' + (i % 10)), stringSize);
                        var str = pool.Add(someString);
                        Assert.Same(pool, str.StringPool);
                    }

                    Assert.Equal(stringsPerPool * (2 + stringSize), pool.UsedBytes);
                });
            t.Start();
            threads.Add(t);
        }

        foreach (var t in threads)
        {
            t.Join();
        }
    }

    [Theory]
    [InlineData(2, 10000, 16)]
    [InlineData(100, 1000, 16)]
    public void Multiple_Deduplicated_Pools_Kept_Separate_Thread_Safe(int numThreads, int stringsPerPool, int stringSize)
    {
        var threads = new List<Thread>();
        for (var ti = 0; ti < numThreads; ++ti)
        {
            var t = new Thread(
                () =>
                {
                    using var pool = StringPool.DeduplicatedUtf8(4096, 1);
                    for (var i = 0; i < stringsPerPool; ++i)
                    {
                        var someString = new string(Convert.ToChar('a' + (i % 10)), stringSize);
                        var str = pool.Add(someString);
                        Assert.Same(pool, str.StringPool);
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
    }
}

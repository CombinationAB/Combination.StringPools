namespace Combination.StringPools.Tests;

public class Test_Allocation
{
    [Fact]
    public void Default_Is_Empty()
    {
        var string1 = PooledUtf8String.Empty;

        PooledUtf8String string2 = default;
        Assert.Equal(string1, string2);
        Assert.Equal(0, string2.Length);
    }

    [Fact]
    public void Freed_On_Dispose()
    {
        var pool = StringPool.Utf8(4096, 1);
        Assert.Equal(0, pool.UsedBytes);
        Assert.Equal(4096, pool.AllocatedBytes);
        pool.Dispose();
        Assert.Equal(0, pool.UsedBytes);
        Assert.Equal(0, pool.AllocatedBytes);
    }

    [Theory]
    [InlineData(16)]
    [InlineData(19)]
    [InlineData(1024)]
    [InlineData(15000)]
    public void Free_Multiple_Pages(int numPages)
    {
        var pool = StringPool.Utf8(4096, numPages);
        Assert.Equal(0, pool.UsedBytes);
        Assert.Equal(4096 * numPages, pool.AllocatedBytes);
        pool.Dispose();
        Assert.Equal(0, pool.UsedBytes);
        Assert.Equal(0, pool.AllocatedBytes);
    }

    [Theory]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(19)]
    [InlineData(1024)]
    [InlineData(16384)]
    [InlineData(65535)]
    public void Add_String_Smaller_Than_Page_Succeeds(int pageSize)
    {
        using var pool = StringPool.Utf8(pageSize, 1);
        Assert.Equal(0, pool.UsedBytes);
        Assert.Equal(pageSize, pool.AllocatedBytes);
        var someString = new string('c', pageSize - 2);
        var pooledString = pool.Add(someString);
        Assert.Equal(pageSize, pool.UsedBytes);
        Assert.Equal(pageSize, pool.AllocatedBytes);
        Assert.Equal(someString, (string)pooledString);
    }

    [Theory]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(19)]
    [InlineData(1024)]
    [InlineData(16384)]
    [InlineData(65535)]
    public void Add_String_Larger_Than_Page_Fails(int pageSize)
    {
        using var pool = StringPool.Utf8(pageSize, 1);
        Assert.Equal(0, pool.UsedBytes);
        Assert.Equal(pageSize, pool.AllocatedBytes);
        var someString = new string('c', pageSize - 1);
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => pool.Add(someString));
        Assert.Contains("String is too long to be pooled", exception.Message);
    }

    [Theory]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(19)]
    [InlineData(1024)]
    [InlineData(16384)]
    [InlineData(65535)]
    public void Two_Strings_Fit_Same_Page(int pageSize)
    {
        using var pool = StringPool.Utf8(pageSize, 1);
        Assert.Equal(0, pool.UsedBytes);
        Assert.Equal(pageSize, pool.AllocatedBytes);
        var string1 = new string('c', (pageSize / 2) - 2);
        var pooledString1 = pool.Add(string1);
        Assert.Equal(pageSize / 2, pool.UsedBytes);
        Assert.Equal(pageSize, pool.AllocatedBytes);
        var string2 = new string('d', pageSize - (pageSize / 2) - 2);
        var pooledString2 = pool.Add(string2);
        Assert.Equal(pageSize, pool.UsedBytes);
        Assert.Equal(pageSize, pool.AllocatedBytes);
        Assert.Equal(string1, (string)pooledString1);
        Assert.Equal(string2, (string)pooledString2);
    }

    [Theory]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(19)]
    [InlineData(1024)]
    [InlineData(16384)]
    [InlineData(65535)]
    public void Two_Strings_Dont_Fit_Need_More_Space(int pageSize)
    {
        var stringSize = (pageSize / 2) - 1;
        using var pool = StringPool.Utf8(pageSize, 1);
        Assert.Equal(0, pool.UsedBytes);
        Assert.Equal(pageSize, pool.AllocatedBytes);
        var string1 = new string('c', stringSize);
        var pooledString1 = pool.Add(string1);
        Assert.Equal(stringSize + 2, pool.UsedBytes);
        Assert.Equal(pageSize, pool.AllocatedBytes);
        var string2 = new string('d', stringSize);
        var pooledString2 = pool.Add(string2);
        Assert.Equal((2 * stringSize) + 4, pool.UsedBytes);
        Assert.Equal(2 * pageSize, pool.AllocatedBytes);
        Assert.Equal(string1, (string)pooledString1);
        Assert.Equal(stringSize, pooledString1.Length);
        Assert.Equal(string2, (string)pooledString2);
        Assert.Equal(stringSize, pooledString2.Length);
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(16, 1)]
    [InlineData(16, 2)]
    public void Dispose_Thread_Safe(int numThreads, int numPages)
    {
        // ReSharper disable AccessToDisposedClosure
        var pool = StringPool.Utf8(4096, numPages);
        var threads = new List<Thread>();
        var numStarted = 0;
        var str = pool.Add("foobar");
        var numDisposed = 0;
        var stringSum = 0;
        for (var ti = 0; ti < numThreads; ++ti)
        {
            var seed = ti;
            var t = new Thread(
                () =>
                {
                    try
                    {
                        var first = true;
                        while (true)
                        {
                            Interlocked.Add(ref stringSum, str.ToString().Length);
                            if (first)
                            {
                                Interlocked.Increment(ref numStarted);
                                first = false;
                            }
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        Interlocked.Increment(ref numDisposed);
                    }
                });
            t.Priority = ThreadPriority.AboveNormal;
            t.Start();
            threads.Add(t);
        }

        SpinWait.SpinUntil(() => numStarted == numThreads);
        pool.Dispose();

        foreach (var t in threads)
        {
            t.Join();
        }

        Assert.Equal(numThreads, numDisposed);
    }

    [Theory]
    [MemberData(nameof(Sizes))]
    public void Successive_Sizes(int stringSize)
    {
        using var pool = StringPool.Utf8(16, 1);
        for (var i = 0; i < 100; ++i)
        {
            var str = new string(Convert.ToChar(i), stringSize);
            var s = pool.Add(str);
            Assert.Equal(str, (string)s);
        }
    }

    public static IEnumerable<object[]> Sizes = Enumerable.Range(0, 14).Select(x => new object[] { x });

    [Theory]
    [InlineData(2, 1)]
    [InlineData(16, 1)]
    [InlineData(16, 2)]
    public void Add_Deduplicated_Dispose_Thread_Safe(int numThreads, int numPages)
    {
        // ReSharper disable AccessToDisposedClosure
        var pool = StringPool.DeduplicatedUtf8(4096, numPages);
        var threads = new List<Thread>();
        var numStarted = 0;
        var numDisposed = 0;
        for (var ti = 0; ti < numThreads; ++ti)
        {
            var seed = ti;
            var t = new Thread(
                () =>
                {
                    try
                    {
                        for (var i = 0; ; ++i)
                        {
                            var str = pool.Add("foobar " + ((seed + i) % 1000));
                            ;
                            if (i == 10000)
                            {
                                Interlocked.Increment(ref numStarted);
                            }
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        Interlocked.Increment(ref numDisposed);
                    }
                });
            t.Priority = ThreadPriority.AboveNormal;
            t.Start();
            threads.Add(t);
        }

        SpinWait.SpinUntil(() => numStarted == numThreads);
        pool.Dispose();

        foreach (var t in threads)
        {
            t.Join();
        }
        Assert.Equal(numThreads, numDisposed);
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(16, 1)]
    [InlineData(16, 2)]
    public void Add_Deduplicated_Thread_Safe(int numThreads, int numPages)
    {
        // ReSharper disable AccessToDisposedClosure
        using var pool = StringPool.DeduplicatedUtf8(4096, numPages);
        var threads = new List<Thread>();
        var numStarted = 0;
        var numStopped = 0;
        var stringSum = 0;
        var stopped = false;
        var o = new object();
        for (var ti = 0; ti < numThreads; ++ti)
        {
            var t = new Thread(
                () =>
                {
                    try
                    {
                        for (var i = 0; !stopped; ++i)
                        {
                            var str = pool.Add("foobar " + (i % 1000));
                            Interlocked.Add(ref stringSum, 2 + str.ToString().Length);
                            if (i == 10000)
                            {
                                Interlocked.Increment(ref numStarted);
                            }
                        }
                    }
                    finally
                    {
                        Interlocked.Increment(ref numStopped);
                    }
                });
            t.Priority = ThreadPriority.AboveNormal;
            t.Start();
            threads.Add(t);
        }

        SpinWait.SpinUntil(() => numStarted == numThreads);
        stopped = true;
        foreach (var t in threads)
        {
            t.Join();
        }
        Assert.Equal(numStopped, numStarted);
        Assert.Equal(stringSum, pool.AddedBytes);
        var used = pool.UsedBytes;
        var sum = 0L;
        for (var i = 0; i < 1000; ++i)
        {
            var len = 2 + ("foobar " + i).Length;
            sum += len;
        }

        Assert.Equal(sum, used);
    }
}

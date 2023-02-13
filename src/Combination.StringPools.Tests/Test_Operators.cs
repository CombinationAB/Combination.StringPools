using System.Runtime.InteropServices;

namespace Combination.StringPools.Tests;

public class Test_Operators
{
    [Fact]
    public void Test_String_Conversion()
    {
        Assert.Equal(string.Empty, PooledUtf8String.Empty.ToString());
        Assert.Equal(string.Empty, (string)PooledUtf8String.Empty);
        using var pool = StringPool.Utf8(4096, 1);
        Assert.Equal(string.Empty, pool.Add(string.Empty).ToString());
        Assert.Equal(string.Empty, pool.Add("").ToString());
        Assert.Equal("Hello", pool.Add("Hello").ToString());
        Assert.Equal("Hello", pool.Add("Hello").ToString());
        Assert.Equal("Hello", (string)pool.Add("Hello"));
    }

    [Fact]
    public void Test_Equality_Same_Pool()
    {
        using var pool = StringPool.Utf8(4096, 1);
        var str1 = pool.Add("Hello");
        var str2 = pool.Add("Hello");
        Assert.Equal(str1, str2);
        Assert.NotEqual(str1.Handle, str2.Handle);
        Assert.True(str1.Equals(str2));
        Assert.True(str1.Equals((object)str2));
        Assert.False(str1.Equals(null));
        Assert.True(str1 == str2);
        Assert.False(str1 != str2);
        Assert.Equal(str1.GetHashCode(), str2.GetHashCode());
    }

    [Fact]
    public void Test_Inequality_Same_Pool()
    {
        using var pool = StringPool.Utf8(4096, 1);
        var str1 = pool.Add("Hello");
        var str2 = pool.Add("World");
        Assert.NotEqual(str1, str2);
        Assert.NotEqual(str1.Handle, str2.Handle);
        Assert.False(str1.Equals(str2));
        Assert.False(str1.Equals((object)str2));
        Assert.False(str1.Equals(null));
        Assert.False(str1 == str2);
        Assert.True(str1 != str2);
        Assert.NotEqual(str1.GetHashCode(), str2.GetHashCode());
    }

    [Fact]
    public void Test_Equality_Same_Pool_Deduplicated()
    {
        using var pool = StringPool.DeduplicatedUtf8(4096, 1);
        var str1 = pool.Add("Hello");
        var str2 = pool.Add("Hello");
        Assert.Equal(str1, str2);
        Assert.Equal(str1.Handle, str2.Handle);
        Assert.True(str1.Equals(str2));
        Assert.True(str1.Equals((object)str2));
        Assert.False(str1.Equals(null));
        Assert.True(str1 == str2);
        Assert.False(str1 != str2);
        Assert.Equal(str1.GetHashCode(), str2.GetHashCode());
    }

    [Fact]
    public void Test_Inequality_Same_Pool_Deduplicated()
    {
        using var pool = StringPool.DeduplicatedUtf8(4096, 1);
        var str1 = pool.Add("Hello");
        var str2 = pool.Add("World");
        Assert.NotEqual(str1, str2);
        Assert.NotEqual(str1.Handle, str2.Handle);
        Assert.False(str1.Equals(str2));
        Assert.False(str1.Equals((object)str2));
        Assert.False(str1.Equals(null));
        Assert.False(str1 == str2);
        Assert.True(str1 != str2);
        Assert.NotEqual(str1.GetHashCode(), str2.GetHashCode());
    }

    [Fact]
    public void Test_Equality_Different_Pools()
    {
        using var pool1 = StringPool.Utf8(4096, 1);
        using var pool2 = StringPool.Utf8(4096, 1);
        var str1 = pool1.Add("Hello");
        var str2 = pool2.Add("Hello");
        Assert.Equal(str1, str2);
        Assert.NotEqual(str1.Handle, str2.Handle);
        Assert.True(str1.Equals(str2));
        Assert.True(str1.Equals((object)str2));
        Assert.False(str1.Equals(null));
        Assert.True(str1 == str2);
        Assert.False(str1 != str2);
        Assert.Equal(str1.GetHashCode(), str2.GetHashCode());
    }

    [Fact]
    public void Test_Inequality_Different_Pools()
    {
        using var pool1 = StringPool.Utf8(4096, 1);
        using var pool2 = StringPool.Utf8(4096, 1);
        var str1 = pool1.Add("Hello");
        var str2 = pool2.Add("World");
        Assert.NotEqual(str1, str2);
        Assert.NotEqual(str1.Handle, str2.Handle);
        Assert.False(str1.Equals(str2));
        Assert.False(str1.Equals((object)str2));
        Assert.False(str1.Equals(null));
        Assert.False(str1 == str2);
        Assert.True(str1 != str2);
        Assert.NotEqual(str1.GetHashCode(), str2.GetHashCode());
    }

    [Fact]
    public void Test_Equality_Different_Pools_Deduplicated()
    {
        using var pool1 = StringPool.DeduplicatedUtf8(4096, 1);
        using var pool2 = StringPool.DeduplicatedUtf8(4096, 1);
        var str1 = pool1.Add("Hello");
        var str2 = pool2.Add("Hello");
        Assert.Equal(str1, str2);
        Assert.NotEqual(str1.Handle, str2.Handle);
        Assert.True(str1.Equals(str2));
        Assert.True(str1.Equals((object)str2));
        Assert.False(str1.Equals(null));
        Assert.True(str1 == str2);
        Assert.False(str1 != str2);
        Assert.Equal(str1.GetHashCode(), str2.GetHashCode());
    }

    [Fact]
    public void Test_Inequality_Different_Pools_Deduplicated()
    {
        using var pool1 = StringPool.DeduplicatedUtf8(4096, 1);
        using var pool2 = StringPool.DeduplicatedUtf8(4096, 1);
        var str1 = pool1.Add("Hello");
        var str2 = pool2.Add("World");
        Assert.NotEqual(str1, str2);
        Assert.NotEqual(str1.Handle, str2.Handle);
        Assert.False(str1.Equals(str2));
        Assert.False(str1.Equals((object)str2));
        Assert.False(str1.Equals(null));
        Assert.False(str1 == str2);
        Assert.True(str1 != str2);
        Assert.NotEqual(str1.GetHashCode(), str2.GetHashCode());
    }
}

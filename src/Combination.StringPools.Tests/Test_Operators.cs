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
    public void Test_Compare_Empty()
    {
        using var pool = StringPool.Utf8(4096, 1);
        var s = pool.Add("foo");
        Assert.NotEqual(PooledUtf8String.Empty, s);
        Assert.NotEqual(s, PooledUtf8String.Empty);
        Assert.False(s == PooledUtf8String.Empty);
        Assert.False(PooledUtf8String.Empty == s);
        Assert.True(s != PooledUtf8String.Empty);
        Assert.True(PooledUtf8String.Empty != s);
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

    [Theory]
    [InlineData("a", "b")]
    [InlineData("b", "a")]
    [InlineData("a", "a")]
    [InlineData("", "")]
    [InlineData(" ", " ")]
    [InlineData("", "b")]
    [InlineData("a", "")]
    [InlineData("a", "å")]
    [InlineData("åå", "å")]
    [InlineData("åaaaå", "åaaaå")]
    public void Test_Compare_Different_Pools(string a, string b)
    {
        using var pool1 = StringPool.DeduplicatedUtf8(4096, 1);
        using var pool2 = StringPool.DeduplicatedUtf8(4096, 1);
        var poolA = pool1.Add(a);
        var poolB = pool2.Add(b);
        Assert.Equal(Sign(string.Compare(a, b, StringComparison.Ordinal)), Sign(poolA.CompareTo(poolB)));
    }

    [Theory]
    [InlineData("a", "b")]
    [InlineData("b", "a")]
    [InlineData("a", "a")]
    [InlineData("", "")]
    [InlineData(" ", " ")]
    [InlineData("", "b")]
    [InlineData("a", "")]
    [InlineData("a", "å")]
    [InlineData("åå", "å")]
    [InlineData("åaaaå", "åaaaå")]
    public void Test_Compare_Same_Pool(string a, string b)
    {
        using var pool = StringPool.DeduplicatedUtf8(4096, 1);
        var poolA = pool.Add(a);
        var poolB = pool.Add(b);
        Assert.Equal(Sign(string.Compare(a, b, StringComparison.Ordinal)), Sign(poolA.CompareTo(poolB)));
    }

    private static int Sign(int x) => x > 0 ? 1 : x < 0 ? -1 : 0;
}

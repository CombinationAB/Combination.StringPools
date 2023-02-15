using System.Text;

// ReSharper disable StringLiteralTypo

namespace Combination.StringPools.Tests;

public class Test_Hash
{
    [Fact]
    public void Same_String_Yield_Same_Hash()
    {
        using var pool1 = StringPool.Utf8(4096, 1);
        var string1 = pool1.Add("some string");
        var string2 = pool1.Add("some string");
        Assert.Equal(string1.GetHashCode(), string2.GetHashCode());
    }

    [Fact]
    public void Two_Pools_Yield_Same_Hash()
    {
        using var pool1 = StringPool.Utf8(4096, 1);
        using var pool2 = StringPool.Utf8(4096, 1);
        var string1 = pool1.Add("some string");
        var string2 = pool2.Add("some string");
        Assert.Equal(string1.GetHashCode(), string2.GetHashCode());
    }

    [Theory]
    [InlineData("Hello")]
    [InlineData("ä")]
    [InlineData("Hällo")]
    [InlineData("煊")]
    [InlineData("a煊")]
    public void Test_Hashes_Same(string str)
    {
        var hash = StringHash.Compute(str);
        var byteHash = StringHash.Compute(Encoding.UTF8.GetBytes(str));
        Assert.Equal(hash, byteHash);
    }

    [Theory]
    [InlineData("", 'a')]
    [InlineData("", 'ä')]
    [InlineData("", '煊')]
    public void Test_Large_Hash_Same(string prefix, char c)
    {
        var str = prefix + new string(c, 2048);
        var hash = StringHash.Compute(str);
        var byteHash = StringHash.Compute(Encoding.UTF8.GetBytes(str));
        Assert.Equal(hash, byteHash);
    }

    [Theory]
    [InlineData("Hello")]
    [InlineData("ä")]
    [InlineData("Hällo")]
    [InlineData("煊")]
    [InlineData("a煊")]
    public void Test_Hashes_Same_Pool(string str)
    {
        using var pool = StringPool.Utf8(4096, 1);
        var hash = StringHash.Compute(str);
        var poolHash = pool.Add(str).GetHashCode();
        Assert.Equal((int)(hash & 0xffffffff), poolHash);
    }
}

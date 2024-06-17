using Monkey.Object;

namespace Monkey.Test;

public class ObjectTest
{
    [Fact]
    public void TestStringHashKey()
    {
        var hello1 = new String("Hello World");
        var hello2 = new String("Hello World");

        var diff1 = new String("My name is johnny");
        var diff2 = new String("My name is johnny");

        Assert.Equal(hello1.GetHashCode(), hello2.GetHashCode());
        Assert.Equal(diff1.GetHashCode(), diff2.GetHashCode());
        Assert.NotEqual(hello1.GetHashCode(), diff2.GetHashCode());
    }

    [Fact]
    public void TestIntegerHashKey()
    {
        var hello1 = new Integer(1);
        var hello2 = new Integer(1);

        var diff1 = new Integer(2);
        var diff2 = new Integer(2);

        Assert.Equal(hello1.GetHashCode(), hello2.GetHashCode());
        Assert.Equal(diff1.GetHashCode(), diff2.GetHashCode());
        Assert.NotEqual(hello1.GetHashCode(), diff2.GetHashCode());
    }

    [Fact]
    public void TestBooleanHashKey()
    {
        var hello1 = new Boolean(true);
        var hello2 = new Boolean(true);

        var diff1 = new Boolean(false);
        var diff2 = new Boolean(false);

        Assert.Equal(hello1.GetHashCode(), hello2.GetHashCode());
        Assert.Equal(diff1.GetHashCode(), diff2.GetHashCode());
        Assert.NotEqual(hello1.GetHashCode(), diff2.GetHashCode());
    }
}

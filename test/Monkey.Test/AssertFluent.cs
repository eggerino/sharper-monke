using System.Collections.Generic;
using System.Linq;

public static class AssertFluent
{
    public static T IsType<T>(object? @object)
    {
        Assert.IsType<T>(@object);
        return (T)@object;
    }

    public static T Single<T>(IEnumerable<T> collection)
    {
        Assert.Single(collection);
        return collection.First();
    }
}

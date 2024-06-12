public static class AssertFluent
{
    public static T IsType<T>(object? @object)
    {
        Assert.IsType<T>(@object);
        return (T)@object;
    }
}

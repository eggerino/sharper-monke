namespace Monkey.Object;

public enum ObjectType
{
    Null = 0,
    Integer,
    Boolean,
    ReturnValue,
}

public interface IObject
{
    ObjectType GetObjectType();
    string Inspect();
}

public record Integer(long Value) : IObject
{
    public ObjectType GetObjectType() => ObjectType.Integer;

    public string Inspect() => Value.ToString();
}

public record Boolean(bool Value) : IObject
{
    public ObjectType GetObjectType() => ObjectType.Boolean;

    public string Inspect() => Value ? "true" : "false";
}

public record Null : IObject
{
    public ObjectType GetObjectType() => ObjectType.Null;

    public string Inspect() => "null";
}

public record ReturnValue(IObject Value) : IObject
{
    public ObjectType GetObjectType() => ObjectType.ReturnValue;

    public string Inspect() => Value.Inspect();
}

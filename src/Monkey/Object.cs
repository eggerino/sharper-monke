using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Monkey.Ast;

namespace Monkey.Object;

public enum ObjectType
{
    Error = 0,
    Null,
    Integer,
    Boolean,
    String,
    ReturnValue,
    Function,
}

public interface IObject
{
    ObjectType GetObjectType();
    string Inspect();
}

public record Null : IObject
{
    public ObjectType GetObjectType() => ObjectType.Null;

    public string Inspect() => "null";
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

public record String(string Value) : IObject
{
    public ObjectType GetObjectType() => ObjectType.String;

    public string Inspect() => Value;
}

public record ReturnValue(IObject Value) : IObject
{
    public ObjectType GetObjectType() => ObjectType.ReturnValue;

    public string Inspect() => Value.Inspect();
}

public record Error(string Message) : IObject
{
    public ObjectType GetObjectType() => ObjectType.Error;

    public string Inspect() => $"ERROR: {Message}";
};

public record Function(ImmutableList<Identifier> Parameters, BlockStatement Body, Environment Environment) : IObject
{
    public ObjectType GetObjectType() => ObjectType.Function;

    public string Inspect()
    {
        var builder = new StringBuilder();
        builder.Append("fn(");
        builder.Append(string.Join(", ", Parameters.Select(x => x.GetDebugString())));
        builder.Append(") {\n");
        builder.Append(Body.GetDebugString());
        builder.Append("}");
        return builder.ToString();
    }
}

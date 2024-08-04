using System;
using System.Collections.Generic;
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
    Builtin,
    Array,
    Hash,
    Quote,
    Macro,
    CompiledFunction,
}

public interface IObject
{
    ObjectType GetObjectType();
    string Inspect();
}

public interface IHashable : IObject { }    // Records implement value semantics with an overriden GetHashCode() and Equals Method
                                            // They can be savely used as keys in an dictionary without checking reference equality

public record Null : IObject
{
    public ObjectType GetObjectType() => ObjectType.Null;

    public string Inspect() => "null";
}

public record Integer(long Value) : IObject, IHashable
{
    public ObjectType GetObjectType() => ObjectType.Integer;

    public string Inspect() => Value.ToString();
}

public record Boolean(bool Value) : IObject, IHashable
{
    public ObjectType GetObjectType() => ObjectType.Boolean;

    public string Inspect() => Value ? "true" : "false";
}

public record String(string Value) : IObject, IHashable
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

public delegate IObject BuiltinFunction(IEnumerable<IObject> arguments);

public record Builtin(BuiltinFunction Function) : IObject
{
    public ObjectType GetObjectType() => ObjectType.Builtin;

    public string Inspect() => "builtin function";
}

public record Array(ImmutableList<IObject> Elements) : IObject
{
    public ObjectType GetObjectType() => ObjectType.Array;

    public string Inspect()
    {
        var builder = new StringBuilder();
        builder.Append("[");
        builder.Append(string.Join(", ", Elements.Select(x => x.Inspect())));
        builder.Append("]");
        return builder.ToString();
    }
}

public record Hash(ImmutableDictionary<IHashable, IObject> Pairs) : IObject
{
    public ObjectType GetObjectType() => ObjectType.Hash;

    public string Inspect()
    {
        var builder = new StringBuilder();
        builder.Append("{");
        builder.Append(string.Join(", ", Pairs.Select(x => $"{x.Key.Inspect()}: {x.Value.Inspect()}")));
        builder.Append("}");
        return builder.ToString();
    }
}

public record Quote(INode? Node) : IObject
{
    public ObjectType GetObjectType() => ObjectType.Quote;

    public string Inspect() => $"Quote({Node?.GetDebugString()})";
}

public record Macro(ImmutableList<Identifier> Parameters, BlockStatement Body, Environment Environment) : IObject
{
    public ObjectType GetObjectType() => ObjectType.Macro;

    public string Inspect()
    {
        var builder = new StringBuilder();
        builder.Append("macro(");
        builder.Append(string.Join(", ", Parameters.Select(x => x.GetDebugString())));
        builder.Append(") {\n");
        builder.Append(Body.GetDebugString());
        builder.Append("}");
        return builder.ToString();
    }
}

public record CompiledFunction(ArraySegment<byte> Instructions, int NumberOfLocals) : IObject
{
    public ObjectType GetObjectType() => ObjectType.CompiledFunction;

    public string Inspect()
    {
        return $"CompiledFunction[{GetHashCode()}]";
    }
}

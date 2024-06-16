using System.Collections.Generic;
using System.Linq;
using Monkey.Object;

namespace Monkey;

public static class Builtins
{
    private static readonly Dictionary<string, Builtin> _builtins = new()
    {
        {"len", new(Len)},
        {"first", new(First)},
        {"last", new(Last)},
        {"rest", new(Rest)},
        {"push", new(Push)},
        {"puts", new(Puts)},
    };

    public static Builtin? Get(string name) => _builtins.TryGetValue(name, out var builtin) ? builtin : null;

    private static IObject Len(IEnumerable<IObject> arguments)
    {
        return arguments.ToList() switch
        {
        [String str] => new Integer(str.Value.Length),
        [Array arr] => new Integer(arr.Elements.Count),
        [var arg] => new Error($"argument to `len` not supported, got {arg.GetObjectType()}"),
        [.. var args] => new Error($"wrong number of arguments. got={args.Count}, want=1"),
        };
    }

    private static IObject First(IEnumerable<IObject> arguments)
    {
        return arguments.ToList() switch
        {
        [Array arr] when arr.Elements.Count > 0 => arr.Elements[0],
        [Array _] => Evaluator.Null,
        [var arg] => new Error($"argument to `first` must be Array, got {arg.GetObjectType()}"),
        [.. var args] => new Error($"wrong number of arguments. got={args.Count}, want=1"),
        };
    }

    private static IObject Last(IEnumerable<IObject> arguments)
    {
        return arguments.ToList() switch
        {
        [Array arr] when arr.Elements.Count > 0 => arr.Elements.Last(),
        [Array _] => Evaluator.Null,
        [var arg] => new Error($"argument to `last` must be Array, got {arg.GetObjectType()}"),
        [.. var args] => new Error($"wrong number of arguments. got={args.Count}, want=1"),
        };
    }

    private static IObject Rest(IEnumerable<IObject> arguments)
    {
        return arguments.ToList() switch
        {
        [Array arr] when arr.Elements.Count > 0 => arr with { Elements = arr.Elements.RemoveAt(0) },
        [Array _] => Evaluator.Null,
        [var arg] => new Error($"argument to `rest` must be Array, got {arg.GetObjectType()}"),
        [.. var args] => new Error($"wrong number of arguments. got={args.Count}, want=1"),
        };
    }

    private static IObject Push(IEnumerable<IObject> arguments)
    {
        return arguments.ToList() switch
        {
        [Array arr, var item] => arr with { Elements = arr.Elements.Add(item) },
        [var arg1, var _] => new Error($"argument to `push` must be Array, got {arg1.GetObjectType()}"),
        [.. var args] => new Error($"wrong number of arguments. got={args.Count}, want=2"),
        };
    }

    private static IObject Puts(IEnumerable<IObject> arguments)
    {
        foreach (var argument in arguments)
        {
            System.Console.WriteLine(argument.Inspect());
        }

        return Evaluator.Null;
    }
}

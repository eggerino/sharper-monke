using System.Collections.Generic;
using System.Collections.Immutable;
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
        {"keys", new(Keys)},
        {"puts", new(Puts)},
    };

    public static Builtin? Get(string name) => _builtins.TryGetValue(name, out var builtin) ? builtin : null;

    private static IObject Len(IEnumerable<IObject> arguments)
    {
        return arguments.ToList() switch
        {
        [String str] => new Integer(str.Value.Length),
        [Array arr] => new Integer(arr.Elements.Count),
        [Hash hash] => new Integer(hash.Pairs.Count),
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
        [] => new Error("no arguments provided for `push`"),
        [Array arr, var item] => arr with { Elements = arr.Elements.Add(item) },
        [Array _, .. var args] => new Error($"wrong number of arguments. got={args.Count + 1}, want=2"),
        [Hash hash, IHashable key, var item] => hash with { Pairs = hash.Pairs.Add(key, item) },
        [Hash hash, var key, var _] => new Error($"second argument to `push` must be hashable, got {key.GetObjectType()}"),
        [Hash _, .. var args] => new Error($"wrong number of arguments. got={args.Count + 1}, want=3"),
        [var arg1, ..] => new Error($"argument to `push` must be Array or Hash, got {arg1.GetObjectType()}"),
        };
    }

    private static IObject Keys(IEnumerable<IObject> arguments)
    {
        return arguments.ToList() switch
        {
        [Hash hash] => new Array(hash.Pairs.Keys.Cast<IObject>().ToImmutableList()),
        [var arg] => new Error($"argument to `keys` must be Hash, got {arg.GetObjectType()}"),
        [.. var args] => new Error($"wrong number of arguments. got={args.Count}, want=1"),
        };
    }

    private static Null Puts(IEnumerable<IObject> arguments)
    {
        foreach (var argument in arguments)
        {
            System.Console.WriteLine(argument.Inspect());
        }

        return Evaluator.Null;
    }
}

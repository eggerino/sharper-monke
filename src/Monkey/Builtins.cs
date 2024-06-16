using System.Collections.Generic;
using System.Linq;
using Monkey.Object;

namespace Monkey;

public static class Builtins
{
    private static readonly Dictionary<string, Builtin> _builtins = new()
    {
        {"len", new(Len)}
    };

    public static Builtin? Get(string name) => _builtins.TryGetValue(name, out var builtin) ? builtin : null;

    private static IObject Len(IEnumerable<IObject> arguments)
    {
        return arguments.ToList() switch
        {
        [String str] => new Integer(str.Value.Length),
        [var arg] => new Error($"argument to `len` not supported, got {arg.GetObjectType()}"),
        [.. var args] => new Error($"wrong number of arguments. got={args.Count}, want=1")
        };
    }
}

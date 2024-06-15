using System.Collections.Generic;
using Monkey.Object;

namespace Monkey;

public class Environment
{
    private readonly Environment? _outer = null;
    private readonly Dictionary<string, IObject> _store = [];

    private Environment(Environment outer) => _outer = outer;

    public Environment() { }

    public Environment NewEnclosedEnvironment() => new(this);

    public IObject? Get(string name) =>
        _store.TryGetValue(name, out var value) switch
        {
            true => value,
            false => _outer switch
            {
                Environment outer => outer.Get(name),
                null => null,
            },
        };

    public IObject Set(string name, IObject value)
    {
        _store[name] = value;
        return value;
    }
}

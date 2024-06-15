using System.Collections.Generic;
using Monkey.Object;

namespace Monkey;

public class Environment
{
    private Dictionary<string, IObject> _store = [];

    public IObject? Get(string name) => _store.TryGetValue(name, out var value) ? value : null;

    public IObject Set(string name, IObject value)
    {
        _store.Add(name, value);
        return value;
    }
}

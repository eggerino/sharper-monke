using System.Collections.Generic;

namespace Monkey;

public static class Scopes
{
    public const string Global = "GLOBAL";
}

public record Symbol(string Name, string Scope, int Index);

public class SymbolTable
{
    private readonly Dictionary<string, Symbol> _store = [];
    private int _numDefinitions = 0;

    public Symbol Define(string name)
    {
        var symbol = new Symbol(Name: name, Scope: Scopes.Global, Index:_numDefinitions);
        _store.Add(name, symbol);
        _numDefinitions++;
        return symbol;
    }

    public Symbol? Resolve(string name) => _store.TryGetValue(name, out var symbol) switch
    {
        true => symbol,
        false => null,
    };
}

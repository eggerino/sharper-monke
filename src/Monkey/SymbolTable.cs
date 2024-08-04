using System.Collections.Generic;

namespace Monkey;

public static class Scopes
{
    public const string Global = "GLOBAL";
    public const string Local = "LOCAL";
    public const string Builtin = "BUILTIN";
}

public record Symbol(string Name, string Scope, int Index);

public class SymbolTable
{
    private readonly SymbolTable? _outer = null;
    private readonly Dictionary<string, Symbol> _store = [];
    private int _numDefinitions = 0;

    private SymbolTable(SymbolTable outer) => _outer = outer;

    public SymbolTable() { }

    public int NumberOfDefinitions => _store.Count;

    public SymbolTable? Outer => _outer;

    public Symbol Define(string name)
    {
        var scope = _outer switch
        {
            null => Scopes.Global,
            _ => Scopes.Local
        };

        var symbol = new Symbol(Name: name, Scope: scope, Index: _numDefinitions);
        _store.Add(name, symbol);
        _numDefinitions++;
        return symbol;
    }

    public Symbol DefineBuiltin(int index, string name)
    {
        var symbol = new Symbol(Name: name, Scope: Scopes.Builtin, Index: index);
        _store.Add(name, symbol);
        return symbol;
    }

    public Symbol? Resolve(string name) => _store.TryGetValue(name, out var symbol) switch
    {
        true => symbol,
        false => _outer?.Resolve(name),
    };

    public SymbolTable NewEnclosedTable()
    {
        return new(this);
    }
}

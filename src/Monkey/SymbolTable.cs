using System.Collections.Generic;

namespace Monkey;

public static class Scopes
{
    public const string Global = "GLOBAL";
    public const string Local = "LOCAL";
    public const string Builtin = "BUILTIN";
    public const string Free = "FREE";
    public const string Function = "FUNCTION";
}

public record Symbol(string Name, string Scope, int Index);

public class SymbolTable
{
    private readonly SymbolTable? _outer = null;
    private readonly List<Symbol> _freeSymbols = new();
    private readonly Dictionary<string, Symbol> _store = [];
    private int _numDefinitions = 0;

    private SymbolTable(SymbolTable outer) => _outer = outer;

    public SymbolTable() { }

    public int NumberOfDefinitions => _store.Count;

    public SymbolTable? Outer => _outer;

    public IReadOnlyList<Symbol> FreeSymbols => _freeSymbols;

    public Symbol Define(string name)
    {
        var scope = _outer switch
        {
            null => Scopes.Global,
            _ => Scopes.Local
        };

        var symbol = new Symbol(Name: name, Scope: scope, Index: _numDefinitions);
        _store[name] = symbol;
        _numDefinitions++;
        return symbol;
    }

    public Symbol DefineBuiltin(int index, string name)
    {
        var symbol = new Symbol(Name: name, Scope: Scopes.Builtin, Index: index);
        _store[name] = symbol;
        return symbol;
    }

    public Symbol DefineFunctionName(string name)
    {
        var symbol = new Symbol(Name: name, Scope: Scopes.Function, 0);
        _store[name] = symbol;
        return symbol;
    }

    private Symbol DefineFree(Symbol original)
    {
        _freeSymbols.Add(original);

        var symbol = original with { Scope = Scopes.Free, Index = _freeSymbols.Count - 1 };
        _store[symbol.Name] = symbol;

        return symbol;
    }

    public Symbol? Resolve(string name)
    {
        if (_store.TryGetValue(name, out var symbol))
        {
            return symbol;
        }

        if (_outer is SymbolTable outer)
        {
            symbol = outer.Resolve(name);

            if (symbol is null)
            {
                return null;
            }

            if (symbol.Scope == Scopes.Global || symbol.Scope == Scopes.Builtin)
            {
                return symbol;
            }

            return DefineFree(symbol);
        }

        return null;
    }

    public SymbolTable NewEnclosedTable()
    {
        return new(this);
    }
}

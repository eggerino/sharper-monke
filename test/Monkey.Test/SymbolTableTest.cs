using System.Collections.Generic;

namespace Monkey.Test;

public class SymbolTableTest
{
    [Fact]
    public void TestDefine()
    {
        Dictionary<string, Symbol> expected = new()
        {
            {"a", new("a", Scopes.Global, 0)},
            {"b", new("b", Scopes.Global, 1)},
            {"c", new("c", Scopes.Local, 0)},
            {"d", new("d", Scopes.Local, 1)},
            {"e", new("e", Scopes.Local, 0)},
            {"f", new("f", Scopes.Local, 1)},
        };
        var global = new SymbolTable();

        var a = global.Define("a");
        Assert.Equal(a, expected["a"]);

        var b = global.Define("b");
        Assert.Equal(b, expected["b"]);

        var firstLocal = global.NewEnclosedTable();
        var c = firstLocal.Define("c");
        Assert.Equal(c, expected["c"]);

        var d = firstLocal.Define("d");
        Assert.Equal(d, expected["d"]);

        var secondLocal = global.NewEnclosedTable();
        var e = secondLocal.Define("e");
        Assert.Equal(e, expected["e"]);

        var f = secondLocal.Define("f");
        Assert.Equal(f, expected["f"]);
    }

    [Fact]
    public void TestResolveGlobal()
    {
        var global = new SymbolTable();
        global.Define("a");
        global.Define("b");

        Symbol[] expected = [new("a", Scopes.Global, 0), new("b", Scopes.Global, 1)];

        foreach (var e in expected)
        {
            var result = global.Resolve(e.Name);
            Assert.NotNull(result);
            Assert.Equal(e, result);
        }
    }

    [Fact]
    public void TestResolveLocal()
    {
        var global = new SymbolTable();
        global.Define("a");
        global.Define("b");

        var local = global.NewEnclosedTable();
        local.Define("c");
        local.Define("d");

        var expected = new[]
        {
            new Symbol("a", Scopes.Global, 0),
            new Symbol("b", Scopes.Global, 1),
            new Symbol("c", Scopes.Local, 0),
            new Symbol("d", Scopes.Local, 1),
        };

        foreach (var e in expected)
        {
            var result = local.Resolve(e.Name);
            Assert.NotNull(result);
            Assert.Equal(e, result);
        }
    }

    [Fact]
    public void TestResolveNestedLocal()
    {
        var global = new SymbolTable();
        global.Define("a");
        global.Define("b");

        var firstLocal = global.NewEnclosedTable();
        firstLocal.Define("c");
        firstLocal.Define("d");

        var secondLocal = firstLocal.NewEnclosedTable();
        secondLocal.Define("e");
        secondLocal.Define("f");

        var tests = new[]
        {
            (firstLocal, new[]
            {
                new Symbol("a", Scopes.Global, 0),
                new Symbol("b", Scopes.Global, 1),
                new Symbol("c", Scopes.Local, 0),
                new Symbol("d", Scopes.Local, 1),
            }),
            (secondLocal, new[]
            {
                new Symbol("a", Scopes.Global, 0),
                new Symbol("b", Scopes.Global, 1),
                new Symbol("e", Scopes.Local, 0),
                new Symbol("f", Scopes.Local, 1),
            }),
        };

        foreach (var (table, expected) in tests)
        {
            foreach (var e in expected)
            {
                var result = table.Resolve(e.Name);
                Assert.NotNull(result);
                Assert.Equal(e, result);
            }
        }
    }
}

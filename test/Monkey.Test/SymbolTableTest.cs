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
        };
        var global = new SymbolTable();

        var a = global.Define("a");
        Assert.Equal(a, expected["a"]);

        var b = global.Define("b");
        Assert.Equal(b, expected["b"]);
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
}

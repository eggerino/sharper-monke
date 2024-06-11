using Monkey.Ast;

namespace Monkey.Test;

public class AstTest
{
    [Fact]
    public void TestDisplayString()
    {
        var program = new Program([
            new LetStatement(
                new Token(TokenType.Let, "let"),
                new Identifier(new Token(TokenType.Identifier, "myVar"), "myVar"),
                new Identifier(new Token(TokenType.Identifier, "anotherVar"), "anotherVar")
            ),
        ]);

        Assert.Equal("let myVar = anotherVar;", program.GetDebugString());
    }
}

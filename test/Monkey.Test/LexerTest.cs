namespace Monkey.Test;

public class LexerTest
{
    [Fact]
    public void TestNextToken()
    {
        var input =
@"let five = 5;
let ten = 10;

let add = fn(x, y) {
  x + y;
};

let result = add(five, ten);
";
        Token[] expected = [
            new Token(TokenType.Let, "let"),
            new Token(TokenType.Identifier, "five"),
            new Token(TokenType.Assign, "="),
            new Token(TokenType.Int, "5"),
            new Token(TokenType.Semicolon, ";"),
            new Token(TokenType.Let, "let"),
            new Token(TokenType.Identifier, "ten"),
            new Token(TokenType.Assign, "="),
            new Token(TokenType.Int, "10"),
            new Token(TokenType.Semicolon, ";"),
            new Token(TokenType.Let, "let"),
            new Token(TokenType.Identifier, "add"),
            new Token(TokenType.Assign, "="),
            new Token(TokenType.Function, "fn"),
            new Token(TokenType.LeftParenthese, "("),
            new Token(TokenType.Identifier, "x"),
            new Token(TokenType.Comma, ","),
            new Token(TokenType.Identifier, "y"),
            new Token(TokenType.RightParenthese, ")"),
            new Token(TokenType.LeftBrace, "{"),
            new Token(TokenType.Identifier, "x"),
            new Token(TokenType.Plus, "+"),
            new Token(TokenType.Identifier, "y"),
            new Token(TokenType.Semicolon, ";"),
            new Token(TokenType.RightBrace, "}"),
            new Token(TokenType.Semicolon, ";"),
            new Token(TokenType.Let, "let"),
            new Token(TokenType.Identifier, "result"),
            new Token(TokenType.Assign, "="),
            new Token(TokenType.Identifier, "add"),
            new Token(TokenType.LeftParenthese, "("),
            new Token(TokenType.Identifier, "five"),
            new Token(TokenType.Comma, ","),
            new Token(TokenType.Identifier, "ten"),
            new Token(TokenType.RightParenthese, ")"),
            new Token(TokenType.Semicolon, ";"),
            new Token(TokenType.EndOfFile, ""),
        ];

        var lexer = Lexer.For(input);
        Token[] actual = [.. lexer.GetTokens()];

        Assert.Equal(expected, actual);
    }
}

namespace Monkey.Test;

public class LexerTest
{
    [Fact]
    public void TestNextToken()
    {
        var input = "=+(){},;";
        Token[] expected = [
            new Token(TokenType.Assign, "="),
            new Token(TokenType.Plus, "+"),
            new Token(TokenType.LeftParenthese, "("),
            new Token(TokenType.RightParenthese, ")"),
            new Token(TokenType.LeftBrace, "{"),
            new Token(TokenType.RightBrace, "}"),
            new Token(TokenType.Comma, ","),
            new Token(TokenType.Semicolon, ";"),
            new Token(TokenType.EndOfFile, ""),
        ];

        var lexer = Lexer.For(input);
        Token[] actual = [.. lexer.GetTokens()];
        
        Assert.Equal(expected, actual);
    }
}

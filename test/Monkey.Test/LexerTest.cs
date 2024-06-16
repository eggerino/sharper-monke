namespace Monkey.Test;

public class LexerTest
{
	[Fact]
	public void TestNextToken()
	{
		var input = @"let five = 5;
let ten = 10;

let add = fn(x, y) {
  x + y;
};

let result = add(five, ten);
!-/*5;
5 < 10 > 5;

if (5 < 10) {
	return true;
} else {
	return false;
}

10 == 10;
10 != 9;
""foobar""
""foo bar""
[1, 2];
";

		Token[] expected = [
			new Token(TokenType.Let, "let"),
			new Token(TokenType.Identifier, "five"),
			new Token(TokenType.Assign, "="),
			new Token(TokenType.Integer, "5"),
			new Token(TokenType.Semicolon, ";"),
			new Token(TokenType.Let, "let"),
			new Token(TokenType.Identifier, "ten"),
			new Token(TokenType.Assign, "="),
			new Token(TokenType.Integer, "10"),
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
			new Token(TokenType.Bang, "!"),
			new Token(TokenType.Minus, "-"),
			new Token(TokenType.Slash, "/"),
			new Token(TokenType.Asterisk, "*"),
			new Token(TokenType.Integer, "5"),
			new Token(TokenType.Semicolon, ";"),
			new Token(TokenType.Integer, "5"),
			new Token(TokenType.LessThan, "<"),
			new Token(TokenType.Integer, "10"),
			new Token(TokenType.GreaterThan, ">"),
			new Token(TokenType.Integer, "5"),
			new Token(TokenType.Semicolon, ";"),
			new Token(TokenType.If, "if"),
			new Token(TokenType.LeftParenthese, "("),
			new Token(TokenType.Integer, "5"),
			new Token(TokenType.LessThan, "<"),
			new Token(TokenType.Integer, "10"),
			new Token(TokenType.RightParenthese, ")"),
			new Token(TokenType.LeftBrace, "{"),
			new Token(TokenType.Return, "return"),
			new Token(TokenType.True, "true"),
			new Token(TokenType.Semicolon, ";"),
			new Token(TokenType.RightBrace, "}"),
			new Token(TokenType.Else, "else"),
			new Token(TokenType.LeftBrace, "{"),
			new Token(TokenType.Return, "return"),
			new Token(TokenType.False, "false"),
			new Token(TokenType.Semicolon, ";"),
			new Token(TokenType.RightBrace, "}"),
			new Token(TokenType.Integer, "10"),
			new Token(TokenType.Equals, "=="),
			new Token(TokenType.Integer, "10"),
			new Token(TokenType.Semicolon, ";"),
			new Token(TokenType.Integer, "10"),
			new Token(TokenType.NotEquals, "!="),
			new Token(TokenType.Integer, "9"),
			new Token(TokenType.Semicolon, ";"),
			new Token(TokenType.String, @"""foobar"""),
			new Token(TokenType.String, @"""foo bar"""),
			new Token(TokenType.LeftBracket, "["),
			new Token(TokenType.Integer, "1"),
			new Token(TokenType.Comma, ","),
			new Token(TokenType.Integer, "2"),
			new Token(TokenType.RightBracket, "]"),
			new Token(TokenType.Semicolon, ";"),
			new Token(TokenType.EndOfFile, ""),
		];

		var lexer = new Lexer(input);
		Token[] actual = [.. lexer.GetTokens()];

		Assert.Equal(expected, actual);
	}
}

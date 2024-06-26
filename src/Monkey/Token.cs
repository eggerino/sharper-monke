namespace Monkey;

public enum TokenType
{
    Illegal,
    EndOfFile,

    // Identifiers + literals
    Identifier,
    Integer,
    String,

    // Operators
    Assign,
    Plus,
    Minus,
    Bang,
    Asterisk,
    Slash,

    LessThan,
    GreaterThan,

    Equals,
    NotEquals,

    // Delimiters
    Comma,
    Semicolon,
    Colon,

    LeftParenthese,
    RightParenthese,
    LeftBrace,
    RightBrace,
    LeftBracket,
    RightBracket,

    // Keywords
    Function,
    Let,
    True,
    False,
    If,
    Else,
    Return,
    Macro,
}

public record Token(TokenType Type, string Literal);

public static class StringToken
{
    public static string RemoveEnclosingQuotes(string literal) => literal.Substring(1, literal.Length - 2);
}

public static class TokenTypeLookup
{
    public static TokenType Identifier(string identifier) => identifier switch
    {
        "fn" => TokenType.Function,
        "let" => TokenType.Let,
        "true" => TokenType.True,
        "false" => TokenType.False,
        "if" => TokenType.If,
        "else" => TokenType.Else,
        "return" => TokenType.Return,
        "macro" => TokenType.Macro,
        _ => TokenType.Identifier,
    };
}

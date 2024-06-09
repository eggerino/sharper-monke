namespace Monkey;

public record Token(TokenType Type, string Literal);

public enum TokenType
{
    Illegal,
    EndOfFile,

    // Identifiers + literals
    Identifier,
    Int,

    // Operators
    Assign,
    Plus,

    // Delimiters
    Comma,
    Semicolon,

    LeftParenthese,
    RightParenthese,
    LeftBrace,
    RightBrace,

    // Keywords
    Function,
    Let,
}

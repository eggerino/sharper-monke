using System.Collections.Generic;

namespace Monkey;

public enum TokenType
{
    Illegal,
    EndOfFile,

    // Identifiers + literals
    Identifier,
    Integer,

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

    LeftParenthese,
    RightParenthese,
    LeftBrace,
    RightBrace,

    // Keywords
    Function,
    Let,
    True,
    False,
    If,
    Else,
    Return,
}

public record Token(TokenType Type, string Literal);

public static class TokenTypeLookup
{
    private static readonly Dictionary<string, TokenType> _keywords = new()
    {
        {"fn", TokenType.Function},
        {"let", TokenType.Let},
        {"true", TokenType.True},
        {"false", TokenType.False},
        {"if", TokenType.If},
        {"else", TokenType.Else},
        {"return", TokenType.Return},
    };

    public static TokenType Identifier(string identifier) => _keywords.TryGetValue(identifier, out var value)
        ? value
        : TokenType.Identifier;
}

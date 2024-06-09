using System.Collections.Generic;

namespace Monkey;

public record Token(TokenType Type, string Literal)
{
    private static readonly Dictionary<string, TokenType> _keywords = new()
    {
        {"fn", TokenType.Function},
        {"let", TokenType.Let},
    };
    
    public static TokenType LookupIdentifier(string identifier) => _keywords.TryGetValue(identifier, out var value)
        ? value
        : TokenType.Identifier;
}

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

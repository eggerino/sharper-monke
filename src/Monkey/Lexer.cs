using System;
using System.Collections.Generic;
using System.Linq;

namespace Monkey;

public class Lexer(string input)
{
    public IEnumerable<Token> GetTokens()
    {
        var position = 0;
        while (position < input.Length)
        {
            position += GetWhiteSpaceLength(position);
            if (position >= input.Length)
                break;

            var token = GetToken(position);
            position += token.Literal.Length;
            yield return token;
        }

        yield return new(TokenType.EndOfFile, "");
    }

    private Token GetToken(int position)
    {
        var (type, literal) = input[position] switch
        {
            // Operators
            '=' => Peek(position) switch
            {
                '=' => (TokenType.Equals, "=="),
                _ => (TokenType.Assign, "="),
            },
            '+' => (TokenType.Plus, "+"),
            '-' => (TokenType.Minus, "-"),
            '!' => Peek(position) switch
            {
                '=' => (TokenType.NotEquals, "!="),
                _ => (TokenType.Bang, "!"),
            },
            '*' => (TokenType.Asterisk, "*"),
            '/' => (TokenType.Slash, "/"),
            '<' => (TokenType.LessThan, "<"),
            '>' => (TokenType.GreaterThan, ">"),

            // Delimiters
            ',' => (TokenType.Comma, ","),
            ';' => (TokenType.Semicolon, ";"),
            '(' => (TokenType.LeftParenthese, "("),
            ')' => (TokenType.RightParenthese, ")"),
            '{' => (TokenType.LeftBrace, "{"),
            '}' => (TokenType.RightBrace, "}"),
            '[' => (TokenType.LeftBracket, "["),
            ']' => (TokenType.RightBracket, "]"),

            '"' => GetString(position),
            var c when char.IsLetter(c) => GetTokenOnLetter(position),
            var c when char.IsDigit(c) => GetTokenOnDigit(position),
            var c => (TokenType.Illegal, c.ToString()),
        };

        return new(type, literal);
    }

    private char? Peek(int position) => (position + 1) < input.Length ? input[position + 1] : null;

    private (TokenType, string) GetString(int position)
    {
        var length = GetStringLength(position);
        var literal = input.Substring(position, length);

        return (TokenType.String, literal);
    }

    private (TokenType, string) GetTokenOnLetter(int position)
    {
        var length = GetIdentifierLength(position);
        var literal = input.Substring(position, length);
        var type = TokenTypeLookup.Identifier(literal);

        return (type, literal);
    }

    private (TokenType, string) GetTokenOnDigit(int position)
    {
        var length = GetNumberLength(position);
        var literal = input.Substring(position, length);

        return (TokenType.Integer, literal);
    }

    private int GetLengthWhile(Func<char, bool> predicate, int position) =>
        input
            .Skip(position)
            .TakeWhile(predicate)
            .Count();

    private int GetStringLength(int position) => GetLengthWhile(c => c != '"', position + 1) + 2;   // Consider quotes part of the string

    private int GetWhiteSpaceLength(int position) => GetLengthWhile(char.IsWhiteSpace, position);

    private int GetIdentifierLength(int position) => GetLengthWhile(char.IsLetter, position);

    private int GetNumberLength(int position) => GetLengthWhile(char.IsDigit, position);
}

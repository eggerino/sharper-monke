using System;
using System.Collections.Generic;

namespace Monkey;

public class Lexer
{
    private readonly string _input;

    private Lexer(string input) => _input = input;

    public static Lexer For(string input) => new(input);

    public IEnumerable<Token> GetTokens()
    {
        var position = 0;
        while (position < _input.Length)
        {
            position += GetWhiteSpaceLength(position);
            if (position >= _input.Length)
                break;

            var input = _input.AsSpan();
            var character = input[position];
            (var token, position) = character switch
            {
                // Operators
                '=' when Peek(position) == '=' => (new Token(TokenType.Equals, "=="), position + 2),
                '=' => (new Token(TokenType.Assign, "="), position + 1),
                '+' => (new Token(TokenType.Plus, "+"), position + 1),
                '-' => (new Token(TokenType.Minus, "-"), position + 1),
                '!' when Peek(position) == '=' => (new Token(TokenType.NotEquals, "!="), position + 2),
                '!' => (new Token(TokenType.Bang, "!"), position + 1),
                '*' => (new Token(TokenType.Asterisk, "*"), position + 1),
                '/' => (new Token(TokenType.Slash, "/"), position + 1),
                '<' => (new Token(TokenType.LessThan, "<"), position + 1),
                '>' => (new Token(TokenType.GreaterThan, ">"), position + 1),

                // Delimiters
                ',' => (new Token(TokenType.Comma, ","), position + 1),
                ';' => (new Token(TokenType.Semicolon, ";"), position + 1),
                '(' => (new Token(TokenType.LeftParenthese, "("), position + 1),
                ')' => (new Token(TokenType.RightParenthese, ")"), position + 1),
                '{' => (new Token(TokenType.LeftBrace, "{"), position + 1),
                '}' => (new Token(TokenType.RightBrace, "}"), position + 1),

                var c when char.IsLetter(c) => GetTokenOnLetter(position),
                var c when char.IsDigit(c) => GetTokenOnDigit(position),
                _ => (new Token(TokenType.Illegal, character.ToString()), position + 1),
            };
            yield return token;
        }

        yield return new(TokenType.EndOfFile, "");
    }

    private char? Peek(int position) => (position + 1) < _input.Length ? _input[position + 1] : null;

    private (Token, int) GetTokenOnLetter(int position)
    {
        var length = GetIdentifierLength(position);
        var literal = _input.Substring(position, length);
        var type = Token.LookupIdentifier(literal);

        return (new Token(type, literal), position + length);
    }

    private (Token, int) GetTokenOnDigit(int position)
    {
        var length = GetNumberLength(position);
        var literal = _input.Substring(position, length);

        return (new Token(TokenType.Int, literal), position + length);
    }

    private int GetWhiteSpaceLength(int position)
    {
        var i = position;
        while (i < _input.Length && char.IsWhiteSpace(_input[i]))
            i++;
        return i - position;
    }

    private int GetIdentifierLength(int position)
    {
        var i = position;
        while (i < _input.Length && char.IsLetter(_input[i]))
            i++;
        return i - position;
    }

    private int GetNumberLength(int position)
    {
        var i = position;
        while (i < _input.Length && char.IsDigit(_input[i]))
            i++;
        return i - position;
    }
}

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
                '=' => (new Token(TokenType.Assign, character.ToString()), position + 1),
                ';' => (new Token(TokenType.Semicolon, character.ToString()), position + 1),
                '(' => (new Token(TokenType.LeftParenthese, character.ToString()), position + 1),
                ')' => (new Token(TokenType.RightParenthese, character.ToString()), position + 1),
                '{' => (new Token(TokenType.LeftBrace, character.ToString()), position + 1),
                '}' => (new Token(TokenType.RightBrace, character.ToString()), position + 1),
                ',' => (new Token(TokenType.Comma, character.ToString()), position + 1),
                '+' => (new Token(TokenType.Plus, character.ToString()), position + 1),
                var c when char.IsLetter(c) => GetTokenOnLetter(position),
                var c when char.IsDigit(c) => GetTokenOnDigit(position),
                _ => (new Token(TokenType.Illegal, character.ToString()), position + 1),
            };
            yield return token;
        }

        yield return new(TokenType.EndOfFile, "");
    }

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

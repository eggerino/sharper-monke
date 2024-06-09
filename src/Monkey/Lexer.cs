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
                _ => (new Token(TokenType.Illegal, character.ToString()), position + 1),
            };
            yield return token;
        }

        yield return new(TokenType.EndOfFile, "");
    }
}

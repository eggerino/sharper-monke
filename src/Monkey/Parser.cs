using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Monkey.Ast;

namespace Monkey;

// TODO: Make class design nicer. Less mutable state, more expressive methods etc.
public class Parser : IDisposable
{
    private IEnumerator<Token> _enumerator;
    private Token _currentToken = null!;
    private Token _peekToken = null!;
    private List<string> _errors;

    public Parser(Lexer lexer)
    {
        _enumerator = lexer.GetTokens().GetEnumerator();
        NextToken();
        NextToken();
        _errors = new();
    }

    public IReadOnlyList<string> Errors => _errors;

    public void Dispose() => _enumerator.Dispose();

    private void NextToken()
    {
        _currentToken = _peekToken;
        _peekToken = PullToken();
    }

    private Token PullToken() => _enumerator.MoveNext()
        ? _enumerator.Current
        : new(TokenType.EndOfFile, "");

    public Program ParseProgram()
    {
        var statements = ImmutableList<IStatement>.Empty.ToBuilder();

        while (_currentToken.Type != TokenType.EndOfFile)
        {
            var statement = ParseStatment();
            if (statement is not null)
            {
                statements.Add(statement);
            }
            NextToken();
        }

        return new(statements.ToImmutable());
    }

    public IStatement? ParseStatment()
    {
        switch (_currentToken.Type)
        {
            case TokenType.Let:
                return ParseLetStatement();

            case TokenType.Return:
                return ParseReturnStatement();

            default:
                return null;
        }
    }

    public LetStatement? ParseLetStatement()
    {
        var token = _currentToken;

        if (!ExpectPeek(TokenType.Identifier))
        {
            return null;
        }

        var name = new Identifier(_currentToken, _currentToken.Literal);

        if (!ExpectPeek(TokenType.Assign))
        {
            return null;
        }

        // TODO: We're skipping the expressions until we
        // encounter a semicolon
        var value = new Identifier(token, "Some BS Expressions");
        while (_currentToken.Type != TokenType.Semicolon)
        {
            NextToken();
        }

        return new(token, name, value);
    }

    private ReturnStatement? ParseReturnStatement()
    {
        var token = _currentToken;

        NextToken();

        // TODO: We're skipping the expressions until we
        // encounter a semicolon
        var value = new Identifier(token, "Some BS Expressions");
        while (_currentToken.Type != TokenType.Semicolon)
        {
            NextToken();
        }

        return new(token, value);
    }

    private bool CurrentTokenIs(TokenType type) => _currentToken.Type == type;

    private bool PeekTokenIs(TokenType type) => _peekToken.Type == type;

    private bool ExpectPeek(TokenType type)
    {
        if (PeekTokenIs(type))
        {
            NextToken();
            return true;
        }
        else
        {
            PeekError(type);
            return false;
        }
    }

    private void PeekError(TokenType type)
    {
        _errors.Add($"expected next token to be {type}, got {_peekToken.Type} instead");
    }
}

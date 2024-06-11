using System.Collections.Generic;
using System.Collections.Immutable;
using Monkey.Ast;

namespace Monkey;

public class Parser(Lexer lexer)
{
    public (Program Program, IReadOnlyList<string> Errors) ParseProgram()
    {
        using var tokens = lexer.GetTokens().GetEnumerator();
        var errors = new List<string>();
        var program = new Impl(tokens, errors).ParseProgram();
        return (program, errors);
    }

    private delegate IExpression? PrefixParse();
    private delegate IExpression InfixParse(IExpression left);

    private enum Precedence
    {
        Lowest = 1,
        Equals,
        LessGreater,
        Sum,
        Product,
        Prefix,
        Call,
    }

    private class Impl
    {
        private readonly IEnumerator<Token> _tokens;
        private readonly IList<string> _errors;

        private Token _currentToken = null!;
        private Token _peekToken = null!;

        private readonly Dictionary<TokenType, PrefixParse> _prefixParses = [];
        private readonly Dictionary<TokenType, InfixParse> _infixParses = [];

        public Impl(IEnumerator<Token> tokens, IList<string> errors)
        {
            _tokens = tokens;
            _errors = errors;

            NextToken();
            NextToken();

            _prefixParses.Add(TokenType.Identifier, ParseIdentifier);
            _prefixParses.Add(TokenType.Integer, ParseInteger);
        }

        private void NextToken()
        {
            _currentToken = _peekToken;
            _peekToken = PullToken();
        }

        private Token PullToken() => _tokens.MoveNext()
            ? _tokens.Current
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
                    return ParseExpressionStatement();
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

        private ExpressionStatement ParseExpressionStatement()
        {
            var token = _currentToken;

            var expression = ParseExpression(Precedence.Lowest);

            if (PeekTokenIs(TokenType.Semicolon))
            {
                NextToken();
            }

            return new(token, expression);
        }

        private IExpression? ParseExpression(Precedence precedence)
        {
            if (_prefixParses.TryGetValue(_currentToken.Type, out var prefix))
            {
                return prefix();
            }

            return null;
        }

        private Identifier ParseIdentifier()
        {
            return new Identifier(_currentToken, _currentToken.Literal);
        }

        private IntegerLiteral? ParseInteger()
        {
            var token = _currentToken;

            if (long.TryParse(_currentToken.Literal, out var value))
            {
                return new IntegerLiteral(token, value);
            }

            _errors.Add($"Could not parse {_currentToken.Literal} as integer.");
            return null;
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
}
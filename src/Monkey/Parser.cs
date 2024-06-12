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
    private delegate IExpression? InfixParse(IExpression left);

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
        private static readonly IReadOnlyDictionary<TokenType, Precedence> _precedences = new Dictionary<TokenType, Precedence>
        {
            { TokenType.Equals, Precedence.Equals },
            { TokenType.NotEquals, Precedence.Equals },
            { TokenType.LessThan, Precedence.LessGreater },
            { TokenType.GreaterThan, Precedence.LessGreater },
            { TokenType.Plus, Precedence.Sum },
            { TokenType.Minus, Precedence.Sum },
            { TokenType.Slash, Precedence.Product },
            { TokenType.Asterisk, Precedence.Product },
        };

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
            _prefixParses.Add(TokenType.Minus, ParsePrefixExpression);
            _prefixParses.Add(TokenType.Bang, ParsePrefixExpression);

            _infixParses.Add(TokenType.Plus, ParseInfixExpression);
            _infixParses.Add(TokenType.Minus, ParseInfixExpression);
            _infixParses.Add(TokenType.Slash, ParseInfixExpression);
            _infixParses.Add(TokenType.Asterisk, ParseInfixExpression);
            _infixParses.Add(TokenType.Equals, ParseInfixExpression);
            _infixParses.Add(TokenType.NotEquals, ParseInfixExpression);
            _infixParses.Add(TokenType.LessThan, ParseInfixExpression);
            _infixParses.Add(TokenType.GreaterThan, ParseInfixExpression);
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
            if (!_prefixParses.TryGetValue(_currentToken.Type, out var prefix))
            {
                NoPrefixParseError(_currentToken.Type);
                return null;
            }
            var expression = prefix();

            while (!PeekTokenIs(TokenType.Semicolon) && precedence < PeekPrecedence())
            {
                if (!_infixParses.TryGetValue(_peekToken.Type, out var infix))
                {
                    return expression;
                }

                NextToken();

                if (expression is null)
                {
                    _errors.Add($"Infix operator {@_currentToken.Literal} is not prefixed by a valid expression.");
                    return expression;
                }

                expression = infix(expression);
            }

            return expression;
        }

        private void NoPrefixParseError(TokenType type)
        {
            _errors.Add($"no prefix parse function for {type} found");
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

        private PrefixExpression? ParsePrefixExpression()
        {
            var token = _currentToken;
            var @operator = _currentToken.Literal;

            NextToken();

            var right = ParseExpression(Precedence.Prefix);

            if (right is null)
            {
                _errors.Add($"Prefix operator {@operator} is not prefixing a valid expression.");
                return null;
            }

            return new(token, @operator, right);
        }

        private InfixExpression? ParseInfixExpression(IExpression left)
        {
            var token = _currentToken;
            var @operator = _currentToken.Literal;
            var precedence = CurrentPrecedence();

            NextToken();

            var right = ParseExpression(precedence);

            if (right is null)
            {
                _errors.Add($"Infix operator {@operator} is not prefixing a valid expression.");
                return null;
            }

            return new(token, left, @operator, right);
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

        private void PeekError(TokenType type) => _errors.Add($"expected next token to be {type}, got {_peekToken.Type} instead");

        private Precedence PeekPrecedence() => _precedences.GetValueOrDefault(_peekToken.Type, Precedence.Lowest);

        private Precedence CurrentPrecedence() => _precedences.GetValueOrDefault(_currentToken.Type, Precedence.Lowest);
    }
}

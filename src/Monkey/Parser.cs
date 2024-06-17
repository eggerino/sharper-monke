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
        Index,
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
            { TokenType.LeftParenthese, Precedence.Call },
            { TokenType.LeftBracket, Precedence.Index },
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
            _prefixParses.Add(TokenType.True, ParseBoolean);
            _prefixParses.Add(TokenType.False, ParseBoolean);
            _prefixParses.Add(TokenType.LeftParenthese, ParseGroupedExpression);
            _prefixParses.Add(TokenType.If, ParseIfExpression);
            _prefixParses.Add(TokenType.Function, ParseFunctionLiteral);
            _prefixParses.Add(TokenType.String, ParseStringLiteral);
            _prefixParses.Add(TokenType.LeftBracket, ParseArrayLiteral);
            _prefixParses.Add(TokenType.LeftBrace, ParseHashLiteral);

            _infixParses.Add(TokenType.Plus, ParseInfixExpression);
            _infixParses.Add(TokenType.Minus, ParseInfixExpression);
            _infixParses.Add(TokenType.Slash, ParseInfixExpression);
            _infixParses.Add(TokenType.Asterisk, ParseInfixExpression);
            _infixParses.Add(TokenType.Equals, ParseInfixExpression);
            _infixParses.Add(TokenType.NotEquals, ParseInfixExpression);
            _infixParses.Add(TokenType.LessThan, ParseInfixExpression);
            _infixParses.Add(TokenType.GreaterThan, ParseInfixExpression);
            _infixParses.Add(TokenType.LeftParenthese, ParseCallExpresion);
            _infixParses.Add(TokenType.LeftBracket, ParseIndexExpression);
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

            NextToken();
            var value = ParseExpression(Precedence.Lowest);
            if (value is null)
            {
                _errors.Add("The = sign of the let statement is not followed by an expression.");
                return null;
            }

            if (PeekTokenIs(TokenType.Semicolon))
            {
                NextToken();
            }

            return new(token, name, value);
        }

        private ReturnStatement? ParseReturnStatement()
        {
            var token = _currentToken;

            NextToken();

            var value = ParseExpression(Precedence.Lowest);
            if (value is null)
            {
                _errors.Add("The return keyword is not followed by an expression.");
                return null;
            }

            if (PeekTokenIs(TokenType.Semicolon))
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

        private Boolean ParseBoolean()
        {
            return new(_currentToken, CurrentTokenIs(TokenType.True));
        }

        private IExpression? ParseGroupedExpression()
        {
            NextToken();

            var expression = ParseExpression(Precedence.Lowest);

            if (!ExpectPeek(TokenType.RightParenthese))
            {
                return null;
            }

            return expression;
        }

        private IfExpression? ParseIfExpression()
        {
            var token = _currentToken;

            if (!ExpectPeek(TokenType.LeftParenthese))
            {
                return null;
            }

            NextToken();
            var condition = ParseExpression(Precedence.Lowest);
            if (condition is null)
            {
                _errors.Add($"Left paranthese in a if expression is not followed by an expression.");
                return null;
            }

            if (!ExpectPeek(TokenType.RightParenthese))
            {
                return null;
            }

            if (!ExpectPeek(TokenType.LeftBrace))
            {
                return null;
            }

            var consequence = ParseBlockStatement();
            BlockStatement? alternative = null;

            if (PeekTokenIs(TokenType.Else))
            {
                NextToken();

                if (!ExpectPeek(TokenType.LeftBrace))
                {
                    return null;
                }

                alternative = ParseBlockStatement();
            }

            return new(token, condition, consequence, alternative);
        }

        private BlockStatement ParseBlockStatement()
        {
            var token = _currentToken;
            var statements = ImmutableList<IStatement>.Empty.ToBuilder();

            NextToken();

            while (!CurrentTokenIs(TokenType.RightBrace) && !CurrentTokenIs(TokenType.EndOfFile))
            {
                var statement = ParseStatment();
                if (statement is not null)
                {
                    statements.Add(statement);
                }
                NextToken();
            }

            return new(token, statements.ToImmutable());
        }

        private FunctionLiteral? ParseFunctionLiteral()
        {
            var token = _currentToken;

            if (!ExpectPeek(TokenType.LeftParenthese))
            {
                return null;
            }

            var parameters = ParseFunctionParameters();
            if (parameters is null)
            {
                return null;
            }

            if (!ExpectPeek(TokenType.LeftBrace))
            {
                return null;
            }

            var body = ParseBlockStatement();

            return new(token, parameters, body);
        }

        private ImmutableList<Identifier>? ParseFunctionParameters()
        {
            var identifiers = ImmutableList<Identifier>.Empty.ToBuilder();
            if (PeekTokenIs(TokenType.RightParenthese))
            {
                NextToken();
                return [];
            }

            NextToken();
            identifiers.Add(new Identifier(_currentToken, _currentToken.Literal));

            while (PeekTokenIs(TokenType.Comma))
            {
                NextToken();
                NextToken();
                identifiers.Add(new Identifier(_currentToken, _currentToken.Literal));
            }

            if (!ExpectPeek(TokenType.RightParenthese))
            {
                return null;
            }

            return identifiers.ToImmutable();
        }

        private StringLiteral ParseStringLiteral()
        {
            return StringLiteral.From(_currentToken);
        }

        private ArrayLiteral? ParseArrayLiteral()
        {
            var token = _currentToken;
            var elements = ParseExpressionList(TokenType.RightBracket);
            if (elements is null)
            {
                return null;
            }
            return new(token, elements);
        }

        private HashLiteral? ParseHashLiteral()
        {
            var token = _currentToken;
            var pairs = ImmutableList<(IExpression Key, IExpression Value)>.Empty.ToBuilder();

            while (!PeekTokenIs(TokenType.RightBrace))
            {
                NextToken();
                var key = ParseExpression(Precedence.Lowest);
                if (key is null)
                {
                    return null;
                }

                if (!ExpectPeek(TokenType.Colon))
                {
                    return null;
                }
                NextToken();

                var value = ParseExpression(Precedence.Lowest);
                if (value is null)
                {
                    return null;
                }

                pairs.Add((key, value));

                if (!PeekTokenIs(TokenType.RightBrace) && !ExpectPeek(TokenType.Comma))
                {
                    return null;
                }
            }

            if (!ExpectPeek(TokenType.RightBrace))
            {
                return null;
            }

            return new(token, pairs.ToImmutable());
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

        private CallExpression? ParseCallExpresion(IExpression function)
        {
            var token = _currentToken;
            var arguments = ParseExpressionList(TokenType.RightParenthese);
            if (arguments is null)
            {
                return null;
            }
            return new(token, function, arguments);
        }

        private ImmutableList<IExpression>? ParseExpressionList(TokenType end)
        {
            if (PeekTokenIs(end))
            {
                NextToken();
                return [];
            }

            NextToken();
            var arguments = ImmutableList<IExpression>.Empty.ToBuilder();
            var argument = ParseExpression(Precedence.Lowest);
            if (argument is null)
            {
                return null;
            }
            arguments.Add(argument);

            while (PeekTokenIs(TokenType.Comma))
            {
                NextToken();
                NextToken();
                argument = ParseExpression(Precedence.Lowest);
                if (argument is null)
                {
                    return null;
                }
                arguments.Add(argument);
            }

            if (!ExpectPeek(end))
            {
                return null;
            }

            return arguments.ToImmutable();
        }

        private IndexExpression? ParseIndexExpression(IExpression left)
        {
            var token = _currentToken;

            NextToken();

            var index = ParseExpression(Precedence.Lowest);
            if (index is null)
            {
                return null;
            }

            if (!ExpectPeek(TokenType.RightBracket))
            {
                return null;
            }

            return new(token, left, index);
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

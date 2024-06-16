using System.Collections.Generic;
using System.Linq;
using Monkey.Ast;

namespace Monkey.Test;

public class ParserTest
{
    [Theory]
    [InlineData("let x = 5;", "x", 5L)]
    [InlineData("let y = true;", "y", true)]
    [InlineData("let foobar = y;", "foobar", "y")]
    public void TestLetStatements(string input, string expectedIdentifier, object expectedValue)
    {
        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();

        CheckParserErrors(errors);
        var statement = Assert.Single(program.Statements);
        TestLetStatement(statement, expectedIdentifier);
        var letStatement = Assert.IsType<LetStatement>(statement);
        TestLiteralExpression(letStatement.Value, expectedValue);
    }

    [Theory]
    [InlineData("return 5;", 5L)]
    [InlineData("return true;", true)]
    [InlineData("return foobar;", "foobar")]
    public void TestReturnStatements(string input, object expectedValue)
    {
        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();

        CheckParserErrors(errors);
        var statement = Assert.Single(program.Statements);
        var returnStatement = Assert.IsType<ReturnStatement>(statement);
        Assert.Equal("return", returnStatement.GetTokenLiteral());
        TestLiteralExpression(returnStatement.ReturnValue, expectedValue);
    }

    [Fact]
    public void TestIdentifierExrpession()
    {
        var input = "foobar;";

        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();

        CheckParserErrors(errors);
        var statement = Assert.Single(program.Statements);
        var expressionStatement = Assert.IsType<ExpressionStatement>(statement);
        var identifier = Assert.IsType<Identifier>(expressionStatement.Expression);
        Assert.Equal("foobar", identifier.Value);
        Assert.Equal("foobar", identifier.GetTokenLiteral());
    }

    [Fact]
    public void TestIntegerLiteralExpression()
    {
        var input = "5;";

        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();

        CheckParserErrors(errors);
        var statement = Assert.Single(program.Statements);
        var expressionStatement = Assert.IsType<ExpressionStatement>(statement);
        var literal = Assert.IsType<IntegerLiteral>(expressionStatement.Expression);
        Assert.Equal(5, literal.Value);
        Assert.Equal("5", literal.GetTokenLiteral());
    }

    [Theory]
    [InlineData("!5;", "!", 5L)]
    [InlineData("-15;", "-", 15L)]
    [InlineData("!foobar;", "!", "foobar")]
    [InlineData("-foobar;", "-", "foobar")]
    [InlineData("!true;", "!", true)]
    [InlineData("!false;", "!", false)]
    public void TestParsingPrefixExpressions(string input, string @operator, object value)
    {
        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();

        CheckParserErrors(errors);
        var statement = Assert.Single(program.Statements);
        var expressionStatement = Assert.IsType<ExpressionStatement>(statement);
        var expression = Assert.IsType<PrefixExpression>(expressionStatement.Expression);
        Assert.Equal(@operator, expression.Operator);
        TestLiteralExpression(expression.Right, value);
    }

    [Theory]
    [InlineData("5 + 5;", 5, "+", 5)]
    [InlineData("5 - 5;", 5, "-", 5)]
    [InlineData("5 * 5;", 5, "*", 5)]
    [InlineData("5 / 5;", 5, "/", 5)]
    [InlineData("5 > 5;", 5, ">", 5)]
    [InlineData("5 < 5;", 5, "<", 5)]
    [InlineData("5 == 5;", 5, "==", 5)]
    [InlineData("5 != 5;", 5, "!=", 5)]
    [InlineData("foobar + barfoo;", "foobar", "+", "barfoo")]
    [InlineData("foobar - barfoo;", "foobar", "-", "barfoo")]
    [InlineData("foobar * barfoo;", "foobar", "*", "barfoo")]
    [InlineData("foobar / barfoo;", "foobar", "/", "barfoo")]
    [InlineData("foobar > barfoo;", "foobar", ">", "barfoo")]
    [InlineData("foobar < barfoo;", "foobar", "<", "barfoo")]
    [InlineData("foobar == barfoo;", "foobar", "==", "barfoo")]
    [InlineData("foobar != barfoo;", "foobar", "!=", "barfoo")]
    [InlineData("true == true", true, "==", true)]
    [InlineData("true != false", true, "!=", false)]
    [InlineData("false == false", false, "==", false)]
    public void TestParsingInfixExpressions(string input, object leftValue, string @operator, object rightValue)
    {
        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();

        CheckParserErrors(errors);
        var statement = Assert.Single(program.Statements);
        var expressionStatement = Assert.IsType<ExpressionStatement>(statement);
        var expression = Assert.IsType<InfixExpression>(expressionStatement.Expression);
        TestInfixExpression(expression, leftValue, @operator, rightValue);
    }

    [Theory]
    [InlineData("-a * b", "((-a) * b)")]
    [InlineData("!-a", "(!(-a))")]
    [InlineData("a + b + c", "((a + b) + c)")]
    [InlineData("a + b - c", "((a + b) - c)")]
    [InlineData("a * b * c", "((a * b) * c)")]
    [InlineData("a * b / c", "((a * b) / c)")]
    [InlineData("a + b / c", "(a + (b / c))")]
    [InlineData("a + b * c + d / e - f", "(((a + (b * c)) + (d / e)) - f)")]
    [InlineData("3 + 4; -5 * 5", "(3 + 4)((-5) * 5)")]
    [InlineData("5 > 4 == 3 < 4", "((5 > 4) == (3 < 4))")]
    [InlineData("5 < 4 != 3 > 4", "((5 < 4) != (3 > 4))")]
    [InlineData("3 + 4 * 5 == 3 * 1 + 4 * 5", "((3 + (4 * 5)) == ((3 * 1) + (4 * 5)))")]
    [InlineData("true", "true")]
    [InlineData("false", "false")]
    [InlineData("3 > 5 == false", "((3 > 5) == false)")]
    [InlineData("3 < 5 == true", "((3 < 5) == true)")]
    [InlineData("1 + (2 + 3) + 4", "((1 + (2 + 3)) + 4)")]
    [InlineData("(5 + 5) * 2", "((5 + 5) * 2)")]
    [InlineData("2 / (5 + 5)", "(2 / (5 + 5))")]
    [InlineData("(5 + 5) * 2 * (5 + 5)", "(((5 + 5) * 2) * (5 + 5))")]
    [InlineData("-(5 + 5)", "(-(5 + 5))")]
    [InlineData("!(true == true)", "(!(true == true))")]
    [InlineData("a + add(b * c) + d", "((a + add((b * c))) + d)")]
    [InlineData("add(a, b, 1, 2 * 3, 4 + 5, add(6, 7 * 8))", "add(a, b, 1, (2 * 3), (4 + 5), add(6, (7 * 8)))")]
    [InlineData("add(a + b + c * d / f + g)", "add((((a + b) + ((c * d) / f)) + g))")]
    [InlineData("a * [1, 2, 3, 4][b * c] * d", "((a * ([1, 2, 3, 4][(b * c)])) * d)")]
    [InlineData("add(a * b[2], b[1], 2 * [1, 2][1])", "add((a * (b[2])), (b[1]), (2 * ([1, 2][1])))")]
    public void TestOperatorPrecedenceParsing(string input, string ast)
    {
        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();
        var actual = program.GetDebugString();

        CheckParserErrors(errors);
        Assert.Equal(ast, actual);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void TestBooleanExpression(string input, bool value)
    {
        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();

        CheckParserErrors(errors);
        var statement = Assert.Single(program.Statements);
        var expressionStatement = Assert.IsType<ExpressionStatement>(statement);
        var boolExpression = Assert.IsType<Boolean>(expressionStatement.Expression);
        Assert.Equal(value, boolExpression.Value);
    }

    [Fact]
    public void TestIfExpression()
    {
        var input = "if (x < y) { x }";

        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();

        CheckParserErrors(errors);
        var statement = Assert.Single(program.Statements);
        var expressionStatement = Assert.IsType<ExpressionStatement>(statement);
        var expression = Assert.IsType<IfExpression>(expressionStatement.Expression);
        TestInfixExpression(expression.Condition, "x", "<", "y");
        var consequenceStatement = Assert.Single(expression.Consequence.Statements);
        var consequenceExpression = Assert.IsType<ExpressionStatement>(consequenceStatement);
        TestIdentifier(consequenceExpression.Expression!, "x");
        Assert.Null(expression.Alternative);
    }

    [Fact]
    public void TestIfElseExpression()
    {
        var input = "if (x < y) { x } else { y }";

        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();

        CheckParserErrors(errors);
        var statement = Assert.Single(program.Statements);
        var expressionStatement = Assert.IsType<ExpressionStatement>(statement);
        var expression = Assert.IsType<IfExpression>(expressionStatement.Expression);
        TestInfixExpression(expression.Condition, "x", "<", "y");
        var consequenceStatement = Assert.Single(expression.Consequence.Statements);
        var consequenceExpression = Assert.IsType<ExpressionStatement>(consequenceStatement);
        TestIdentifier(consequenceExpression.Expression!, "x");
        Assert.NotNull(expression.Alternative);
        var alternativeStatement = Assert.Single(expression.Alternative.Statements);
        var alternativeExpression = Assert.IsType<ExpressionStatement>(alternativeStatement);
        TestIdentifier(alternativeExpression.Expression!, "y");
    }

    [Fact]
    public void TestFunctionLiteralParsing()
    {
        var input = "fn(x, y) { x + y; }";

        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();

        CheckParserErrors(errors);
        var statement = Assert.Single(program.Statements);
        var expressionStatement = Assert.IsType<ExpressionStatement>(statement);
        var function = Assert.IsType<FunctionLiteral>(expressionStatement.Expression);
        Assert.Equal(2, function.Parameters.Count);
        TestLiteralExpression(function.Parameters[0], "x");
        TestLiteralExpression(function.Parameters[1], "y");
        var bodyStatement = Assert.Single(function.Body.Statements);
        var bodyExpressionStatement = Assert.IsType<ExpressionStatement>(bodyStatement);
        TestInfixExpression(bodyExpressionStatement.Expression!, "x", "+", "y");
    }

    [Theory]
    [InlineData("fn() {};")]
    [InlineData("fn(x) {};", "x")]
    [InlineData("fn(x, y, z) {};", "x", "y", "z")]
    public void TestFunctionParameterParsing(string input, params string[] parameters)
    {
        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();

        CheckParserErrors(errors);
        var statement = Assert.Single(program.Statements);
        var expressionStatement = Assert.IsType<ExpressionStatement>(statement);
        var function = Assert.IsType<FunctionLiteral>(expressionStatement.Expression);
        Assert.Equal(parameters.Length, function.Parameters.Count);
        foreach (var (expectedParameter, parameter) in parameters.Zip(function.Parameters))
        {
            TestLiteralExpression(parameter, expectedParameter);
        }
    }

    [Fact]
    public void TestCallExpressionParsing()
    {
        var input = "add(1, 2 * 3, 4 + 5);";

        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();

        CheckParserErrors(errors);
        var statement = Assert.Single(program.Statements);
        var expressionStatement = Assert.IsType<ExpressionStatement>(statement);
        var callExpression = Assert.IsType<CallExpression>(expressionStatement.Expression);
        TestIdentifier(callExpression.Function, "add");
        Assert.Equal(3, callExpression.Arguments.Count);
        TestLiteralExpression(callExpression.Arguments[0], 1);
        TestInfixExpression(callExpression.Arguments[1], 2, "*", 3);
        TestInfixExpression(callExpression.Arguments[2], 4, "+", 5);
    }

    [Theory]
    [InlineData("add();", "add")]
    [InlineData("add(1);", "add", "1")]
    [InlineData("add(1, 2 * 3, 4 + 5);", "add", "1", "(2 * 3)", "(4 + 5)")]
    public void TestCallExpressionParameterParsing(string input, string function, params string[] arguments)
    {
        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();

        CheckParserErrors(errors);
        var statement = Assert.Single(program.Statements);
        var expressionStatement = Assert.IsType<ExpressionStatement>(statement);
        var callExpression = Assert.IsType<CallExpression>(expressionStatement.Expression);
        TestIdentifier(callExpression.Function, function);
        Assert.Equal(arguments.Length, callExpression.Arguments.Count);
        foreach (var (expectedArgument, argument) in arguments.Zip(callExpression.Arguments))
        {
            Assert.Equal(expectedArgument, argument.GetDebugString());
        }
    }

    [Fact]
    public void TestStringLiteralExpression()
    {
        var input = @"""hello world"";";

        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();

        CheckParserErrors(errors);
        var statement = Assert.Single(program.Statements);
        var expressionStatement = Assert.IsType<ExpressionStatement>(statement);
        var literal = Assert.IsType<StringLiteral>(expressionStatement.Expression);
        Assert.Equal("hello world", literal.Value);
    }

    [Fact]
    public void TestParsingArrayLiterals()
    {
        var input = "[1, 2 * 2, 3 + 3]";

        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();

        CheckParserErrors(errors);
        var statement = Assert.Single(program.Statements);
        var expressionStatement = Assert.IsType<ExpressionStatement>(statement);
        var array = Assert.IsType<ArrayLiteral>(expressionStatement.Expression);
        Assert.Equal(3, array.Elements.Count);
        TestIntegerLiteral(array.Elements[0], 1);
        TestInfixExpression(array.Elements[1], 2, "*", 2);
        TestInfixExpression(array.Elements[2], 3, "+", 3);
    }

    [Fact]
    public void TestParsingIndexExpressions()
    {
        var input = "myArray[1 + 1]";

        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();

        CheckParserErrors(errors);
        var statement = Assert.Single(program.Statements);
        var expressionStatement = Assert.IsType<ExpressionStatement>(statement);
        var index = Assert.IsType<IndexExpression>(expressionStatement.Expression);
        TestIdentifier(index.Left, "myArray");
        TestInfixExpression(index.Index, 1, "+", 1);
    }

    private static void CheckParserErrors(IReadOnlyList<string> errors)
    {
        if (errors.Count == 0)
        {
            return;
        }

        Assert.Fail("\n\t- " + string.Join("\n\t- ", errors));
    }

    private static void TestLetStatement(IStatement statement, string name)
    {
        Assert.Equal("let", statement.GetTokenLiteral());

        var letStatement = Assert.IsType<LetStatement>(statement);

        Assert.Equal(name, letStatement.Name.Value);
        Assert.Equal(name, letStatement.Name.GetTokenLiteral());
    }

    private static void TestIdentifier(IExpression expression, string value)
    {
        var identifier = Assert.IsType<Identifier>(expression);
        Assert.Equal(value, identifier.Value);
        Assert.Equal(value, identifier.GetTokenLiteral());
    }

    private static void TestIntegerLiteral(IExpression expression, long value)
    {
        var integerLiteral = Assert.IsType<IntegerLiteral>(expression);
        Assert.Equal(value, integerLiteral.Value);
        Assert.Equal(value.ToString(), integerLiteral.GetTokenLiteral());
    }

    private static void TestBooleanLiteral(IExpression expression, bool value)
    {
        var booleanLiteral = Assert.IsType<Boolean>(expression);
        Assert.Equal(value, booleanLiteral.Value);
        Assert.Equal(value ? "true" : "false", booleanLiteral.GetTokenLiteral());
    }

    private static void TestLiteralExpression(IExpression expression, object expected)
    {
        System.Action test = expected switch
        {
            int x => () => TestIntegerLiteral(expression, x),
            long x => () => TestIntegerLiteral(expression, x),
            string x => () => TestIdentifier(expression, x),
            bool x => () => TestBooleanLiteral(expression, x),
            _ => throw new System.Exception($"Type of expression not handled. got={expected.GetType()}"),
        };
        test();
    }

    private static void TestInfixExpression(IExpression expression, object left, string @operator, object right)
    {
        var infix = Assert.IsType<InfixExpression>(expression);
        TestLiteralExpression(infix.Left, left);
        Assert.Equal(@operator, infix.Operator);
        TestLiteralExpression(infix.Right, right);
    }
}

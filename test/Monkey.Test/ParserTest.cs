using System.Collections.Generic;
using System.Linq;
using Monkey.Ast;

namespace Monkey.Test;

public class ParserTest
{
    [Fact]
    public void TestLetStatements()
    {
        var input = @"
let x = 5;
let y = 10;
let foobar = 838383;
";
        string[] expectedIdentifiers = ["x", "y", "foobar"];

        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();

        CheckParserErrors(errors);
        Assert.Equal(expectedIdentifiers.Length, program.Statements.Count);
        foreach (var (statement, name) in program.Statements.Zip(expectedIdentifiers))
        {
            TestLetStatement(statement, name);
        }
    }

    private void TestLetStatement(IStatement statement, string name)
    {
        Assert.Equal("let", statement.GetTokenLiteral());

        var letStatement = AssertFluent.IsType<LetStatement>(statement);

        Assert.Equal(name, letStatement.Name.Value);
        Assert.Equal(name, letStatement.Name.GetTokenLiteral());
    }

    [Fact]
    public void TestReturnStatements()
    {
        var input = @"
return 5;
return 10;
return 993322;
";

        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();

        CheckParserErrors(errors);
        Assert.Equal(3, program.Statements.Count);
        foreach (var statement in program.Statements)
        {
            Assert.IsType<ReturnStatement>(statement);
            Assert.Equal("return", statement.GetTokenLiteral());
        }
    }

    [Fact]
    public void TestIdentifierExrpession()
    {
        var input = "foobar;";

        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();

        CheckParserErrors(errors);
        Assert.Single(program.Statements);
        var statement = AssertFluent.IsType<ExpressionStatement>(program.Statements.First());
        var identifier = AssertFluent.IsType<Identifier>(statement.Expression);
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
        Assert.Single(program.Statements);
        var statement = AssertFluent.IsType<ExpressionStatement>(program.Statements.First());
        var literal = AssertFluent.IsType<IntegerLiteral>(statement.Expression);
        Assert.Equal(5, literal.Value);
        Assert.Equal("5", literal.GetTokenLiteral());
    }

    [Theory]
    [InlineData("!5;", "!", 5L)]
    [InlineData("-15;", "-", 15L)]
    public void TestParsingPrefixExpressions(string input, string @operator, long integerValue)
    {
        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();

        CheckParserErrors(errors);
        Assert.Single(program.Statements);
        var statement = AssertFluent.IsType<ExpressionStatement>(program.Statements.First());
        var expression = AssertFluent.IsType<PrefixExpression>(statement.Expression);
        Assert.Equal(@operator, expression.Operator);
        TestIntegerLiteral(expression.Right, integerValue);
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
    public void TestParsingInfixExpressions(string input, long leftValue, string @operator, long rightValue)
    {
        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();

        CheckParserErrors(errors);
        Assert.Single(program.Statements);
        var statement = AssertFluent.IsType<ExpressionStatement>(program.Statements.First());
        var expression = AssertFluent.IsType<InfixExpression>(statement.Expression);
        TestIntegerLiteral(expression.Left, leftValue);
        Assert.Equal(@operator, expression.Operator);
        TestIntegerLiteral(expression.Right, rightValue);
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
    public void TestOperatorPrecedenceParsing(string input, string ast)
    {
        var lexer = new Lexer(input);
        var parser = new Parser(lexer);
        var (program, errors) = parser.ParseProgram();
        var actual = program.GetDebugString();

        CheckParserErrors(errors);
        Assert.Equal(ast, actual);
    }

    private void TestIntegerLiteral(IExpression expression, long value)
    {
        var integerLiteral = AssertFluent.IsType<IntegerLiteral>(expression);
        Assert.Equal(value, integerLiteral.Value);
        Assert.Equal(value.ToString(), integerLiteral.GetTokenLiteral());
    }

    private void CheckParserErrors(IReadOnlyList<string> errors)
    {
        if (errors.Count == 0)
        {
            return;
        }

        Assert.Fail("\n\t- " + string.Join("\n\t- ", errors));
    }
}

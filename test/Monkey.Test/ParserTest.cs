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

        Assert.IsType<LetStatement>(statement);
        LetStatement letStatement = (LetStatement)statement;

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
        Assert.IsType<ExpressionStatement>(program.Statements.First());
        var statement = (ExpressionStatement)program.Statements.First();
        Assert.IsType<Identifier>(statement.Expression);
        var identifier = (Identifier)statement.Expression;
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
        Assert.IsType<ExpressionStatement>(program.Statements.First());
        var statement = (ExpressionStatement)program.Statements.First();
        Assert.IsType<IntegerLiteral>(statement.Expression);
        var literal = (IntegerLiteral)statement.Expression;
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
        Assert.IsType<ExpressionStatement>(program.Statements.First());
        var statement = (ExpressionStatement)program.Statements.First();
        Assert.IsType<PrefixExpression>(statement.Expression);
        var expression = (PrefixExpression)statement.Expression;
        Assert.Equal(@operator, expression.Operator);
        TestIntegerLiteral(expression.Right, integerValue);
    }

    private void TestIntegerLiteral(IExpression expression, long value)
    {
        Assert.IsType<IntegerLiteral>(expression);
        var integerLiteral = (IntegerLiteral)expression;
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

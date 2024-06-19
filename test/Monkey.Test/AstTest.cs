using System.Linq;
using Monkey.Ast;

namespace Monkey.Test;

public class AstTest
{
    [Fact]
    public void TestDebugString()
    {
        var program = new Program([
            new LetStatement(
                new Token(TokenType.Let, "let"),
                new Identifier(new Token(TokenType.Identifier, "myVar"), "myVar"),
                new Identifier(new Token(TokenType.Identifier, "anotherVar"), "anotherVar")
            ),
        ]);

        Assert.Equal("let myVar = anotherVar;", program.GetDebugString());
    }

    [Fact]
    public void TestModifyOne()
    {
        Assert.Equal(Two(), One().Transform(TurnOneIntoTwo));
    }

    [Fact]
    public void TestModifyProgram()
    {
        var actual = new Program([new ExpressionStatement(_token, One())]).Transform(TurnOneIntoTwo);
        var program = Assert.IsType<Program>(actual);
        var statement = Assert.Single(program.Statements);
        var expression = Assert.IsType<ExpressionStatement>(statement);
        var integer = Assert.IsType<IntegerLiteral>(expression.Expression);
        Assert.Equal(2L, integer.Value);
    }

    [Fact]
    public void TestModifyInfix()
    {
        Assert.Equal(new InfixExpression(_token, Two(), "+", Two()), new InfixExpression(_token, One(), "+", One()).Transform(TurnOneIntoTwo));
        Assert.Equal(new InfixExpression(_token, Two(), "+", Two()), new InfixExpression(_token, Two(), "+", Two()).Transform(TurnOneIntoTwo));
    }

    [Fact]
    public void TestModifyPrefix()
    {
        Assert.Equal(new PrefixExpression(_token, "-", Two()), new PrefixExpression(_token, "-", One()).Transform(TurnOneIntoTwo));
        Assert.Equal(new PrefixExpression(_token, "-", Two()), new PrefixExpression(_token, "-", Two()).Transform(TurnOneIntoTwo));
    }

    [Fact]
    public void TestModifyIndex()
    {
        Assert.Equal(new IndexExpression(_token, Two(), Two()), new IndexExpression(_token, One(), One()).Transform(TurnOneIntoTwo));
        Assert.Equal(new IndexExpression(_token, Two(), Two()), new IndexExpression(_token, Two(), Two()).Transform(TurnOneIntoTwo));
    }

    [Fact]
    public void TestModifyReturn()
    {
        Assert.Equal(new ReturnStatement(_token, Two()), new ReturnStatement(_token, One()).Transform(TurnOneIntoTwo));
    }

    [Fact]
    public void TestModifyLet()
    {
        Assert.Equal(new LetStatement(_token, new Identifier(_token, "stuff"), Two()),
            new LetStatement(_token, new Identifier(_token, "stuff"), One()).Transform(TurnOneIntoTwo));
    }

    [Fact]
    public void TestModifyIf()
    {
        var input = new IfExpression(_token, One(), new(_token, [new ExpressionStatement(_token, One())]), new(_token, [new ExpressionStatement(_token, One())]));
        var expected = new IfExpression(_token, Two(), new(_token, [new ExpressionStatement(_token, Two())]), new(_token, [new ExpressionStatement(_token, Two())]));

        var actual = input.Transform(TurnOneIntoTwo);
        var ifExpr = Assert.IsType<IfExpression>(actual);
        Assert.Equal(expected.Condition, ifExpr.Condition);
        Assert.Equal(expected.Consequence.Statements, ifExpr.Consequence.Statements);
        Assert.Equal(expected.Alternative?.Statements, ifExpr.Alternative?.Statements);
    }

    [Fact]
    public void TestModifyFunctionLiteral()
    {
        var input = new FunctionLiteral(_token, [], new(_token, [new ExpressionStatement(_token, One())]));
        var expected = new FunctionLiteral(_token, [], new(_token, [new ExpressionStatement(_token, Two())]));

        var actual = input.Transform(TurnOneIntoTwo);
        var func = Assert.IsType<FunctionLiteral>(actual);
        Assert.Equal(expected.Body.Statements, func.Body.Statements);
    }

    [Fact]
    public void TestModifyArrayLietral()
    {
        Assert.Equal(new ArrayLiteral(_token, [Two()]).Elements, ((ArrayLiteral)new ArrayLiteral(_token, [One()]).Transform(TurnOneIntoTwo)).Elements);
    }

    [Fact]
    public void TestModifyHashLiteral()
    {
        Assert.Equal(
            new HashLiteral(_token, [(Two(), Two()), (Two(), Two())]).Pairs,
            ((HashLiteral)new HashLiteral(_token, [(One(), One()), (One(), One())]).Transform(TurnOneIntoTwo)).Pairs
        );
    }

    private static readonly Token _token = new(TokenType.Illegal, "");
    private static IntegerLiteral One() => new(_token, 1L);
    private static IntegerLiteral Two() => new(_token, 2L);
    private static INode TurnOneIntoTwo(INode node) => node switch
    {
        IntegerLiteral x when x.Value == 1 => Two(),
        _ => node,
    };
}

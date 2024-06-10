using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Monkey.Ast;

public interface INode
{
    string GetTokenLiteral();
    string DisplayString();
}

public interface IStatement : INode {}

public interface IExpression : INode {}

public record Program(ImmutableList<IStatement> Statements) : INode
{
    public string GetTokenLiteral() => Statements.FirstOrDefault()?.GetTokenLiteral() ?? "";

    public string DisplayString()
    {
        var builder = new StringBuilder();
        foreach (var statement in Statements)
        {
            builder.Append(statement.DisplayString());
        }
        return builder.ToString();
    }
}

public record LetStatement(Token Token, Identifier Name, IExpression Value) : IStatement
{
    public string GetTokenLiteral() => Token.Literal;

    public string DisplayString()
    {
        var builder = new StringBuilder();
        builder.Append(GetTokenLiteral());
        builder.Append(" ");
        builder.Append(Name.DisplayString());
        builder.Append(" = ");
        builder.Append(Value.DisplayString());
        builder.Append(";");
        return builder.ToString();
    }
}

public record ReturnStatement(Token Token, IExpression ReturnValue) : IStatement
{
    public string GetTokenLiteral() => Token.Literal;

    public string DisplayString()
    {
        var builder = new StringBuilder();
        builder.Append(GetTokenLiteral());
        builder.Append(" ");
        builder.Append(ReturnValue.DisplayString());
        builder.Append(";");
        return builder.ToString();
    }
}

public record ExpressionStatement(Token Token, IExpression Expression) : IStatement
{
    public string GetTokenLiteral() => Token.Literal;

    public string DisplayString() => Expression.DisplayString();
}

public record Identifier(Token Token, string Value) : IExpression
{
    public string GetTokenLiteral() => Token.Literal;

    public string DisplayString() => Value;
}

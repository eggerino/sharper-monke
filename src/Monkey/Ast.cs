using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Monkey.Ast;

public interface INode
{
    string GetTokenLiteral();
    string GetDebugString();
}

public interface IStatement : INode { }

public interface IExpression : INode { }

public record Program(ImmutableList<IStatement> Statements) : INode
{
    public string GetTokenLiteral() => Statements.FirstOrDefault()?.GetTokenLiteral() ?? "";

    public string GetDebugString()
    {
        var builder = new StringBuilder();
        foreach (var statement in Statements)
        {
            builder.Append(statement.GetDebugString());
        }
        return builder.ToString();
    }
}

public record LetStatement(Token Token, Identifier Name, IExpression Value) : IStatement
{
    public string GetTokenLiteral() => Token.Literal;

    public string GetDebugString()
    {
        var builder = new StringBuilder();
        builder.Append(GetTokenLiteral());
        builder.Append(" ");
        builder.Append(Name.GetDebugString());
        builder.Append(" = ");
        builder.Append(Value.GetDebugString());
        builder.Append(";");
        return builder.ToString();
    }
}

public record ReturnStatement(Token Token, IExpression ReturnValue) : IStatement
{
    public string GetTokenLiteral() => Token.Literal;

    public string GetDebugString()
    {
        var builder = new StringBuilder();
        builder.Append(GetTokenLiteral());
        builder.Append(" ");
        builder.Append(ReturnValue.GetDebugString());
        builder.Append(";");
        return builder.ToString();
    }
}

public record ExpressionStatement(Token Token, IExpression? Expression) : IStatement
{
    public string GetTokenLiteral() => Token.Literal;

    public string GetDebugString() => Expression?.GetDebugString() ?? "";
}

public record Identifier(Token Token, string Value) : IExpression
{
    public string GetTokenLiteral() => Token.Literal;

    public string GetDebugString() => Value;
}

public record IntegerLiteral(Token Token, long Value) : IExpression
{
    public string GetTokenLiteral() => Token.Literal;

    public string GetDebugString() => Token.Literal;
}

public record PrefixExpression(Token Token, string Operator, IExpression Right) : IExpression
{
    public string GetTokenLiteral() => Token.Literal;

    public string GetDebugString()
    {
        var builder = new StringBuilder();
        builder.Append("(");
        builder.Append(Operator);
        builder.Append(Right.GetDebugString());
        builder.Append(")");
        return builder.ToString();
    }
}

public record InfixExpression(Token Token, IExpression Left, string Operator, IExpression Right) : IExpression
{
    public string GetTokenLiteral() => Token.Literal;

    public string GetDebugString()
    {
        var builder = new StringBuilder();
        builder.Append("(");
        builder.Append(Left.GetDebugString());
        builder.Append(" ");
        builder.Append(Operator);
        builder.Append(" ");
        builder.Append(Right.GetDebugString());
        builder.Append(")");
        return builder.ToString();
    }
}

public record Boolean(Token Token, bool Value) : IExpression
{
    public string GetTokenLiteral() => Token.Literal;

    public string GetDebugString() => Token.Literal;
}

public record BlockStatement(Token Token, ImmutableList<IStatement> Statements) : IStatement
{
    public string GetTokenLiteral() => Token.Literal;

    public string GetDebugString()
    {
        return string.Join("", Statements.Select(x => x.GetDebugString()));
    }
}

public record IfExpression(Token Token, IExpression Condition, BlockStatement Consequence, BlockStatement? Alternative) : IExpression
{
    public string GetTokenLiteral() => Token.Literal;

    public string GetDebugString()
    {
        var builder = new StringBuilder();
        builder.Append("if");
        builder.Append(Condition.GetDebugString());
        builder.Append(" ");
        builder.Append(Consequence.GetDebugString());

        if (Alternative is not null)
        {
            builder.Append("else ");
            builder.Append(Alternative.GetDebugString());
        }

        return builder.ToString();
    }
}

public record FunctionLiteral(Token Token, ImmutableList<Identifier> Parameters, BlockStatement Body) : IExpression
{
    public string GetTokenLiteral() => Token.Literal;

    public string GetDebugString()
    {
        var builder = new StringBuilder();
        builder.Append(Token.Literal);
        builder.Append("(");
        builder.Append(string.Join(", ", Parameters.Select(x => x.GetDebugString())));
        builder.Append(")");
        builder.Append(Body.GetDebugString());
        return builder.ToString();
    }
}

public record CallExpression(Token Token, IExpression Function, ImmutableList<IExpression> Arguments) : IExpression
{
    public string GetTokenLiteral() => Token.Literal;

    public string GetDebugString()
    {
        var builder = new StringBuilder();
        builder.Append(Function.GetDebugString());
        builder.Append("(");
        builder.Append(string.Join(", ", Arguments.Select(x => x.GetDebugString())));
        builder.Append(")");
        return builder.ToString();
    }
}

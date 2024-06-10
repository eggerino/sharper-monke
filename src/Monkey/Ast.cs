using System.Collections.Immutable;
using System.Linq;

namespace Monkey.Ast;

public interface INode
{
    string GetTokenLiteral();
}

public interface IStatement : INode {}

public interface IExpression : INode {}

public record Program(ImmutableList<IStatement> Statements) : INode
{
    public string GetTokenLiteral() => Statements.FirstOrDefault()?.GetTokenLiteral() ?? "";
}

public record LetStatement(Token Token, Identifier Name, IExpression Value) : IStatement
{
    public string GetTokenLiteral() => Token.Literal;
}

public record Identifier(Token Token, string Value) : IExpression
{
    public string GetTokenLiteral() => Token.Literal;
}

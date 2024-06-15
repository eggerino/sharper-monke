using System.Collections.Generic;
using Monkey.Ast;
using Monkey.Object;

namespace Monkey;

public static class Evaluator
{
    public static IObject? Eval(INode node)
    {
        return node switch
        {
            Program x => EvalStatements(x.Statements),
            ExpressionStatement x when x.Expression is not null => Eval(x.Expression),
            IntegerLiteral x => new Integer(x.Value),
            _ => null,
        };
    }

    private static IObject? EvalStatements(IEnumerable<IStatement> statements)
    {
        IObject? result = null;
        foreach (var statement in statements)
        {
            result = Eval(statement);
        }
        return result;
    }
}
